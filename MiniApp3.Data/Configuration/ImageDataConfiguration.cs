using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration
{
    internal class ImageDataConfiguration : IEntityTypeConfiguration<ImageData>
    {
        public void Configure(EntityTypeBuilder<ImageData> builder)
        {
            //builder.HasKey(x => x.Id);
            builder.Property(x => x.OriginalFileName).IsRequired();
            builder.Property(x => x.OriginalType).IsRequired();
            builder.Property(x => x.OriginalContent).IsRequired();
            builder.Property(x => x.FullScreenContent).IsRequired();
            builder.Property(x => x.ThumbnailContent).IsRequired();
        }
    }
}
