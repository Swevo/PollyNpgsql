// <copyright file="ResilientNpgsqlConnection.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql;

/// <summary>
/// A decorator around <see cref="NpgsqlConnection"/> that executes every query and command
/// inside a Polly v8 <see cref="ResiliencePipeline"/>. Create one via
/// <see cref="PollyNpgsqlExtensions.WithPolly(NpgsqlConnection, ResiliencePipeline)"/>.
/// </summary>
public sealed class ResilientNpgsqlConnection(NpgsqlConnection inner, ResiliencePipeline pipeline)
{
    /// <summary>Gets the underlying <see cref="NpgsqlConnection"/>.</summary>
    public NpgsqlConnection InnerConnection => inner;

    /// <summary>
    /// Opens the connection, retrying on transient failures.
    /// </summary>
    public Task OpenAsync(CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask(inner.OpenAsync(ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Executes a non-query command and returns the number of rows affected.
    /// </summary>
    public Task<int> ExecuteAsync(
        string sql,
        NpgsqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(async ct =>
        {
            await using var cmd = BuildCommand(sql, parameters);
            return await cmd.ExecuteNonQueryAsync(ct);
        }, cancellationToken).AsTask();

    /// <summary>
    /// Executes a command and returns the first column of the first row, or <c>default</c>.
    /// </summary>
    public Task<T?> ExecuteScalarAsync<T>(
        string sql,
        NpgsqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(async ct =>
        {
            await using var cmd = BuildCommand(sql, parameters);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is DBNull or null ? default : (T?)result;
        }, cancellationToken).AsTask();

    /// <summary>
    /// Executes a query and maps each row to <typeparamref name="T"/> using <paramref name="mapper"/>.
    /// </summary>
    public Task<List<T>> QueryAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> mapper,
        NpgsqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(async ct =>
        {
            await using var cmd = BuildCommand(sql, parameters);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var results = new List<T>();
            while (await reader.ReadAsync(ct))
                results.Add(mapper(reader));
            return results;
        }, cancellationToken).AsTask();

    /// <summary>
    /// Executes a query and returns the first mapped row, or <c>default</c> if no rows are found.
    /// </summary>
    public Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> mapper,
        NpgsqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(async ct =>
        {
            await using var cmd = BuildCommand(sql, parameters);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? mapper(reader) : default;
        }, cancellationToken).AsTask();

    private NpgsqlCommand BuildCommand(string sql, NpgsqlParameter[]? parameters)
    {
        var cmd = inner.CreateCommand();
        cmd.CommandText = sql;
        if (parameters is { Length: > 0 })
            cmd.Parameters.AddRange(parameters);
        return cmd;
    }
}
