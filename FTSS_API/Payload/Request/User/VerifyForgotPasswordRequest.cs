namespace FTSS_API.Payload.Request;

public class VerifyForgotPasswordRequest
{
    public Guid userId {  get; set; }
    public string otp { get; set; }

}