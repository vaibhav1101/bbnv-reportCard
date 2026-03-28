namespace BBNVReportCard.Models;

// ─── Per-subject data (one row in the marks table) ───────────────────────────

public class PrePrimarySubject
{
    public string SubjectKey { get; set; } = string.Empty;   // raw DB name
    public string DisplayName { get; set; } = string.Empty;  // display label
    public bool IsGradeOnly { get; set; }                     // Drawing, Cleanliness

    // Academic marks (null = absent/not entered, string for * or + markers)
    public decimal? QMarks { get; set; }
    public bool QFailed { get; set; }      // * marker
    public bool QPromoted { get; set; }    // + marker

    public decimal? HYMarks { get; set; }
    public bool HYFailed { get; set; }
    public bool HYPromoted { get; set; }

    public decimal? AMarks { get; set; }
    public bool AFailed { get; set; }
    public bool APromoted { get; set; }

    public decimal MaxMarks { get; set; } = 50m;

    // Weighted scores (10% / 10% / 80% split)
    public decimal QWeighted  => IsGradeOnly ? 0m : Math.Round((QMarks  ?? 0m) * 0.10m, 2);
    public decimal HYWeighted => IsGradeOnly ? 0m : Math.Round((HYMarks ?? 0m) * 0.10m, 2);
    public decimal AWeighted  => IsGradeOnly ? 0m : Math.Round((AMarks  ?? 0m) * 0.80m, 2);
    public decimal Total      => QWeighted + HYWeighted + AWeighted;

    // Grade-only subjects
    public string? QGrade  { get; set; }
    public string? HYGrade { get; set; }
    public string? AGrade  { get; set; }

    // Display helpers – shows mark with * or + if applicable
    public string QDisplay  => FormatMark(QMarks,  QFailed,  QPromoted);
    public string HYDisplay => FormatMark(HYMarks, HYFailed, HYPromoted);
    public string ADisplay  => FormatMark(AMarks,  AFailed,  APromoted);

    private static string FormatMark(decimal? m, bool failed, bool promoted)
    {
        if (m == null) return "Ab";
        var s = m.Value.ToString("0");
        if (failed)   s = s + "*";
        if (promoted) s = s + "+";
        return s;
    }
}

// ─── Full Pre-Primary report card view model ──────────────────────────────────

public class PrePrimaryReportCardViewModel
{
    // ── School constants ──────────────────────────────────────────────────────
    public string SchoolName    { get; set; } = "BRAHMRISHI BAWRA NARMADA VIDYAPEETH SENIOR SEC. SCHOOL";
    public string SchoolLine2   { get; set; } = "RECOGNIZED BY GOVT. OF M.P. (SCHOOL CODE - 712287)";
    public string SchoolAddress { get; set; } = "Vanvasi Chetna Aashram, New Station Road, Gwarighat, Jabalpur (M.P.)";
    public string SchoolEmail   { get; set; } = "bbnvidyapeeth99@gmail.com";
    public string RegNo         { get; set; } = "J.J.- 4751";
    public string SchoolCode    { get; set; } = "712287";
    public string DiseCode      { get; set; } = "23390222204";

    // ── Student info ──────────────────────────────────────────────────────────
    public int     StudentId    { get; set; }
    public string  RollNo       { get; set; } = string.Empty;
    public string  StudentName  { get; set; } = string.Empty;
    public string  ClassName    { get; set; } = string.Empty;
    public string  AcademicYear { get; set; } = string.Empty;
    public string? AdmissionNo  { get; set; }
    public string? DOB          { get; set; }
    public string? BloodGroup   { get; set; }
    public string? FatherName   { get; set; }
    public string? MotherName   { get; set; }
    public string? Address      { get; set; }
    public string? Caste        { get; set; }
    public string? Aadhar       { get; set; }
    public string? SSSMID       { get; set; }

    // ── Subjects ──────────────────────────────────────────────────────────────
    public List<PrePrimarySubject> AcademicSubjects  { get; set; } = new();
    public List<PrePrimarySubject> GradeOnlySubjects { get; set; } = new();

