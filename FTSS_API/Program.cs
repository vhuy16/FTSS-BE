using FTSS_API;
using FTSS_API.Constant;
using FTSS_API.Middlewares;
using FTSS_API.Payload.Request.Email;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Supabase;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // Add this for claim handling
using Microsoft.Extensions.Logging; // Add this for logging

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLazyResolution();

// Add other custom services and configurations
builder.Services.AddAuthentication();
builder.Services.AddDatabase();
builder.Services.AddUnitOfWork();
builder.Services.AddCustomServices();
builder.Services.AddJwtValidation();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddHttpClientServices();
builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(
        builder.Configuration["Supabase:Url"],
        builder.Configuration["Supabase:ApiKey"],
        new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true,
        }));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole(); // Or configure other log sinks
    logging.SetMinimumLevel(LogLevel.Information);
});


// Configure CORS - Corrected and simplified
builder.Services.AddCors(options =>
{
   options.AddPolicy(name: CorsConstant.PolicyName,
    policy =>
    {
        // Replace with your actual front-end URL(s) or * if you allow any origin
      policy.WithOrigins("http://localhost:3000", "https://localhost:44346")
        .AllowAnyHeader()
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH") //Explicitly list the required methods
         .AllowCredentials(); //Remove if credentials are not needed
       });
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

    // Cấu hình bảo mật (JWT)
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        BearerFormat = "JWT",
        Scheme = "Bearer",
        Description = "Enter 'Bearer' [space] and then your token in the text input below.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            new string[] { }
        },
    };
    c.AddSecurityRequirement(securityRequirement);

    // Đọc file XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.AddSecurityRequirement(securityRequirement);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Middleware order is crucial
app.UseRouting();
app.UseHttpsRedirection(); // Corrected middleware order
app.UseCors(CorsConstant.PolicyName); // Apply CORS before auth
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>(); // Add exception handling middleware before mapping controllers.
app.MapControllers();

// Run the application
app.Run();


// Middleware example
public class RequestLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggerMiddleware> _logger;
    public RequestLoggerMiddleware(RequestDelegate next, ILogger<RequestLoggerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
       _logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
        await _next(context);
    }
}