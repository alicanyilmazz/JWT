using AutoMapper;
using MiniApp3.Core.Dtos;
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
            //CreateMap<ImageDbServiceResponse, ImageDbServiceRequest>().ReverseMap();
        }
    }
}
