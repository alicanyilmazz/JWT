using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Entities
{
    public class ImageData
    {
        public ImageData() 
        {
            this.Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public string OriginalType { get; set; }
        public string  OriginalFileName { get; set; }
        public byte[] OriginalContent { get; set; }
        public byte[] ThumbnailContent { get; set; }
        public byte[] FullScreenContent { get; set; }

    }
}
