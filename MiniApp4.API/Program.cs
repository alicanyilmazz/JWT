using Microsoft.EntityFrameworkCore;
using MiniApp4.API.Utilities.Visual.Abstract;
using MiniApp4.API.Utilities.Visual.Concrete;
using MiniApp4.Core.Repositories;
using MiniApp4.Core.Services;
using MiniApp4.Core.UnitOfWork;
using MiniApp4.Data.Context;
using MiniApp4.Data.Repositories;
using MiniApp4.Data.UnitOfWork;
using MiniApp4.Service.Services;
using SharedLibrary.Configuration;
using SharedLibrary.Extensions.Authorization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // CORE , DATA
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>)); // CORE , SERVICE
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // CORE , DATA
builder.Services.AddTransient<IImageManager, ImageManager>();
//builder.Services.AddTransient<IImageServices, ImageService>();
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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
