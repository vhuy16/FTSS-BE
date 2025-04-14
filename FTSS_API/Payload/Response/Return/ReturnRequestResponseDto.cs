namespace FTSS_API.Payload.Response.Return;

public class ReturnRequestResponseDto
{
    public Guid ReturnRequestId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MediaDto> Media { get; set; }
}

public class MediaDto
{
    public string MediaLink { get; set; }
    public string MediaType { get; set; }
}