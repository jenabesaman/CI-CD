using DSTV3.Common.BO.Enums;
using DSTV3.Common.BO.Models.CatalougeModels;
using DSTV3.Common.Utility;
using System.Text.Json;
using DSTV3.UploadInterface.Api.MongoGenericRepository;
using DSTV3.UploadInterface.Api.Entities;
using DSTV3.Common.BO.Common;

namespace DSTV3.UploadInterface.Api.Utilities
{
    public class File
    {


        public Dictionary<string, string> Upload(string userid, IConfiguration configuration, IMongoRepository<DFile> fileRepository, byte[] file, string filename, string message, ServiceName servicename, bool isauthentication, string mimetype, UploadGateway uploadGateway, string email, string phonenumber, UploaderBO? uploader = null)
        {

            string fileid = Guid.NewGuid().ToString().Replace("-", "");
            EncryptionTools encryptionTools = new EncryptionTools();

            var unsecuredStorage = configuration.GetValue<string>("Storage:Unsecured");
            if (!Directory.Exists(unsecuredStorage))
                Directory.CreateDirectory(unsecuredStorage);

            FileInfo fileInfo = new FileInfo(filename);

            string md5 = HashGenerator.Md5(file);
            string sha256 = HashGenerator.Sha256(file);
            string sha1 = HashGenerator.Sha1(file);
            var File = fileRepository.FindOne(c => c.Sha256.Equals(sha256));
            if (File == null)
            {
                var PassEncrypt = configuration.GetValue<string>("PassEncrypt");
                var passEnc = Tools.ConvertStringToByteArray(PassEncrypt);
                string path = Path.Combine(unsecuredStorage, fileid);
                var encryptFile = encryptionTools.Encrypt(file,passEnc);
                System.IO.File.WriteAllBytes(path, encryptFile);
            }
            var result = WriteCatalogueFile(userid, filename, fileRepository, fileid, file, fileInfo, unsecuredStorage, sha1, sha256, md5, message, servicename, isauthentication, mimetype, uploadGateway, email, phonenumber, uploader);
            return result;
        }



        private Dictionary<string, string> WriteCatalogueFile(string userid, string filename, IMongoRepository<DFile> fileRepository, string fileid, byte[] file, FileInfo fileInfo, string directory, string sha1, string sha256, string md5, string message, ServiceName servicename, bool isauthentication, string mimetype, UploadGateway uploadGateway, string email, string phonenumber, UploaderBO? uploader = null)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            string Newotp = string.Empty;
            string result = string.Empty;
            string uploaderid = Guid.NewGuid().ToString().Replace("-", "");
            string otp = $"DANA{Tools.RandomCode(8)}";
            var log = Tools.Initialog(servicename, $"{message}: {fileid}");
            var File = fileRepository.FindOne(c => c.Sha256.Equals(sha256));

            if (isauthentication == true)
            {
                uploaderid = string.Empty;
                result = otp;
                Newotp = otp;
            }
            else
                result = fileid;



            if (File == null)
            {
                var dFile = InsertFile(uploaderid,fileid, sha1, sha256, md5, filename, mimetype, file.Length, fileInfo.Extension, Newotp, fileRepository);
                var Uploader = Tools.InsertUploader(uploaderid,userid, string.Empty, string.Empty, phonenumber, email, Newotp, uploadGateway);
                CatalogueBO catalogueBO = new CatalogueBO();
                catalogueBO.CreateDate = DateTime.Now;
                catalogueBO.File ??= new();
                catalogueBO.File.FileId = fileid;
                catalogueBO.File.Sha1 = sha1;
                catalogueBO.File.Sha256 = sha256;
                catalogueBO.File.Md5 = md5;
                catalogueBO.File.Name = filename;
                catalogueBO.File.MimeType = new Common.BO.Models.CatalougeModels.MimeTypeModels.FileInfos()
                {
                    Description = string.Empty,
                    Extension = string.Empty,
                    MimeType = string.Empty,
                    Status = Common.BO.Models.CatalougeModels.MimeTypeModels.Enums.MatchResultType.NotFound,
                    Header = mimetype
                };
                catalogueBO.File.Malware = null;
                catalogueBO.File.Size = file.Length;
                catalogueBO.File.Extension = fileInfo.Extension;
                catalogueBO.File.NSRL = null;
                catalogueBO.File.AvResult = string.Empty;
                catalogueBO.File.OfficeArchiveResult = string.Empty;
                catalogueBO.Uploaders ??= new();
                catalogueBO.Uploaders.Add(Uploader);
                catalogueBO.Logs ??= new();
                catalogueBO.Logs.Add(log);
                string pathCata = Path.Combine(directory, dFile.FileId + ".Catalogue");
                var jsonModel = JsonSerializer.Serialize(catalogueBO);
                System.IO.File.WriteAllText(pathCata, jsonModel);

                if (isauthentication == true)
                {
                    string tempPath = Path.Combine(directory, dFile.OTP + ".temp");
                    System.IO.File.WriteAllText(tempPath, dFile.FileId);
                }
            }
            else
            {
                var dFile = InsertFile(uploaderid,File.FileId, File.Sha256, File.Sha1, File.Md5, fileInfo.Name, File.MimeType, File.Size, File.Extension, Newotp, fileRepository);
                var Uploader = Tools.InsertUploader(uploaderid,userid, string.Empty, string.Empty, phonenumber, email, Newotp, uploadGateway);
                string path = Path.Combine(directory, dFile.FileId + ".Catalogue");
                string fileContents = System.IO.File.ReadAllText(path);
                CatalogueBO catalogue = JsonSerializer.Deserialize<CatalogueBO>(fileContents);
                catalogue.Logs ??= new();
                catalogue.Logs.Add(log);
                catalogue.Uploaders ??= new();
                catalogue.Uploaders.Add(Uploader);
                var jsonModel = JsonSerializer.Serialize(catalogue);
                System.IO.File.WriteAllText(path,jsonModel);

                if (isauthentication == true)
                {
                    string tempPath = Path.Combine(directory, dFile.OTP + ".temp");
                    System.IO.File.WriteAllText(tempPath, dFile.FileId);
                }
                else
                {
                    result = dFile.FileId;
                }
            }
            keyValuePairs.Add("FileId", result);
            keyValuePairs.Add("UploaderId", uploaderid);
            return keyValuePairs;
        }





        private DFile InsertFile(string uploaderid, string fileid, string sha1, string sha256, string md5, string filename, string mimetype, long size, string extension, string otp, IMongoRepository<DFile> fileRepository)
        {

            DFile dFile = new DFile();
            dFile.OTP = otp;
            dFile.FileId = fileid;
            dFile.UploaderId = uploaderid;
            dFile.Sha1 = sha1;
            dFile.Sha256 = sha256;
            dFile.Md5 = md5;
            dFile.Name = filename;
            dFile.MimeType = mimetype;
            dFile.Size = size;
            dFile.Extension = extension;
            dFile.InsertDateTime = DateTime.Now;
            dFile.UpdateDateTime = DateTime.Now;
            dFile.InsertUserId = string.Empty;
            dFile.UpdateUserId = string.Empty;
            dFile.Status = true;
            fileRepository.InsertOne(dFile);
            return dFile;
        }

    }
}





