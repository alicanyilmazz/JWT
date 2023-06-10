using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Configuration.StoredProcedureConfiguration.Select
{
    public class ServerImagesInformationConfiguration : IEntityTypeConfiguration<ServerImagesInformation>
    {
        public void Configure(EntityTypeBuilder<ServerImagesInformation> builder)
        {
            builder.HasNoKey();
        }
    }
}
