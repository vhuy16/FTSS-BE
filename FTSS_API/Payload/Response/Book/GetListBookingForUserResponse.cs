﻿using static FTSS_API.Payload.Response.Order.GetOrderResponse;

namespace FTSS_API.Payload.Response.Book
{
    public class GetListBookingForUserResponse
    {
        public Guid Id { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? Status { get; set; }
        public string? Address { get; set; }
        public string? BookingCode { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal? TotalPrice { get; set; }
        public Guid? OrderId { get; set; }

        public bool? IsAssigned { get; set; }
        public Guid? MissionId { get; set; }
        public List<ServicePackageResponse> Services { get; set; } = new();
        public PaymentResponse Payment { get; set; } = new PaymentResponse();
    }
}
