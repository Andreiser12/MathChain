using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MathChain.Application.DTOs;
using MathChain.Application.Extensions;
using MathChain.Persistence.Extentions;
using MathChain.Gateway.Extensions;
using MathChain.Infrastructure.Extensions;

DotNetEnv.Env.Load();

var importFolder = args.Length >= 1 && args[0] == "import-ipfs" ? (args.Length >= 2 ? args[1] : string.Empty) : null;
var builder = WebApplication.CreateBuilder(importFolder == null ? args : args.Skip(args.Length >= 2 ? 2 : 1).ToArray());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddEnvelopeValidationResponses();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7087")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("MathchainDB");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'MathchainDB' is not configured. " +
        "Set it with: dotnet user-secrets set \"ConnectionStrings:MathchainDB\" \"<connection string>\" --project API");
}

builder.Services.AddPersistenceLayer(connectionString);
builder.Services.AddGatewayLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddWalletAuthentication(builder.Configuration);
builder.Services.AddSingleton(builder.Configuration.GetSection("SignIn").Get<SignInSettings>() ?? new SignInSettings());
builder.Services.AddApplicationLayer();

var app = builder.Build();

app.UseEnvelopeErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (importFolder != null)
{
    var folder = importFolder.Length > 0 ? importFolder : Path.Combine(app.Environment.ContentRootPath, "Storage", "ipfs");
    using var scope = app.Services.CreateScope();
    var report = await scope.ServiceProvider.GetRequiredService<MathChain.Infrastructure.Ipfs.IpfsImporter>().ImportFolderAsync(folder);

    Console.WriteLine($"Imported: {report.Imported}");
    Console.WriteLine($"Already stored: {report.AlreadyStored}");
    Console.WriteLine($"Rejected: {report.Rejected.Count}");
    report.Rejected.ForEach(line => Console.WriteLine($"  {line}"));
    Console.WriteLine($"References without content: {report.MissingReferences.Count}");
    report.MissingReferences.ForEach(r => Console.WriteLine($"  {r.Source} {r.OwnerId}: {r.Cid}"));
    return;
}

app.Run();
