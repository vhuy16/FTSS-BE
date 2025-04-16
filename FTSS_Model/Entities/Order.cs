using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Order
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public Guid? VoucherId { get; set; }

    public decimal? Shipcost { get; set; }

    public string? Address { get; set; }

    public Guid? SetupPackageId { get; set; }

    public string? PhoneNumber { get; set; }

    public string? RecipientName { get; set; }

    public bool? IsEligible { get; set; }

    public bool? IsAssigned { get; set; }

    public string? OrderCode { get; set; }

    public DateTime? InstallationDate { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Mission> Missions { get; set; } = new List<Mission>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual SetupPackage? SetupPackage { get; set; }

    public virtual User? User { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
