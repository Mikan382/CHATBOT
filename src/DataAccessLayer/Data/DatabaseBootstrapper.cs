using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Data.Seed;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await Prn222SeedData.SeedAsync(db);

        await NormalizeDocumentProgressAsync(db);
        await SeedIdentityAsync(scope.ServiceProvider);
        await SeedSampleDocumentsAsync(scope.ServiceProvider);
    }

    private static async Task NormalizeDocumentProgressAsync(AppDbContext db)
    {
        foreach (var document in db.Documents)
        {
            switch (document.IndexStatus)
            {
                case DocumentIndexStatus.Indexed:
                    document.IndexProgressPercent = 100;
                    document.IndexStage = "Indexed";
                    break;
                case DocumentIndexStatus.Failed:
                    document.IndexStage = string.IsNullOrWhiteSpace(document.IndexStage) ? "Failed" : document.IndexStage;
                    break;
                case DocumentIndexStatus.Processing:
                    document.IndexProgressPercent = Math.Clamp(document.IndexProgressPercent, 10, 95);
                    document.IndexStage = string.IsNullOrWhiteSpace(document.IndexStage) ? "Processing" : document.IndexStage;
                    break;
                default:
                    document.IndexProgressPercent = 0;
                    document.IndexStage = "Queued";
                    break;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedUserAsync(userManager, configuration, "Student", "student@prn222.local", "Student Demo", UserRoleNames.Student);
        await SeedUserAsync(userManager, configuration, "Teacher", "teacher@prn222.local", "Teacher Demo", UserRoleNames.Teacher);
        await SeedUserAsync(userManager, configuration, "Admin", "admin@prn222.local", "Admin Demo", UserRoleNames.Admin);
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string key,
        string defaultEmail,
        string defaultFullName,
        string role)
    {
        var email = configuration[$"SeedUsers:{key}:Email"] ?? defaultEmail;
        var fullName = configuration[$"SeedUsers:{key}:FullName"] ?? defaultFullName;
        var password = configuration[$"SeedUsers:{key}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            // Fresh-install fallback: app is usable without user-secrets.
            // Override via SeedUsers:{key}:Password in production.
            password = "Prn222@123";
            Console.WriteLine($"[SEED] No password configured for '{key}'. Using default dev password for {defaultEmail}.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedSampleDocumentsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var docId1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var docId2 = Guid.Parse("20000000-0000-0000-0000-000000000002");

        if (await db.Documents.AnyAsync(x => x.Id == docId1))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync("admin@prn222.local");
        var uploaderId = admin?.Id;

        var chapter2Id = Guid.Parse("10000000-0000-0000-0000-000000000002"); // Async & Parallel
        var chapter7Id = Guid.Parse("10000000-0000-0000-0000-000000000007"); // SignalR

        var content1 = """
            Chương 2: Lập trình bất đồng bộ và song song trong .NET

            Lập trình bất đồng bộ (asynchronous programming) cho phép chương trình thực hiện các tác vụ tốn thời gian như gọi mạng, đọc ghi tệp, hoặc truy vấn cơ sở dữ liệu mà không chặn luồng (thread) hiện tại. Trong .NET, mô hình async/await dựa trên kiểu Task và Task<T> giúp viết mã bất đồng bộ trông gần giống mã tuần tự.

            Từ khóa async đánh dấu một phương thức có thể chứa await. Từ khóa await tạm dừng việc thực thi phương thức cho đến khi Task hoàn thành, nhưng không chặn luồng gọi — luồng được trả về thread pool để phục vụ công việc khác. Nhờ vậy ứng dụng web ASP.NET Core có thể phục vụ nhiều yêu cầu đồng thời với ít luồng hơn.

            CancellationToken được dùng để hủy một tác vụ bất đồng bộ một cách hợp tác. Phương thức nhận CancellationToken nên kiểm tra trạng thái hủy và dừng sớm khi được yêu cầu, giúp giải phóng tài nguyên và tránh treo yêu cầu.

            Lập trình song song (parallel programming) khác với bất đồng bộ: nó dùng nhiều luồng để chạy đồng thời các phần việc nặng về CPU, ví dụ qua Parallel.For hoặc PLINQ. Khi nhiều luồng cùng truy cập dữ liệu chia sẻ, cần bảo đảm an toàn luồng (thread-safety) bằng khóa (lock) hoặc các kiểu dữ liệu đồng bộ như ConcurrentDictionary để tránh race condition.

            Task.WhenAll cho phép chờ nhiều Task đồng thời và nhận về tất cả kết quả khi tất cả hoàn thành. Task.WhenAny trả về Task đầu tiên hoàn thành, hữu ích khi cần timeout hoặc "race" giữa các nguồn dữ liệu.

            Phương thức bất đồng bộ nên luôn trả về Task hoặc Task<T> thay vì void (trừ trường hợp event handler). Trả về void ngăn caller theo dõi và xử lý ngoại lệ. Tránh .Result và .Wait() vì chúng có thể gây deadlock trong môi trường có SynchronizationContext như ASP.NET Core.

            ConfigureAwait(false) nên được dùng trong code thư viện (library) để tránh bắt lại SynchronizationContext gốc sau await, giúp tránh deadlock và cải thiện hiệu năng. Trong ứng dụng ASP.NET Core, không cần ConfigureAwait(false) vì framework không có SynchronizationContext.
            """;

        var content2 = """
            Chương 7: Giao tiếp thời gian thực với SignalR

            SignalR là thư viện ASP.NET Core cho phép máy chủ đẩy dữ liệu đến client theo thời gian thực mà không cần client liên tục polling. SignalR tự động chọn giao thức vận chuyển tốt nhất: WebSocket (ưu tiên), Server-Sent Events, hoặc Long Polling.

            Hub là thành phần trung tâm của SignalR, đóng vai trò điều phối giao tiếp giữa server và client. Mỗi Hub là một class kế thừa Hub hoặc Hub<T>. Các phương thức public trong Hub có thể được client gọi trực tiếp; server cũng có thể gọi phương thức JavaScript/TypeScript trên client qua Clients.All, Clients.Caller, Clients.Group.

            Groups cho phép gom nhiều kết nối vào nhóm. Dùng Groups.AddToGroupAsync để thêm kết nối vào nhóm và Clients.Group(groupName) để gửi tới tất cả thành viên nhóm đó. Điều này hữu ích cho phòng chat, kênh thông báo theo chủ đề.

            Lifetime của kết nối SignalR được quản lý qua OnConnectedAsync và OnDisconnectedAsync. Khi kết nối ngắt, server tự động dọn dẹp state liên quan đến kết nối đó.

            Khi dùng SignalR với ASP.NET Core Identity, cần cấu hình xác thực và ủy quyền đúng. Hub có thể được bảo vệ với [Authorize]. Context.User chứa thông tin người dùng đã đăng nhập; Context.ConnectionId định danh duy nhất kết nối hiện tại.

            SignalR JavaScript client được cài qua npm (@microsoft/signalr) hoặc CDN. Khởi tạo bằng HubConnectionBuilder, sau đó gọi connection.start() để kết nối. Đăng ký handler bằng connection.on("MethodName", callback) và gọi hub method bằng connection.invoke("MethodName", ...args).
            """;

        db.Documents.Add(new Document
        {
            Id = docId1,
            ChapterId = chapter2Id,
            UploadedByUserId = uploaderId,
            OriginalFileName = "chuong-02-lap-trinh-bat-dong-bo.txt",
            FileType = ".txt",
            FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(content1),
            ContentText = content1,
            IndexStatus = DocumentIndexStatus.Pending,
            IndexProgressPercent = 0,
            IndexStage = "Queued",
            UploadedAtUtc = DateTime.UtcNow
        });

        db.Documents.Add(new Document
        {
            Id = docId2,
            ChapterId = chapter7Id,
            UploadedByUserId = uploaderId,
            OriginalFileName = "chuong-07-signalr-giao-tiep-thoi-gian-thuc.txt",
            FileType = ".txt",
            FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(content2),
            ContentText = content2,
            IndexStatus = DocumentIndexStatus.Pending,
            IndexProgressPercent = 0,
            IndexStage = "Queued",
            UploadedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        Console.WriteLine("[SEED] Seeded 2 sample Vietnamese documents (status: Pending, will be indexed on startup).");
    }
}
