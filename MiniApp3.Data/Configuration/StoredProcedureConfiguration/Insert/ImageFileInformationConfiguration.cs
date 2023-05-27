using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration.StoredProcedureConfiguration.Insert
{
    public class ImageFileInformationConfiguration : IEntityTypeConfiguration<ImageFileInformation>
    {
        public void Configure(EntityTypeBuilder<ImageFileInformation> builder)
        {
            builder.HasNoKey();
            builder.Ignore(x => x.Type);
        }
    }
}
