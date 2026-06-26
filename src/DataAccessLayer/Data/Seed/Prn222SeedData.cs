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

    private static IReadOnlyList<EvaluationQuestion> BuildQuestions()
    {
        var rows = new List<(int Order, int Chapter, string Question, string GroundTruth)>
        {
            (1, 1, "Môn học PRN222 là gì?", "PRN222 là môn Lập trình ứng dụng đa nền tảng nâng cao với .NET. Môn học tập trung vào phát triển ứng dụng web .NET với ASP.NET Core MVC, Razor Pages, Blazor, SignalR, Worker Service, EF Core, lập trình bất đồng bộ và dependency injection."),
            (2, 1, "Những công cụ chính nào được yêu cầu cho PRN222?", "Yêu cầu công cụ của PRN222 gồm kết nối Internet, Visual Studio .NET 2022 trở lên, SQL Server 2019 trở lên và .NET 8.0 trở lên."),
            (3, 1, "PRN222 có bao nhiêu tín chỉ và môn tiên quyết là gì?", "PRN222 có 3 tín chỉ và môn tiên quyết là PRN212."),
            (4, 1, "Các CLO chính của PRN222 bao gồm những nhóm kỹ năng nào?", "Các CLO bao gồm lập trình bất đồng bộ và song song, dependency injection, ASP.NET Core MVC, Razor Pages, Blazor, giao tiếp thời gian thực và Worker Service."),

            (5, 1, "Lập trình mạng trong .NET giúp sinh viên hiểu điều gì?", "Lập trình mạng giúp sinh viên hiểu cách ứng dụng .NET giao tiếp qua mạng bằng HTTP, socket và các giao thức trao đổi dữ liệu giữa client, server và các dịch vụ."),
            (6, 1, "HTTP khác với lập trình socket như thế nào?", "HTTP là giao thức ứng dụng request/response có cấu trúc, trong khi lập trình socket là API cấp thấp hơn để trao đổi luồng byte hoặc datagram một cách linh hoạt hơn."),
            (7, 1, "Khi nào nên dùng HttpClient trong .NET?", "Nên dùng HttpClient khi ứng dụng .NET cần gọi HTTP API, gửi yêu cầu, nhận phản hồi và xử lý dữ liệu từ các dịch vụ bên ngoài."),
            (8, 1, "Tại sao code mạng cần xử lý timeout?", "Timeout ngăn yêu cầu bị treo vô thời hạn khi mạng hoặc dịch vụ phía sau chậm, bảo vệ thread, tài nguyên và trải nghiệm người dùng."),
            (9, 1, "Nên xử lý lỗi mạng trong ứng dụng web như thế nào?", "Ứng dụng nên bắt ngoại lệ, trả về thông báo có kiểm soát, ghi log lỗi, dùng timeout và áp dụng chính sách retry hợp lý thay vì để lỗi mạng làm crash yêu cầu."),
            (10, 1, "Lập trình mạng liên quan đến chatbot RAG như thế nào?", "Chatbot RAG dùng lập trình mạng để nhận yêu cầu từ trình duyệt, gọi API AI hoặc endpoint fine-tuned và trả về phản hồi qua HTTP hoặc SignalR."),

            (11, 2, "async/await được dùng để làm gì trong C#?", "async/await được dùng để viết code bất đồng bộ dễ đọc, giải phóng thread trong khi chờ I/O như gọi cơ sở dữ liệu, HTTP API hoặc thao tác hệ thống tệp."),
            (12, 2, "Task đại diện cho điều gì trong .NET?", "Task đại diện cho một thao tác bất đồng bộ có thể hoàn thành trong tương lai với kết quả hoặc lỗi."),
            (13, 2, "Lập trình song song khác lập trình bất đồng bộ như thế nào?", "Lập trình bất đồng bộ tối ưu việc chờ I/O mà không chặn thread, còn lập trình song song phân chia công việc nặng về CPU trên nhiều nhân xử lý."),
            (14, 2, "Vai trò của CancellationToken là gì?", "CancellationToken truyền tín hiệu hủy đến các thao tác bất đồng bộ hoặc tác vụ nền để chúng có thể dừng theo cách có kiểm soát."),
            (15, 2, "Tại sao nên tránh dùng .Result hoặc .Wait() trong ASP.NET Core?", "Chặn bằng .Result hoặc .Wait() có thể gây thiếu thread, deadlock trong một số ngữ cảnh và giảm khả năng mở rộng."),
            (16, 2, "Nên dùng async ở đâu trong việc lập chỉ mục tài liệu nền?", "Lập chỉ mục nền nên dùng async cho đọc tệp, truy cập cơ sở dữ liệu và gọi dịch vụ bên ngoài để thread không bị giữ trong khi chờ I/O."),

            (17, 3, "Dependency Injection trong .NET là gì?", "Dependency Injection cung cấp các phụ thuộc từ container thay vì để class tự tạo, cải thiện tính tách biệt, khả năng kiểm thử và thay thế implementation."),
            (18, 3, "Ba lifetime DI phổ biến trong ASP.NET Core là gì?", "Ba lifetime phổ biến là Singleton, Scoped và Transient."),
            (19, 3, "Những phụ thuộc nào phù hợp với Scoped lifetime?", "Scoped phù hợp cho các phụ thuộc sống trong phạm vi một yêu cầu hoặc scope, như DbContext trong ASP.NET Core."),
            (20, 3, "Tại sao DbContext thường được đăng ký là Scoped?", "DbContext thường là Scoped để mỗi yêu cầu có unit of work riêng, tránh chia sẻ trạng thái giữa các yêu cầu và giữ việc xử lý transaction nhất quán."),
            (21, 3, "Lợi ích của constructor injection là gì?", "Constructor injection làm rõ các phụ thuộc, đảm bảo phụ thuộc cần thiết có sẵn khi class được tạo và giúp mock dễ hơn khi kiểm thử."),
            (22, 3, "HostedService nên truy cập DbContext như thế nào?", "HostedService là singleton, nên phải tạo scope bằng IServiceScopeFactory và resolve các phụ thuộc scoped như DbContext bên trong scope đó."),

            (23, 4, "Mô hình MVC bao gồm những thành phần nào?", "MVC gồm Model, View và Controller. Model đại diện cho dữ liệu và nghiệp vụ, View hiển thị giao diện, Controller nhận yêu cầu và điều phối xử lý."),
            (24, 4, "Trách nhiệm của Controller trong ASP.NET Core MVC là gì?", "Controller nhận yêu cầu HTTP, gọi các service cần thiết và trả về View hoặc dữ liệu phản hồi."),
            (25, 4, "View nên chứa loại logic nào trong MVC?", "View chỉ nên chứa logic trình bày nhẹ và không nên chứa logic nghiệp vụ hay truy cập cơ sở dữ liệu trực tiếp."),
            (26, 4, "Model binding trong ASP.NET Core MVC là gì?", "Model binding ánh xạ dữ liệu từ route values, query string, form hoặc request body vào tham số action hoặc view model."),
            (27, 4, "Cơ chế nào thường dùng để validation trong MVC?", "Validation trong MVC thường dùng Data Annotations trên model hoặc view model kết hợp với ModelState trong controller."),
            (28, 4, "EF Core nên thuộc layer nào trong kiến trúc 3 tầng?", "EF Core thuộc tầng data access hoặc repository, nơi nó ánh xạ entity sang cơ sở dữ liệu và thực thi truy vấn."),
            (29, 4, "Tại sao đồ án yêu cầu MVC kết hợp 3-Layers?", "Yêu cầu này chứng minh sinh viên có thể tách biệt Presentation, Business Logic và Data Access thay vì đặt toàn bộ logic vào UI."),

            (30, 5, "Sự khác biệt chính giữa Razor Pages và MVC là gì?", "Razor Pages được tổ chức theo trang với PageModel và handler, còn MVC được tổ chức theo controller và action."),
            (31, 5, "PageModel làm gì trong Razor Pages?", "PageModel chứa handler, trạng thái trang và logic xử lý yêu cầu cho một Razor Page."),
            (32, 5, "Các handler method phổ biến trong Razor Pages là gì?", "Các handler phổ biến gồm OnGet, OnPost và các biến thể như OnPostUploadAsync hoặc OnGetDetailsAsync."),
            (33, 5, "Razor syntax được dùng để làm gì?", "Razor syntax kết hợp HTML với C# phía server để hiển thị nội dung động."),
            (34, 5, "Khi nào Razor Pages phù hợp hơn MVC?", "Razor Pages phù hợp cho màn hình tập trung vào trang như form, CRUD đơn giản hoặc màn hình có logic gắn chặt với một trang."),
            (35, 5, "Thuộc tính nào thường dùng để binding form trong Razor Pages?", "Razor Pages thường dùng BindProperty để bind dữ liệu form vào thuộc tính của PageModel."),

            (36, 6, "Blazor là gì?", "Blazor là framework xây dựng giao diện web bằng Razor component và C#, cho phép tạo component tương tác thay vì chỉ hiển thị HTML tĩnh."),
            (37, 6, "Một Blazor component thường gồm những phần nào?", "Một Blazor component thường gồm Razor markup, code C#, parameter, state và event handler."),
            (38, 6, "Data binding trong Blazor được dùng để làm gì?", "Data binding đồng bộ trạng thái C# của component với UI hiển thị hoặc giá trị input."),
            (39, 6, "Blazor Server khác Blazor WebAssembly như thế nào?", "Blazor Server chạy logic trên server và tương tác qua SignalR, còn Blazor WebAssembly chạy trong trình duyệt thông qua WebAssembly."),
            (40, 6, "Tại sao Blazor phù hợp cho dashboard?", "Blazor phù hợp cho dashboard vì hỗ trợ component tái sử dụng, cập nhật state, xử lý sự kiện và tổ hợp UI bằng C#."),
            (41, 6, "Blazor thể hiện kỹ năng nào của PRN222?", "Blazor thể hiện kỹ năng phát triển ứng dụng web theo hướng component với ASP.NET Core và Razor component."),

            (42, 7, "SignalR được dùng để làm gì?", "SignalR được dùng cho giao tiếp thời gian thực giữa server và client, như chat, thông báo hoặc dashboard trực tiếp."),
            (43, 7, "Hub trong SignalR là gì?", "Hub là class phía server định nghĩa các phương thức client có thể gọi và phương thức server dùng để gửi tin nhắn về client."),
            (44, 7, "Khi nào nên dùng group trong SignalR?", "Group được dùng để gửi tin nhắn đến một nhóm client cụ thể, ví dụ các trình duyệt tham gia cùng phiên chat."),
            (45, 7, "Tại sao ứng dụng chat nên dùng SignalR?", "Ứng dụng chat nên dùng SignalR để tin nhắn hiển thị ngay lập tức trên nhiều client mà không cần reload trang hoặc polling liên tục."),
            (46, 7, "Server nên làm gì khi SignalR gửi lỗi đến client?", "Server nên gửi sự kiện lỗi rõ ràng để UI hiển thị và ghi log lỗi ở phía server."),

            (47, 8, "Worker Service được dùng để làm gì trong ASP.NET Core?", "Worker Service hay HostedService được dùng để chạy tác vụ nền chạy lâu hoặc công việc ngoài vòng đời yêu cầu HTTP."),
            (48, 8, "BackgroundService khác controller xử lý yêu cầu như thế nào?", "BackgroundService chạy độc lập ở nền, còn controller chỉ xử lý các yêu cầu HTTP đến."),
            (49, 8, "Tại sao lập chỉ mục tài liệu nên chạy ở nền?", "Lập chỉ mục có thể tốn thời gian đọc tệp, chia nhỏ văn bản và lưu dữ liệu, nên chạy ở nền giúp yêu cầu upload trả về nhanh hơn."),
            (50, 8, "HostedService cần lưu ý điều gì khi dùng phụ thuộc scoped?", "HostedService là singleton, nên phải tạo scope riêng để dùng phụ thuộc scoped như DbContext với lifecycle đúng.")
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
