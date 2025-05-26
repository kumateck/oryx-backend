using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using Microsoft.AspNetCore.Http;

namespace DOMAIN.Entities.AttendanceRecords;

public class CreateAttendanceRequest : BaseEntity
{
    [Required]
    public IFormFile Attendance { get; set; }
}
