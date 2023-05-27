using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration.EntityConfigurations
{
    public class ImageFileDetailConfiguration : IEntityTypeConfiguration<ImageFileDetail>
    {
        public void Configure(EntityTypeBuilder<ImageFileDetail> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
