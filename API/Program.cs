using MathChain.Blockchain;
using MathChain.Blockchain.Services;
using MathChain.Domain.Interfaces;
using MathChain.Domain.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var blockchainConfig = new BlockchainConfig();

builder.Services.AddSingleton(blockchainConfig);
builder.Services.AddSingleton<IBlockchainService, BlockchainService>();
builder.Services.AddSingleton<IMathProblemRepository, InMemoryMathProblemRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
