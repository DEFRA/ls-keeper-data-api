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

# =================================================================
# === DLQ SETUP ===================================================
# =================================================================
# 1. Create the Dead-Letter Queue (DLQ) first.
dlq_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue-deadletter \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS Dead-Letter Queue created: $dlq_url"

# 2. Get the ARN of the DLQ, which is needed for the main queue's redrive policy.
dlq_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$dlq_url" \
  --attribute-names QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "DLQ ARN: $dlq_arn"
# =================================================================

# Create the main SQS queue.
queue_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "SQS Main Queue created: $queue_url"

# =================================================================
# === CONFIGURE REDRIVE POLICY ====================================
# =================================================================
# 3. Define the Redrive Policy, linking the main queue to the DLQ.
#    maxReceiveCount is the number of times a message is received before being moved.
redrive_policy_json=$(cat <<EOF
{
  "deadLetterTargetArn": "$dlq_arn",
  "maxReceiveCount": "3"
}
EOF
)

# 4. Apply the Redrive Policy to the main queue.
echo "Configuring Redrive Policy..."
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$dlq_arn\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
  --endpoint-url=$ENDPOINT_URL
# =================================================================

# Get the main SQS Queue ARN for the SNS subscription policy.
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

echo "Bootstrapping Complete"