using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration.StoredProcedureConfiguration.Select
{
    public class ImageIndexConfiguration : IEntityTypeConfiguration<ImageIndex>
    {
        public void Configure(EntityTypeBuilder<ImageIndex> builder)
        {
            builder.HasNoKey();
        }
    }
}
