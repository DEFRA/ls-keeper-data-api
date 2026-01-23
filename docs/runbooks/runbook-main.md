# Keeper Data API Runbook

## Service Overview

### What the API does
The Keeper Data API is a .NET 8 ASP.NET Core service that manages livestock keeper and site data for DEFRA. It provides REST endpoints for querying parties (keepers) and sites (holdings), handling data ingestion from external sources, and maintaining synchronization between different livestock management systems.

Key business functions:
- **Data Query Services**: Provides paginated REST endpoints to search and retrieve party and site information
- **Data Integration**: Consumes messages from AWS SQS queues to import data from CTS (Cattle Tracing System) and SAM (Sheep and Goat Identification Database)
- **Data Synchronization**: Orchestrates bulk and incremental data scans from bridge services
- **Event Processing**: Handles intake events for livestock data updates and maintains consistency across systems

### High-level Architecture

```
    ┌───────────────────────┐                           ┌──────────────────────────────────┐
    │   Keeper Data Bridge  │──────────────────────────▶│  ls_keeper_data_intake_queue     │
    └───────────────────────┘                           │  (SQS)                           │
                                                        └──────────────────────────────────┘
                                                                          │
                                                                          ▼
    ┌───────────────────────┐                           ┌──────────────────────────────────┐              ┌─────────────────┐
    │ Keeper Data Bridge    │◀──────────────────────────│    Keeper Data API               │◀────────────▶│    MongoDB      │
    │ API (External)        │                           │   • Queue Consumer               │              └─────────────────┘
    └───────────────────────┘                           │   • Data Ingestion               │      ┌─────────────────────────────────┐             
                 ▲                                      │   • Completeion notification     │─────▶│ ls_keeper_data_import_complete  │              
                 │                                      │                                  │      │       (SNS Topic)               │
                 │                                      │                                  │      └─────────────────────────────────┘
                 │                                      └──────────────────────────────────┘   
                 └─────────────────── API Calls ─────────────────────┘    │
                                                                          │
                                                                          │
                                                                          ▼
                                                         ┌──────────────────────────────────┐              ┌─────────────────┐
                                                         │    Keeper Data API (External)    │─────────────▶│   Consumers     │
                                                         │       • REST Endpoints           │              │  (Downstream    │
                                                         │       • Data Query Services      │              │   Services)     │
                                                         └──────────────────────────────────┘              └─────────────────┘


```

### Dependencies

**Primary Dependencies:**
- **MongoDB**: Database storing multiple types of livestock data:
  - **Party Data**: Livestock keepers (parties, ctsParties, samParties)
  - **Site Data**: Holdings and premises (sites, ctsHoldings, samHoldings)
  - **Reference Data**: Species, countries, roles, premises types, production usage, site identifier types
  - **Relationship Data**: Party-site relationships, group marks, communications
  - **System Data**: Distributed locks, working collections
- **AWS SQS**: Message queue for processing intake events (`ls_keeper_data_intake_queue`)
- **AWS SNS**: Notification topics for event publishing (`ls_keeper_data_import_complete`)
- **AWS S3**: Storage for comparison reports (configured but limited usage)
- **Data Bridge API**: External service for CTS/SAM data integration

**Infrastructure Dependencies:**
- **Localstack**: AWS services emulation for local development
- **Container Runtime**: Docker for containerized deployment
- **Redis**: Caching layer (configured for future use, not currently active)

## Ownership & Contacts

**Primary Team:** TBD

**On-call Escalation:** TBD

**Communication Channels:**
- Slack Alerts:
  - #cas-team-alerts-non-prod (Defra Digital team Slack)
  - #cas-team-alerts-prod (Defra Digital team Slack))
- Teams:
  - MST-Defra-LITP Digital Delivery (Defra Teams)
- Jira:
  - https://eaflood.atlassian.net/jira/software/c/projects/ULITP/boards/6643
- Confluence:
  - https://eaflood.atlassian.net/wiki/spaces/LDD/pages/5785682190/Keeper+Reference+Data+Service+KRDS

## Operational Characteristics

**Expected Throughput:**
- API: TBD
- Queue Processing: Configurable via MaxNumberOfMessages and WaitTimeSeconds
- Bulk Scans: TBD

