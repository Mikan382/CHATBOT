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

        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/Login";
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

        services.AddSingleton<IIndexingQueue, IndexingQueue>();
        services.AddSingleton<TextChunker>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddSingleton<ITextChunker>(_ => new FixedSizeChunker(1000, 150));
        services.AddSingleton<ITextChunker, SentenceChunker>();

        services.AddScoped<BenchmarkRetrievalService>();
        services.AddSingleton<IBenchmarkJobRunner, BenchmarkJobRunner>();

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentEmbeddingRepository, DocumentEmbeddingRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IUserAdminRepository, UserAdminRepository>();

        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<DocumentIndexingService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IEvaluationService, EvaluationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<RetrievalService>();
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        services.AddHttpClient<IGeminiClient, GeminiClient>();
        services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>();
        services.AddHttpClient<IFineTuneClient, FineTuneClient>();
        services.AddHostedService<BackgroundIndexingService>();
        services.AddHostedService<PendingDocumentQueueHostedService>();

        services.AddSingleton<EmbeddingClientFactory>();
        services.AddScoped<RagasScorer>();
        services.AddHttpClient();

        return services;
    }

    public static Task InitializeApplicationDatabaseAsync(this IServiceProvider services)
    {
        return DatabaseBootstrapper.InitializeAsync(services);
    }
}
