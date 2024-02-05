using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSTV3.UploadInterface.Api.Models.MongoDbModels
{
    public interface IMongoDbSettings
    {
        string DatabaseName { get; set; }
        string ConnectionString { get; set; }
    }
}
