using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data.Seed;
using Prn222Chatbot.Web.Domain.Enums;

namespace Prn222Chatbot.Web.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await Prn222SeedData.SeedAsync(db);

        var seedDocuments = Prn222SeedData.BuildSeedDocuments();
        foreach (var seedDocument in seedDocuments)
        {
            var existing = await db.Documents
                .Include(x => x.Chunks)
                .FirstOrDefaultAsync(x => x.OriginalFileName == seedDocument.OriginalFileName);

            if (existing is null)
            {
                db.Documents.Add(seedDocument);
                continue;
            }

            if (existing.ContentText != seedDocument.ContentText)
            {
                existing.ChapterId = seedDocument.ChapterId;
                existing.FileType = seedDocument.FileType;
                existing.FileSizeBytes = seedDocument.FileSizeBytes;
                existing.ContentText = seedDocument.ContentText;
                existing.IndexStatus = DocumentIndexStatus.Pending;
                existing.IndexError = null;
                db.DocumentChunks.RemoveRange(existing.Chunks);
            }
        }

        await db.SaveChangesAsync();
    }
}
