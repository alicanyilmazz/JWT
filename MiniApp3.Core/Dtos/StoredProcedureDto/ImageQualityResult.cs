using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Dtos.StoredProcedureDto
{
    public class ImageQualityResult
    {
        public string Name { get; set; }
        public int Rate { get; set; }
        public int ResizeWidth { get; set; }
        public bool IsOriginal { get; set; }
    }
}
