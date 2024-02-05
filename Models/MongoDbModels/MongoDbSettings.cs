using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSTV3.UploadInterface.Api.Models.MongoDbModels
{
    public class MongoDbSettings : IMongoDbSettings
    {
        public string DatabaseName { get; set; }
        public string ConnectionString { get; set; }
    }
}
