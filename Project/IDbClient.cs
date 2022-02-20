using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database;

/// <summary>
/// Exposes low-level database operations.
/// </summary>
public interface IDbClient : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets a <see cref="DbProviderFactory"/> using the "Instance" field of the specified factory type.
    /// </summary>
    /// <param name="typeName">The assembly qualified type name of the <see cref="DbProviderFactory"/> that will be retrieved.</param>
    public static DbProviderFactory? GetProviderFactory(string typeName) {
        const string DefaultFactoryFieldName = "Instance";

        var type = Type.GetType(typeName: typeName);

        if (type is null) {
            ThrowHelper.ThrowArgumentException(
                message: $"The specified assembly qualified name \"{typeName}\" could not be found within the collection of loaded assemblies.",
                name: nameof(typeName)
            );
        }

        var field = type.GetField(
            bindingAttr: (BindingFlags.Public | BindingFlags.Static),
            name: DefaultFactoryFieldName
        );

        if (field is null) {
            ThrowHelper.ThrowMissingFieldException(
                className: type.AssemblyQualifiedName,
                fieldName: DefaultFactoryFieldName
            );
        }

        return ((DbProviderFactory?)field.GetValue(obj: default));
    }

    /// <summary>
    /// The default level that will be used during log operations.
    /// </summary>
    protected const LogLevel DefaultLogLevel = LogLevel.Trace;

    private static DbResult CreateResult(IDbCommand command, int resultCode) {
        var outputParameters = new List<DbParameter>();

        foreach (IDbDataParameter parameter in command.Parameters) {
            var parameterDirection = parameter.Direction;

            if (parameterDirection is (ParameterDirection.InputOutput or ParameterDirection.Output)) {
                outputParameters.Add(item: DbParameter.New(dbDataParameter: parameter));
            }

            if (ParameterDirection.ReturnValue == parameterDirection) {
                resultCode = ((int)parameter.Value!);
            }
        }

        return DbResult.New(
            parameters: outputParameters,
            resultCode: resultCode
        );
    }

    /// <summary>
    /// Gets the default database command builder.
    /// </summary>
    DbCommandBuilder CommandBuilder { get; init; }
    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    DbConnection Connection { get; init; }
    /// <summary>
    /// Gets the logger that is associated with this database client.
    /// </summary>
    ILogger Logger { get; init; }
    /// <summary>
    /// Gets the underlying database provider factory.
    /// </summary>
    DbProviderFactory ProviderFactory { get; init; }

    /// <summary>
    /// Indicates whether the underlying resources of this database client have been disposed.
    /// </summary>
    public bool IsDisposed { get; }
    /// <summary>
    /// Indicates whether this client owns the underlying database connection.
    /// </summary>
    public bool OwnsConnection { get; init; }

    private bool ConnectionIsBrokenOrClosed() {
        if (IsDisposed) {
            ThrowHelper.ThrowObjectDisposedException(
                message: "Unable to make connection attempt with a client that has already been disposed.",
                objectName: nameof(IDbClient)
            );
        }

        var connectionState = Connection.State;

        return (connectionState is (ConnectionState.Closed or ConnectionState.Broken));
    }
    private DbFullyQualifiedIdentifier CreateIdentifier(
        string schemaName,
        string objectName
    ) =>
        DbFullyQualifiedIdentifier.New(
            commandBuilder: CommandBuilder,
            objectName: objectName,
            schemaName: schemaName
        );
    private DbCommand CreateSelectWildcardFromCommand(
        string schemaName,
        string objectName
    ) {
        var objectIdentifier = CreateIdentifier(
            objectName: objectName,
            schemaName: schemaName
        );

        return DbCommand.New(
            text: $"select * from {objectIdentifier};",
            type: CommandType.Text
        );
    }
    private DbStoredProcedureCall CreateStoredProcedureCall(
        string schemaName,
        string name,
        params DbParameter[] parameters
    ) =>
        DbStoredProcedureCall.New(
            name: CreateIdentifier(
                    objectName: name,
                    schemaName: schemaName
                )
                .ToString(),
            parameters: parameters
        );
    private void LogBeginTransaction(IsolationLevel isolationLevel) {
        if (Logger.IsEnabled(DefaultLogLevel)) {
            DbClientLogging.BeginTransaction(
                isolationLevel: isolationLevel,
                logger: Logger,
                logLevel: DefaultLogLevel
            );
        }
    }
    private void LogExecute(DbCommand command) {
        if (Logger.IsEnabled(DefaultLogLevel)) {
            DbClientLogging.Execute(
                logger: Logger,
                logLevel: DefaultLogLevel,
                text: command.Text,
                timeout: command.Timeout,
                type: command.Type
            );
        }
    }
    private void LogOpenConnection() {
        if (Logger.IsEnabled(DefaultLogLevel)) {
            var connectionStringBuilder = ProviderFactory.CreateConnectionStringBuilder();

            if (connectionStringBuilder is not null) {
                connectionStringBuilder.ConnectionString = Connection.ConnectionString;
                connectionStringBuilder.Add("Persist Security Info", false);

                _ = connectionStringBuilder.Remove("Password");
                _ = connectionStringBuilder.Remove("PWD");
                _ = connectionStringBuilder.Remove("UID");
                _ = connectionStringBuilder.Remove("User ID");
            }

            DbClientLogging.OpenConnection(
                connectionString: (connectionStringBuilder?.ConnectionString ?? "(null)"),
                logger: Logger,
                logLevel: DefaultLogLevel
            );
        }
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
    public DbTransaction BeginTransaction(IsolationLevel isolationLevel) {
        OpenConnection();
        LogBeginTransaction(isolationLevel: isolationLevel);

        return Connection.BeginTransaction(isolationLevel: isolationLevel);
    }
    /// <summary>
    /// Begins a database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
    public async ValueTask<DbTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default
    ) {
        await OpenConnectionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        LogBeginTransaction(isolationLevel: isolationLevel);

        return await Connection
            .BeginTransactionAsync(
                cancellationToken: cancellationToken,
                isolationLevel: isolationLevel
            )
            .ConfigureAwait(continueOnCapturedContext: false);
    }
    /// <summary>
    /// Enumerates each result set in the specified data reader.
    /// </summary>
    /// <param name="reader">The data reader that will be enumerated.</param>
    public IEnumerable<DbResultSet> EnumerateResultSets(DbDataReader reader) {
        do {
            yield return DbResultSet.New(reader: reader);
        } while (reader.NextResult());
    }
    /// <summary>
    /// Enumerates each result set in the specified data reader asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="reader">The data reader that will be enumerated.</param>
    public async IAsyncEnumerable<DbResultSet> EnumerateResultSetsAsync(
        DbDataReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        do {
            yield return DbResultSet.New(reader: reader);
        } while (await reader
            .NextResultAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false)
        );
    }
    /// <summary>
    /// Creates a new database reader from the specified table or view name and then enumerates each row.
    /// </summary>
    /// <param name="name">The name of the table or view.</param>
    /// <param name="schemaName">The name of the schema.</param>
    public IEnumerable<DbRow> EnumerateTableOrView(
        string schemaName,
        string name
    ) {
        using var reader = ExecuteReader(
                behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult),
                command: CreateSelectWildcardFromCommand(
                    objectName: name,
                    schemaName: schemaName
                )
            );
        using var enumerator = EnumerateResultSets(reader: reader)
            .GetEnumerator();

        if (enumerator.MoveNext()) {
            foreach (var row in enumerator.Current) {
                yield return row;
            }
        }
    }
    /// <summary>
    /// Creates a new database reader from the specified table or view name and then enumerates each row asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="name">The name of the table or view.</param>
    /// <param name="schemaName">The name of the schema.</param>
    public async IAsyncEnumerable<DbRow> EnumerateTableOrViewAsync(
        string schemaName,
        string name,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        using var dataReader = await ExecuteReaderAsync(
                behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult),
                cancellationToken: cancellationToken,
                command: CreateSelectWildcardFromCommand(
                    objectName: name,
                    schemaName: schemaName
                )
            )
            .ConfigureAwait(continueOnCapturedContext: false);
        var enumerator = EnumerateResultSetsAsync(
                cancellationToken: cancellationToken,
                reader: dataReader
            )
            .GetAsyncEnumerator(cancellationToken: cancellationToken);

        if (await enumerator
            .MoveNextAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false)
        ) {
            foreach (var row in enumerator.Current) {
                yield return row;
            }
        }
    }
    /// <summary>
    /// Executes a database command and returns a result.
    /// </summary>
    /// <param name="command">The database command that will be executed.</param>
    public DbResult Execute(DbCommand command) {
        OpenConnection();
        LogExecute(command: command);

        using var dbcommand = command.ToDbCommand(connection: Connection);

        return CreateResult(
            command: dbcommand,
            resultCode: dbcommand.ExecuteNonQuery()
        );
    }
    /// <summary>
    /// Executes a database command asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="command">The database command that will be executed.</param>
    public async ValueTask<DbResult> ExecuteAsync(
        DbCommand command,
        CancellationToken cancellationToken = default
    ) {
        await OpenConnectionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        LogExecute(command: command);

        using var dbCommand = command.ToDbCommand(connection: Connection);

        return CreateResult(
            command: dbCommand,
            resultCode: await dbCommand
                .ExecuteNonQueryAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
        );
    }
    /// <summary>
    /// Executes a database command and returns a data reader.
    /// </summary>
    /// <param name="behavior">Specifies how the data reader will behave.</param>
    /// <param name="command">The database command that will be executed.</param>
    public DbDataReader ExecuteReader(
        DbCommand command,
        CommandBehavior behavior
    ) {
        OpenConnection();
        LogExecute(command: command);

        using var dbCommand = command.ToDbCommand(connection: Connection);

        return dbCommand.ExecuteReader(behavior: behavior);
    }
    /// <summary>
    /// Executes a database command and returns a data reader.
    /// </summary>
    /// <param name="behavior">Specifies how the data reader will behave.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="command">The database command that will be executed.</param>
    public async ValueTask<DbDataReader> ExecuteReaderAsync(
        DbCommand command,
        CommandBehavior behavior,
        CancellationToken cancellationToken = default
    ) {
        await OpenConnectionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        LogExecute(command: command);

        using var dbCommand = command.ToDbCommand(connection: Connection);

        return await dbCommand
            .ExecuteReaderAsync(
                behavior: behavior,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(continueOnCapturedContext: false);
    }
    /// <summary>
    /// Executes a stored procedure and returns a result.
    /// </summary>
    /// <param name="name">The name of the stored procedure.</param>
    /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
    /// <param name="schemaName">The name of the schema.</param>
    public DbResult ExecuteStoredProcedure(
        string schemaName,
        string name,
        params DbParameter[] parameters
    ) =>
        Execute(
            command: CreateStoredProcedureCall(
                name: name,
                parameters: parameters,
                schemaName: schemaName
            )
        );
    /// <summary>
    /// Executes a stored procedure and returns a result asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="name">The name of the stored procedure.</param>
    /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
    /// <param name="schemaName">The name of the schema.</param>
    public async ValueTask<DbResult> ExecuteStoredProcedureAsync(
        string schemaName,
        string name,
        DbParameter[] parameters,
        CancellationToken cancellationToken = default
    ) =>
        await ExecuteAsync(
            cancellationToken: cancellationToken,
            command: CreateStoredProcedureCall(
                name: name,
                parameters: parameters,
                schemaName: schemaName
            )
        )
        .ConfigureAwait(continueOnCapturedContext: false);
    /// <summary>
    /// Attempts to open the underlying connection.
    /// </summary>
    public void OpenConnection() {
        if (ConnectionIsBrokenOrClosed()) {
            LogOpenConnection();
            Connection.Open();
        }
    }
    /// <summary>
    /// Attempts to open the underlying connection asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask OpenConnectionAsync(CancellationToken cancellationToken = default) {
        if (ConnectionIsBrokenOrClosed()) {
            LogOpenConnection();
            await Connection
                .OpenAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
    /// <summary>
    /// Convert this instance to the specified type of <see cref="DbClient"/>.
    /// </summary>
    /// <typeparam name="TClient">The type of <see cref="DbClient"/> that this instance will be converted to.</typeparam>
    public TClient ToDbClient<TClient>() where TClient : DbClient =>
        ((TClient)this);
}
