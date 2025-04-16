using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Category
{
    public Guid Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string LinkImage { get; set; } = null!;

    public bool? IsFishTank { get; set; }

    public bool? IsObligatory { get; set; }

    public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
}
