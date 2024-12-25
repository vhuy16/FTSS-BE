using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Voucher
{
    public Guid Id { get; set; }

    public string VoucherCode { get; set; } = null!;

    public decimal? Price { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
