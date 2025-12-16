#!/bin/bash
sed -i 's/\r$//' "$0"

export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export ENDPOINT_URL=http://localhost:4566

set -e

# S3
echo "Create comparison reports bucket..."

## S3: Create comparison reports bucket
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

# SQS

echo "Create intake DLQ..."

# Create intake DLQ
dlq_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue-deadletter \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "Intake DLQ created: $dlq_url"

# Get the ARN of the DLQ, which is needed for the redrive policy
dlq_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$dlq_url" \
  --attribute-names QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "Intake DLQ ARN: $dlq_arn"

# Create intake queue
queue_url=$(awslocal sqs create-queue \
  --queue-name ls_keeper_data_intake_queue \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')
echo "Intake queue created: $queue_url"

# Define the Redrive Policy, linking the main queue to the DLQ.
redrive_policy_json=$(cat <<EOF
{
  "deadLetterTargetArn": "$dlq_arn",
  "maxReceiveCount": "3"
}
EOF
)

# Set redrive policy for intake DLQ
echo "Set redrive policy for intake DLQ..."
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$dlq_arn\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
  --endpoint-url=$ENDPOINT_URL
# =================================================================

# Get the Intake queue ARN
queue_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$queue_url" \
  --attribute-name QueueArn \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'Attributes.QueueArn')
echo "Intake queue ARN: $queue_arn"

# SNS

# Create data bridge events topic
topic_arn=$(awslocal sns create-topic \
  --name ls-keeper-data-bridge-events \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'TopicArn')
echo "Data bridge events topic created: $topic_arn"

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

# Set the SQS policy on the intake queue
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"Policy\": \"$policy_escaped\"}" \
  --endpoint-url=$ENDPOINT_URL

echo "Intake queue policy configured successfully"

# Subscribe the intake queue to the SNS Topic
subscription_arn=$(awslocal sns subscribe \
  --topic-arn "$topic_arn" \
  --protocol sqs \
  --notification-endpoint "$queue_arn" \
  --endpoint-url=$ENDPOINT_URL \
  --output text \
  --query 'SubscriptionArn')
echo "SNS Topic subscription complete for main queue: $subscription_arn"

# Create import completed topic
topic_arn=$(awslocal sns create-topic \
  --name ls_keeper_data_import_complete \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'TopicArn')
echo "Import completed topic created: $topic_arn"

echo "Bootstrapping Complete"