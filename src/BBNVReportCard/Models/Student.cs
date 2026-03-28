namespace BBNVReportCard.Models;

public class Student
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? MotherName { get; set; }
    public string? FatherName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? TotalWorkingDays { get; set; }
    public int? DaysPresent { get; set; }
}
