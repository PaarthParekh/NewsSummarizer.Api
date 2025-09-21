using System.Data;
using System.Text.Json;

namespace NewsSummarizer.Api.Services;

public class BatchResultImporter
{
    private readonly IDbConnection _db;

    public BatchResultImporter(IDbConnection db) { _db = db; }

    public async Task<int> ImportAsync(string jsonl, CancellationToken ct = default)
    {
        using var reader = new StringReader(jsonl);
        _db.Open();
        int count = 0;
        for (string? line = await reader.ReadLineAsync(); line != null; line = await reader.ReadLineAsync())
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var root = JsonDocument.Parse(line).RootElement;

            var id = root.GetProperty("custom_id").GetString()!;
            var msg = root.GetProperty("response").GetProperty("body")
                .GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;

            // msg is the model's JSON string; store as-is (or parse if you want columns)
            using var cmd = _db.CreateCommand();
            cmd.CommandText = @"INSERT INTO articles (url, title, summary) VALUES (@url, @title, @summary)";

            // You might encode url/title into custom_id (e.g., "url|title") when creating the batch
            var (url, title) = SplitId(id);
            AddParam(cmd, "@url", url);
            AddParam(cmd, "@title", title);
            AddParam(cmd, "@summary", msg);
            cmd.ExecuteNonQuery();
            count++;
        }
        return count;
    }

    private static (string url, string title) SplitId(string customId)
    {
        var parts = customId.Split('|', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (customId, customId);
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; cmd.Parameters.Add(p);
    }
}
