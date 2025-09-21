namespace NewsSummarizer.Api.Services;

public interface ITextSummarizer
{
    Task<string> SummarizeAsync(string title, string content, CancellationToken ct = default);
}