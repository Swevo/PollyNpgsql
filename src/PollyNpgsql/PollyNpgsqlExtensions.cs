// <copyright file="PollyNpgsqlExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql;

/// <summary>
/// Extension methods for wrapping an <see cref="NpgsqlConnection"/> with a Polly v8 resilience pipeline.
/// </summary>
public static class PollyNpgsqlExtensions
{
    /// <summary>
    /// Wraps <paramref name="connection"/> in a <see cref="ResilientNpgsqlConnection"/> that
    /// executes every query and command inside the supplied <paramref name="pipeline"/>.
    /// </summary>
    public static ResilientNpgsqlConnection WithPolly(
        this NpgsqlConnection connection,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return new ResilientNpgsqlConnection(connection, pipeline);
    }
}
