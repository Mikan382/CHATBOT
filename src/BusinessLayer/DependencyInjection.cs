using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using BusinessLayer.AI;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using BusinessLayer.Retrieval;

namespace BusinessLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddSingleton<ITextChunker>(_ => new FixedSizeChunker(1000, 150));
        services.AddSingleton<ITextChunker, SentenceChunker>();

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentEmbeddingRepository, DocumentEmbeddingRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IUserAdminRepository, UserAdminRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<DocumentIndexingService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IChunkingSettingsService, ChunkingSettingsService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<RetrievalService>();
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        services.AddHttpClient<IGeminiClient, GeminiClient>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }

    public static async Task InitializeApplicationDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        await DatabaseBootstrapper.InitializeAsync(
            scope.ServiceProvider,
            (user, password) => passwordHasher.HashPassword(user, password),
            DocumentContentHasher.Compute);
    }
}
