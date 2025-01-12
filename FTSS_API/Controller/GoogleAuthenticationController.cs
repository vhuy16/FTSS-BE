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

    public GoogleAuthenticationController(ILogger<GoogleAuthenticationController> logger, IUserService userService,
        IGoogleAuthenService googleAuthenticationService) : base(logger)
    {
        _userService = userService;
        _googleAuthenticationService = googleAuthenticationService;
    }

    /// <summary>
    /// Thực hiện đăng nhập bằng tài khoản Google.
    /// </summary>
    [HttpGet(ApiEndPointConstant.GoogleAuthentication.GoogleLogin)]
    public IActionResult Login()
    {
        var props = new AuthenticationProperties { RedirectUri = $"api/v1/google-auth/signin-google/" };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Xác thực và tạo tài khoản thông qua Google.
    /// </summary>
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
                _logger.LogError("Create new user account failed with account");
                return Problem(MessageConstant.UserMessage.CreateUserAdminFail);
            }
        }

        var token = await _userService.CreateTokenByEmail(googleAuthResponse.Email);

        // Tạo nội dung HTML response để gửi về trình duyệt
        string htmlResponse = $@"
                    <html>
                    <body>
                    <script type='text/javascript'>
                    // Gửi token về cửa sổ cha
                    window.opener.postMessage({{
                        accessToken: '{token}',
                        
                    }}, '*');
                    window.close(); // Đóng popup
                    </script>
                    <p>Đang xử lý đăng nhập, vui lòng chờ...</p>
                    </body>
                    </html>";

        // Trả về nội dung HTML
        return Content(htmlResponse, "text/html");
    }

    /// <summary>
    /// Đăng xuất tài khoản Google.
    /// </summary>
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