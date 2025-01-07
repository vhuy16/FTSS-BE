using System.Text;
using FTSS_API.Service.Implement;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Repository.Implement;
using FTSS_Repository.Interface;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MRC_API.Utils;

namespace FTSS_API;

public static class DependencyInjection
{
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork<MyDbContext>, UnitOfWork<MyDbContext>>();
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<MyDbContext>(options => options.UseSqlServer(GetConnectionString()));
        return services;
    }

    private static string CreateClientId(IConfiguration configuration)
    {
        var clientId = configuration.GetValue<string>("Oauth:ClientId");
        return clientId;
    }
    private static string CreateClientSecret(IConfiguration configuration)
    {
        var clientSecret = configuration.GetValue<string>("Oauth:ClientSecret");
        return clientSecret;
    }

    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISubCategoryService, SubCategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<GoogleUtils.GoogleDriveService>();
        services.AddScoped<SupabaseUltils>();
        // services.AddScoped<IVNPayService, VNPayService>();
        // services.AddScoped<IOrderService, OrderService>();
     
        services.AddScoped<IGoogleAuthenService, GoogleAuthenService>();
        // services.AddScoped<ICartService, CartService>();
        services.AddScoped<IEmailSender, EmailSender>();
        // services.AddScoped<IPayService, PayService>();
        //
        // services.AddScoped<PaymentUltils.Utils>();
        services.AddScoped<HtmlSanitizerUtils>();
   
        
        return services;
    }
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services)
    {
        services.AddHttpClient(); // Registers HttpClient
        return services;
    }

    public static IServiceCollection AddJwtValidation(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                //options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = "BeanMindSystem",
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"))
                };
            })
            .AddCookie(
                options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Chỉ gửi cookie qua HTTPS
                    options.Cookie.SameSite = SameSiteMode.None; // Cho phép gửi cookie cross-origin
                })
            
        .AddGoogle(options =>
        {
        options.ClientId = CreateClientId(configuration);
        options.ClientSecret = CreateClientSecret(configuration);
        options.SaveTokens = true;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        
        });
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.None; // Cho phép cross-origin
            options.Secure = CookieSecurePolicy.Always; // Chỉ gửi cookie qua HTTPS
        });
        return services;
    }
 
        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            services.AddTransient(typeof(Lazy<>), typeof(LazyResolver<>));
            return services;
        }

        private class LazyResolver<T> : Lazy<T> where T : class
        {
            public LazyResolver(IServiceProvider serviceProvider)
                : base(() => serviceProvider.GetRequiredService<T>())
            {
            }
        
    }
    private static string GetConnectionString()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();
        var strConn = config["ConnectionStrings:DefautDB"];

        return strConn;
    }
}
