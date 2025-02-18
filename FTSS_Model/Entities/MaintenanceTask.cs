using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class MaintenanceTask
{
    public Guid Id { get; set; }

    public Guid MaintenanceScheduleId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? TaskDescription { get; set; }

    public string? Status { get; set; }

    public bool? IsDelete { get; set; }

    public string? Address { get; set; }

    public Guid? Userid { get; set; }

    public virtual MaintenanceSchedule MaintenanceSchedule { get; set; } = null!;

    public virtual User? User { get; set; }
}
