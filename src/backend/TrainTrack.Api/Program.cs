using TrainTrack.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Run();

/// <summary>Udostępnia typ Program testom (WebApplicationFactory, testy architektury).</summary>
public partial class Program;
