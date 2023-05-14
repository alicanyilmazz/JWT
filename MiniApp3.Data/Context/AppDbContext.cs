using Microsoft.EntityFrameworkCore;
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
