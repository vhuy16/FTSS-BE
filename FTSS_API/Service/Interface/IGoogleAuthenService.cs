using FTSS_API.Payload.Response;

namespace FTSS_API.Service.Interface;

public interface IGoogleAuthenService
{
    Task<GoogleAuthResponse> AuthenticateGoogleUser(HttpContext context);
}