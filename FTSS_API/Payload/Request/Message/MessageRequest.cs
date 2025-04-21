namespace FTSS_API.Payload.Request.Message;

public class MessageRequest
{
    public string Text { get; set; }
    public Guid RoomId {get; set;}
}