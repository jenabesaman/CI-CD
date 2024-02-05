using DSTV3.Common.BO.Models.CatalougeModels;

namespace DSTV3.UploadInterface.Api.Models.UploadModels
{
    public class FileInfoModel
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }
        public string Md5 { get; set; }
        public string FileId { get; set; }
    }
}
