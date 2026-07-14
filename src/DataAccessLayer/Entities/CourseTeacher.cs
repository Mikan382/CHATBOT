namespace DataAccessLayer.Entities;

public class CourseTeacher
{
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid TeacherUserId { get; set; }
    public ApplicationUser? Teacher { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
