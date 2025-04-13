namespace FTSS_API.Payload.Response.Book
{
    public class GetHistoryOrderResponse
    {
        public DateTime? ScheduleDate { get; set; }
        public List<ServicePackageResponse> Services { get; set; } = new();
    }
}
