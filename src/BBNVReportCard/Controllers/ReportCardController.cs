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

        return View(vm);
    }

    [HttpGet("ReportCard/Pdf/{studentId:int}")]
    public async Task<IActionResult> Pdf(int studentId, string term, string academicYear)
    {
        var vm = await _service.BuildReportCardAsync(studentId, term, academicYear);
        if (vm == null) return NotFound();

        return new ViewAsPdf("Generate", vm)
        {
            FileName = $"ReportCard_{vm.Student.RollNumber}_{term}_{academicYear}.pdf",
            PageSize = Rotativa.AspNetCore.Options.Size.A4,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
        };
    }

    [HttpGet]
    public async Task<IActionResult> ClassList(string className, string section, string term, string academicYear)
    {
        var students = await _service.GetStudentsByClassAsync(className, section);
        ViewBag.ClassName = className;
        ViewBag.Section = section;
        ViewBag.Term = term;
        ViewBag.AcademicYear = academicYear;
        return View(students);
    }
}
