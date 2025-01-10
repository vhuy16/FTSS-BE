using FTSS_API;
using FTSS_API.Constant;
using FTSS_API.Middlewares;
using FTSS_API.Payload.Request.Email;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
   

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLazyResolution();
// Add other custom services and configurations
builder.Services.AddAuthentication();
// builder.Services.AddInfrastructure();
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

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll",
//         policyBuilder =>
//         {
//             policyBuilder.AllowAnyOrigin()
//                          .AllowAnyMethod()
//                          .AllowAnyHeader();
//         });
// });
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(name: CorsConstant.PolicyName,
//         policy => { policy.WithOrigins( "http://localhost:3000", "localhost:44346").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
// });
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsConstant.PolicyName,
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:44346")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
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
    // c.MapType<OrderStatus>(() => new OpenApiSchema
    // {
    //     Type = "string",
    //     Enum = Enum.GetNames(typeof(OrderStatus))
    //            .Select(name => new OpenApiString(name) as IOpenApiAny)
    //            .ToList()
    // });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseCors(CorsConstant.PolicyName);
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run the application
app.Run();
