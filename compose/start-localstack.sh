#!/bin/bash
sed -i 's/\r$//' "$0"

export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export ENDPOINT_URL=http://localhost:4566

set -e

# S3 buckets
echo "Bootstrapping S3 setup..."

## Create 'test-comparison-reports-bucket' Bucket
existing_bucket=$(awslocal s3api list-buckets \
  --query "Buckets[?Name=='test-comparison-reports-bucket'].Name" \
  --output text)

if [ "$existing_bucket" == "test-comparison-reports-bucket" ]; then
  echo "S3 bucket already exists: test-comparison-reports-bucket"
else
  awslocal s3api create-bucket --bucket test-comparison-reports-bucket --region eu-west-2 \
    --create-bucket-configuration LocationConstraint=eu-west-2 \
    --endpoint-url=http://localhost:4566
  echo "S3 bucket created: test-comparison-reports-bucket"
fi

echo "Bootstrapping SQS setup..."

# Create the Dead-Letter Queue (DLQ) first.
dlq_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue-deadletter \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS Dead-Letter Queue created: $dlq_url"

# Get the ARN of the DLQ, which is needed for the main queue's redrive policy.
dlq_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$dlq_url" \
  --attribute-names QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "DLQ ARN: $dlq_arn"

# Create the main SQS queue.
queue_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS Main Queue created: $queue_url"


# Define the Redrive Policy, linking the main queue to the DLQ.
redrive_policy_json=$(cat <<EOF
{
  "deadLetterTargetArn": "$dlq_arn",
  "maxReceiveCount": "3"
}
EOF
)

# Apply the Redrive Policy to the main queue.
echo "Configuring Redrive Policy..."
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$dlq_arn\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
  --endpoint-url=$ENDPOINT_URL
# =================================================================

# Get the SQS Queue ARN
queue_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$queue_url" \
  --attribute-name QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "SQS Main Queue ARN: $queue_arn"

# Create SNS Topic.
topic_arn=$(awslocal sns create-topic \
  --name ls-keeper-data-bridge-events \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'TopicArn')
echo "SNS Topic created: $topic_arn"

# Construct the policy JSON inline with escaped quotes
policy_escaped=$(cat <<EOF | tr -d '\n' | sed 's/"/\\"/g'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "$queue_arn",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "$topic_arn"
        }
      }
    }
  ]
}
EOF
)

# Set the SQS policy on the main queue.
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"Policy\": \"$policy_escaped\"}" \
  --endpoint-url=$ENDPOINT_URL

echo "SQS policy configured successfully"

# Subscribe the main SQS queue to the SNS Topic.
subscription_arn=$(awslocal sns subscribe \
  --topic-arn "$topic_arn" \
  --protocol sqs \
  --notification-endpoint "$queue_arn" \
  --endpoint-url=$ENDPOINT_URL \
  --output text \
  --query 'SubscriptionArn')
echo "SNS Topic subscription complete for main queue: $subscription_arn"

# =================================================================
# FIFO Queue Setup
# =================================================================
echo "Bootstrapping FIFO SQS setup..."

# Create the Dead-Letter Queue (DLQ) for FIFO first.
fifo_dlq_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_fifo_queue.fifo \
  --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"false"}' \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS FIFO Dead-Letter Queue created: $fifo_dlq_url"

# Get the ARN of the FIFO DLQ
fifo_dlq_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$fifo_dlq_url" \
  --attribute-names QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "FIFO DLQ ARN: $fifo_dlq_arn"

# Create the main FIFO SQS queue.
fifo_queue_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_standard_fifo_queue.fifo \
  --attributes '{"FifoQueue":"true","ContentBasedDeduplication":"false"}' \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS FIFO Main Queue created: $fifo_queue_url"

# Apply the Redrive Policy to the FIFO main queue.
echo "Configuring FIFO Redrive Policy..."
awslocal sqs set-queue-attributes \
  --queue-url "$fifo_queue_url" \
  --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$fifo_dlq_arn\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
  --endpoint-url=$ENDPOINT_URL

# Get the FIFO SQS Queue ARN
fifo_queue_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$fifo_queue_url" \
  --attribute-name QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "SQS FIFO Main Queue ARN: $fifo_queue_arn"

# Create SNS FIFO Topic for FIFO queue.
fifo_topic_arn=$(awslocal sns create-topic \
  --name ls-keeper-data-bridge-events-fifo.fifo \
  --attributes '{"FifoTopic":"true","ContentBasedDeduplication":"false"}' \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'TopicArn')
echo "SNS FIFO Topic created: $fifo_topic_arn"

# Construct the policy JSON for FIFO queue
fifo_policy_escaped=$(cat <<EOF | tr -d '\n' | sed 's/"/\\"/g'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "$fifo_queue_arn",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "$fifo_topic_arn"
        }
      }
    }
  ]
}
EOF
)

# Set the SQS policy on the FIFO queue.
awslocal sqs set-queue-attributes \
  --queue-url "$fifo_queue_url" \
  --attributes "{\"Policy\": \"$fifo_policy_escaped\"}" \
  --endpoint-url=$ENDPOINT_URL

echo "SQS FIFO policy configured successfully"

# Subscribe the FIFO SQS queue to the SNS FIFO Topic.
fifo_subscription_arn=$(awslocal sns subscribe \
  --topic-arn "$fifo_topic_arn" \
  --protocol sqs \
  --notification-endpoint "$fifo_queue_arn" \
  --endpoint-url=$ENDPOINT_URL \
  --output text \
  --query 'SubscriptionArn')
echo "SNS FIFO Topic subscription complete for FIFO queue: $fifo_subscription_arn"

echo "Bootstrapping Complete"