using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database;

/// <summary>
/// Represents a set of rows from a database query along with the metadata about the query that returned them.
/// </summary>
/// <param name="FieldMetadata">The metadata of the fields that are returned by the result set.</param>
/// <param name="FieldNameToOrdinalMap">A dictionary that takes a field name to its ordinal position.</param>
/// <param name="Reader">The data reader that generates the result set.</param>
public readonly record struct DbResultSet(
    IReadOnlyList<DbFieldMetadata> FieldMetadata,
    IReadOnlyDictionary<string, int> FieldNameToOrdinalMap,
    DbDataReader Reader
) : IAsyncEnumerable<DbRow>, IEnumerable<DbRow>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbResultSet"/> struct.
    /// </summary>
    /// <param name="reader">The data reader that will be enumerated.</param>
    public static DbResultSet New(DbDataReader reader) {
        var fieldCount = reader.FieldCount;
        var fieldMetadata = new DbFieldMetadata[fieldCount];
        var fieldNameCounters = new Dictionary<string, int>(
            capacity: fieldCount,
            comparer: StringComparer.OrdinalIgnoreCase
        );
        var fieldNameToOrdinalMap = new Dictionary<string, int>(
            capacity: fieldCount,
            comparer: StringComparer.OrdinalIgnoreCase
        );

        for (var i = 0; (i < fieldCount); ++i) {
            var fieldName = reader.GetName(ordinal: i);

            fieldMetadata[i] = DbFieldMetadata.New(
                clrType: reader.GetFieldType(ordinal: i),
                dbType: reader.GetDataTypeName(ordinal: i),
                name: fieldName,
                ordinal: i
            );

            if (fieldNameCounters.TryAdd(
                key: fieldName,
                value: 0
            )) {
                fieldNameToOrdinalMap[key: fieldName] = i;
            }
            else {
                var fieldNameCount = fieldNameCounters[key: fieldName] += 1;

                fieldNameToOrdinalMap[key: $"{fieldName}_{fieldNameCount}"] = i;
            }
        }

        return new(
            FieldMetadata: fieldMetadata,
            FieldNameToOrdinalMap: new ReadOnlyDictionary<string, int>(
                dictionary: fieldNameToOrdinalMap
            ),
            Reader: reader
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DbRow GetValues(int fieldCount) {
        var fieldValues = new object[fieldCount];

        _ = Reader.GetValues(values: fieldValues);

        return DbRow.New(
            fieldNameToOrdinalMap: FieldNameToOrdinalMap,
            fieldValues: Array.AsReadOnly(array: fieldValues)
        );
    }

    /// <summary>
    /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/> asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async IAsyncEnumerator<DbRow> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
        var fieldCount = FieldMetadata.Count;

        while (
            await Reader
                .ReadAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
        ) {
            yield return GetValues(fieldCount: fieldCount);
        }
    }
    /// <summary>
    /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
    /// </summary>
    public IEnumerator<DbRow> GetEnumerator() {
        var fieldCount = FieldMetadata.Count;

        while (Reader.Read()) {
            yield return GetValues(fieldCount: fieldCount);
        }
    }
    /// <summary>
    /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
