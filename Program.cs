using System.Data.Common;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;
using NewsSummarizer.Api.Services;
using NewsSummarizer.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// --- Read config
var kafkaCfg = builder.Configuration.GetSection("Kafka");
var mySqlConnStr = builder.Configuration.GetSection("MySql")["ConnectionString"]!;
var sumCfg = builder.Configuration.GetSection("Summarization");
var oai = builder.Configuration.GetSection("OpenAI");
var openAISettings = new OpenAISettings(
    ApiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? oai["ApiKey"] ?? "",
    ModelBatch: oai["ModelBatch"] ?? "gpt-5-nano-2025-08-07",
    EndpointBase: oai["EndpointBase"] ?? "https://api.openai.com/v1"
);
builder.Services.AddSingleton(openAISettings);
builder.Services.AddSingleton(new KafkaSettings(
    kafkaCfg["BootstrapServers"]!, kafkaCfg["Topic"]!, kafkaCfg["GroupId"]!
));
builder.Services.AddScoped<BatchRequestWriter>();
builder.Services.AddScoped<OpenAIBatchClient>();
builder.Services.AddScoped<BatchResultImporter>();
// --- Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "NewsSummarizer API", Version = "v1" });
});
builder.Services.AddHttpClient();
// MySQL connection factory (DbConnection so we get async APIs)
builder.Services.AddScoped<DbConnection>(_ => new MySqlConnection(mySqlConnStr));

// Ensure table exists on startup
builder.Services.AddHostedService<DbInitializer>();

// Kafka consumer background service
builder.Services.AddHostedService<NewsConsumer>();

// Kafka producer (singleton)
builder.Services.AddSingleton<IProducer<Null, string>>(_ =>
{
    var cfg = new ProducerConfig
    {
        BootstrapServers = kafkaCfg["BootstrapServers"],
        EnableIdempotence = true
    };
    return new ProducerBuilder<Null, string>(cfg).Build();
});



var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

NewsSummarizer.Api.Endpoints.BatchEndpoints.Map(app);

app.Run();

