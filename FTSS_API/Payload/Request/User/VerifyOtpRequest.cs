namespace FTSS_API.Payload.Request;

public class VerifyOtpRequest
{
    public string email { get; set; }
    public string otpCheck {  get; set; }
}