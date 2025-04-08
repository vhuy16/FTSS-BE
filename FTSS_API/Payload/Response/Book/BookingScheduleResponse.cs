﻿namespace FTSS_API.Payload.Response.Book
{
    public class BookingScheduleResponse
    {
        public Guid Id { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? Status { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal? TotalPrice { get; set; }

        public Guid? UserId { get; set; }
        public string UserName { get; set; } = null!;

        public string? FullName { get; set; } = null!;

        public Guid? OrderId { get; set; }
        public string Url { get; set; } = null!;
        public string BookingCode { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
    }
}
