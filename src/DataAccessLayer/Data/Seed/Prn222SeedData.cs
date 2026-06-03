using DataAccessLayer.Entities;

namespace DataAccessLayer.Data.Seed;

public static class Prn222SeedData
{
    public static readonly Guid CourseId = Guid.Parse("1a5d6a34-f69e-4c56-a432-943f6d28d222");

    public static readonly IReadOnlyList<(Guid Id, int Order, string Clo, string Title, string Summary)> Chapters =
    [
        (Guid.Parse("10000000-0000-0000-0000-000000000001"), 1, "CLO1", "Chapter 01: Networking Programming", "HTTP, sockets, networking fundamentals, and how .NET applications communicate across processes and services."),
        (Guid.Parse("10000000-0000-0000-0000-000000000002"), 2, "CLO1", "Chapter 02: Asynchronous and Parallel Programming in .NET", "Task-based async, await, parallel execution, cancellation, and thread-safety fundamentals in .NET."),
        (Guid.Parse("10000000-0000-0000-0000-000000000003"), 3, "CLO1", "Chapter 03: Dependency Injection in .NET", "Service lifetimes, IoC, constructor injection, configuration, and testable application design."),
        (Guid.Parse("10000000-0000-0000-0000-000000000004"), 4, "CLO2", "Chapter 04: Building Web Application using ASP.NET Core MVC", "Model-View-Controller, routing, controllers, views, model binding, validation, and EF Core-backed web apps."),
        (Guid.Parse("10000000-0000-0000-0000-000000000005"), 5, "CLO3", "Chapter 05: Building Websites Using ASP.NET Core Razor Pages", "Page-focused programming with PageModel, handlers, routing, form binding, validation, and Razor syntax."),
        (Guid.Parse("10000000-0000-0000-0000-000000000006"), 6, "CLO4", "Chapter 06: Building a Web App with Blazor and ASP.NET Core", "Component-based UI, Razor components, binding, event handling, server/web assembly hosting concepts."),
        (Guid.Parse("10000000-0000-0000-0000-000000000007"), 7, "CLO5", "Chapter 07: Real-Time Communication", "SignalR hubs, clients, groups, connection lifetime, and real-time messaging patterns."),
        (Guid.Parse("10000000-0000-0000-0000-000000000008"), 8, "CLO5", "Chapter 08: Implement Background Tasks with Worker Service", "Hosted services, BackgroundService, queues, scoped dependencies, and long-running background processing.")
    ];

    public static async Task SeedAsync(AppDbContext db)
    {
        var course = db.Courses.FirstOrDefault(x => x.Id == CourseId);
        if (course is null)
        {
            db.Courses.Add(new Course
            {
                Id = CourseId,
                Code = "PRN222",
                Name = "Advanced Cross-Platform Application Programming With .NET",
                Description = "Advanced cross-platform application programming with .NET: ASP.NET Core MVC, Razor Pages, Blazor, SignalR, Worker Service, EF Core, asynchronous programming, and dependency injection.",
                Tools = "Visual Studio 2022+, .NET 8+, SQL Server 2019+"
            });
        }
        else
        {
            course.Code = "PRN222";
            course.Name = "Advanced Cross-Platform Application Programming With .NET";
            course.Description = "Advanced cross-platform application programming with .NET: ASP.NET Core MVC, Razor Pages, Blazor, SignalR, Worker Service, EF Core, asynchronous programming, and dependency injection.";
            course.Tools = "Visual Studio 2022+, .NET 8+, SQL Server 2019+";
        }

        foreach (var chapter in Chapters)
        {
            var existing = db.Chapters.FirstOrDefault(x => x.Id == chapter.Id);
            if (existing is null)
            {
                db.Chapters.Add(new Chapter
                {
                    Id = chapter.Id,
                    CourseId = CourseId,
                    Order = chapter.Order,
                    Clo = chapter.Clo,
                    Title = chapter.Title,
                    Summary = chapter.Summary
                });
            }
            else
            {
                existing.CourseId = CourseId;
                existing.Order = chapter.Order;
                existing.Clo = chapter.Clo;
                existing.Title = chapter.Title;
                existing.Summary = chapter.Summary;
            }
        }

        foreach (var question in BuildQuestions())
        {
            var existing = db.EvaluationQuestions.FirstOrDefault(x => x.Id == question.Id);
            if (existing is null)
            {
                db.EvaluationQuestions.Add(question);
            }
            else
            {
                existing.ChapterId = question.ChapterId;
                existing.Order = question.Order;
                existing.Question = question.Question;
                existing.GroundTruth = question.GroundTruth;
            }
        }

        await db.SaveChangesAsync();
    }

