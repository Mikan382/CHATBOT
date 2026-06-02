using Prn222Chatbot.Web.Repositories;
using Prn222Chatbot.Web.Services;

namespace Prn222Chatbot.Web.Services.Retrieval;

public class RetrievalService
{
    private readonly IDocumentRepository _documentRepository;

    public RetrievalService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken)
    {
        var terms = TextNormalizer.Terms(query);
        if (terms.Count == 0)
        {
            return [];
        }

        var chunks = await _documentRepository.ListIndexedChunksAsync(cancellationToken);
        var scored = chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = Score(terms, chunk.NormalizedContent)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunkDto(
                x.Chunk.Id,
                x.Chunk.DocumentId,
                x.Chunk.SourceName,
                x.Chunk.Document?.Chapter?.Title ?? "PRN222",
                x.Chunk.ChunkIndex,
                x.Chunk.Content,
                x.Score))
            .ToList();

        return scored;
    }

    private static double Score(IReadOnlyList<string> terms, string normalizedContent)
    {
        var contentTerms = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (contentTerms.Length == 0)
        {
            return 0;
        }

        var matches = 0d;
        foreach (var term in terms.Distinct())
        {
            var exact = contentTerms.Count(x => x == term);
            if (exact > 0)
            {
                matches += exact * 2;
            }
            else if (normalizedContent.Contains(term, StringComparison.Ordinal))
            {
                matches += 0.5;
            }
        }

        return matches / (Math.Sqrt(terms.Count) * Math.Sqrt(contentTerms.Length) + 1);
    }
}
