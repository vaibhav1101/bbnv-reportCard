using BBNVReportCard.Models;
using BBNVReportCard.Services;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;

namespace BBNVReportCard.Controllers;

public class ReportCardController : Controller
{
    private readonly ReportCardService _service;
    private readonly IConfiguration _config;

    public ReportCardController(ReportCardService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ReportCardSearchModel
        {
            AcademicYear = _config["AppSettings:AcademicYear"] ?? "2025-2026",
            AvailableClasses = await _service.GetClassesAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(ReportCardSearchModel search)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        Student? student = null;

        if (search.StudentId.HasValue)
        {
            student = await _service.GetStudentAsync(search.StudentId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(search.RollNumber)
                 && !string.IsNullOrWhiteSpace(search.ClassName)
                 && !string.IsNullOrWhiteSpace(search.Section))
        {
            student = await _service.GetStudentByRollAsync(
                search.RollNumber, search.ClassName, search.Section);
        }

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

        // Resolve level & stream, then route to the matching format view
        var (level, stream) = SchoolLevelResolver.Resolve(student.ClassName, search.Stream);
        vm.Level = level;
        vm.Stream = stream;

        var viewPath = SchoolLevelResolver.ViewPath(level, stream);
        return View(viewPath, vm);
    }

    [HttpGet("ReportCard/Pdf/{studentId:int}")]
    public async Task<IActionResult> Pdf(int studentId, string term, string academicYear, string? stream = null)
    {
        var vm = await _service.BuildReportCardAsync(studentId, term, academicYear);
        if (vm == null) return NotFound();

        var (level, resolvedStream) = SchoolLevelResolver.Resolve(vm.Student.ClassName, stream);
        vm.Level = level;
        vm.Stream = resolvedStream;

        var viewPath = SchoolLevelResolver.ViewPath(level, resolvedStream);

        return new ViewAsPdf(viewPath, vm)
        {
            FileName = $"ReportCard_{vm.Student.RollNumber}_{term}_{academicYear}.pdf",
            PageSize = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
        };
    }

    [HttpGet]
    public async Task<IActionResult> ClassList(string className, string section, string term, string academicYear, string? stream = null)
    {
        var students = await _service.GetStudentsByClassAsync(className, section);
        ViewBag.ClassName = className;
        ViewBag.Section = section;
        ViewBag.Term = term;
        ViewBag.AcademicYear = academicYear;
        ViewBag.Stream = stream;
        return View(students);
    }
}
