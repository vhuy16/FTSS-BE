using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class SolutionProduct
{
    public Guid Id { get; set; }

    public Guid SolutionId { get; set; }

    public Guid ProductId { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Solution Solution { get; set; } = null!;
}
