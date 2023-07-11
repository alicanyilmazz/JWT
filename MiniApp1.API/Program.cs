
using Microsoft.EntityFrameworkCore;
using MiniApp1.Core.Repositories;
using MiniApp1.Core.Services;
using MiniApp1.Core.UnitOfWork;
using MiniApp1.Data.Context;
using MiniApp1.Data.Repositories.GenericRepositories;
using MiniApp1.Data.UnitOfWork;
using MiniApp1.Service.Services;
using SharedLibrary.Configuration;
using SharedLibrary.Extensions.Authorization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddTransient(typeof(IEntityRepository<>), typeof(EntityRepository<>)); // CORE , DATA
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>)); // CORE , SERVICE
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // CORE , DATA
builder.Services.AddDbContext<AppDbContext>(x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), option =>
    {
        option.MigrationsAssembly(Assembly.GetAssembly(typeof(AppDbContext)).GetName().Name); // //option.MigrationsAssembly("MiniApp1.Data");
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
