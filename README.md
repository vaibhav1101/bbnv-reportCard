# BBNV Report Card

ASP.NET Core MVC application to generate school report cards connected to MS SQL Server.

## Features
- Search student by Roll Number or Student ID
- View report card as styled HTML in browser
- Print-ready layout via browser print
- Download as PDF (via Rotativa/wkhtmltopdf)
- MS SQL Server backend

## Getting Started (Codespace)

1. Open this repo in GitHub Codespaces — the `.devcontainer` config installs .NET 8 and SQL tools automatically.

2. Update the connection string in `src/BBNVReportCard/appsettings.json`:
   ```json
   "DefaultConnection": "Server=<YOUR_SERVER>;Database=<DB>;User Id=<USER>;Password=<PASS>;TrustServerCertificate=True;"
   ```

3. Run the app:
   ```bash
   cd src/BBNVReportCard
   dotnet run
   ```

4. Open the forwarded port 5000 in your browser.

## Database Schema (expected)

```sql
-- Students table
CREATE TABLE Students (
    StudentId INT PRIMARY KEY IDENTITY,
    StudentName NVARCHAR(150) NOT NULL,
    RollNumber NVARCHAR(20) NOT NULL,
    ClassName NVARCHAR(20) NOT NULL,
    Section NVARCHAR(5) NOT NULL,
    MotherName NVARCHAR(150),
    FatherName NVARCHAR(150),
    DateOfBirth DATE
);

-- Subjects table
CREATE TABLE Subjects (
    SubjectId INT PRIMARY KEY IDENTITY,
    SubjectName NVARCHAR(100) NOT NULL,
    SubjectCode NVARCHAR(20),
    SortOrder INT DEFAULT 0
);

-- Marks table
CREATE TABLE Marks (
    MarkId INT PRIMARY KEY IDENTITY,
    StudentId INT FOREIGN KEY REFERENCES Students(StudentId),
    SubjectId INT FOREIGN KEY REFERENCES Subjects(SubjectId),
    Term NVARCHAR(30) NOT NULL,
    AcademicYear NVARCHAR(10) NOT NULL,
    MaxMarks DECIMAL(5,2),
    MarksObtained DECIMAL(5,2),
    Grade NVARCHAR(5),
    Remarks NVARCHAR(200)
);

-- Attendance table
CREATE TABLE Attendance (
    AttendanceId INT PRIMARY KEY IDENTITY,
    StudentId INT FOREIGN KEY REFERENCES Students(StudentId),
    Term NVARCHAR(30) NOT NULL,
    AcademicYear NVARCHAR(10) NOT NULL,
    TotalWorkingDays INT,
    DaysPresent INT
);
```

> **Note:** The schema above is a default template. Update `ReportCardService.cs` queries to match your actual database schema.

## Project Structure

```
src/BBNVReportCard/
├── Controllers/ReportCardController.cs  - HTTP endpoints
├── Models/                              - Data models & view models
├── Services/ReportCardService.cs        - SQL Server data layer
├── Views/ReportCard/                    - Razor views (Index, Generate)
├── wwwroot/css/report-card.css          - Styles + print CSS
├── appsettings.json                     - Config (connection string)
└── Program.cs                           - App startup
```
