using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Core.Models
{
    public class UserApp : IdentityUser
    {
        public string Country { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}
