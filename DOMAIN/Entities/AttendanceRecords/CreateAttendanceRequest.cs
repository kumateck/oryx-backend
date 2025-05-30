using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using Microsoft.AspNetCore.Http;

namespace DOMAIN.Entities.AttendanceRecords;

public class CreateAttendanceRequest
{
    [Required] public IFormFile Attendance { get; set; }
}
