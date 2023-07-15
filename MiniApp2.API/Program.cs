using Microsoft.EntityFrameworkCore;
using MiniApp2.Core.Repositories;
using MiniApp2.Core.Services;
using MiniApp2.Core.UnitOfWork;
using MiniApp2.Data.Context;
using MiniApp2.Data.Repositories.GenericRepositories;
using MiniApp2.Data.UnitOfWork;
using MiniApp2.Service.Services;
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CountryPolicy", policy =>
    {
        policy.RequireClaim("country", "Turkey", "Crypus");
    });
});
// Configure the HTTP request pipeline.
var app = builder.Build();
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