    public static IReadOnlyList<Document> BuildSeedDocuments()
    {
        return Chapters.Select(chapter => new Document
        {
            Id = Guid.NewGuid(),
            ChapterId = chapter.Id,
            OriginalFileName = $"PRN222_Chapter_{chapter.Order:00}_Guide.txt",
            FileType = ".txt",
            FileSizeBytes = BuildChapterGuide(chapter.Order, chapter.Title, chapter.Clo, chapter.Summary).Length,
            ContentText = BuildChapterGuide(chapter.Order, chapter.Title, chapter.Clo, chapter.Summary),
            UploadedAtUtc = DateTime.UtcNow
        }).ToList();
    }

    private static string BuildChapterGuide(int order, string title, string clo, string summary)
    {
        return $"""
        {title}
        CLO: {clo}

        Overview:
        {summary}

        Learning goals:
        Students should explain the core concepts, implement a small practice exercise, and connect this topic to a real ASP.NET Core web application.

        Review notes:
        - Understand the role of this chapter in PRN222.
        - Use the correct English technical terms.
        - Build a small .NET 8 example.
        - Compare classroom demo usage with production usage.
        """;
    }

    private static IReadOnlyList<EvaluationQuestion> BuildQuestions()
    {
        var rows = new List<(int Order, int Chapter, string Question, string GroundTruth)>
        {
            (1, 1, "What is PRN222 about?", "PRN222 is Advanced Cross-Platform Application Programming With .NET. It focuses on .NET web application development with ASP.NET Core MVC, Razor Pages, Blazor, SignalR, Worker Service, EF Core, asynchronous programming, and dependency injection."),
            (2, 1, "Which main tools are required for PRN222?", "The PRN222 tooling requirement includes Internet access, Visual Studio .NET 2022 or later, SQL Server 2019 or later, and .NET 8.0 or later."),
            (3, 1, "How many credits does PRN222 have and what is the prerequisite?", "PRN222 has 3 credits and the prerequisite course is PRN212."),
            (4, 1, "Which skill groups do the main PRN222 CLOs cover?", "The CLOs cover asynchronous and parallel programming, dependency injection, ASP.NET Core MVC, Razor Pages, Blazor, real-time communication, and Worker Service."),

            (5, 1, "What does Networking Programming in .NET help students understand?", "Networking Programming helps students understand how .NET applications communicate over networks using HTTP, sockets, and data exchange protocols between clients, servers, and services."),
            (6, 1, "How is HTTP different from socket programming?", "HTTP is a structured request/response application protocol, while socket programming is a lower-level API for exchanging byte streams or datagrams more flexibly."),
            (7, 1, "When should HttpClient be used in .NET?", "HttpClient should be used when a .NET application needs to call HTTP APIs, send requests, receive responses, and process data from external services."),
            (8, 1, "Why does networking code need timeout handling?", "Timeouts prevent requests from hanging indefinitely when the network or downstream service is slow, protecting threads, resources, and user experience."),
            (9, 1, "How should network failures be handled in a web application?", "The application should catch exceptions, return controlled messages, log failures, use timeouts, and apply reasonable retry policies instead of letting network errors crash the request."),
            (10, 1, "How does networking relate to a RAG chatbot?", "A RAG chatbot uses networking to receive browser requests, call AI APIs or fine-tuned endpoints, and return responses over HTTP or SignalR."),

            (11, 2, "What is async/await used for in C#?", "Async/await is used to write readable asynchronous code that frees a thread while waiting for I/O such as database calls, HTTP APIs, or file system operations."),
            (12, 2, "What does Task represent in .NET?", "Task represents an asynchronous operation that may complete in the future with either a result or an error."),
            (13, 2, "How is parallel programming different from asynchronous programming?", "Asynchronous programming optimizes waiting for I/O without blocking threads, while parallel programming splits CPU-bound work across multiple cores."),
            (14, 2, "What is the role of CancellationToken?", "CancellationToken passes a cancellation signal to asynchronous operations or background tasks so they can stop in a controlled way."),
            (15, 2, "Why should .Result or .Wait() be avoided in ASP.NET Core?", "Blocking with .Result or .Wait() can cause thread starvation, possible deadlocks in some contexts, and reduced scalability."),
            (16, 2, "Where should async be used in background document indexing?", "Background indexing should use async for file reading, database access, and external service calls so threads are not held while waiting for I/O."),

            (17, 3, "What is Dependency Injection in .NET?", "Dependency Injection provides dependencies from a container instead of letting a class create them directly, improving decoupling, testability, and implementation replacement."),
            (18, 3, "What are the three common DI lifetimes in ASP.NET Core?", "The common lifetimes are Singleton, Scoped, and Transient."),
            (19, 3, "Which dependencies are suitable for Scoped lifetime?", "Scoped services are suitable for dependencies that live within a request or scope, such as DbContext in ASP.NET Core."),
            (20, 3, "Why is DbContext usually registered as Scoped?", "DbContext is usually Scoped so each request has its own unit of work, avoids sharing state between requests, and keeps transaction handling consistent."),
            (21, 3, "What is the benefit of constructor injection?", "Constructor injection makes dependencies explicit, ensures required dependencies are available when the class is created, and makes mocking easier for tests."),
            (22, 3, "How should a HostedService access DbContext?", "HostedService is singleton, so it should create a scope with IServiceScopeFactory and resolve scoped dependencies such as DbContext inside that scope."),

            (23, 4, "Which components make up the MVC pattern?", "MVC includes Model, View, and Controller. Model represents data and business concepts, View renders UI, and Controller receives requests and coordinates processing."),
            (24, 4, "What is the responsibility of a Controller in ASP.NET Core MVC?", "A Controller receives HTTP requests, calls the necessary services, and returns a View or response data."),
            (25, 4, "What kind of logic should a View contain in MVC?", "A View should contain only lightweight presentation logic and should not contain business logic or direct database access."),
            (26, 4, "What is model binding in ASP.NET Core MVC?", "Model binding maps data from route values, query strings, forms, or request bodies into action parameters or view models."),
            (27, 4, "What mechanism is commonly used for validation in MVC?", "MVC validation commonly uses Data Annotations on models or view models together with ModelState in the controller."),
            (28, 4, "Which layer should EF Core belong to in a 3-layer architecture?", "EF Core belongs to the data access or repository layer, where it maps entities to the database and executes queries."),
            (29, 4, "Why does the assignment require MVC plus 3-Layers?", "The requirement proves that students can separate Presentation, Business Logic, and Data Access instead of placing all logic in the UI."),

            (30, 5, "What is the main difference between Razor Pages and MVC?", "Razor Pages are organized around pages with PageModel and handlers, while MVC is organized around controllers and actions."),
            (31, 5, "What does PageModel do in Razor Pages?", "PageModel contains handlers, page state, and request processing logic for a Razor Page."),
            (32, 5, "What are common handler methods in Razor Pages?", "Common handlers include OnGet, OnPost, and variants such as OnPostUploadAsync or OnGetDetailsAsync."),
            (33, 5, "What is Razor syntax used for?", "Razor syntax mixes HTML with server-side C# to render dynamic content."),
            (34, 5, "When are Razor Pages more suitable than MVC?", "Razor Pages are suitable for page-focused screens such as forms, simple CRUD pages, or screens whose logic belongs closely to a single page."),
            (35, 5, "Which property is commonly used for form binding in Razor Pages?", "Razor Pages commonly use BindProperty to bind form data into PageModel properties."),

            (36, 6, "What is Blazor?", "Blazor is a framework for building web UI with Razor components and C#, enabling interactive components instead of only static HTML rendering."),
            (37, 6, "What parts does a Blazor component usually contain?", "A Blazor component usually contains Razor markup, C# code, parameters, state, and event handlers."),
            (38, 6, "What is data binding used for in Blazor?", "Data binding synchronizes C# component state with displayed UI or input values."),
            (39, 6, "How is Blazor Server different from Blazor WebAssembly?", "Blazor Server runs logic on the server and interacts through SignalR, while Blazor WebAssembly runs in the browser through WebAssembly."),
            (40, 6, "Why is Blazor suitable for dashboards?", "Blazor is suitable for dashboards because it supports reusable components, state updates, event handling, and C#-based UI composition."),
            (41, 6, "Which PRN222 skill does Blazor demonstrate?", "Blazor demonstrates component-based web application development with ASP.NET Core and Razor components."),

            (42, 7, "What is SignalR used for?", "SignalR is used for real-time communication between server and client, such as chat, notifications, or live dashboards."),
            (43, 7, "What is a Hub in SignalR?", "A Hub is a server-side class that defines methods clients can call and methods the server can use to send messages back to clients."),
            (44, 7, "When should SignalR groups be used?", "Groups are used to send messages to a specific set of clients, such as browsers that join the same chat session."),
            (45, 7, "Why should a chat app use SignalR?", "A chat app should use SignalR so messages appear in real time across multiple clients without page reloads or continuous polling."),
            (46, 7, "What should the server do when SignalR sends an error to a client?", "The server should send a clear error event for the UI to display and log the failure on the server."),

            (47, 8, "What is Worker Service used for in ASP.NET Core?", "Worker Service or HostedService is used to run long-running background tasks or work outside the HTTP request lifecycle."),
            (48, 8, "How is BackgroundService different from a request controller?", "BackgroundService runs independently in the background, while a controller only handles incoming HTTP requests."),
            (49, 8, "Why should document indexing run in the background?", "Indexing can spend time reading files, chunking text, and saving data, so running it in the background lets the upload request return faster."),
            (50, 8, "What should a HostedService consider when using scoped dependencies?", "HostedService is singleton, so it must create a separate scope to use scoped dependencies such as DbContext with the correct lifecycle.")
        };

        return rows.Select(row => new EvaluationQuestion
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{row.Order:000000000000}"),
            ChapterId = Chapters[row.Chapter - 1].Id,
            Order = row.Order,
            Question = row.Question,
            GroundTruth = row.GroundTruth
        }).ToList();
    }
}