**Latency SLOs:**
- API Queries: TBD
- Health Checks: TBD
- Queue Message Processing: TBD

**Rate Limits:**
- Queue polling: Configured via QueueConsumerOptions
- API pagination: Default 10, max 500, Sites max 100

**Scheduled Operations:**
- CTS Bulk Scan: "0 0 4 * * ?" (4:00 AM daily) - currently disabled
- SAM Bulk Scan: "0 0 6 * * ?" (6:00 AM daily) - currently disabled  
- CTS Daily Scan: "0 0 4 * * ?" (4:00 AM daily) - currently disabled
- SAM Daily Scan: "0 0 6 * * ?" (6:00 AM daily) - currently disabled

**Deployment:**
All deployment operations driven through the CDP portal: https://portal.cdp-int.defra.cloud/services/ls-keeper-data-api

## Monitoring & Observability

### Dashboards
Each environment has 2 corresponding Grafana dashboards:

**Service Dashboard** - Common performance metrics:
- CPU, memory and network usage
- Request rates and response times
- Error rates and status codes

**Custom Dashboard** - API-specific metrics:
- Overall health status
- Integration connectivity (MongoDB, SQS/SNS, Data Bridge API)
- Queue status and processing rates
- Logged errors and warnings

All dashboards are linked in the CDP portal.

### Key Metrics

**HTTP Request Metrics (via ApplicationMetrics):**
- `requests_total`: Total number of requests by operation and status
- `duration_milliseconds`: Duration of operations in milliseconds
- `http_requests`: Request count with method, endpoint, status_code, status tags (via ExceptionHandlingMiddleware)
- `http_errors`: Error count with error_type, exception_type, status_code tags

**Health Check Metrics:**
- `keeperdata.health.status`: Current health status (2=Healthy, 1=Degraded, 0=Unhealthy)

**Queue Processing Metrics:**
- All queue metrics available under `AWS/SQS` namespace in CDP's CloudWatch

### Log Locations

**Local Development:**
- Container logs via `docker compose logs keeperdata_api`
- Structured JSON logging with ECS format

**Deployed environments:**
- Logs stored in CloudWatch and accessed via OpenSearch
- Environment-specific log access links available in CDP portal

### Health Check Endpoints

- **Primary**: `GET /health` - Returns comprehensive system health
- **Basic**: `GET /` - Simple aliveness check (returns "Alive!")

### Alerts Configuration

**Standard CDP Alerts:**
- Standard set of CDP platform alerts configured for all environments

**Service-specific Alerts:**
- `ls-keeper-data-api-sqs-dlq`: Triggered if any messages reach the dead letter queue
- `ls-keeper-data-api-health-status`: Triggered if the healthcheck reports unhealthy

## Common Failure Modes & Incident Procedures

**TBD** - No specific failure modes or incidents have been identified yet. This section will be updated as operational experience is gained and patterns emerge.

## Local Tools

**Note:** For detailed local development procedures and setup instructions, see [README.md](README.md)

### Local Development
```bash
# Start full environment
docker compose up --build -d

# View logs
docker compose logs -f keeperdata_api

# Stop environment
docker compose down

# Run tests
dotnet test

# Format code
dotnet format KeeperData.Api.sln
```

### Health Checks
```bash
# Basic health check
curl http://localhost:5555/health

# Pretty-printed health status
curl http://localhost:5555/health | jq '.'

# Simple aliveness
curl http://localhost:5555/
```

### Queue Management
```bash
# Check queue status (LocalStack)
aws --endpoint-url=http://localhost:4566 sqs get-queue-attributes \
  --queue-url http://sqs.eu-west-2.localhost.localstack.cloud:4566/000000000000/ls_keeper_data_intake_queue \
  --attribute-names All

# Purge queue (CAUTION: Data loss)
aws --endpoint-url=http://localhost:4566 sqs purge-queue \
  --queue-url http://sqs.eu-west-2.localhost.localstack.cloud:4566/000000000000/ls_keeper_data_intake_queue
```

### Service Restart
```bash
# Docker Compose restart
docker compose restart keeperdata_api

# Full environment restart
docker compose down && docker compose up -d
```

### Database Operations
```bash
# Connect to MongoDB (local)
mongosh mongodb://localhost:27019

# Check database status
mongosh --eval "db.adminCommand('ping')"
```

