using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;


namespace FTSS_API.Controller;
[ApiController]
[Route(ApiEndPointConstant.GoogleAuthentication.GoogleAuthenticationEndpoint)]
public class GoogleAuthenticationController : BaseController<GoogleAuthenticationController>
{
    private readonly IUserService _userService;
    private readonly IGoogleAuthenService _googleAuthenticationService;

    public GoogleAuthenticationController(ILogger<GoogleAuthenticationController> logger, IUserService userService, IGoogleAuthenService googleAuthenticationService) : base(logger)
    {
        _userService = userService;
        _googleAuthenticationService = googleAuthenticationService;
    }


    [HttpGet(ApiEndPointConstant.GoogleAuthentication.GoogleLogin)]
    public IActionResult Login()
    {
        // var props = new AuthenticationProperties { RedirectUri = $"https://mrc.vn/auth/callback" };
        var props = new AuthenticationProperties { RedirectUri = $"api/v1/google-auth/signin-google/" };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }


    [HttpGet(ApiEndPointConstant.GoogleAuthentication.GoogleSignIn)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SignInAndSignUpByGoogle()
    {
        var googleAuthResponse = await _googleAuthenticationService.AuthenticateGoogleUser(HttpContext);
        var checkAccount = await _userService.GetAccountByEmail(googleAuthResponse.Email);
        if (!checkAccount)
        {
            var response = await _userService.CreateNewUserAccountByGoogle(googleAuthResponse);
            if (response == null)
            {
                _logger.LogError($"Create new user account failed with account");
                return Problem(MessageConstant.UserMessage.CreateUserAdminFail);
            }
        }
        var token = await _userService.CreateTokenByEmail(googleAuthResponse.Email);
        googleAuthResponse.Token = token;
        return Ok(new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Login successful",
            data = googleAuthResponse
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
     
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Logout successful",
            data = null
        });
    }
} 