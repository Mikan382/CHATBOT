namespace BusinessLayer.AI;

public record GeminiGenerationResult(
    string Text,
    long InputTokens,
    long OutputTokens,
    long TotalTokens);
