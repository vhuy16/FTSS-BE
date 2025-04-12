using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;
[Index(nameof(ProductName), Name = "IX_Product_ProductName")]
[Index(nameof(SubCategoryId), Name = "IX_Product_SubCategoryId")]
[Index(nameof(Price), Name = "IX_Product_Price")]
[Index(nameof(Status), Name = "IX_Product_Status")]
[Index(nameof(CreateDate), Name = "IX_Product_CreateDate")]
[Index(nameof(IsDelete), Name = "IX_Product_IsDelete")]
public partial class Product
{
    public Guid Id { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Size { get; set; }

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int? Quantity { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? Status { get; set; }

    public Guid? SubCategoryId { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<SetupPackageDetail> SetupPackageDetails { get; set; } = new List<SetupPackageDetail>();

    public virtual ICollection<SolutionProduct> SolutionProducts { get; set; } = new List<SolutionProduct>();

    public virtual SubCategory? SubCategory { get; set; }
}
