﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Core.Dtos
{
    public class CreateRoleDto
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
