using Docker.DotNet;
using Docker.DotNet.Models;

namespace KeeperData.Api.Tests.Integration.Helpers;

public static class DockerContainerHelper
{
    private static readonly object s_lock = new();

    public static async Task EnsureContainerRemovedAsync(string containerName)
    {
        await Task.Run(() =>
        {
            lock (s_lock)
            {
                try
                {
                    using var dockerClient = new DockerClientConfiguration().CreateClient();

                    var containers = dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                    {
                        All = true,
                        Filters = new Dictionary<string, IDictionary<string, bool>>
                        {
                            ["name"] = new Dictionary<string, bool> { [containerName] = true }
                        }
                    }).GetAwaiter().GetResult();

                    var existingContainer = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
                    if (existingContainer != null)
                    {
                        // Stop the container if it's running
                        if (existingContainer.State == "running")
                        {
                            dockerClient.Containers.StopContainerAsync(existingContainer.ID, new ContainerStopParameters
                            {
                                WaitBeforeKillSeconds = 5
                            }).GetAwaiter().GetResult();
                        }

                        // Remove the container
                        dockerClient.Containers.RemoveContainerAsync(existingContainer.ID, new ContainerRemoveParameters
                        {
                            Force = true
                        }).GetAwaiter().GetResult();
                    }
                }
                catch (DockerApiException ex)
                {
                    // If the container doesn't exist or other Docker API errors, we can safely ignore
                    // as the goal is to ensure the container is removed
                    System.Diagnostics.Debug.WriteLine($"Docker API exception when removing container '{containerName}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to remove Docker container '{containerName}'. Error: {ex.Message}", ex);
                }
            }
        });
    }
}