namespace FTSS_API.Payload.Request;

public class VerifyOtpRequest
{
    public Guid UserId { get; set; }
    public string otpCheck {  get; set; }
}