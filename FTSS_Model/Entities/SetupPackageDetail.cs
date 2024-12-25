using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class SetupPackageDetail
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid SetupPackageId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual SetupPackage SetupPackage { get; set; } = null!;
}
