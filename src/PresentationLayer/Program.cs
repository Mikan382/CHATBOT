using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using PresentationLayer.Hubs;
using DataAccessLayer.Repositories;
using BusinessLayer.Services;
using BusinessLayer.AI;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using BusinessLayer.Retrieval;
using DataAccessLayer.Enums;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json.");

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSession();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
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
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/login";
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
builder.Services.AddSingleton<IIndexingQueue, IndexingQueue>();
builder.Services.AddSingleton<TextChunker>();
// Chunkers enumerable for benchmark (resolved by StrategyName)
builder.Services.AddSingleton<ITextChunker, TextChunker>();
builder.Services.AddSingleton<ITextChunker>(_ => new FixedSizeChunker(1000, 150));
builder.Services.AddSingleton<ITextChunker, SentenceChunker>();
builder.Services.AddScoped<BenchmarkRetrievalService>();
builder.Services.AddSingleton<BenchmarkJobRunner>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentEmbeddingRepository, DocumentEmbeddingRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ChapterService>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<RetrievalService>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>();
builder.Services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>();
builder.Services.AddHttpClient<IFineTuneClient, FineTuneClient>();
builder.Services.AddHostedService<BackgroundIndexingService>();
builder.Services.AddHostedService<PendingDocumentQueueHostedService>();

// Research module services
builder.Services.AddSingleton<EmbeddingClientFactory>();
builder.Services.AddScoped<RagasScorer>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");

await DatabaseBootstrapper.InitializeAsync(app.Services);

app.Run();
