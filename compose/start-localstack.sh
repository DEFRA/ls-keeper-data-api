#!/bin/bash
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

# S3 buckets
# aws --endpoint-url=http://localhost:4566 s3 mb s3://my-bucket

# SQS queues
# aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name my-queue

# aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name ls_keeper_data_intake_queue --region eu-west-2

echo "Bootstrapping SQS setup..."

# Create SNS Topics
queue_url=$(awslocal sqs create-queue  \
  --queue-name ls-keeper-data-bridge-events \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')

echo "SQS Queue created: $queue_url"