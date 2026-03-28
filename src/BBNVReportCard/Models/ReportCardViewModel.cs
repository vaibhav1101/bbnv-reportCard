namespace BBNVReportCard.Models;

public class ReportCardViewModel
{
    public Student Student { get; set; } = new();
    public List<SubjectMark> Subjects { get; set; } = new();
    public string Term { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public string? ClassTeacherName { get; set; }
    public string? ClassTeacherRemarks { get; set; }
    public string? PrincipalRemarks { get; set; }
    public decimal? TotalMarksObtained => Subjects.Sum(s => s.MarksObtained);
    public decimal? TotalMaxMarks => Subjects.Sum(s => s.MaxMarks);
    public decimal? Percentage => TotalMaxMarks > 0
        ? Math.Round((TotalMarksObtained!.Value / TotalMaxMarks!.Value) * 100, 2)
        : null;
    public string? OverallGrade { get; set; }
    public string? Result { get; set; }
    public SchoolLevel Level { get; set; }
    public Stream Stream { get; set; } = Stream.None;
}

public class ReportCardSearchModel
{
    public int? StudentId { get; set; }
    public string? RollNumber { get; set; }
    public string? ClassName { get; set; }
    public string? Section { get; set; }
    public string? Stream { get; set; }
    public string Term { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public List<ClassOption> AvailableClasses { get; set; } = new();
}

public class ClassOption
{
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Display => $"{ClassName} - {Section}";
}
