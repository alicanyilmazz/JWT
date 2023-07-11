using AutoMapper;
using MiniApp2.Core.Dtos;
using MiniApp2.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp2.Service.DtoMappers
{
    internal class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<Message, MessageDto>().ReverseMap();
        }
    }
}
