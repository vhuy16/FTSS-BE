using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class SetupPackage
{
    public Guid Id { get; set; }

    public string SetupName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public Guid? Userid { get; set; }

    public string? Image { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<SetupPackageDetail> SetupPackageDetails { get; set; } = new List<SetupPackageDetail>();

    public virtual User? User { get; set; }
}
