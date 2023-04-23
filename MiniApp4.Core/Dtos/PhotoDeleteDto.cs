using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp4.Core.Dtos
{
    public class PhotoDeleteDto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string PhotoId { get; set; }
    }
}
