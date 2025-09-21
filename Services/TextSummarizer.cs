using System.Text;
using System.Text.Json;

namespace NewsSummarizer.Api.Services;

public class BatchRequestWriter
{
    // Builds OpenAI Batch JSONL body for chat completions
    // Spec: each line => { custom_id, method, url, body } targeting /v1/chat/completions
    public string BuildJsonl(IEnumerable<(string Id, string Title, string Content)> items, string model)
    {
        var sb = new StringBuilder();
        foreach (var it in items)
        {
            var payload = new
            {
                custom_id = it.Id,
                method = "POST",
                url = "/v1/chat/completions",
                body = new
                {
                    model = model,
                    temperature = 0.2,
                    messages = new object[]
                    {
                        new { role = "user", content = Prompt(it.Title, it.Content) }
                    }
                }
            };
            sb.AppendLine(JsonSerializer.Serialize(payload));
        }
        return sb.ToString();
    }

    private static string Prompt(string title, string content) => $@"
Summarize the following news article as strict JSON with keys:
- title (string)
- bullet_points (array, 3-5 terse bullets)
- sentiment (""positive""|""neutral""|""negative"")
- key_entities (array of strings: people/orgs/tickers)

TITLE:
{title}

CONTENT:
{Truncate(content, 8000)}
";

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];
}