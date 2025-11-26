using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BattleShip.App;
using BattleShip.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to point to the API
// Note: In development, API is likely on a different port (e.g., 5200).
// We need to set the BaseAddress correctly.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5200") });

// Register GameClient as Scoped because it depends on Scoped HttpClient
builder.Services.AddScoped<GameClient>();

await builder.Build().RunAsync();
