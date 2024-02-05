using DSTV3.Common.BO.Entities;
using DSTV3.Common.BO.Models.CatalougeModels;
using System.Security.Cryptography;

namespace DSTV3.UploadInterface.Api.Entities
{
    public class DFile : BaseEntity
    {
        public string UploaderId { get; set; }
        public string FileId { get; set; }
        public string OTP { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
        public string UserId { get; set; }
        public string FileStatusMessage { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }
        public string Md5 { get; set; }

    }
}
