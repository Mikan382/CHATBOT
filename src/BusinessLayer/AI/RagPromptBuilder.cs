namespace BusinessLayer.AI;

public static class RagPromptBuilder
{
    public static string BuildSystemInstruction()
    {
        return """
        You are a study assistant for PRN222 - Advanced Cross-Platform Application Programming With .NET.
        Answer only from the provided document context.
        If the context does not contain enough information, clearly state that the current documents are insufficient for an accurate answer.
        Prefer precise .NET and ASP.NET Core terminology, keep the explanation concise, and cite source files when available.
        You can respond in either Vietnamese or English depending on the language of the student's question.
        """;
    }

    public static string BuildPrompt(string question, IReadOnlyList<RetrievedChunkDto> chunks, IReadOnlyList<FineTuneHistoryMessage> history)
    {
        var historyText = history.Count == 0
            ? "No previous conversation history."
            : string.Join("\n", history.TakeLast(8).Select(x => $"{x.Role}: {x.Content}"));

        var context = chunks.Count == 0
            ? "No relevant chunk was found in the indexed documents."
            : string.Join("\n\n", chunks.Select((chunk, index) =>
                $"[Source {index + 1}: {chunk.SourceName}, {chunk.ChapterTitle}, chunk #{chunk.ChunkIndex}]\n{chunk.Content}"));

        return $"""
        Recent conversation history:
        {historyText}

        Student question:
        {question}

        Document context:
        {context}

        Answer requirements:
        - Respond in the same language as the student's question. If the question is in Vietnamese, answer in Vietnamese. If in English, answer in English.
        - Use only the information in the document context.
        - If an answer is possible, add a source line in this format: [Source: file_name, chunk #n].
        - If the context is insufficient, clearly state that the documents are insufficient.
        """;
    }
}
