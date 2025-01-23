using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Voucher
{
    public Guid Id { get; set; }

    public string VoucherCode { get; set; } = null!;

    public decimal Discount { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? Status { get; set; }

    public int? Quantity { get; set; }

    public decimal? MaximumOrderValue { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? DiscountType { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