    // ── Attendance ────────────────────────────────────────────────────────────
    public string QAttendance  { get; set; } = string.Empty;  // "20/50"
    public string HYAttendance { get; set; } = string.Empty;  // "24/55"
    public string AAttendance  { get; set; } = string.Empty;  // "77/110"

    // ── Remarks (right summary panel) ─────────────────────────────────────────
    public string? QuarterlyRemark  { get; set; }
    public string? HalfYearlyRemark { get; set; }
    public string? AnnualRemark     { get; set; }

    // ── Rank ─────────────────────────────────────────────────────────────────
    public int? RankInClass { get; set; }

    // ── Calculated totals ─────────────────────────────────────────────────────
    public decimal QTotalMax  => AcademicSubjects.Sum(s => s.MaxMarks);
    public decimal QTotalObt  => AcademicSubjects.Sum(s => s.QMarks ?? 0m);
    public decimal HYTotalMax => AcademicSubjects.Sum(s => s.MaxMarks);
    public decimal HYTotalObt => AcademicSubjects.Sum(s => s.HYMarks ?? 0m);
    public decimal ATotalMax  => AcademicSubjects.Sum(s => s.MaxMarks);
    public decimal ATotalObt  => AcademicSubjects.Sum(s => s.AMarks  ?? 0m);

    public decimal QWeightedTotal  => AcademicSubjects.Sum(s => s.QWeighted);
    public decimal HYWeightedTotal => AcademicSubjects.Sum(s => s.HYWeighted);
    public decimal AWeightedTotal  => AcademicSubjects.Sum(s => s.AWeighted);
    public decimal GrandTotal      => AcademicSubjects.Sum(s => s.Total);

    // ── Percentages ───────────────────────────────────────────────────────────
    public decimal? QPercentage  => QTotalMax  > 0 ? Math.Round(QTotalObt  / QTotalMax  * 100, 2) : null;
    public decimal? HYPercentage => HYTotalMax > 0 ? Math.Round(HYTotalObt / HYTotalMax * 100, 2) : null;
    public decimal? APercentage  => ATotalMax  > 0 ? Math.Round(ATotalObt  / ATotalMax  * 100, 2) : null;

    // Total Average % = GrandTotal / (QMax×10% + HYMax×10% + AMax×80%) × 100
    //                  = GrandTotal / AMax × 100  (since weights sum to same base)
    public decimal? TotalPercentage => ATotalMax > 0
        ? Math.Round(GrandTotal / ATotalMax * 100, 2) : null;

    // ── Division ──────────────────────────────────────────────────────────────
    public string QDivision  => GetDivision(QPercentage);
    public string HYDivision => GetDivision(HYPercentage);
    public string ADivision  => GetDivision(APercentage);

    // ── Pass / Fail ───────────────────────────────────────────────────────────
    public string QResult  => GetResult(QPercentage,  AcademicSubjects.Select(s => s.QMarks));
    public string HYResult => GetResult(HYPercentage, AcademicSubjects.Select(s => s.HYMarks));
    public string AResult  => GetResult(APercentage,  AcademicSubjects.Select(s => s.AMarks));

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string GetDivision(decimal? pct) => pct switch
    {
        null      => string.Empty,
        >= 60     => "First",
        >= 45     => "Second",
        >= 35     => "Third",
        _         => "Fail"
    };

    private static string GetResult(decimal? pct, IEnumerable<decimal?> marks)
    {
        if (pct == null) return string.Empty;
        if (pct < 33)    return "Failed";
        // Any subject with marks ≤ 16 (< 33% of 50) = Failed
        if (marks.Any(m => m.HasValue && m.Value <= 16)) return "Failed";
        return "Passed";
    }

    public static string GetRemarks(decimal? pct) => pct switch
    {
        null  => string.Empty,
        < 36  => "Unsatisfactory",
        < 50  => "Need Hard Work",
        < 60  => "Satisfactory",
        < 70  => "Good",
        < 80  => "Very Good",
        _     => "Excellent"
    };

    public static string MarksToGrade(decimal marks) => marks switch
    {
        < 8  => "D",
        < 15 => "C",
        < 20 => "B",
        _    => "A"
    };
}
