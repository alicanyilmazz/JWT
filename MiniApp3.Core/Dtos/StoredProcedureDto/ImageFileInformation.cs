using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Dtos.StoredProcedureDto
{
    public class ImageFileInformation
    {
        public Guid ImageId { get; set; }
        public string Folder { get; set; }
        public string Extension { get; set; }
        public ICollection<string> Type { get; set; } = new List<string>();
    }
}
