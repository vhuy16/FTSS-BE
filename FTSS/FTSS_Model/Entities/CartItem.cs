using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class CartItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid CartId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public string? Status { get; set; }

    public bool? IsDelete { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
