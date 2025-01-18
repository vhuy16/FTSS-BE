using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid? PaymentId { get; set; }

    public DateTime? TransactionDate { get; set; }

    public decimal? Amount { get; set; }

    public string? TransactionType { get; set; }

    public string? Status { get; set; }

    public virtual Payment? Payment { get; set; }
}
