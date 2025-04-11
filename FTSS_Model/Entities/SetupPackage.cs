using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;
[Index(nameof(Userid), IsUnique = false, Name = "idx_setuppackage_userid")]
[Index(nameof(Status), IsUnique = false, Name = "idx_setuppackage_status")]
[Index(nameof(CreateDate), IsUnique = false, Name = "idx_setuppackage_createdate")]
[Index(nameof(ModifyDate), IsUnique = false, Name = "idx_setuppackage_modifydate")]
[Index(nameof(IsDelete), IsUnique = false, Name = "idx_setuppackage_isdelete")]
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

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<SetupPackageDetail> SetupPackageDetails { get; set; } = new List<SetupPackageDetail>();

    public virtual User? User { get; set; }
}
