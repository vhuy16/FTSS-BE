using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class SubCategory
{
    public Guid Id { get; set; }

    public string SubCategoryName { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public virtual Category Category { get; set; } = null!;
}
