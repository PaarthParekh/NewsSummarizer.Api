namespace NewsSummarizer.Api.Settings;

public record KafkaSettings(string BootstrapServers, string Topic, string GroupId);