using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class IssueProduct
{
    public Guid Id { get; set; }

    public Guid IssueId { get; set; }

    public Guid ProductId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public virtual Issue Issue { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
