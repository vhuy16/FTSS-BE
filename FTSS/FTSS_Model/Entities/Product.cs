using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Size { get; set; }

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? Model3Did { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual ICollection<IssueProduct> IssueProducts { get; set; } = new List<IssueProduct>();

    public virtual Model3D? Model3D { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<SetupPackageDetail> SetupPackageDetails { get; set; } = new List<SetupPackageDetail>();
}
