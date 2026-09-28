using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaDeReservas.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// builder necesario para que el JWTAuthorization pueda leer el usuario
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JWTAuthHandler>();

builder.Services.AddHttpClient("WebAPI", c =>
{
    // Usa el puerto de WebAPI/Properties/launchSettings.json
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5068/";
    c.BaseAddress = new Uri(apiBaseUrl);
})
    // Aqui se agrega el handler que se encarga de agregar el token JWT a las solicitudes HTTP salientes hacia la WebAPI.
    .AddHttpMessageHandler<JWTAuthHandler>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Login";

        o.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;                                
        o.Cookie.SameSite = SameSiteMode.Strict;                   
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();  
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
