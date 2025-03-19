using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class ServicePackage
{
    public Guid Id { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? Status { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
