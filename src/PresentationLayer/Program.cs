using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.AI;
using BusinessLayer.Indexing;
using BusinessLayer.Parsing;
using BusinessLayer.Payment;
using BusinessLayer.Retrieval;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using PresentationLayer.Hubs;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var versionValue = context.Principal?.FindFirstValue("user_version");
            var userId = Guid.Empty;
            var userVersion = 0L;
            var validClaims = Guid.TryParse(idValue, out userId)
                && long.TryParse(versionValue, out userVersion)
                && !string.IsNullOrWhiteSpace(email)
                && !string.IsNullOrWhiteSpace(role);

            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            if (!validClaims || !await authService.IsPrincipalCurrentAsync(
                    userId,
                    email!,
                    role!,
                    userVersion,
                    context.HttpContext.RequestAborted))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/chatHub"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/chatHub")
                || HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.Configure<VnPayOptions>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.SectionName))
    .Validate(options => options.TopK is >= 1 and <= 20, "Rag:TopK must be between 1 and 20.")
    .Validate(options => options.MinimumSimilarityScore is >= -1 and <= 1,
        "Rag:MinimumSimilarityScore must be between -1 and 1.")
    .Validate(options => options.HistoryMessageCount is >= 0 and <= 50,
        "Rag:HistoryMessageCount must be between 0 and 50.")
    .ValidateOnStart();

builder.Services.AddSingleton<ITextChunker, ParagraphChunker>();
builder.Services.AddSingleton<ITextChunker>(_ => new FixedSizeChunker());
builder.Services.AddSingleton<ITextChunker, SentenceChunker>();

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentEmbeddingRepository, DocumentEmbeddingRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBenchmarkRepository, BenchmarkRepository>();

builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IChunkingSettingsService, ChunkingSettingsService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<IPaymentGateway, VnPayGateway>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();
builder.Services.AddScoped<RetrievalService>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

builder.Services.AddHttpClient<IGeminiClient, GeminiClient>(client => client.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>(client => client.Timeout = TimeSpan.FromSeconds(120));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/chatHub");

await InitializeApplicationDatabaseAsync(app.Services);

app.Run();

static async Task InitializeApplicationDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
    await DatabaseBootstrapper.InitializeAsync(
        db,
        configuration,
        (user, password) => passwordHasher.HashPassword(user, password));
}
