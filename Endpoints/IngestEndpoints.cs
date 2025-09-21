using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NewsSummarizer.Api.Services;
using NewsSummarizer.Api.Settings;
using SmartReader;

namespace NewsSummarizer.Api.Endpoints;

public static class BatchEndpoints
{
    public static void Map(WebApplication app)
    {
        // 1) Create a batch job from a list of URLs (fetch + extract now, summarize later in batch)
        app.MapPost("/batch/create", async (
            BatchCreateRequest req,
            IHttpClientFactory httpFactory,
            BatchRequestWriter writer,
            OpenAIBatchClient client,
            OpenAISettings oai,
            CancellationToken ct) =>
        {
            if (req?.Urls == null || req.Urls.Count == 0)
                return Results.BadRequest(new { error = "urls required" });

            var http = httpFactory.CreateClient();
            var items = new List<(string Id, string Title, string Content)>();

            foreach (var url in req.Urls)
            {
                var html = await http.GetStringAsync(url, ct);
                var article = await new Reader(url, html).GetArticleAsync();
                var title = article?.Title ?? url;
                var text = article?.TextContent ?? html;
                // custom_id contains url|title so we can re-attach on import
                items.Add(($"{url}|{title}", title, text));
            }

            var jsonl = writer.BuildJsonl(items, oai.ModelBatch);
            var fileId = await client.UploadJsonlAsync(jsonl, ct);
            var batchId = await client.CreateBatchAsync(fileId, ct);

            return Results.Ok(new { batchId, fileId });
        })
        .WithOpenApi();

        // 2) Check status
        app.MapGet("/batch/{batchId}/status", async (string batchId, OpenAIBatchClient client, CancellationToken ct) =>
        {
            var doc = await client.GetBatchAsync(batchId, ct);
            return Results.Ok(doc.ToString());
        })
        .WithOpenApi();

        // 3) Download results and import to DB
        app.MapPost("/batch/{batchId}/results", async (
            string batchId,
            OpenAIBatchClient client,
            BatchResultImporter importer,
            CancellationToken ct) =>
        {
            var outFileId = await client.GetOutputFileIdAsync(batchId, ct);
            if (string.IsNullOrEmpty(outFileId))
                return Results.BadRequest(new { error = "Batch not finished or no output_file_id yet" });

            var jsonl = await client.DownloadFileAsync(outFileId!, ct);
            var rows = await importer.ImportAsync(jsonl, ct);
            return Results.Ok(new { imported = rows });
        })
        .WithOpenApi();
    }

    public record BatchCreateRequest(List<string> Urls);
}
