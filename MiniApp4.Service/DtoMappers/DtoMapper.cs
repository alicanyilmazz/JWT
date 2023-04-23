using AutoMapper;
using MiniApp4.Core.Dtos;
using MiniApp4.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp4.Service.DtoMappers
{
    internal class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<PhotoDto, Photo>().ReverseMap();
            CreateMap<DocumentDto, Document>().ReverseMap();
            CreateMap<VideoDto, Video>().ReverseMap();
        }
    }
}
