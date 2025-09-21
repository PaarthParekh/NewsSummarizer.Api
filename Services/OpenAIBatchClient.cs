using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NewsSummarizer.Api.Settings;

namespace NewsSummarizer.Api.Services;

public class OpenAIBatchClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OpenAISettings _s;

    public OpenAIBatchClient(IHttpClientFactory httpFactory, OpenAISettings s)
    {
        _httpFactory = httpFactory; _s = s;
    }

    HttpClient NewClient()
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(_s.EndpointBase);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _s.ApiKey);
        return http;
    }

    public async Task<string> UploadJsonlAsync(string jsonl, CancellationToken ct = default)
    {
        var http = NewClient();
        using var content = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes(jsonl);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/jsonl");
        content.Add(fileContent, "file", "batch.jsonl");
        content.Add(new StringContent("batch"), "purpose");

        using var resp = await http.PostAsync("/v1/files", content, ct);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("id").GetString()!;
    }

    public async Task<string> CreateBatchAsync(string inputFileId, CancellationToken ct = default)
    {
        var http = NewClient();
        var body = new
        {
            input_file_id = inputFileId,
            endpoint = "/v1/chat/completions",
            completion_window = "24h" // standard batch window
        };
        using var resp = await http.PostAsJsonAsync("/v1/batches", body, ct);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("id").GetString()!;
    }

    public async Task<JsonElement> GetBatchAsync(string batchId, CancellationToken ct = default)
    {
        var http = NewClient();
        using var resp = await http.GetAsync($"/v1/batches/{batchId}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    public async Task<string?> GetOutputFileIdAsync(string batchId, CancellationToken ct = default)
    {
        var doc = await GetBatchAsync(batchId, ct);
        if (doc.TryGetProperty("output_file_id", out var of))
            return of.GetString();
        return null;
    }

    public async Task<string> DownloadFileAsync(string fileId, CancellationToken ct = default)
    {
        var http = NewClient();
        using var resp = await http.GetAsync($"/v1/files/{fileId}/content", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct); // returns JSONL
    }
}
