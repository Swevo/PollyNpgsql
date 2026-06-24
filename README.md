# PollyNpgsql

[![NuGet](https://img.shields.io/nuget/v/PollyNpgsql.svg)](https://www.nuget.org/packages/PollyNpgsql)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyNpgsql.svg)](https://www.nuget.org/packages/PollyNpgsql)
[![CI](https://github.com/Swevo/PollyNpgsql/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyNpgsql/actions)

**Polly v8 resilience for Npgsql (PostgreSQL)** — retry, timeout, and circuit-breaker for `NpgsqlConnection` queries and commands, plus a built-in `PostgresTransientErrors` predicate covering all common PostgreSQL transient SQLSTATE codes. Zero changes to your existing SQL.

```csharp
// One line of setup
var resilient = connection.WithPolly(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            ShouldHandle = PostgresTransientErrors.IsTransient, // built-in ✔
        })
        .AddTimeout(TimeSpan.FromSeconds(10)));

// Use exactly like NpgsqlConnection
var orders = await resilient.QueryAsync(
    "SELECT id, total FROM orders WHERE customer_id = $1",
    r => new Order(r.GetInt32(0), r.GetDecimal(1)),
    parameters: [new NpgsqlParameter { Value = customerId }]);
```

---

## Installation

```bash
dotnet add package PollyNpgsql
```

Targets **net6.0**, **net8.0**, and **net9.0**.
Dependencies: `Polly.Core 8.*`, `Npgsql 9.*`, `Microsoft.Extensions.DependencyInjection.Abstractions 8.*`

---

## PostgresTransientErrors — the key feature

Knowing *which* PostgreSQL errors are safe to retry is the hard part. PollyNpgsql ships a pre-built `PostgresTransientErrors.IsTransient` predicate so you don't have to look up SQLSTATE codes.

```csharp
new RetryStrategyOptions
{
    MaxRetryAttempts = 3,
    ShouldHandle = PostgresTransientErrors.IsTransient,
}
```

| SQLSTATE | Name | When it occurs |
|----------|------|----------------|
| `40001` | `serialization_failure` | Concurrent transaction conflict |
| `40P01` | `deadlock_detected` | Two transactions blocking each other |
| `53300` | `too_many_connections` | Connection pool exhausted |
| `53400` | `configuration_limit_exceeded` | Resource limit hit |
| `57P03` | `cannot_connect_now` | Server starting up or shutting down |
| `08000` | `connection_exception` | General connection error |
| `08006` | `connection_failure` | Connection dropped mid-query |
| `08001` | `sqlclient_unable_to_establish_sqlconnection` | Cannot reach server |
| `08004` | `sqlserver_rejected_establishment_of_sqlconnection` | Server rejected connection |

The raw set is also available if you need to extend it:
```csharp
// Add custom codes
var myStates = PostgresTransientErrors.SqlStates.ToHashSet();
myStates.Add("57014"); // query_canceled (e.g. statement_timeout)

new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(ex =>
        ex is PostgresException pg && myStates.Contains(pg.SqlState))
}
```

---

## Quick start

### Inline pipeline

```csharp
using PollyNpgsql;

await using var connection = new NpgsqlConnection(connectionString);
var resilient = connection.WithPolly(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = PostgresTransientErrors.IsTransient,
        })
        .AddTimeout(TimeSpan.FromSeconds(30)));

await resilient.OpenAsync();

// Execute non-query
await resilient.ExecuteAsync(
    "INSERT INTO events (type, payload) VALUES ($1, $2)",
    parameters: [new("type") { Value = "OrderPlaced" }, new("payload") { Value = json }]);

// Query with mapper
var orders = await resilient.QueryAsync(
    "SELECT id, total FROM orders WHERE customer_id = $1",
    reader => new Order(reader.GetInt32(0), reader.GetDecimal(1)),
    parameters: [new NpgsqlParameter { Value = customerId }]);

// Scalar
var count = await resilient.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM orders");
```

### Dependency injection

```csharp
// Program.cs
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

builder.Services.AddPollyNpgsql(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = PostgresTransientErrors.IsTransient,
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
        }));

// Repository
public class OrderRepository(NpgsqlConnection db, ResiliencePipeline pipeline)
{
    public async Task<List<Order>> GetByCustomerAsync(int customerId)
    {
        var resilient = db.WithPolly(pipeline);
        await resilient.OpenAsync();
        return await resilient.QueryAsync(
            "SELECT id, total FROM orders WHERE customer_id = $1",
            r => new Order(r.GetInt32(0), r.GetDecimal(1)),
            parameters: [new NpgsqlParameter { Value = customerId }]);
    }
}
```

---

## Supported operations

| Method | Description |
|--------|-------------|
| `OpenAsync` | Open the connection with retry |
| `ExecuteAsync` | Execute non-query, returns rows affected |
| `ExecuteScalarAsync<T>` | Execute scalar, returns first column of first row |
| `QueryAsync<T>` | Query with row mapper, returns `List<T>` |
| `QueryFirstOrDefaultAsync<T>` | Query with row mapper, returns first row or `default` |

---

## Pipeline order

```
[Timeout] → [Retry] → [Circuit Breaker] → [Npgsql]
```

```csharp
pipeline
    .AddTimeout(TimeSpan.FromSeconds(30))   // 1. Overall deadline
    .AddRetry(retryOptions)                 // 2. Retry transient failures
    .AddCircuitBreaker(cbOptions)           // 3. Open circuit under load
```

---

## Related packages

| Package | Downloads | Description |
|---|---|---|
| [PollyDapper](https://www.nuget.org/packages/PollyDapper) | [![Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper) | Polly v8 resilience for Dapper (works with any IDbConnection including Npgsql) |
| [PollySqlClient](https://www.nuget.org/packages/PollySqlClient) | [![Downloads](https://img.shields.io/nuget/dt/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient) | Polly v8 resilience for SQL Server and Azure SQL with SqlServerTransientErrors predicate |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollyMongo](https://www.nuget.org/packages/PollyMongo) | [![Downloads](https://img.shields.io/nuget/dt/PollyMongo.svg)](https://www.nuget.org/packages/PollyMongo) | Polly v8 resilience for MongoDB.Driver |
| [PollyAzureBlob](https://www.nuget.org/packages/PollyAzureBlob) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob) | Polly v8 resilience for Azure Blob Storage |
| [PollyCosmosDb](https://www.nuget.org/packages/PollyCosmosDb) | [![Downloads](https://img.shields.io/nuget/dt/PollyCosmosDb.svg)](https://www.nuget.org/packages/PollyCosmosDb) | Polly v8 resilience for Azure Cosmos DB with CosmosTransientErrors predicate |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience pipelines for MediatR |
| [PollyAzureEventHub](https://github.com/Swevo/PollyAzureEventHub) | Polly v8 for Azure Event Hubs |
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureServiceBus.svg)](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience for Azure Service Bus |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyElasticsearch](https://github.com/Swevo/PollyElasticsearch) | Polly v8 for Elastic.Clients.Elasticsearch |
| [PollyAzureKeyVault](https://github.com/Swevo/PollyAzureKeyVault) | Polly v8 for Azure Key Vault |
| [PollySendGrid](https://github.com/Swevo/PollySendGrid) | Polly v8 for SendGrid |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Jitter, linear & custom backoff for Polly v8 retry |

---

## License

MIT
