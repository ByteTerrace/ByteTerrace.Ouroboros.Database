namespace ByteTerrace.Ouroboros.Database;

/// <summary>
/// Exposes operations for constructing instances of the <see cref="DbClient"/> class.
/// </summary>
public interface IDbClientFactory<TClient> where TClient : DbClient
{
    /// <summary>
    /// Initializes a new instance of the database client from the specified name.
    /// </summary>
    /// <param name="name">The name of the database client.</param>
    IDbClient NewClient(string name);
}
