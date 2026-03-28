using BBNVReportCard.Models;
using Microsoft.Data.SqlClient;

namespace BBNVReportCard.Services;

public class ReportCardService
{
    private readonly string _connectionString;
    private readonly IConfiguration _config;

    public ReportCardService(IConfiguration config)
    {
        _config = config;
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<List<ClassOption>> GetClassesAsync()
    {
        var classes = new List<ClassOption>();
        const string sql = @"
            SELECT DISTINCT ClassName, Section
            FROM Students
            ORDER BY ClassName, Section";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            classes.Add(new ClassOption
            {
                ClassName = reader.GetString(0),
                Section = reader.GetString(1)
            });
        }
        return classes;
    }

    public async Task<Student?> GetStudentAsync(int studentId)
    {
        const string sql = @"
            SELECT StudentId, StudentName, RollNumber, ClassName, Section,
                   MotherName, FatherName, DateOfBirth
            FROM Students
            WHERE StudentId = @StudentId";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapStudent(reader);
        return null;
    }

    public async Task<Student?> GetStudentByRollAsync(string rollNumber, string className, string section)
    {
        const string sql = @"
            SELECT StudentId, StudentName, RollNumber, ClassName, Section,
                   MotherName, FatherName, DateOfBirth
            FROM Students
            WHERE RollNumber = @RollNumber AND ClassName = @ClassName AND Section = @Section";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RollNumber", rollNumber);
        cmd.Parameters.AddWithValue("@ClassName", className);
        cmd.Parameters.AddWithValue("@Section", section);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapStudent(reader);
        return null;
    }

    public async Task<List<Student>> GetStudentsByClassAsync(string className, string section)
    {
        var students = new List<Student>();
        const string sql = @"
            SELECT StudentId, StudentName, RollNumber, ClassName, Section,
                   MotherName, FatherName, DateOfBirth
            FROM Students
            WHERE ClassName = @ClassName AND Section = @Section
            ORDER BY RollNumber";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClassName", className);
        cmd.Parameters.AddWithValue("@Section", section);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            students.Add(MapStudent(reader));
        return students;
    }

    public async Task<List<SubjectMark>> GetMarksAsync(int studentId, string term, string academicYear)
    {
        var marks = new List<SubjectMark>();
        const string sql = @"
            SELECT s.SubjectName, s.SubjectCode, m.MaxMarks, m.MarksObtained, m.Grade, m.Remarks
            FROM Marks m
            JOIN Subjects s ON m.SubjectId = s.SubjectId
            WHERE m.StudentId = @StudentId AND m.Term = @Term AND m.AcademicYear = @AcademicYear
            ORDER BY s.SortOrder, s.SubjectName";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@Term", term);
        cmd.Parameters.AddWithValue("@AcademicYear", academicYear);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            marks.Add(new SubjectMark
            {
                SubjectName = reader.GetString(0),
                SubjectCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                MaxMarks = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                MarksObtained = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4),
                Remarks = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return marks;
    }

    public async Task<(int workingDays, int present)> GetAttendanceAsync(int studentId, string term, string academicYear)
    {
        const string sql = @"
            SELECT TotalWorkingDays, DaysPresent
            FROM Attendance
            WHERE StudentId = @StudentId AND Term = @Term AND AcademicYear = @AcademicYear";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@Term", term);
        cmd.Parameters.AddWithValue("@AcademicYear", academicYear);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return (reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1));
        return (0, 0);
    }

    public async Task<ReportCardViewModel?> BuildReportCardAsync(int studentId, string term, string academicYear)
    {
        var student = await GetStudentAsync(studentId);
        if (student == null) return null;

        var marks = await GetMarksAsync(studentId, term, academicYear);
        var (workingDays, present) = await GetAttendanceAsync(studentId, term, academicYear);

        student.TotalWorkingDays = workingDays;
        student.DaysPresent = present;

        var vm = new ReportCardViewModel
        {
            Student = student,
            Subjects = marks,
            Term = term,
            AcademicYear = academicYear,
            SchoolName = _config["AppSettings:SchoolName"] ?? "BBNV School",
            SchoolAddress = _config["AppSettings:SchoolAddress"] ?? string.Empty
        };

        vm.OverallGrade = CalculateGrade(vm.Percentage);
        vm.Result = marks.Any(m => m.MarksObtained < 33) ? "FAIL" : "PASS";

        return vm;
    }

    private static string? CalculateGrade(decimal? percentage)
    {
        if (percentage == null) return null;
        return percentage switch
        {
            >= 91 => "A+",
            >= 81 => "A",
            >= 71 => "B+",
            >= 61 => "B",
            >= 51 => "C",
            >= 41 => "D",
            _ => "F"
        };
    }

    private static Student MapStudent(SqlDataReader r) => new()
    {
        StudentId = r.GetInt32(0),
        StudentName = r.GetString(1),
        RollNumber = r.GetString(2),
        ClassName = r.GetString(3),
        Section = r.GetString(4),
        MotherName = r.IsDBNull(5) ? null : r.GetString(5),
        FatherName = r.IsDBNull(6) ? null : r.GetString(6),
        DateOfBirth = r.IsDBNull(7) ? null : r.GetDateTime(7)
    };
}
