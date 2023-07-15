using AutoMapper;
using MiniApp1.Core.Dtos;
using MiniApp1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp1.Service.DtoMappers
{
    internal class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<Weather, WeatherDto>().ReverseMap();
        }
    }
}
