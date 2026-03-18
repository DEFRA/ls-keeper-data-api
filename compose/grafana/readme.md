# Local Grafana Monitoring

This directory contains the configuration and provisioning files for running Grafana locally alongside the Keeper Data API. It is designed to emulate the monitoring capabilities we have in the deployed AWS/CDP environments.

## Getting Started

Grafana is included in the default local development stack via `docker-compose.override.yml`. 

To start Grafana along with the rest of the application, simply run your standard docker-compose up command from the root of the repository:

```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```
z
## Accessing the Dashboard

    URL:http://localhost:3000

    Login: Authentication is bypassed locally. Anonymous access is enabled with Admin privileges, so you will be taken straight in without needing a username or password.

## Available Dashboards

Once in Grafana, navigate to Dashboards (the four-squares icon on the left menu) -> Dashboards. You will see the auto-provisioned ls-keeper-data-api (custom) dashboard.

This dashboard provides panels for:

    Scan Progress: Tracks the number of items (Holdings, Parties, Herds, etc.) processed by the Daily and Bulk background scanning jobs.

    Overall Health: Shows the combined API health check status (Healthy, Degraded, Unhealthy).

    Detailed Health: Breaks down health by dependencies (MongoDB, S3, DataBridge API, SQS/SNS).

    Queues: Monitors the depth of the main Intake queue and the Dead Letter Queue (DLQ).

    Operations: Tracks API request throughput and duration via custom CloudWatch metrics.

## Architecture & Data Sources

Local Grafana is configured (via datasources.yaml) to pull data directly from LocalStack's CloudWatch emulator.

Whenever the local .NET API publishes an EMF (Embedded Metric Format) log or records a metric using IApplicationMetrics, it is sent to LocalStack. Grafana then queries LocalStack to visualize this data.
Modifying Dashboards

If you want to tweak the dashboard (add new panels, change colors, fix queries) and save those changes for the rest of the team:

    Make your changes directly in the local Grafana UI in your browser.

    Click the Save icon (or press Ctrl+S / Cmd+S).

    Click the Dashboard settings (gear icon) at the top right.

    Go to the JSON Model tab.

    Copy the entire JSON payload.

    Paste and overwrite the contents of compose/grafana/dashboards/ls-keeper-data-api.json in your IDE.

    Commit the changes to source control.

(Note: Because dashboards are provisioned from disk, restarting the Grafana container will revert any unsaved UI changes back to what is in the .json file.)
Troubleshooting

    No data is showing on the graphs:
    Metrics are only generated when the API is actively doing something. Try hitting the http://localhost:5555/health endpoint a few times, or trigger a local import scan via the Swagger UI to generate some data points. Ensure the Grafana time picker (top right) is set to a short recent window (e.g., "Last 15 minutes").

    Grafana container is crash-looping:
    Check the docker logs (docker logs kda-grafana). Ensure LocalStack is healthy, as Grafana depends on it to start properly.