using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Repositories.StoredProcedureRepositories;
using MiniApp3.Core.Services.Database;
using MiniApp3.Core.Services.Visual.Database;
using MiniApp3.Core.Services.Visual.Server;
using MiniApp3.Core.UnitOfWork;
using MiniApp3.Data.Context;
using MiniApp3.Data.Repositories.GenericRepositories;
using MiniApp3.Data.Repositories.StoredProcedureRepositories.Query;
using MiniApp3.Data.UnitOfWork;
using MiniApp3.Service.Services;
using MiniApp3.Service.Services.ImageSaveServices.Database.Managers;
using MiniApp3.Service.Services.ImageSaveServices.Database.Services.ReadServices;
using MiniApp3.Service.Services.ImageSaveServices.Database.Services.SaveServices;
using MiniApp3.Service.Services.ImageSaveServices.Server.Managers;
using MiniApp3.Service.Services.ImageSaveServices.Server.Services.ReadServices;
using MiniApp3.Service.Services.ImageSaveServices.Server.Services.SaveServices;
using SharedLibrary.Configuration;
using SharedLibrary.Extensions.Authorization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1", new OpenApiInfo { Title = "DATABASE PHOTO API", Version = "v1" });
});
builder.Services.AddTransient(typeof(IEntityRepository<>), typeof(EntityRepository<>)); // CORE , DATA
//builder.Services.AddTransient(typeof(IImageQualityStoredProcedureRepository), typeof(ImageQualityStoredProcedureRepository)); // CORE , DATA
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>)); // CORE , SERVICE
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // CORE , DATA
builder.Services.AddTransient<IImageDbSaveManager, ImageDbSaveManager>();
builder.Services.AddTransient<IImageDbSaveServices, ImageDbSaveServiceDefault>();
builder.Services.AddTransient<IImageDbReadService, ImageDbReadService>();
builder.Services.AddTransient<IImageServerSaveManager, ImageServerSaveManager>();
builder.Services.AddTransient<IImageServerSaveService, MultistagedTransactionImageSaveService>();
builder.Services.AddTransient<IImageServerReadService, ImageServerReadServiceDefault>();
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
