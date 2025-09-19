#!/bin/bash
sed -i 's/\r$//' "$0"

export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

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

# Create SQS resources
queue_url=$(awslocal sqs create-queue  \
  --queue-name ls_keeper_data_intake_queue \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')

echo "SQS Queue created: $queue_url"

# Get the SQS Queue ARN
queue_arn=$(awslocal sqs get-queue-attributes \
  --queue-url "$queue_url" \
  --attribute-name QueueArn \
  --output text \
  --query 'Attributes.QueueArn')

echo "SQS Queue ARN: $queue_arn"

# Create SNS Topics
topic_arn=$(awslocal sns create-topic \
  --name ls-keeper-data-bridge-events \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'TopicArn')

echo "SNS Topic created: $topic_arn"

# Construct the policy JSON inline with escaped quotes
policy_json=$(cat <<EOF
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

# Set SQS policy
awslocal sqs set-queue-attributes \
  --queue-url "$queue_url" \
  --attributes "{\"Policy\": \"$(
    echo "$policy_json" | jq -c
  )\"}"

# Subscribe the Queue to the Topic
awslocal sns subscribe \
  --topic-arn "$topic_arn" \
  --protocol sqs \
  --notification-endpoint "$queue_arn"

echo "SNS Topic subscription complete"

echo "Bootstrapping Complete"
