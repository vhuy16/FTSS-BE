using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Issue
{
    public Guid Id { get; set; }

    public string? IssueName { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }
    public DateTime? ModifiedDate {get; set;}

    public bool? IsDelete { get; set; }

    public Guid? IssueCategoryId { get; set; }

    public virtual IssueCategory? IssueCategory { get; set; }

    public virtual ICollection<IssueProduct> IssueProducts { get; set; } = new List<IssueProduct>();

    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();
}
