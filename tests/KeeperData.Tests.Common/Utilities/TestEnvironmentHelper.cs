namespace KeeperData.Tests.Common.Utilities;

/// <summary>
/// Provides helper methods for detecting test execution environments.
/// </summary>
public static class TestEnvironmentHelper
{
    /// <summary>
    /// Determines if the tests are running in a CI/CD environment.
    /// </summary>
    /// <returns>True if running in CI/CD (GitHub Actions, Azure DevOps, SonarQube, etc.), false otherwise.</returns>
    public static bool IsRunningInCi()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDID")) ||  // Azure DevOps
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_HOME")) ||    // Jenkins
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITLAB_CI")) ||       // GitLab CI
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CIRCLECI")) ||        // CircleCI
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TRAVIS")) ||          // Travis CI
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONAR_SCANNER_OPTS")); // SonarQube
    }

    /// <summary>
    /// Determines if the tests are running in GitHub Actions specifically.
    /// </summary>
    /// <returns>True if running in GitHub Actions, false otherwise.</returns>
    public static bool IsRunningInGitHubActions()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    }

    /// <summary>
    /// Determines if the tests are running locally (not in CI/CD).
    /// </summary>
    /// <returns>True if running locally, false if in CI/CD.</returns>
    public static bool IsRunningLocally()
    {
        return !IsRunningInCi();
    }
}