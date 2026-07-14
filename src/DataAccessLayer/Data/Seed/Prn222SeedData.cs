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
        (Guid.Parse("10000000-0000-0000-0000-000000000008"), 8, "CLO5", "Chapter 08: ASP.NET Core Application Services", "Service orchestration, dependency injection, request processing, and maintainable layered application design.")
    ];

    public static void AddTo(AppDbContext db)
    {
        db.Courses.Add(new Course
        {
            Id = CourseId,
            Code = "PRN222",
            Name = "Advanced Cross-Platform Application Programming With .NET",
            Description = "Advanced cross-platform application programming with .NET: ASP.NET Core MVC, Blazor, SignalR, EF Core, asynchronous programming, and dependency injection.",
            Tools = "Visual Studio 2022+, .NET 8+, SQL Server 2019+"
        });

        db.Chapters.AddRange(Chapters.Select(chapter => new Chapter
        {
            Id = chapter.Id,
            CourseId = CourseId,
            Order = chapter.Order,
            Clo = chapter.Clo,
            Title = chapter.Title,
            Summary = chapter.Summary
        }));
    }
}
