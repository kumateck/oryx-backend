using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DOMAIN.Entities.AttendanceRecords;

public class CreateAttendanceRequest
{
    [Required] public IFormFile Attendance { get; set; }
}

public class FileExportResult
{
    public byte[] FileBytes { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
}

public class FileFormat
{
    public string FileType { get; set; }
}
