using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MathChain.Application.Extensions;
using MathChain.Persistence.Extentions;
using MathChain.Gateway.Extensions;
using MathChain.Infrastructure.Extensions;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

var connectionString = builder.Configuration.GetConnectionString("MathchainDB") ??
    throw new InvalidOperationException("Connection string 'MathChainDb' was not found.");

builder.Services.AddPersistenceLayer(connectionString);
builder.Services.AddGatewayLayer();
builder.Services.AddInfrastructureLayer();
builder.Services.AddApplicationLayer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");

app.UseAuthorization();

app.MapControllers();

app.Run();
