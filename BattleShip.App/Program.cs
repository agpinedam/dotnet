using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BattleShip.App;
using BattleShip.App.Services;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using BattleShip.Protos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string backendUrl = "http://localhost:5200";

// Configure HttpClient to point to the API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });

// Register gRPC Client
builder.Services.AddSingleton(services =>
{
    // Create a gRPC-Web channel
    var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());
    var channel = GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions { HttpHandler = httpHandler });
    return new Game.GameClient(channel);
});

// Register GameClient as Scoped because it depends on Scoped HttpClient
builder.Services.AddScoped<GameClient>();
builder.Services.AddScoped<MultiplayerGameClient>();

await builder.Build().RunAsync();
