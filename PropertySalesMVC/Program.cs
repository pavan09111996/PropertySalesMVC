using PropertySalesMVC.Logging;
using PropertySalesMVC.Repositories;
using PropertySalesMVC.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// =======================
// ADD SERVICES
// =======================
builder.Services.AddControllersWithViews();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddHttpContextAccessor();


// Session configuration (REQUIRED)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Repositories
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IEnquiryRepository, EnquiryRepository>();
builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();

// Services
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IEnquiryService, EnquiryService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();

builder.Services.AddLogging();

// Persists Error/Critical log entries to the ErrorLogs table (see Logging/DatabaseLogger.cs).
builder.Services.AddSingleton<ILoggerProvider>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    return new DatabaseLoggerProvider(connectionString, httpContextAccessor);
});

var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ENABLE SESSION (VERY IMPORTANT)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
