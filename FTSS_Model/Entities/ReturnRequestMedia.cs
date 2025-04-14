using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class ReturnRequestMedia
{
    public Guid Id { get; set; }

    public Guid ReturnRequestId { get; set; }

    public string MediaLink { get; set; } = null!;

    public string MediaType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDelete { get; set; }

    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
}
