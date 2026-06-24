// <copyright file="PollyNpgsqlServiceCollectionExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql.Tests;

public class PollyNpgsqlServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPollyNpgsql_WithBuilder_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        services.AddPollyNpgsql(pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(5)));

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ResiliencePipeline>());
    }

    [Fact]
    public void AddPollyNpgsql_WithPrebuiltPipeline_RegistersSameInstance()
    {
        var services = new ServiceCollection();
        var prebuilt = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromSeconds(5)).Build();
        services.AddPollyNpgsql(prebuilt);

        var provider = services.BuildServiceProvider();
        Assert.Same(prebuilt, provider.GetService<ResiliencePipeline>());
    }

    [Fact]
    public void AddPollyNpgsql_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddPollyNpgsql(_ => { }));
    }

    [Fact]
    public void AddPollyNpgsql_NullConfigure_ThrowsArgumentNullException()
    {
        Action<ResiliencePipelineBuilder>? configure = null;
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddPollyNpgsql(configure!));
    }
}
