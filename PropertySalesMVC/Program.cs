using PropertySalesMVC;
using PropertySalesMVC.Helpers;

var builder = WebApplication.CreateBuilder(args);

// =======================
// ADD SERVICES
// =======================
builder.Services.AddControllersWithViews();

// Session configuration (REQUIRED)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ADO.NET helper
builder.Services.AddScoped<DbHelper>();

builder.Services.AddLogging();
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
