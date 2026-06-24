// <copyright file="PollyNpgsqlExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql.Tests;

public class PollyNpgsqlExtensionsTests
{
    private readonly ResiliencePipeline _pipeline = ResiliencePipeline.Empty;

    [Fact]
    public void WithPolly_NullConnection_ThrowsArgumentNullException()
    {
        NpgsqlConnection? connection = null;
        Assert.Throws<ArgumentNullException>(() => connection!.WithPolly(_pipeline));
    }

    [Fact]
    public void WithPolly_NullPipeline_ThrowsArgumentNullException()
    {
        using var connection = new NpgsqlConnection("Host=localhost");
        ResiliencePipeline? pipeline = null;
        Assert.Throws<ArgumentNullException>(() => connection.WithPolly(pipeline!));
    }

    [Fact]
    public void WithPolly_ValidArguments_ReturnsResilientNpgsqlConnection()
    {
        using var connection = new NpgsqlConnection("Host=localhost");
        var result = connection.WithPolly(_pipeline);
        Assert.NotNull(result);
        Assert.IsType<ResilientNpgsqlConnection>(result);
    }

    [Fact]
    public void WithPolly_ExposesInnerConnection()
    {
        using var connection = new NpgsqlConnection("Host=localhost");
        var result = connection.WithPolly(_pipeline);
        Assert.Same(connection, result.InnerConnection);
    }
}
