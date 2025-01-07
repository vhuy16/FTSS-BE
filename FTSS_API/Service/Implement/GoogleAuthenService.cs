using System.Security.Claims;
using AutoMapper;
using FTSS_API.Payload.Response;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Repository.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FTSS_API.Service.Implement;

public class GoogleAuthenService :BaseService<GoogleAuthenService>, IGoogleAuthenService
{
    public GoogleAuthenService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<GoogleAuthenService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
    }

    public async Task<GoogleAuthResponse> AuthenticateGoogleUser(HttpContext context)
    {
        var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (authenticateResult.Principal == null) return null;
        var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);
        var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null) return null;
        var accessToken = authenticateResult.Properties.GetTokenValue("access_token");

        return new GoogleAuthResponse
        {
            FullName = name,
            Email = email,
            Token = accessToken
        };
    }
}