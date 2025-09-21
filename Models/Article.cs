namespace NewsSummarizer.Api.Models;

public record Article(
    long Id,
    string Url,
    string? Title,
    string? Summary,
    byte[]? Embedding,
    DateTime CreatedAt
);