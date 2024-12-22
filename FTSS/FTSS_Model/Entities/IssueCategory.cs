using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class IssueCategory
{
    public Guid Id { get; set; }

    public string IssueCategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
