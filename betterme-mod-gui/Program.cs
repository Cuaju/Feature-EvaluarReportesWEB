using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using MultimediaService;
using MultimediaService;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages();

builder.Services.AddSession(options =>
{
    options.Cookie.Name     = ".Betterme.Session";
    options.IdleTimeout     = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient("AuthProxy", c =>
{
    c.BaseAddress = new Uri("http://localhost:6968/");
});

builder.Services.AddHttpClient("ReportsApi", c => {
    c.BaseAddress = new Uri("http://localhost:6972/");
});

builder.Services.AddHttpClient("PostsApi", c =>
    c.BaseAddress = new Uri("http://localhost:5017/"));

builder.Services
  .AddGrpcClient<MultimediaService.MultimediaService.MultimediaServiceClient>(o =>
      o.Address = new Uri("http://localhost:6979"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();    
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext ctx, IHttpClientFactory http) =>
{
    var payload = await ctx.Request.ReadFromJsonAsync<object>();

    var client = http.CreateClient("AuthProxy");
    var response = await client.PostAsJsonAsync("api/authentication/login", payload);

    ctx.Response.StatusCode = (int)response.StatusCode;

    if (response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (body.TryGetProperty("accessToken", out var tok))
        {
            ctx.Session.SetString("token", tok.GetString()!);
        }
    }
    await response.Content.CopyToAsync(ctx.Response.Body);
});

app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Path.Equals("/Login", StringComparison.OrdinalIgnoreCase)
        && !ctx.Request.Cookies.ContainsKey("accessToken"))
    {
        ctx.Response.Redirect("/Login");
        return;
    }
    await next();
});

app.MapRazorPages();

app.MapFallbackToPage("/Login")
   .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
app.Run();
