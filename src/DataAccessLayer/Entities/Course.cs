namespace DataAccessLayer.Entities;

public class Course
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tools { get; set; } = "";
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<CourseTeacher> TeacherAssignments { get; set; } = new List<CourseTeacher>();
}
