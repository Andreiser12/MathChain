using MathChain.Blazor;
using MathChain.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AppSession>();

builder.Services.AddScoped(sp =>
{
    var tokenHandler = new SessionTokenHandler(
        sp.GetRequiredService<AppSession>(),
        sp.GetRequiredService<NavigationManager>(),
        sp.GetRequiredService<IConfiguration>())
    {
        InnerHandler = new HttpClientHandler()
    };

    return new HttpClient(tokenHandler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

builder.Services.AddScoped<ApiService>();

builder.Services.AddScoped<WolframService>();

builder.Services.AddScoped<IpfsService>();

builder.Services.AddMudServices();

await builder.Build().RunAsync();


