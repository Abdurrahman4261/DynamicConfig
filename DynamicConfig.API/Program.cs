using DynamicConfig.Library.Context;  
using DynamicConfig.Library.Service;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()  
              .AllowAnyMethod()  
              .AllowAnyHeader(); 
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDbConnection")));

builder.Services.AddScoped<MongoDbContext>(sp =>
    new MongoDbContext(sp.GetRequiredService<IMongoClient>(), "ConfigDb"));

builder.Services.AddScoped<ConfigurationService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAllOrigins"); 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();  

app.UseAuthorization();

app.MapControllers();  

app.Run();
