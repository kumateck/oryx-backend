using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Alerts;

public class CreateAlertRequest
{
    public string Title { get; set; }
    public string ModelType { get; set; }
    public AlertType AlertType { get; set; }
    public TimeSpan TimeFrame { get; set; }
    public bool IsDisabled { get; set; }
}

public class Alert : BaseEntity
{
    [StringLength(255)]
    public string Title { get; set; }
    [StringLength(255)]
    public string ModelType { get; set; }
    public AlertType AlertType { get; set; }
    public TimeSpan TimeFrame { get; set; }
    public bool IsDisabled { get; set; }
}

public enum AlertType
{
    Email, 
    InApp,
    Combination
}


public class AlertDto : BaseDto
{
    public string Title { get; set; }
    public string ModelType { get; set; }
    public AlertType AlertType { get; set; }
    public TimeSpan TimeFrame { get; set; }
    public bool IsDisabled { get; set; }
}