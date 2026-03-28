namespace BBNVReportCard.Models;

public enum SchoolLevel
{
    PrePrimary,       // Nursery, LKG, UKG
    Primary,          // Class 1 – 5
    Middle,           // Class 6 – 8
    Senior,           // Class 9 – 10
    SeniorSecondary   // Class 11 – 12 (stream-specific)
}

public enum Stream
{
    None,       // Pre-Primary through Senior
    Math,
    Bio,
    Commerce,
    Arts
}

public static class SchoolLevelResolver
{
    /// <summary>
    /// Derives the SchoolLevel and Stream from a class name string.
    /// Override this mapping to match your school's actual class naming convention.
    /// </summary>
    public static (SchoolLevel Level, Stream Stream) Resolve(string className, string? stream = null)
    {
        var cls = className.Trim().ToUpperInvariant();

        // Pre-Primary: Nursery, LKG, UKG, PP, KG
        if (cls is "NURSERY" or "LKG" or "UKG" or "PP" or "KG" or "PRE-PRIMARY" or "PREPRIMARY")
            return (SchoolLevel.PrePrimary, Stream.None);

        // Try to extract numeric part
        var number = ExtractNumber(cls);

        if (number is >= 1 and <= 5)
            return (SchoolLevel.Primary, Stream.None);

        if (number is >= 6 and <= 8)
            return (SchoolLevel.Middle, Stream.None);

        if (number is 9 or 10)
            return (SchoolLevel.Senior, Stream.None);

        if (number is 11 or 12)
        {
            var s = ResolveStream(stream);
            return (SchoolLevel.SeniorSecondary, s);
        }

        return (SchoolLevel.Primary, Stream.None); // default fallback
    }

    private static Stream ResolveStream(string? stream)
    {
        if (string.IsNullOrWhiteSpace(stream)) return Stream.None;
        return stream.Trim().ToUpperInvariant() switch
        {
            "MATH" or "MATHEMATICS" or "PCM" or "SCIENCE-MATH" => Stream.Math,
            "BIO" or "BIOLOGY" or "PCB" or "SCIENCE-BIO"       => Stream.Bio,
            "COMMERCE" or "COM"                                 => Stream.Commerce,
            "ARTS" or "HUMANITIES" or "ART"                     => Stream.Arts,
            _                                                   => Stream.None
        };
    }

    public static string ViewPath(SchoolLevel level, Stream stream) => level switch
    {
        SchoolLevel.PrePrimary      => "Formats/PrePrimary/ReportCard",
        SchoolLevel.Primary         => "Formats/Primary/ReportCard",
        SchoolLevel.Middle          => "Formats/Middle/ReportCard",
        SchoolLevel.Senior          => "Formats/Senior/ReportCard",
        SchoolLevel.SeniorSecondary => stream switch
        {
            Stream.Math     => "Formats/SeniorSecondary/Math/ReportCard",
            Stream.Bio      => "Formats/SeniorSecondary/Bio/ReportCard",
            Stream.Commerce => "Formats/SeniorSecondary/Commerce/ReportCard",
            Stream.Arts     => "Formats/SeniorSecondary/Arts/ReportCard",
            _               => "Formats/SeniorSecondary/Math/ReportCard"
        },
        _ => "Generate"
    };

    private static int? ExtractNumber(string cls)
    {
        var digits = new string(cls.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }
}
