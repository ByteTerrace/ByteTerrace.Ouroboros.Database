namespace ByteTerrace.Ouroboros.Database;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal class DbClientFactoryOptions<TClientOptions> where TClientOptions : DbClientOptions
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    public IList<Action<TClientOptions>> ClientOptionsActions { get; init; }

    public DbClientFactoryOptions() {
        ClientOptionsActions = new List<Action<TClientOptions>>();
    }
}
