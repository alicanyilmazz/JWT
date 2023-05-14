using Microsoft.EntityFrameworkCore;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using MiniApp3.Core.UnitOfWork;
using MiniApp3.Data.Context;
using MiniApp3.Data.Repositories;
using MiniApp3.Data.UnitOfWork;
using MiniApp3.Service.Services;
using SharedLibrary.Configuration;
using SharedLibrary.Extensions.Authorization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>)); // CORE , DATA
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>)); // CORE , SERVICE
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // CORE , DATA
builder.Services.AddTransient<IImageProcessingManager, ImageProcessingManager>();
builder.Services.AddTransient<IImageProcessingServices, DatabaseSingleTransactionImageProcessingService>();
builder.Services.AddTransient<IImageReadService, ImageReadService>();
builder.Services.AddDbContext<AppDbContext>(x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), option =>
    {
        option.MigrationsAssembly(Assembly.GetAssembly(typeof(AppDbContext)).GetName().Name); // //option.MigrationsAssembly("AuthServer.Data");
    });
});

builder.Services.Configure<CustomTokenOption>(builder.Configuration.GetSection("TokenOptions"));
var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<CustomTokenOption>();
builder.AddCustomTokenAuth(tokenOptions);

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
