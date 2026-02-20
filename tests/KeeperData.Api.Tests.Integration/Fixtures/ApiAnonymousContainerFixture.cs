using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Fixtures;

public class ApiAnonymousContainerFixture() : ApiContainerFixture(enableAnonymization: true);