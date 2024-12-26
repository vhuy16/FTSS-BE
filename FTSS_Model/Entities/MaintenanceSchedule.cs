using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class MaintenanceSchedule
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime? ScheduleDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    public virtual User User { get; set; } = null!;
}
