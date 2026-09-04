using DOMAIN.Entities.Base;
using DOMAIN.Entities.ShiftSchedules;

namespace DOMAIN.Entities.ShiftTypes;

public class ShiftType : BaseEntity
{
    public string ShiftName { get; set; }

    public RotationType RotationType { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public List<ShiftSchedule> ShiftSchedules { get; set; }

    public List<DayOfWeek> ApplicableDays { get; set; }

}

public enum RotationType
{
    Fixed,
    Rotational
}