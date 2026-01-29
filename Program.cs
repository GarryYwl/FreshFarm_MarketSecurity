using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// EF Core + SQL Server
builder.Services.AddDbContext<FreshFarmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Data Protection + app services
builder.Services.AddDataProtection();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<PasswordService>();

// reCAPTCHA needs HttpClient
builder.Services.AddHttpClient();
builder.Services.AddScoped<ReCaptchaService>();

var app = builder.Build();

// ✅ Exception handling (500)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Errors/Error"); // our custom 500 page
    app.UseHsts();
}
else
{
    // In dev you can still keep the developer exception page
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// 404/403 (status codes)
app.UseStatusCodePagesWithReExecute("/Errors/{0}");

app.MapRazorPages();

app.Run();
