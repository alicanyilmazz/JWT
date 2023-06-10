using Microsoft.EntityFrameworkCore;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<ImageData> ImageData { get; set; }
        public DbSet<ImageFile> ImageFile { get; set; }
        public DbSet<ImageFileDetail> ImageFileDetail { get; set; }
        public DbSet<ImageQuality> ImageQuality { get; set; }

        //Comment For Migration
        public DbSet<ImageQualityResponse> ImageQualityResult { get; set; }
        public DbSet<ServerImagesInformation> ServerImagesInformation { get; set; }
        public DbSet<ImageFileInformation> ImageFileInformation { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            //modelBuilder.HasSequence<int>("PHOTO_ID").StartsAt(0).IncrementsBy(1);
            base.OnModelCreating(modelBuilder);
        }
    }
}
