namespace NewsSummarizer.Api.Settings;

public record OpenAISettings(
    string ApiKey,
    string ModelBatch,           // e.g., "gpt-5-nano-2025-08-07"
    string EndpointBase          // "https://api.openai.com/v1"
);