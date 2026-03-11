using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.DeadLetter;
using KeeperData.Core.Messaging;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Services
{
    public partial class DeadLetterQueueService(
        IAmazonSQS amazonSqs,
        IOptions<IntakeEventQueueOptions> options,
        ILogger<DeadLetterQueueService> logger)
        : IDeadLetterQueueService
    {
        private readonly IntakeEventQueueOptions _queueConsumerOptions = options.Value;

        private const string StringDataType = "String";

        public async Task<QueueStats> GetQueueStatsAsync(string queueUrl, CancellationToken ct = default)
        {
            var response = await amazonSqs.GetQueueAttributesAsync(queueUrl, new List<string>
            {
                "ApproximateNumberOfMessages",
                "ApproximateNumberOfMessagesNotVisible",
                "ApproximateNumberOfMessagesDelayed"
            }, ct);

            return new QueueStats
            {
                QueueUrl = queueUrl,
                ApproximateMessageCount = response.ApproximateNumberOfMessages,
                ApproximateMessagesNotVisible = response.ApproximateNumberOfMessagesNotVisible,
                ApproximateMessagesDelayed = response.ApproximateNumberOfMessagesDelayed,
                CheckedAt = DateTime.UtcNow
            };
        }

        public async Task<DeadLetterMessagesResult> PeekDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default)
        {
            var dlqUrl = _queueConsumerOptions.DeadLetterQueueUrl!;

            var receiveResponse = await amazonSqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = dlqUrl,
                MaxNumberOfMessages = Math.Min(maxMessages, 10),
                VisibilityTimeout = 0,
                MessageSystemAttributeNames = new List<string> { "All" },
                MessageAttributeNames = new List<string> { "All" }
            }, ct);

            var statsResponse = await amazonSqs.GetQueueAttributesAsync(dlqUrl, new List<string>
            {
                "ApproximateNumberOfMessages"
            }, ct);

            var messages = receiveResponse.Messages.Select(m => new DeadLetterMessageDto
            {
                MessageId = m.MessageId,
                OriginalMessageId = GetMessageAttribute(m, "DLQ_OriginalMessageId"),
                FailureReason = GetMessageAttribute(m, "DLQ_FailureReason"),
                FailureMessage = GetMessageAttribute(m, "DLQ_FailureMessage"),
                FailureTimestamp = GetMessageAttribute(m, "DLQ_FailureTimestamp"),
                ReceiveCount = GetMessageAttribute(m, "DLQ_ReceiveCount"),
                CorrelationId = GetMessageAttribute(m, "CorrelationId"),
                MessageType = GetMessageAttribute(m, "Subject"),
                Body = m.Body
            }).ToList();

            return new DeadLetterMessagesResult
            {
                Messages = messages,
                TotalApproximateCount = statsResponse.ApproximateNumberOfMessages,
                CheckedAt = DateTime.UtcNow
            };
        }

        public async Task<RedriveSummary> RedriveDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default)
        {
            var dlqUrl = _queueConsumerOptions.DeadLetterQueueUrl!;
            var mainQueueUrl = _queueConsumerOptions.QueueUrl;
            var startedAt = DateTime.UtcNow;

            var summary = new RedriveSummaryBuilder();

            for (var i = 0; i < maxMessages; i++)
            {
                var message = await TryReceiveMessageAsync(dlqUrl, ct);
                if (message == null)
                    break;

                var correlationId = GetMessageAttribute(message, "CorrelationId");
                var redriveResult = await RedriveMessageAsync(message, dlqUrl, mainQueueUrl, correlationId, ct);

                summary.RecordResult(redriveResult, correlationId);
            }

            var remainingCount = await GetApproximateMessageCountAsync(dlqUrl, ct);

            return summary.Build(remainingCount, startedAt);
        }

        public async Task<PurgeResult> PurgeDeadLetterQueueAsync(CancellationToken ct = default)
        {
            var dlqUrl = _queueConsumerOptions.DeadLetterQueueUrl!;

            var statsBeforePurge = await amazonSqs.GetQueueAttributesAsync(dlqUrl, new List<string>
            {
                "ApproximateNumberOfMessages"
            }, ct);

            var approximateCount = statsBeforePurge.ApproximateNumberOfMessages;

            logger.LogWarning(
                "Purging dead letter queue {QueueUrl}. Approximate messages to be purged: {Count}. This is a destructive operation.",
                dlqUrl, approximateCount);

            await amazonSqs.PurgeQueueAsync(new PurgeQueueRequest
            {
                QueueUrl = dlqUrl
            }, ct);

            return new PurgeResult
            {
                Purged = true,
                ApproximateMessagesPurged = approximateCount,
                PurgedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> MoveToDeadLetterQueueAsync(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_queueConsumerOptions.DeadLetterQueueUrl))
            {
                logger.LogWarning("No DLQ configured for message {messageId}", message.MessageId);
                return false;
            }

            var sendSucceeded = false;
            var deleteSucceeded = false;

            try
            {
                // Extend visibility timeout to prevent race conditions
                await amazonSqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = message.ReceiptHandle,
                    VisibilityTimeout = 300
                }, cancellationToken);

                // Message loses it's meta data when moved to DLQ, so we need to add info back in
                var attributes = new Dictionary<string, MessageAttributeValue>(message.MessageAttributes ?? new Dictionary<string, MessageAttributeValue>());

                attributes["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = ex.GetType().Name, DataType = StringDataType };
                attributes["DLQ_FailureMessage"] = new MessageAttributeValue { StringValue = ex.Message.Substring(0, Math.Min(256, ex.Message.Length)), DataType = StringDataType };
                attributes["DLQ_FailureTimestamp"] = new MessageAttributeValue { StringValue = DateTime.UtcNow.ToString("O"), DataType = StringDataType };
                attributes["DLQ_OriginalMessageId"] = new MessageAttributeValue { StringValue = message.MessageId, DataType = StringDataType };
                attributes["DLQ_ReceiveCount"] = new MessageAttributeValue
                {
                    StringValue = (message.Attributes ?? []).GetValueOrDefault("ApproximateReceiveCount", "0"),
                    DataType = "Number"
                };
                if (!attributes.ContainsKey("CorrelationId"))
                {
                    attributes["CorrelationId"] = new MessageAttributeValue
                    {
                        DataType = StringDataType,
                        StringValue = CorrelationIdContext.Value
                    };
                }

                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = _queueConsumerOptions.DeadLetterQueueUrl,
                    MessageBody = message.Body,
                    MessageAttributes = attributes
                };

                // Send to DLQ
                var sendResponse = await amazonSqs.SendMessageAsync(sendRequest, cancellationToken);
                sendSucceeded = true;

                logger.LogInformation("Message {originalMessageId} sent to DLQ with new ID {dlqMessageId}",
                    message.MessageId, sendResponse.MessageId);

                // Only delete if send is confirmed successful
                await amazonSqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                deleteSucceeded = true;

                logger.LogInformation("Message {messageId} successfully moved to DLQ", message.MessageId);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Let cancellation exceptions propagate - don't swallow them
                throw;
            }
            catch (Exception dlqEx)
            {
                var status = sendSucceeded
                    ? "CRITICAL: Sent to DLQ but DELETE FAILED - DUPLICATE WILL OCCUR"
                    : "Failed to send to DLQ - message will retry";

                logger.LogError(dlqEx, "{status}. MessageId: {messageId}, SendSucceeded: {sendSucceeded}, DeleteSucceeded: {deleteSucceeded}",
                    status, message.MessageId, sendSucceeded, deleteSucceeded);

                if (sendSucceeded && !deleteSucceeded)
                {
                    logger.LogError("Send succeeded but delete failed for MessageId: {messageId}", message.MessageId);
                }

                return false;
            }
        }

        private async Task<Message?> TryReceiveMessageAsync(string queueUrl, CancellationToken ct)
        {
            var receiveResponse = await amazonSqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                VisibilityTimeout = 60,
                MessageSystemAttributeNames = new List<string> { "All" },
                MessageAttributeNames = new List<string> { "All" }
            }, ct);

            return receiveResponse.Messages.Count > 0 ? receiveResponse.Messages[0] : null;
        }

        private async Task<RedriveResult> RedriveMessageAsync(
            Message message,
            string dlqUrl,
            string mainQueueUrl,
            string? correlationId,
            CancellationToken ct)
        {
            var sendResult = await TrySendToMainQueueAsync(message, mainQueueUrl, correlationId, ct);
            if (!sendResult.Success)
            {
                return RedriveResult.Failed();
            }

            var deleteResult = await TryDeleteFromDlqAsync(message, dlqUrl, correlationId, ct);
            if (!deleteResult.Success)
            {
                logger.LogError(
                    "CRITICAL: DUPLICATE message detected. Message {MessageId} exists in both queues. " +
                    "CorrelationId: {CorrelationId}",
                    message.MessageId, correlationId);
                return RedriveResult.Duplicated();
            }

            return RedriveResult.Success();
        }

        private async Task<OperationResult> TrySendToMainQueueAsync(
            Message message,
            string mainQueueUrl,
            string? correlationId,
            CancellationToken ct)
        {
            try
            {
                var cleanedAttributes = message.MessageAttributes
                    .Where(kvp => !kvp.Key.StartsWith("DLQ_"))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = mainQueueUrl,
                    MessageBody = message.Body,
                    MessageAttributes = cleanedAttributes
                };

                await amazonSqs.SendMessageAsync(sendRequest, ct);

                logger.LogInformation(
                    "Redrove message {MessageId} with CorrelationId {CorrelationId} from DLQ to main queue.",
                    message.MessageId, correlationId);

                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send message {MessageId} to main queue during redrive.",
                    message.MessageId);

                return OperationResult.Failed();
            }
        }

        private async Task<OperationResult> TryDeleteFromDlqAsync(
            Message message,
            string dlqUrl,
            string? correlationId,
            CancellationToken ct)
        {
            try
            {
                await amazonSqs.DeleteMessageAsync(dlqUrl, message.ReceiptHandle, ct);
                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete message {MessageId} from DLQ after successful send. CorrelationId: {CorrelationId}",
                    message.MessageId, correlationId);

                return OperationResult.Failed();
            }
        }

        private async Task<int> GetApproximateMessageCountAsync(string queueUrl, CancellationToken ct)
        {
            var stats = await amazonSqs.GetQueueAttributesAsync(queueUrl, new List<string> { "ApproximateNumberOfMessages" }, ct);
            return stats.ApproximateNumberOfMessages;
        }

        private static string? GetMessageAttribute(Message message, string key)
        {
            return message.MessageAttributes.TryGetValue(key, out var value) ? value.StringValue : null;
        }

        private class RedriveSummaryBuilder
        {
            private int _redriven;
            private int _failed;
            private int _duplicated;
            private readonly List<string> _correlationIds = [];

            public void RecordResult(RedriveResult result, string? correlationId)
            {
                switch (result.Type)
                {
                    case RedriveResultType.Success:
                        _redriven++;
                        break;
                    case RedriveResultType.Failed:
                        _failed++;
                        break;
                    case RedriveResultType.Duplicated:
                        _duplicated++;
                        break;
                }

                if (!string.IsNullOrEmpty(correlationId))
                {
                    _correlationIds.Add(correlationId);
                }
            }

            public RedriveSummary Build(int remainingCount, DateTime startedAt)
            {
                return new RedriveSummary
                {
                    MessagesRedriven = _redriven,
                    MessagesFailed = _failed,
                    MessagesDuplicated = _duplicated,
                    MessagesRemainingApprox = remainingCount,
                    CorrelationIds = _correlationIds,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }
    }
}