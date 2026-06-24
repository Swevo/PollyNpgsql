// <copyright file="PostgresTransientErrorsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyNpgsql.Tests;

public class PostgresTransientErrorsTests
{
    [Theory]
    [InlineData("40001")] // serialization_failure
    [InlineData("40P01")] // deadlock_detected
    [InlineData("53300")] // too_many_connections
    [InlineData("53400")] // configuration_limit_exceeded
    [InlineData("57P03")] // cannot_connect_now
    [InlineData("08000")] // connection_exception
    [InlineData("08006")] // connection_failure
    [InlineData("08001")] // sqlclient_unable_to_establish_sqlconnection
    [InlineData("08004")] // sqlserver_rejected_establishment_of_sqlconnection
    public void SqlStates_ContainsTransientCode(string sqlState)
    {
        Assert.Contains(sqlState, PostgresTransientErrors.SqlStates);
    }

    [Theory]
    [InlineData("23505")] // unique_violation — not transient
    [InlineData("23503")] // foreign_key_violation — not transient
    [InlineData("42601")] // syntax_error — not transient
    [InlineData("28000")] // invalid_authorization_specification — not transient
    public void SqlStates_DoesNotContainNonTransientCode(string sqlState)
    {
        Assert.DoesNotContain(sqlState, PostgresTransientErrors.SqlStates);
    }

    [Fact]
    public void IsTransient_ReturnsPredicateBuilder()
    {
        var predicate = PostgresTransientErrors.IsTransient;
        Assert.NotNull(predicate);
    }
}
