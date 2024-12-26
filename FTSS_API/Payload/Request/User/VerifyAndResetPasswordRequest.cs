namespace FTSS_API.Payload.Request;

public class VerifyAndResetPasswordRequest
{
    public string NewPassword { get; set; }
    public string ComfirmPassword { get; set; }
}