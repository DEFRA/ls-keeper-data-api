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
        var messagesToRetrieve = Math.Clamp(maxMessages, 1, DeadLetterQueueServiceConstants.Limits.MaxSqsReceiveMessages);

        var receiveResponse = await amazonSqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = dlqUrl,
            MaxNumberOfMessages = messagesToRetrieve,
            MessageSystemAttributeNames = [DeadLetterQueueServiceConstants.AllAttributes],
            MessageAttributeNames = [DeadLetterQueueServiceConstants.AllAttributes]
        }, ct);

        var messages = receiveResponse.Messages?
            .Select(MapToDeadLetterMessageDto)
            .ToList() ?? [];

        var totalCount = await GetApproximateMessageCountAsync(dlqUrl, ct);

        return new DeadLetterMessagesResult
        {
            Messages = messages,
            TotalApproximateCount = totalCount,
            CheckedAt = DateTime.UtcNow
        };
    }

    private static DeadLetterMessageDto MapToDeadLetterMessageDto(Message message)
    {
        return new DeadLetterMessageDto
        {
            MessageId = message.MessageId,
            OriginalMessageId = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.DlqOriginalMessageId),
            FailureReason = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureReason),
            FailureMessage = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureMessage),
            FailureTimestamp = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.DlqFailureTimestamp),
            ReceiveCount = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.DlqReceiveCount),
            CorrelationId = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.CorrelationId),
            MessageType = GetMessageAttribute(message, DeadLetterQueueServiceConstants.MessageAttributes.Subject),
            Body = message.Body
        };
    }

    public async Task<RedriveSummary> RedriveDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default)
    {
        var dlqUrl = _queueConsumerOptions.DeadLetterQueueUrl!;
        var mainQueueUrl = _queueConsumerOptions.QueueUrl;
        var startedAt = DateTime.UtcNow;

        var summary = new RedriveSummaryBuilder();
        var messagesToRedrive = Math.Clamp(maxMessages, 1, DeadLetterQueueServiceConstants.Limits.MaxSqsReceiveMessages);

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