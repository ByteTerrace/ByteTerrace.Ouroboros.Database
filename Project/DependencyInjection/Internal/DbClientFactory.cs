using Microsoft.Toolkit.Diagnostics;

namespace ByteTerrace.Ouroboros.Database;
internal sealed class DbClientFactory : IDbClientFactory<DbClient>, IDbConnectionFactory
{
    public static DbClientFactory New(Action<DbClientOptions> optionsAction) =>
        new(optionsAction: optionsAction);

    public Action<DbClientOptions> OptionsAction { get; init; }

    private DbClientFactory(Action<DbClientOptions> optionsAction) {
        OptionsAction = optionsAction;
    }

    public IDbClient NewClient(string name) {
        var clientOptions = DbClientOptions.New();

        OptionsAction(obj: clientOptions);

        var providerFactory = clientOptions.ProviderFactory;

        if (providerFactory is null) {
            ThrowHelper.ThrowArgumentNullException(name: $"{nameof(clientOptions)}.{nameof(clientOptions.ProviderFactory)}");
        }

        var connection = ((IDbConnectionFactory)this).NewConnection(
            name: name,
            providerFactory: providerFactory
        );

        connection.ConnectionString = clientOptions.ConnectionString;
        clientOptions.Connection = connection;

        return DbClient.New(options: clientOptions);
    }
}
