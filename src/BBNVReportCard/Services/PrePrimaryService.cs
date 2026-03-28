using BBNVReportCard.Models;
using Microsoft.Data.SqlClient;

namespace BBNVReportCard.Services;

public class PrePrimaryService
{
    private readonly string _connectionString;

    // Maps DB subject name → display label (as seen in the PDF)
    private static readonly Dictionary<string, string> DisplayNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["English"]              = "ENGLISH (SPECIAL)",
            ["Hindi"]                = "HINDI (GENERAL)",
            ["Mathmatics"]           = "MATHS",
            ["Environmental science"]= "E.V.S.",
            ["COMPUTER"]             = "COMPUTER",
            ["G.K. / MORAL SCIENCE"] = "G.K. / MORAL SCIENCE",
            ["DRAWING"]              = "DRAWING",
            ["CLEANLINESS"]          = "CLEANLINESS",
        };

    // Subjects that show GRADE only (no numeric marks in the marks table)
    private static readonly HashSet<string> GradeOnlySubjects =
        new(StringComparer.OrdinalIgnoreCase) { "DRAWING", "CLEANLINESS" };

    // Display order: academic subjects first, then grade-only
    private static readonly List<string> SubjectOrder = new()
    {
        "English", "Hindi", "Mathmatics", "Environmental science",
        "COMPUTER", "G.K. / MORAL SCIENCE",
        "DRAWING", "CLEANLINESS"
    };

    public PrePrimaryService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // ── Main entry point ─────────────────────────────────────────────────────

    public async Task<PrePrimaryReportCardViewModel?> BuildAsync(int studentId, string academicYear)
    {
        var rows = await FetchRowsAsync(studentId, academicYear);
        if (rows.Count == 0) return null;

        var first = rows[0];

        var vm = new PrePrimaryReportCardViewModel
        {
            StudentId   = first.StudentId,
            RollNo      = first.RollNo,
            StudentName = first.StudentName,
            ClassName   = first.Class,
            AcademicYear= first.Session,
            AdmissionNo = first.AdmissionNo,
            DOB         = first.DOB,
            BloodGroup  = first.BloodGroup,
            FatherName  = first.FatherName,
            MotherName  = first.MotherName,
            Address     = first.Address,
            Caste       = first.Caste,
            Aadhar      = first.Aadhar,
            SSSMID      = first.SSSMID,

            // Attendance (same on every row – use first)
            QAttendance  = FormatAttendance(first.Attendance,    first.TotalDays),
            HYAttendance = FormatAttendance(first.HlfAttendance, first.HlfTotal),
            AAttendance  = FormatAttendance(first.YrAttendance,  first.YrTotal),
        };

        // Build subject list in defined order
        var bySubject = rows.ToDictionary(r => r.Subject, StringComparer.OrdinalIgnoreCase);

        foreach (var key in SubjectOrder)
        {
            if (!bySubject.TryGetValue(key, out var row)) continue;

            bool gradeOnly = GradeOnlySubjects.Contains(key);
            var subject = new PrePrimarySubject
            {
                SubjectKey  = key,
                DisplayName = DisplayNames.TryGetValue(key, out var dn) ? dn : key.ToUpper(),
                IsGradeOnly = gradeOnly,
                MaxMarks    = 50m,
            };

            if (gradeOnly)
            {
                subject.QGrade  = ResolveGrade(row.Quarterly);
                subject.HYGrade = ResolveGrade(row.HalfYearly);
                subject.AGrade  = ResolveGrade(row.FinalExam);
            }
            else
            {
                var (qm, qf, qp)   = ParseMark(row.Quarterly);
                var (hym, hyf, hyp)= ParseMark(row.HalfYearly);
                var (am, af, ap)   = ParseMark(row.FinalExam);

                subject.QMarks    = qm;  subject.QFailed  = qf;  subject.QPromoted  = qp;
                subject.HYMarks   = hym; subject.HYFailed = hyf; subject.HYPromoted = hyp;
                subject.AMarks    = am;  subject.AFailed  = af;  subject.APromoted  = ap;

                vm.AcademicSubjects.Add(subject);
                continue;
            }

            vm.GradeOnlySubjects.Add(subject);
        }

        // Remarks (calculated from percentage if not in DB)
        vm.QuarterlyRemark  = string.IsNullOrWhiteSpace(first.Remarks)
            ? PrePrimaryReportCardViewModel.GetRemarks(vm.QPercentage)
            : first.Remarks;
        vm.HalfYearlyRemark = PrePrimaryReportCardViewModel.GetRemarks(vm.HYPercentage);
        vm.AnnualRemark     = PrePrimaryReportCardViewModel.GetRemarks(vm.APercentage);

        return vm;
    }

    // ── Overload: lookup by roll number ──────────────────────────────────────

    public async Task<PrePrimaryReportCardViewModel?> BuildByRollAsync(string rollNo, string academicYear)
    {
        var studentId = await GetStudentIdByRollAsync(rollNo, academicYear);
        if (studentId == null) return null;
        return await BuildAsync(studentId.Value, academicYear);
    }

    // ── SQL fetching ──────────────────────────────────────────────────────────

    private async Task<List<RawPrePrimaryRow>> FetchRowsAsync(int studentId, string academicYear)
    {
        const string sql = @"
            SELECT RollNo, StudentId, StudentName, Class, Session, Subject,
                   FINALEXAM, HALFYEARLY, QUARTERLY,
                   AdmissionNo, DOB, BloodGroup, Address,
                   FATHERNAME, MOTHERNAME, AADHAR, SSSMID, Caste,
                   Attendance, TotalDays,
                   HLF_TOTAL, HLF_ATTENDANCE,
                   YR_TOTAL,  YR_ATTENDANCE,
                   REMARKS
            FROM   View_ReportCard_Pre_Primary
            WHERE  StudentId = @StudentId
              AND  Session   = @Session";

        var rows = new List<RawPrePrimaryRow>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@Session",   academicYear);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) rows.Add(MapRow(r));
        return rows;
    }

    private async Task<int?> GetStudentIdByRollAsync(string rollNo, string academicYear)
    {
        const string sql = @"
            SELECT TOP 1 StudentId
            FROM   View_ReportCard_Pre_Primary
            WHERE  RollNo  = @RollNo
              AND  Session = @Session";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RollNo",  rollNo);
        cmd.Parameters.AddWithValue("@Session", academicYear);
        var result = await cmd.ExecuteScalarAsync();
        return result is int i ? i : null;
    }

    // Fetch all students in a class for class-list view
    public async Task<List<(int StudentId, string RollNo, string Name)>> GetClassStudentsAsync(
        string className, string academicYear)
    {
        const string sql = @"
            SELECT DISTINCT StudentId, RollNo, StudentName
            FROM   View_ReportCard_Pre_Primary
            WHERE  Class   = @Class
              AND  Session = @Session
            ORDER BY RollNo";

        var list = new List<(int, string, string)>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Class",   className);
        cmd.Parameters.AddWithValue("@Session", academicYear);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((r.GetInt32(0), r.GetString(1), r.GetString(2)));
        return list;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RawPrePrimaryRow MapRow(SqlDataReader r) => new()
    {
        RollNo        = Str(r, 0),
        StudentId     = r.IsDBNull(1) ? 0 : r.GetInt32(1),
        StudentName   = Str(r, 2),
        Class         = Str(r, 3),
        Session       = Str(r, 4),
        Subject       = Str(r, 5),
        FinalExam     = Str(r, 6),
        HalfYearly    = Str(r, 7),
        Quarterly     = Str(r, 8),
        AdmissionNo   = Str(r, 9),
        DOB           = Str(r, 10),
        BloodGroup    = Str(r, 11),
        Address       = Str(r, 12),
        FatherName    = Str(r, 13),
        MotherName    = Str(r, 14),
        Aadhar        = Str(r, 15),
        SSSMID        = Str(r, 16),
        Caste         = Str(r, 17),
        Attendance    = NullableInt(r, 18),
        TotalDays     = NullableInt(r, 19),
        HlfTotal      = NullableInt(r, 20),
        HlfAttendance = NullableInt(r, 21),
        YrTotal       = NullableInt(r, 22),
        YrAttendance  = NullableInt(r, 23),
        Remarks       = Str(r, 24),
    };

    // Parse mark string: strips * (fail) and + (promoted) markers
    // Returns (numericValue, isFailed, isPromoted)
    private static (decimal? marks, bool failed, bool promoted) ParseMark(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("Ab", StringComparison.OrdinalIgnoreCase))
            return (null, false, false);

        bool failed   = raw.Contains('*');
        bool promoted = raw.Contains('+');

        var cleaned = raw.Replace("*", "");
        if (promoted)
        {
            var idx = cleaned.IndexOf('+');
            if (idx >= 0) cleaned = cleaned[..idx];
        }

        return decimal.TryParse(cleaned.Trim(), out var v) ? (v, failed, promoted) : (null, failed, promoted);
    }

    // Resolve grade: if numeric → convert via lookup; otherwise use as-is
    private static string ResolveGrade(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "-";
        if (raw.Equals("Ab", StringComparison.OrdinalIgnoreCase)) return "Ab";
        if (decimal.TryParse(raw, out var m))
            return PrePrimaryReportCardViewModel.MarksToGrade(m);
        return raw.ToUpper();
    }

    private static string FormatAttendance(int? present, int? total)
        => (present.HasValue && total.HasValue) ? $"{present}/{total}" : string.Empty;

    private static string Str(SqlDataReader r, int i)
        => r.IsDBNull(i) ? string.Empty : r.GetValue(i).ToString() ?? string.Empty;

    private static int? NullableInt(SqlDataReader r, int i)
        => r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));

    // ── Internal DTO ──────────────────────────────────────────────────────────

    private class RawPrePrimaryRow
    {
        public string RollNo      { get; set; } = string.Empty;
        public int    StudentId   { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Class       { get; set; } = string.Empty;
        public string Session     { get; set; } = string.Empty;
        public string Subject     { get; set; } = string.Empty;
        public string FinalExam   { get; set; } = string.Empty;
        public string HalfYearly  { get; set; } = string.Empty;
        public string Quarterly   { get; set; } = string.Empty;
        public string? AdmissionNo{ get; set; }
        public string? DOB        { get; set; }
        public string? BloodGroup { get; set; }
        public string? Address    { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? Aadhar     { get; set; }
        public string? SSSMID     { get; set; }
        public string? Caste      { get; set; }
        public int?   Attendance    { get; set; }
        public int?   TotalDays     { get; set; }
        public int?   HlfTotal      { get; set; }
        public int?   HlfAttendance { get; set; }
        public int?   YrTotal       { get; set; }
        public int?   YrAttendance  { get; set; }
        public string? Remarks      { get; set; }
    }
}
