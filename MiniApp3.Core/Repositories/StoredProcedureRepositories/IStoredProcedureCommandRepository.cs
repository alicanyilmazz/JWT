using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Repositories.StoredProcedureRepositories
{
    public interface IStoredProcedureCommandRepository
    {
        public  Task SaveImageImageFile(ImageFile image);
        public  Task SaveImageImageFileDetail(ImageFileDetail imageFileDetails);
    }
}
