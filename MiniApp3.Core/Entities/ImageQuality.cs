using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Entities
{
    public class ImageQuality
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rate { get; set; }
        public int ResizeWidth { get; set; }
        public bool IsOriginal { get; set; }
    }
}