## CDP Tools

### Health Checks
```bash
# Basic health check
curl http://localhost:8085/health

# Pretty-printed health status
curl http://localhost:8085/health | jq '.'

# Simple aliveness
curl http://localhost:8085/
```

### Queue Management

**Note:** Manual Queue procedures TBD

### DLQ Redrive Process

**Non-production environments:**
- Follow the process outlined here: https://portal.cdp-int.defra.cloud/documentation/how-to/sqs-sns.md#how-do-i-re-drive-messages-on-the-dead-letter-queue-

**Production environments:**
- Make a request to the CDP team on the #cdp-support Slack channel


### Database Operations

**Note:** Mongo procedures TBD

## Release & Rollback Procedures

### Deployment Process
All deployments handled through the CDP Portal

### Rollback Process
Rollbacks are handled be redeploying the previous version through the CDP Portal

### Validation Steps
- Health check returns "Healthy"
- API endpoints respond correctly
- Queue processing resumed
- External integrations working
- No error spike in logs

## Configuration & Secrets

### Configuration Sources
- **Primary**: Environment variables
- **Local**: `docker-compose.override.yml`
- **Deployed**: Environment variables defined in `cdp-app-config` repo

### Key Configuration
```json
{
  "Mongo": {
    "DatabaseUri": "Connection string",
    "DatabaseName": "ls-keeper-data-api"
  },
  "QueueConsumerOptions": {
    "IntakeEventQueueOptions": {
      "QueueUrl": "SQS queue URL",
      "Disabled": false
    }
  },
  "ApiClients": {
    "DataBridgeApi": {
      "BaseUrl": "External API base URL",
      "BridgeApiSubscriptionKey": "API key"
    }
  }
}
```

### Secret Rotation
1. Update secrets in secret management system
2. Update environment configuration
3. Restart service to pick up changes
4. Verify connectivity to all dependent services

## Security & Compliance

### Authentication/Authorization
- **Internal Service**: No authentication implemented locally
- **Health Endpoints**: Anonymous access allowed
- **AWS Services**: Cognito based identity management, owned by CDP team

### Data Classification
- **PII**: Contains personal information of livestock keepers
- **Business Critical**: Essential for livestock traceability
- **Retention**: Follow DEFRA data retention policies

### Audit Logging
- All API requests logged with correlation IDs
- Queue message processing tracked
- Health check executions recorded
- Configuration changes audited

### Compliance Notes
- GDPR compliance for personal data
- DEFRA data handling requirements
- AWS security best practices

## Appendices

### API Specifications
- **OpenAPI/Swagger**: Available at `/swagger` (development)
- **Endpoints**:
  - `GET /api/parties` - Search parties/keepers
  - `GET /api/parties/{id}` - Get party by ID
  - `GET /api/sites` - Search sites/holdings
  - `GET /api/sites/{id}` - Get site by ID

### Known Quirks & Tribal Knowledge

**Queue Processing:**
- Messages are throttled with configurable delays between processing
- Dead letter queue configured with 3 retry attempts
- SNS/SQS integration requires specific IAM permissions

**MongoDB:**
- Transactions disabled by default in configuration
- Connection pooling managed automatically
- Health check uses simple ping command

**Scheduled Jobs:**
- All scheduled jobs currently disabled by default (EnabledFrom: 2030)
- Jobs use Quartz scheduler with cron expressions
- Concurrent execution prevented via `DisallowConcurrentExecution`

**Development Environment:**
- LocalStack containers must start before API
- Redis configured but not actively used in current version
- Different Docker Compose overrides for Mac ARM vs Intel

### Useful Queries & Commands

**MongoDB Queries:**
```javascript
// Check collections
show collections

// Count documents in collections
db.parties.countDocuments()
db.sites.countDocuments()

// Recent updates
db.parties.find().sort({lastUpdatedDate: -1}).limit(10)
```

**Log Analysis:**
```bash
# Find correlation ID traces
grep "correlationId.*abc-123" <log-file>

# Monitor queue processing
grep "HandleMessageAsync" <log-file> | tail -f

# Check health check patterns
grep "health.*check" <log-file>
```

---
*Last Updated: January 2026*
*Version: 1.0*