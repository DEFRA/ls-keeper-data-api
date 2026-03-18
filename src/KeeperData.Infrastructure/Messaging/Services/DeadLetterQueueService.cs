using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.DeadLetter;
using KeeperData.Core.Messaging;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Services;

public partial class DeadLetterQueueService(
    IAmazonSQS amazonSqs,
    IOptions<IntakeEventQueueOptions> options,
    ILogger<DeadLetterQueueService> logger)
    : IDeadLetterQueueService
{
    private readonly IntakeEventQueueOptions _queueConsumerOptions = options.Value;

    public async Task<QueueStats> GetQueueStatsAsync(string queueUrl, CancellationToken ct = default)
    {
        var response = await amazonSqs.GetQueueAttributesAsync(queueUrl, [
            DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages,
            DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessagesNotVisible,
            DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessagesDelayed
        ], ct);

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
        var messagesToRetrieve = maxMessages;

        if (maxMessages <= 0)
        {
            var stats = await amazonSqs.GetQueueAttributesAsync(dlqUrl,
                [DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages], ct);
            messagesToRetrieve = stats.ApproximateNumberOfMessages;
            
            logger.LogInformation("Peeking all {Count} messages from DLQ (max allowed: {MaxAllowed})", 
                messagesToRetrieve, _queueConsumerOptions.MaxPeekMessages);
            
            // Cap to configured maximum
            messagesToRetrieve = Math.Min(messagesToRetrieve, _queueConsumerOptions.MaxPeekMessages);
            
            if (stats.ApproximateNumberOfMessages == 0)
            {
                logger.LogInformation("DLQ is empty");
                return new DeadLetterMessagesResult
                {
                    Messages = [],
                    TotalApproximateCount = 0,
                    CheckedAt = DateTime.UtcNow
                };
            }
        }
        else
        {
            // Also cap explicit requests to the configured maximum
            var originalRequest = messagesToRetrieve;
            messagesToRetrieve = Math.Min(maxMessages, _queueConsumerOptions.MaxPeekMessages);
            
            if (messagesToRetrieve < originalRequest)
            {
                logger.LogWarning("Requested {Requested} messages, capped to configured maximum of {Capped}", 
                    originalRequest, messagesToRetrieve);
            }
        }

        var messageMap = new Dictionary<string, DeadLetterMessageDto>();
        var receiptHandles = new List<string>();
        var batchSize = DeadLetterQueueServiceConstants.Limits.MaxSqsReceiveMessages;
        var attemptsWithoutNewMessages = 0;
        const int maxAttemptsWithoutNewMessages = 3;
        const int tempVisibilityTimeoutSeconds = 30;

        try
        {
            while (messageMap.Count < messagesToRetrieve && attemptsWithoutNewMessages < maxAttemptsWithoutNewMessages)
            {
                var messagesToRequest = Math.Min(messagesToRetrieve - messageMap.Count, batchSize);

                var receiveResponse = await amazonSqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = dlqUrl,
                    MaxNumberOfMessages = messagesToRequest,
                    VisibilityTimeout = tempVisibilityTimeoutSeconds,
                    MessageSystemAttributeNames = [DeadLetterQueueServiceConstants.AllAttributes],
                    MessageAttributeNames = [DeadLetterQueueServiceConstants.AllAttributes]
                }, ct);

                if (receiveResponse.Messages == null || receiveResponse.Messages.Count == 0)
                    break;

                var newMessagesFound = 0;
                foreach (var m in receiveResponse.Messages)
                {
                    if (!messageMap.ContainsKey(m.MessageId))
                    {
                        messageMap[m.MessageId] = new DeadLetterMessageDto
                        {
                            MessageId = m.MessageId,
                            OriginalMessageId = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.DlqOriginalMessageId),
                            FailureReason = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureReason),
                            FailureMessage = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureMessage),
                            FailureTimestamp = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureTimestamp),
                            ReceiveCount = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.DlqReceiveCount),
                            CorrelationId = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.CorrelationId),
                            MessageType = GetMessageAttribute(m, DeadLetterQueueServiceConstants.MessageAttributes.Subject),
                            Body = m.Body
                        };
                        receiptHandles.Add(m.ReceiptHandle);
                        newMessagesFound++;
                    }
                }

                attemptsWithoutNewMessages = newMessagesFound == 0 ? attemptsWithoutNewMessages + 1 : 0;
            }

            await RestoreMessageVisibilityAsync(dlqUrl, receiptHandles, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error peeking DLQ messages, attempting to restore visibility");
            await RestoreMessageVisibilityAsync(dlqUrl, receiptHandles, ct);
            throw;
        }

        var statsResponse = await amazonSqs.GetQueueAttributesAsync(dlqUrl,
            [DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages], ct);

        return new DeadLetterMessagesResult
        {
            Messages = messageMap.Values.ToList(),
            TotalApproximateCount = statsResponse.ApproximateNumberOfMessages,
            CheckedAt = DateTime.UtcNow
        };
    }

    private async Task RestoreMessageVisibilityAsync(string queueUrl, List<string> receiptHandles, CancellationToken ct)
    {
        if (receiptHandles.Count == 0)
            return;

        // Restore visibility by setting timeout to 0 (make immediately visible)
        var tasks = receiptHandles.Select(receiptHandle =>
            amazonSqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle,
                VisibilityTimeout = 0
            }, ct));

        try
        {
            await Task.WhenAll(tasks);
            logger.LogDebug("Restored visibility for {Count} messages", receiptHandles.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to restore visibility for some messages");
        }
    }

    public async Task<RedriveSummary> RedriveDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default)
    {
        var dlqUrl = _queueConsumerOptions.DeadLetterQueueUrl!;
        var mainQueueUrl = _queueConsumerOptions.QueueUrl;
        var startedAt = DateTime.UtcNow;

        var summary = new RedriveSummaryBuilder();

        var messagesToRedrive = maxMessages;
        if (maxMessages <= 0)
        {
            var stats = await amazonSqs.GetQueueAttributesAsync(dlqUrl,
                [DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages], ct);
            messagesToRedrive = stats.ApproximateNumberOfMessages;

            logger.LogInformation("Redriving all {Count} messages from DLQ (max allowed: {MaxAllowed})", 
                messagesToRedrive, _queueConsumerOptions.MaxRedriveMessages);
            
            // Cap to configured maximum
            messagesToRedrive = Math.Min(messagesToRedrive, _queueConsumerOptions.MaxRedriveMessages);

            if (stats.ApproximateNumberOfMessages == 0)
            {
                logger.LogInformation("DLQ is empty, nothing to redrive");
                return summary.Build(0, startedAt);
            }
        }
        else
        {
            // Also cap explicit requests to the configured maximum
            var originalRequest = messagesToRedrive;
            messagesToRedrive = Math.Min(maxMessages, _queueConsumerOptions.MaxRedriveMessages);
            
            if (messagesToRedrive < originalRequest)
            {
                logger.LogWarning("Requested to redrive {Requested} messages, capped to configured maximum of {Capped}", 
                    originalRequest, messagesToRedrive);
            }
        }

        for (var i = 0; i < messagesToRedrive; i++)
        {
            var message = await TryReceiveMessageAsync(dlqUrl, ct);
            if (message == null)
                break;

            var correlationId = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.CorrelationId);
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
            DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages
        }, ct);

        var approximateCount = statsBeforePurge.ApproximateNumberOfMessages;

        logger.LogWarning(DeadLetterQueueServiceConstants.LogMessages.PurgingQueue, dlqUrl, approximateCount);

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
            logger.LogWarning(DeadLetterQueueServiceConstants.LogMessages.NoDlqConfigured, message.MessageId);
            return false;
        }

        var sendSucceeded = false;
        var deleteSucceeded = false;

        try
        {
            await amazonSqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = message.ReceiptHandle,
                VisibilityTimeout = DeadLetterQueueServiceConstants.Timeouts.ExtendedVisibilitySeconds
            }, cancellationToken);

            var attributes = new Dictionary<string, MessageAttributeValue>(message.MessageAttributes ?? new Dictionary<string, MessageAttributeValue>())
            {
                [DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureReason] = new()
                {
                    StringValue = ex.GetType().Name,
                    DataType = DeadLetterQueueServiceConstants.StringDataType
                },
                [DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureMessage] = new()
                {
                    StringValue = ex.Message.Substring(0, Math.Min(DeadLetterQueueServiceConstants.Limits.MaxSqsMessageAttributeLength, ex.Message.Length)),
                    DataType = DeadLetterQueueServiceConstants.StringDataType
                },
                [DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureTimestamp] = new()
                {
                    StringValue = DateTime.UtcNow.ToString("O"),
                    DataType = DeadLetterQueueServiceConstants.StringDataType
                },
                [DeadLetterQueueServiceConstants.MessageAttributes.DlqOriginalMessageId] = new()
                {
                    StringValue = message.MessageId,
                    DataType = DeadLetterQueueServiceConstants.StringDataType
                },
                [DeadLetterQueueServiceConstants.MessageAttributes.DlqReceiveCount] = new()
                {
                    StringValue = (message.Attributes ?? []).GetValueOrDefault(DeadLetterQueueServiceConstants.SqsAttributes.ApproximateReceiveCount, "0"),
                    DataType = DeadLetterQueueServiceConstants.NumberDataType
                }
            };

            if (!attributes.ContainsKey(DeadLetterQueueServiceConstants.MessageAttributes.CorrelationId))
            {
                attributes[DeadLetterQueueServiceConstants.MessageAttributes.CorrelationId] = new MessageAttributeValue
                {
                    DataType = DeadLetterQueueServiceConstants.StringDataType,
                    StringValue = CorrelationIdContext.Value
                };
            }

            var sendRequest = new SendMessageRequest
            {
                QueueUrl = _queueConsumerOptions.DeadLetterQueueUrl,
                MessageBody = message.Body,
                MessageAttributes = attributes
            };

            var sendResponse = await amazonSqs.SendMessageAsync(sendRequest, cancellationToken);
            sendSucceeded = true;

            logger.LogInformation(DeadLetterQueueServiceConstants.LogMessages.SentToDlq, message.MessageId, sendResponse.MessageId);

            await amazonSqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            deleteSucceeded = true;

            logger.LogInformation(DeadLetterQueueServiceConstants.LogMessages.MovedToDlq, message.MessageId);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception dlqEx)
        {
            var status = sendSucceeded
                ? DeadLetterQueueServiceConstants.LogMessages.SendSucceededDeleteFailed
                : DeadLetterQueueServiceConstants.LogMessages.FailedToSend;

            logger.LogError(dlqEx, "{Status}. MessageId: {MessageId}, SendSucceeded: {SendSucceeded}, DeleteSucceeded: {DeleteSucceeded}",
                status, message.MessageId, sendSucceeded, deleteSucceeded);

            if (sendSucceeded && !deleteSucceeded)
            {
                logger.LogError(DeadLetterQueueServiceConstants.LogMessages.DeleteFailed, message.MessageId);
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
            VisibilityTimeout = DeadLetterQueueServiceConstants.Timeouts.ReceiveMessageVisibilitySeconds,
            MessageSystemAttributeNames = new List<string> { DeadLetterQueueServiceConstants.AllAttributes },
            MessageAttributeNames = new List<string> { DeadLetterQueueServiceConstants.AllAttributes }
        }, ct);

        return receiveResponse.Messages?.Count > 0 ? receiveResponse.Messages[0] : null;
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
            logger.LogError(DeadLetterQueueServiceConstants.LogMessages.DuplicateDetected, message.MessageId, correlationId);
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
                .Where(kvp => !kvp.Key.StartsWith(DeadLetterQueueServiceConstants.MessageAttributes.DlqPrefix))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var sendRequest = new SendMessageRequest
            {
                QueueUrl = mainQueueUrl,
                MessageBody = message.Body,
                MessageAttributes = cleanedAttributes
            };

            await amazonSqs.SendMessageAsync(sendRequest, ct);

            logger.LogInformation(DeadLetterQueueServiceConstants.LogMessages.RedroveFromDlq, message.MessageId, correlationId);

            return OperationResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, DeadLetterQueueServiceConstants.LogMessages.FailedToSendToMainQueue, message.MessageId);

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
            logger.LogError(ex, DeadLetterQueueServiceConstants.LogMessages.FailedToDeleteFromDlq, message.MessageId, correlationId);

            return OperationResult.Failed();
        }
    }

    private async Task<int> GetApproximateMessageCountAsync(string queueUrl, CancellationToken ct)
    {
        var stats = await amazonSqs.GetQueueAttributesAsync(queueUrl, new List<string> { DeadLetterQueueServiceConstants.SqsAttributes.ApproximateNumberOfMessages }, ct);
        return stats.ApproximateNumberOfMessages;
    }

    private static string? GetMessageAttribute(Message message, string key)
    {
        return message.MessageAttributes.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    private sealed class RedriveSummaryBuilder
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