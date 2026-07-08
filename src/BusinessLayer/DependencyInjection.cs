using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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
        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (HttpMethods.IsPost(context.Request.Method) || context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        services.AddAuthorization();

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

        services.AddHttpClient<IGeminiClient, GeminiClient>();
        services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>();
        services.AddHttpClient();

        return services;
    }

    public static async Task InitializeApplicationDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        await DatabaseBootstrapper.InitializeAsync(
            scope.ServiceProvider,
            (user, password) => passwordHasher.HashPassword(user, password),
            (user, password) => passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed);
    }
}
