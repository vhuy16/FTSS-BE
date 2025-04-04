using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Solution
{
    public Guid Id { get; set; }

    public string SolutionName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsDelete { get; set; }

    public Guid IssueId { get; set; }

    public virtual Issue Issue { get; set; } = null!;

    public virtual ICollection<SolutionProduct> SolutionProducts { get; set; } = new List<SolutionProduct>();
}
