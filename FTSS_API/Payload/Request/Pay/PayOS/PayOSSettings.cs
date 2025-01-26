namespace FTSS_API.Payload.Pay;

public class PayOSSettings
{
    public string ClientId { get; set; }
    public string ApiKey { get; set; }
    public string ChecksumKey { get; set; }
    public string ReturnUrl { get; set; }
    public string ReturnUrlFail { get; set; }
}