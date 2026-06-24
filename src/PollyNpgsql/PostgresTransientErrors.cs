// <copyright file="PostgresTransientErrors.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql;

/// <summary>
/// Pre-built <see cref="PredicateBuilder"/> covering the PostgreSQL SQLSTATE codes that
/// represent transient conditions safe to retry.
/// </summary>
/// <remarks>
/// Pass <see cref="IsTransient"/> directly to <see cref="RetryStrategyOptions.ShouldHandle"/>:
/// <code>
/// new RetryStrategyOptions { ShouldHandle = PostgresTransientErrors.IsTransient }
/// </code>
/// </remarks>
public static class PostgresTransientErrors
{
    /// <summary>
    /// PostgreSQL SQLSTATE codes that represent transient errors safe to retry.
    /// </summary>
    /// <list type="bullet">
    ///   <item><c>40001</c> — serialization_failure</item>
    ///   <item><c>40P01</c> — deadlock_detected</item>
    ///   <item><c>53300</c> — too_many_connections</item>
    ///   <item><c>53400</c> — configuration_limit_exceeded</item>
    ///   <item><c>57P03</c> — cannot_connect_now (startup/shutdown)</item>
    ///   <item><c>08000</c> — connection_exception</item>
    ///   <item><c>08006</c> — connection_failure</item>
    ///   <item><c>08001</c> — sqlclient_unable_to_establish_sqlconnection</item>
    ///   <item><c>08004</c> — sqlserver_rejected_establishment_of_sqlconnection</item>
    /// </list>
    public static readonly IReadOnlySet<string> SqlStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "40001", // serialization_failure
        "40P01", // deadlock_detected
        "53300", // too_many_connections
        "53400", // configuration_limit_exceeded
        "57P03", // cannot_connect_now
        "08000", // connection_exception
        "08006", // connection_failure
        "08001", // sqlclient_unable_to_establish_sqlconnection
        "08004", // sqlserver_rejected_establishment_of_sqlconnection
    };

    /// <summary>
    /// A <see cref="PredicateBuilder"/> that matches any <see cref="NpgsqlException"/> whose
    /// <see cref="PostgresException.SqlState"/> is a known transient PostgreSQL error code.
    /// </summary>
    public static PredicateBuilder IsTransient =>
        (PredicateBuilder)new PredicateBuilder().Handle<NpgsqlException>(ex =>
            ex is PostgresException pg && SqlStates.Contains(pg.SqlState));
}
