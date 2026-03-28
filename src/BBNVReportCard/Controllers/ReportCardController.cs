using BBNVReportCard.Models;
using BBNVReportCard.Services;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;

namespace BBNVReportCard.Controllers;

public class ReportCardController : Controller
{
    private readonly ReportCardService    _service;
    private readonly PrePrimaryService    _prePrimaryService;
    private readonly IConfiguration       _config;

    public ReportCardController(
        ReportCardService service,
        PrePrimaryService prePrimaryService,
        IConfiguration config)
    {
        _service           = service;
        _prePrimaryService = prePrimaryService;
        _config            = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ReportCardSearchModel
        {
            AcademicYear     = _config["AppSettings:AcademicYear"] ?? "2025-2026",
            AvailableClasses = await _service.GetClassesAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(ReportCardSearchModel search)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        // Resolve level first (we need the class name from the search form for Pre-Primary)
        var className = search.ClassName ?? string.Empty;
        var (level, stream) = SchoolLevelResolver.Resolve(className, search.Stream);

        // ── Pre-Primary: uses its own service and view model ─────────────────
        if (level == SchoolLevel.PrePrimary)
        {
            PrePrimaryReportCardViewModel? ppVm = null;

            if (search.StudentId.HasValue)
            {
                ppVm = await _prePrimaryService.BuildAsync(
                    search.StudentId.Value, search.AcademicYear);
            }
            else if (!string.IsNullOrWhiteSpace(search.RollNumber))
            {
                ppVm = await _prePrimaryService.BuildByRollAsync(
                    search.RollNumber, search.AcademicYear);
            }

            if (ppVm == null)
            {
                TempData["Error"] = "Student not found or no data for the selected year.";
                return RedirectToAction(nameof(Index));
            }

            return View("Formats/PrePrimary/ReportCard", ppVm);
        }

        // ── All other levels: generic service ─────────────────────────────────
        Student? student = null;

        if (search.StudentId.HasValue)
            student = await _service.GetStudentAsync(search.StudentId.Value);
        else if (!string.IsNullOrWhiteSpace(search.RollNumber)
                 && !string.IsNullOrWhiteSpace(search.ClassName)
                 && !string.IsNullOrWhiteSpace(search.Section))
            student = await _service.GetStudentByRollAsync(
                search.RollNumber, search.ClassName, search.Section);

        if (student == null)
        {
            TempData["Error"] = "Student not found. Please check the details and try again.";
            return RedirectToAction(nameof(Index));
        }

        var vm = await _service.BuildReportCardAsync(
            student.StudentId, search.Term, search.AcademicYear);

        if (vm == null)
        {
            TempData["Error"] = "Report card data not available for the selected term.";
            return RedirectToAction(nameof(Index));
        }

        vm.Level  = level;
        vm.Stream = stream;
        return View(SchoolLevelResolver.ViewPath(level, stream), vm);
    }

    // ── PDF: Pre-Primary ─────────────────────────────────────────────────────

    [HttpGet("ReportCard/PdfPrePrimary/{studentId:int}")]
    public async Task<IActionResult> PdfPrePrimary(int studentId, string academicYear)
    {
        var vm = await _prePrimaryService.BuildAsync(studentId, academicYear);
        if (vm == null) return NotFound();

        return new ViewAsPdf("Formats/PrePrimary/ReportCard", vm)
        {
            FileName        = $"ReportCard_{vm.RollNo}_{academicYear}.pdf",
            PageSize        = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageMargins     = new Rotativa.AspNetCore.Options.Margins(8, 10, 8, 10)
        };
    }

    // ── PDF: other levels ────────────────────────────────────────────────────

    [HttpGet("ReportCard/Pdf/{studentId:int}")]
    public async Task<IActionResult> Pdf(int studentId, string term, string academicYear, string? stream = null)
    {
        var vm = await _service.BuildReportCardAsync(studentId, term, academicYear);
        if (vm == null) return NotFound();

        var (level, resolvedStream) = SchoolLevelResolver.Resolve(vm.Student.ClassName, stream);
        vm.Level  = level;
        vm.Stream = resolvedStream;

        return new ViewAsPdf(SchoolLevelResolver.ViewPath(level, resolvedStream), vm)
        {
            FileName        = $"ReportCard_{vm.Student.RollNumber}_{term}_{academicYear}.pdf",
            PageSize        = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageMargins     = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
        };
    }

    [HttpGet]
    public async Task<IActionResult> ClassList(
        string className, string section, string term, string academicYear, string? stream = null)
    {
        var students = await _service.GetStudentsByClassAsync(className, section);
        ViewBag.ClassName   = className;
        ViewBag.Section     = section;
        ViewBag.Term        = term;
        ViewBag.AcademicYear= academicYear;
        ViewBag.Stream      = stream;
        return View(students);
    }
}
