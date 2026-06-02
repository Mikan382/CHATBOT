using Microsoft.EntityFrameworkCore;
using Prn222Chatbot.Web.Data;
using Prn222Chatbot.Web.Hubs;
using Prn222Chatbot.Web.Repositories;
using Prn222Chatbot.Web.Services;
using Prn222Chatbot.Web.Services.Ai;
using Prn222Chatbot.Web.Services.Indexing;
using Prn222Chatbot.Web.Services.Parsing;
using Prn222Chatbot.Web.Services.Retrieval;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json.");

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSession();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<IIndexingQueue, IndexingQueue>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<RetrievalService>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>();
builder.Services.AddHttpClient<IFineTuneClient, FineTuneClient>();
builder.Services.AddHostedService<BackgroundIndexingService>();
builder.Services.AddHostedService<PendingDocumentQueueHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");

await DatabaseBootstrapper.InitializeAsync(app.Services);

app.Run();
