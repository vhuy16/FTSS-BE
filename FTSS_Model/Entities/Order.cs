using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;
[Index(nameof(UserId), IsUnique = false, Name = "idx_order_userid")]
[Index(nameof(Status), IsUnique = false, Name = "idx_order_status")]
[Index(nameof(CreateDate), IsUnique = false, Name = "idx_order_createdate")]
[Index(nameof(ModifyDate), IsUnique = false, Name = "idx_order_modifydate")]
[Index(nameof(IsDelete), IsUnique = false, Name = "idx_order_isdelete")]
[Index(nameof(VoucherId), IsUnique = false, Name = "idx_order_voucherid")]
[Index(nameof(SetupPackageId), IsUnique = false, Name = "idx_order_setuppackageid")]
[Index(nameof(OrderCode), IsUnique = true, Name = "idx_order_ordercode")] // OrderCode thường là duy nhất
[Index(nameof(UserId), nameof(Status), IsUnique = false, Name = "idx_order_userid_status")] // Chỉ mục composite
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

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Mission> Missions { get; set; } = new List<Mission>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual SetupPackage? SetupPackage { get; set; }



    public virtual User? User { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
