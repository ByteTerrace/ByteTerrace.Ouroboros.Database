using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Diagnostics;

namespace ByteTerrace.Ouroboros.Database;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DbClientConfigurationRefresherProvider : IDbClientConfigurationRefresherProvider
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    public IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; init; }

    public DbClientConfigurationRefresherProvider(
        IDbClientFactory clientFactory,
        IConfiguration configuration
    ) {
        var configurationRoot = configuration as IConfigurationRoot;
        var refreshers = new List<IDbClientConfigurationRefresher>();

        if (configurationRoot is not null) {
            foreach (var provider in configurationRoot.Providers) {
                if (provider is IDbClientConfigurationRefresher refresher) {
                    refresher.ClientFactory = clientFactory;

                    refreshers.Add(refresher);
                }
            }
        }

        if (!refreshers.Any()) {
            ThrowHelper.ThrowInvalidOperationException(message: $"Unable to find a {nameof(DbClientConfigurationProvider)} refresher. Please ensure that one has been configured correctly.");
        }

        Refreshers = refreshers;
    }
}
