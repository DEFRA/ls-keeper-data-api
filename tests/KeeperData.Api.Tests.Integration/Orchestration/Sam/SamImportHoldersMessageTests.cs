using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Sam;

[Trait("Dependence", "localstack")]
public class SamImportHoldersMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{

}
