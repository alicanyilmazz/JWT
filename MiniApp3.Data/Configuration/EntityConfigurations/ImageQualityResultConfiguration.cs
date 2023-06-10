using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration.EntityConfigurations
{
    internal class ImageQualityResultConfiguration : IEntityTypeConfiguration<ImageQualityResponse>
    {
        public void Configure(EntityTypeBuilder<ImageQualityResponse> builder)
        {
            builder.HasNoKey();
        }
    }
}
