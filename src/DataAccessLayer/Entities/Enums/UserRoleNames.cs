namespace DataAccessLayer.Enums;

public static class UserRoleNames
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Admin = "Admin";
    public const string TeacherOrAdmin = Teacher + "," + Admin;
    public const string All = Student + "," + Teacher + "," + Admin;
}
