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
    
  
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
}
