using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Mission
{
    public Guid Id { get; set; }

    public string MissionName { get; set; } = null!;

    public string? MissionDescription { get; set; }

    public string? Status { get; set; }

    public bool? IsDelete { get; set; }

    public Guid? Userid { get; set; }

    public DateTime? MissionSchedule { get; set; }

    public Guid? BookingId { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid? OrderId { get; set; }

    public string? CancelReason { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual ICollection<MissionImage> MissionImages { get; set; } = new List<MissionImage>();

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
