﻿using MiniApp3.Core.Dtos;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Services.Visual.Server
{
    public interface IImageServerReadService
    {
        public Task<Response<ImageServerServiceResponse>> GetThumnailPhotoAsync(string id);
        public Task<Response<IEnumerable<ImageServerServiceResponse>>> GetAllPhotoAsync();
    }
}
