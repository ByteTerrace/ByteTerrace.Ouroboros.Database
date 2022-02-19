using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database;

internal sealed class DbClientConfigurationSource : IConfigurationSource
{
    public static DbClientConfigurationSource New(
        Action<DbClientOptions> clientOptionsInitializer,
        Action<DbClientConfigurationProviderOptions> configurationOptionsInitializer,
        string configurationSectionName,
        string name
    ) =>
        new(
            clientOptionsInitializer: clientOptionsInitializer,
            configurationOptionsInitializer: configurationOptionsInitializer,
            configurationSectionName: configurationSectionName,
            name: name
        );

    public Action<DbClientConfigurationProviderOptions> ClientConfigurationProviderOptionsInitializer { get; set; }
    public Action<DbClientOptions> ClientOptionsInitializer { get; set; }
    public string ConfigurationSectionName { get; set; }
    public string Name { get; set; }

    private DbClientConfigurationSource(
        Action<DbClientOptions> clientOptionsInitializer,
        Action<DbClientConfigurationProviderOptions> configurationOptionsInitializer,
        string configurationSectionName,
        string name
    ) {
        ClientConfigurationProviderOptionsInitializer = configurationOptionsInitializer;
        ClientOptionsInitializer = clientOptionsInitializer;
        ConfigurationSectionName = configurationSectionName;
        Name = name;
    }

    public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) =>
        DbClientConfigurationProvider.New(
            clientFactory: DbClientFactory.New(
                optionsAction: ClientOptionsInitializer
            ),
            name: Name,
            options: new DbClientConfigurationSourceOptions() {
                ClientConfigurationProviderOptionsActions = new Action<DbClientConfigurationProviderOptions>[] {
                    ClientConfigurationProviderOptionsInitializer,
                }
            }
        );
}
