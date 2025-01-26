﻿using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? PaymentStatus { get; set; }

    public long? OrderCode { get; set; }
    
    public string? Status  {get; set;}
    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
