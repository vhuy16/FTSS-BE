using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Image
{
    public Guid Id { get; set; }

    public string LinkImage { get; set; } = null!;

    public Guid ProductId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public virtual Product Product { get; set; } = null!;
}
