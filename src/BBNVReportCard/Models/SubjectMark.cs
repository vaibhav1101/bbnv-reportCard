namespace BBNVReportCard.Models;

public class SubjectMark
{
    public string SubjectName { get; set; } = string.Empty;
    public string? SubjectCode { get; set; }
    public decimal? MaxMarks { get; set; }
    public decimal? MarksObtained { get; set; }
    public string? Grade { get; set; }
    public string? Remarks { get; set; }
}
