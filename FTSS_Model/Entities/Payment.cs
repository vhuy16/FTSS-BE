using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;
[Index(nameof(OrderId), IsUnique = false, Name = "idx_payment_orderid")]
[Index(nameof(BookingId), IsUnique = false, Name = "idx_payment_bookingid")]
[Index(nameof(PaymentStatus), IsUnique = false, Name = "idx_payment_paymentstatus")]
[Index(nameof(PaymentDate), IsUnique = false, Name = "idx_payment_paymentdate")]
public partial class Payment
{
    public Guid Id { get; set; }

    public Guid? OrderId { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? PaymentStatus { get; set; }

    public long? OrderCode { get; set; }

    public string? BankNumber { get; set; }

    public string? BankName { get; set; }

    public string? BankHolder { get; set; }

    public Guid? BookingId { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Order? Order { get; set; }
}
