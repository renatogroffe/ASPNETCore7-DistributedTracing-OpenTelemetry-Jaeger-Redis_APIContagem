using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using APIContagem.Tracing;

var builder = WebApplication.CreateBuilder(args);

using var connectionRedis = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddSingleton(connectionRedis);

// Documentacao do OpenTelemetry:
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Integracao do OpenTelemetry com Jaeger:
// https://opentelemetry.io/docs/instrumentation/net/exporters/

// Documentacaoo do Jaeger:
// https://www.jaegertracing.io/docs/1.49/

builder.Services.AddOpenTelemetry().WithTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddAspNetCoreInstrumentation()
        .AddRedisInstrumentation(connectionRedis,
            options => options.SetVerboseDatabaseStatements = true)
        .AddJaegerExporter(exporter =>
        {
            exporter.AgentHost = builder.Configuration["Jaeger:AgentHost"];
            exporter.AgentPort = Convert.ToInt32(builder.Configuration["Jaeger:AgentPort"]);
        })
        .AddConsoleExporter();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();