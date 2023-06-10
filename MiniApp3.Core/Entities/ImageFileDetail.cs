using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Entities
{
    public class ImageFileDetail
    {
        public int Id { get; set; }
        public Guid ImageId { get; set; }
        public string Type { get; set; }
        public string QualityRate { get; set; }
    }
}
