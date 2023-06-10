using AutoMapper;
using MiniApp3.Core.Dtos;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.DtoMappers
{
    internal class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<ServerImagesInformation, ImageServerServiceResponse>().ReverseMap();
        }
    }
}
