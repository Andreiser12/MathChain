using MathChain.API.Services;
using MathChain.API.Data;
using MathChain.Blockchain;
using MathChain.Blockchain.Services;
using MathChain.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var blockchainConfig = new BlockchainConfig();

builder.Services.AddSingleton(blockchainConfig);
builder.Services.AddSingleton<IBlockchainService, BlockchainService>();

builder.Services.AddHttpClient<WolframService>();

builder.Services.AddDbContext<MathChainDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MathChainDb");
    options.UseNpgsql(connectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7087")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
