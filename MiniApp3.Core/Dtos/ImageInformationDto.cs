using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Dtos
{
    public class ImageInformationDto
    {
        public Stream? Image { get; set; }
        public bool IsSuccess { get; set; }
    }
}
