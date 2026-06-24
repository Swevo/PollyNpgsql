// <copyright file="PollyNpgsqlServiceCollectionExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql;

/// <summary>
/// Extension methods for registering a shared <see cref="ResiliencePipeline"/> for use
/// with Npgsql in the Microsoft dependency-injection container.
/// </summary>
public static class PollyNpgsqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ResiliencePipeline"/> singleton built from <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddPollyNpgsql(
        this IServiceCollection services,
        Action<ResiliencePipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        return services.AddPollyNpgsql(builder.Build());
    }

    /// <summary>
    /// Registers a pre-built <see cref="ResiliencePipeline"/> singleton.
    /// </summary>
    public static IServiceCollection AddPollyNpgsql(
        this IServiceCollection services,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pipeline);

        services.AddSingleton(pipeline);
        return services;
    }
}
