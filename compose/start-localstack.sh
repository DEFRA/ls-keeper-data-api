#!/bin/bash
sed -i 's/\r$//' "$0"

export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

set -e

echo "Bootstrapping SQS setup..."

# Create SQS resources
queue_url=$(awslocal sqs create-queue  \
  --queue-name ls_keeper_data_intake_queue \
  --endpoint-url=http://localhost:4566 \
  --output text \
  --query 'QueueUrl')

echo "SQS Queue created: $queue_url"

echo "Bootstrapping Complete"
