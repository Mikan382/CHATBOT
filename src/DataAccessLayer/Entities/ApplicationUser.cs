using Microsoft.AspNetCore.Identity;

namespace DataAccessLayer.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
