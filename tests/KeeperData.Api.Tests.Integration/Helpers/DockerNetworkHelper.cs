using Docker.DotNet;
using Docker.DotNet.Models;

namespace KeeperData.Api.Tests.Integration.Helpers;

public static class DockerNetworkHelper
{
    private static readonly object s_lock = new();
    private static readonly HashSet<string> s_createdNetworks = [];

    public static void EnsureNetworkExists(string networkName)
    {
        lock (s_lock)
        {
            if (s_createdNetworks.Contains(networkName))
            {
                return;
            }

            try
            {
                using var dockerClient = new DockerClientConfiguration().CreateClient();

                var networks = dockerClient.Networks.ListNetworksAsync(new NetworksListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool> { [networkName] = true }
                    }
                }).GetAwaiter().GetResult();

                if (networks.Any(n => n.Name == networkName))
                {
                    s_createdNetworks.Add(networkName);
                    return;
                }

                dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
                {
                    Name = networkName,
                    Driver = "bridge"
                }).GetAwaiter().GetResult();

                s_createdNetworks.Add(networkName);
            }
            catch (DockerApiException ex) when (ex.Message.Contains("already exists"))
            {
                // Network already exists, which is fine
                s_createdNetworks.Add(networkName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create Docker network '{networkName}'. Error: {ex.Message}", ex);
            }
        }
    }
}