using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;
[Index(nameof(UserId), IsUnique = false, Name = "idx_booking_userid")]
[Index(nameof(OrderId), IsUnique = false, Name = "idx_booking_orderid")]
[Index(nameof(Status), IsUnique = false, Name = "idx_booking_status")]
[Index(nameof(ScheduleDate), IsUnique = false, Name = "idx_booking_scheduledate")]
[Index(nameof(BookingCode), IsUnique = true, Name = "idx_booking_bookingcode")]
public partial class Booking
{
    public Guid Id { get; set; }

    public DateTime? ScheduleDate { get; set; }

    public string? Status { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public decimal? TotalPrice { get; set; }

    public Guid? UserId { get; set; }

    public Guid? OrderId { get; set; }

    public bool? IsAssigned { get; set; }

    public string? FullName { get; set; }

    public string? BookingCode { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<Mission> Missions { get; set; } = new List<Mission>();

    public virtual Order? Order { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? User { get; set; }
}
