using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database;

/// <summary>
/// Represents a fully qualified database identifier.
/// </summary>
/// <param name="DatabaseName">The escaped name of the database.</param>
/// <param name="ObjectName">The escaped name of the object.</param>
/// <param name="SchemaName">The escaped name of the schema.</param>
/// <param name="ServerName">The escaped name of the server.</param>
public readonly record struct DbFullyQualifiedIdentifier(
    DbQuotedIdentifier DatabaseName,
    DbQuotedIdentifier ObjectName,
    DbQuotedIdentifier SchemaName,
    DbQuotedIdentifier ServerName
)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbFullyQualifiedIdentifier"/> struct.
    /// </summary>
    /// <param name="commandBuilder">The command builder that will be used to escape the specified names.</param>
    /// <param name="databaseName">The escaped name of the database.</param>
    /// <param name="objectName">The escaped name of the object.</param>
    /// <param name="schemaName">The escaped name of the schema.</param>
    /// <param name="serverName">The escaped name of the server.</param>
    public static DbFullyQualifiedIdentifier New(
        DbCommandBuilder commandBuilder,
        string databaseName,
        string objectName,
        string schemaName,
        string serverName
    ) => new(
        DatabaseName: (string.IsNullOrEmpty(databaseName) ? default : DbQuotedIdentifier.New(commandBuilder: commandBuilder, value: databaseName)),
        ObjectName: (string.IsNullOrEmpty(objectName) ? default : DbQuotedIdentifier.New(commandBuilder: commandBuilder, value: objectName)),
        SchemaName: (string.IsNullOrEmpty(schemaName) ? default : DbQuotedIdentifier.New(commandBuilder: commandBuilder, value: schemaName)),
        ServerName: (string.IsNullOrEmpty(serverName) ? default : DbQuotedIdentifier.New(commandBuilder: commandBuilder, value: serverName))
    );
    /// <summary>
    /// Initializes a new instance of the <see cref="DbFullyQualifiedIdentifier"/> struct.
    /// </summary>
    /// <param name="commandBuilder">The command builder that will be used to escape the specified names.</param>
    /// <param name="databaseName">The escaped name of the database.</param>
    /// <param name="objectName">The escaped name of the object.</param>
    /// <param name="schemaName">The escaped name of the schema.</param>
    public static DbFullyQualifiedIdentifier New(
        DbCommandBuilder commandBuilder,
        string databaseName,
        string schemaName,
        string objectName
    ) => New(
        commandBuilder: commandBuilder,
        databaseName: databaseName,
        objectName: objectName,
        schemaName: schemaName,
        serverName: string.Empty
    );
    /// <summary>
    /// Initializes a new instance of the <see cref="DbFullyQualifiedIdentifier"/> struct.
    /// </summary>
    /// <param name="commandBuilder">The command builder that will be used to escape the specified names.</param>
    /// <param name="objectName">The escaped name of the object.</param>
    /// <param name="schemaName">The escaped name of the schema.</param>
    public static DbFullyQualifiedIdentifier New(
        DbCommandBuilder commandBuilder,
        string schemaName,
        string objectName
    ) => New(
        commandBuilder: commandBuilder,
        databaseName: string.Empty,
        objectName: objectName,
        schemaName: schemaName,
        serverName: string.Empty
    );
    /// <summary>
    /// Returns the fully qualified database identifier as a string.
    /// </summary>
    public override string ToString() =>
        (
            (string.IsNullOrEmpty(ServerName.Value) ? string.Empty : $"{ServerName.Value}.")
          + (string.IsNullOrEmpty(DatabaseName.Value) ? string.Empty : $"{DatabaseName.Value}.")
          + (string.IsNullOrEmpty(SchemaName.Value) ? string.Empty : $"{SchemaName.Value}.")
          + (string.IsNullOrEmpty(ObjectName.Value) ? string.Empty : $"{ObjectName.Value}")
        );
}
