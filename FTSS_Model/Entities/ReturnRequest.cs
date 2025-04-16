using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class ReturnRequest
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDelete { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReturnRequestMedia> ReturnRequestMedia { get; set; } = new List<ReturnRequestMedia>();

    public virtual User User { get; set; } = null!;
}
