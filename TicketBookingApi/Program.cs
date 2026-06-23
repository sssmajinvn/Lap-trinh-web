using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TicketBookingApi.Models;
using TicketBookingApi.Identity;
using TicketBookingApi.Services;
using TicketBookingApi.Hubs;

// Fix: Cho phép ghi DateTime.UtcNow vào cột PostgreSQL 'timestamp without time zone'
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── DbContext ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NeonDb")));

// ── Identity (Custom Store – dùng bảng thongtintaikhoan) ──────
builder.Services.AddIdentityCore<Thongtintaikhoan>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddUserStore<CustomUserStore>();

builder.Services.AddScoped<IPasswordHasher<Thongtintaikhoan>, BcryptPasswordHasher>();

// ── JWT Authentication ─────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? "NHOM7_SECRET_KEY_DEFAULT_FALLBACK_123456789";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Đọc token từ query string cho SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/seathub") || path.StartsWithSegments("/notificationhub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireClaim("admin", "true"));
});

// ── Services ───────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<ITmdbService, TmdbService>();
builder.Services.AddScoped<IMoMoService, MoMoService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IPendingOrderMetadataService, PendingOrderMetadataService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TemporarySeatLockService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<TicketCleanupService>();

// ── CORS ───────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ── Controllers + Swagger ──────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket Booking API",
        Version = "v1",
        Description = "API đặt vé xem phim – Nhóm 7"
    });

    // Cấu hình Swagger hỗ trợ JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo dạng: **Bearer {token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Booking API v1");
    c.RoutePrefix = "swagger"; // Swagger tại: https://localhost:{port}/swagger
});

// app.UseHttpsRedirection(); // Tắt khi chạy sau ngrok (tránh 307 redirect loop)
// Redirect /index.html → / (IIS Express tự thêm index.html vào URL, cần redirect về trang chủ MVC)
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.ToLower() == "/index.html")
    {
        context.Response.Redirect("/");
        return;
    }
    await next();
});
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<SeatHub>("/seathub");
app.MapHub<NotificationHub>("/notificationhub");

app.Run();
