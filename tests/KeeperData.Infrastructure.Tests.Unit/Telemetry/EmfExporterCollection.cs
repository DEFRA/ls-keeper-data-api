namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

// EmfExporter holds static state (logger, namespace, CloudWatch client), so these classes must not run concurrently.
[CollectionDefinition("EmfExporter", DisableParallelization = true)]
public class EmfExporterCollection
{
}