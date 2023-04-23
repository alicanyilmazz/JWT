using Microsoft.EntityFrameworkCore;
using MiniApp4.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp4.Data.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Video> Videos { get; set; }
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
