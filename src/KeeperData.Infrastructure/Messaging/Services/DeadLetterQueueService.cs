using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.Messaging;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Services
{
    public class DeadLetterQueueService : IDeadLetterQueueService
    {
        private readonly IAmazonSQS _amazonSQS;
        private readonly ILogger<DeadLetterQueueService> _logger;
        private readonly IntakeEventQueueOptions _queueConsumerOptions;

        private const string StringDataType = "String";

        public DeadLetterQueueService(
            IAmazonSQS amazonSQS,
            IOptions<IntakeEventQueueOptions> options,
            ILogger<DeadLetterQueueService> logger)
        {
            _amazonSQS = amazonSQS;
            _logger = logger;
            _queueConsumerOptions = options.Value;
        }

        public async Task<bool> MoveToDeadLetterQueueAsync(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_queueConsumerOptions.DeadLetterQueueUrl))
            {
                _logger.LogWarning("No DLQ configured for message {messageId}", message.MessageId);
                return false;
            }

            var sendSucceeded = false;
            var deleteSucceeded = false;

            try
            {
                // Extend visibility timeout to prevent race conditions
                await _amazonSQS.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
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
                var sendResponse = await _amazonSQS.SendMessageAsync(sendRequest, cancellationToken);
                sendSucceeded = true;

                _logger.LogInformation("Message {originalMessageId} sent to DLQ with new ID {dlqMessageId}",
                    message.MessageId, sendResponse.MessageId);

                // Only delete if send is confirmed successful
                await _amazonSQS.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                deleteSucceeded = true;

                _logger.LogInformation("Message {messageId} successfully moved to DLQ", message.MessageId);
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

                _logger.LogError(dlqEx, "{status}. MessageId: {messageId}, SendSucceeded: {sendSucceeded}, DeleteSucceeded: {deleteSucceeded}",
                    status, message.MessageId, sendSucceeded, deleteSucceeded);

                if (sendSucceeded && !deleteSucceeded)
                {
                    _logger.LogError("Send succeeded but delete failed for MessageId: {messageId}", message.MessageId);
                }

                return false;
            }
        }
    }
}