using DSTV3.Common.BO.Common;
using DSTV3.Common.BO.Entities;
using DSTV3.Common.BO.Enums;
using DSTV3.Common.BO.Models.CatalougeModels;
using DSTV3.Common.BO.Models.UploadModels;
using DSTV3.Common.Utility;
using DSTV3.Common.Utility.ErrorHandling;
using DSTV3.UploadInterface.Api.Entities;
using DSTV3.UploadInterface.Api.Models.UploadModels;
using DSTV3.UploadInterface.Api.MongoGenericRepository;
using DSTV3.UploadInterface.Api.Utilities;
using DSTV3.UploadInterface.Api.Utilities.Sender.Email;
using DSTV3.UploadInterface.Api.Utilities.Sender.SMS;
using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using ZstdSharp.Unsafe;
using File = DSTV3.UploadInterface.Api.Utilities.File;

namespace DSTV3.UploadInterface.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _Configuration;
        private readonly IError _ILogError;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IMongoRepository<DFile> _fileRepository;
        private readonly string _directory;
        private readonly IMongoRepository<DUser> _userRepository;
        private readonly IError _LogError;

        public UploadController(IConfiguration configuration, IError LogError,
            IEmailSender emailSender, ISmsSender smsSender, IMongoRepository<DFile> fileRepository,
            IMongoRepository<DUser> userRepository, IError logError)
        {
            _Configuration = configuration;
            _ILogError = LogError;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _fileRepository = fileRepository;
            _directory = configuration.GetValue<string>("Storage:Unsecured");
            _userRepository = userRepository;
            _LogError = logError;
        }


        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 904857600)]
        [RequestSizeLimit(250 * 1024 * 1024)]
        public IActionResult File(IFormFile file)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var mimetype = Tools.ReadFile(file);
                    var FileSize = Convertor.ToMegabytes(file.Length);
                      if (FileSize >= 200)
                          return Ok(new { Status = false, Message = "حجم فایل نمی تواند  بیشتر از 200 مگابایت باشد" });
                    Dictionary<string, string> Result = new();
                    var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                    string UserInfo = new JWTUtility(_Configuration).ValidateToken(token);

                    if (!string.IsNullOrEmpty(UserInfo))
                    {
                        byte[] bytesFile = Tools.ConvertFormFileToByteArray(file);
                        Result = new File().Upload(string.Empty, _Configuration, _fileRepository, bytesFile, file.FileName, $"File uploaded to insecure database successfully: {Result}", ServiceName.File, true, mimetype, UploadGateway.Web, UserInfo, UserInfo);
                        _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded an insecure database successfully: {Result}");
                        var recotp = Result.Where(c => c.Key.Equals("FileId")).FirstOrDefault();
                        return Ok(new { Status = true, Message = "با موفقیت انجام شد", OTP = recotp.Value });
                    }
                    else
                    {
                        byte[] bytesFile = Tools.ConvertFormFileToByteArray(file);
                        Result = new File().Upload(string.Empty, _Configuration, _fileRepository, bytesFile, file.FileName, $"File uploaded to insecure database successfully: {Result}", ServiceName.File, false, mimetype, UploadGateway.Web, null, null);
                        _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded an insecure database successfully: {Result}");
                        return Ok(new { Status = true, Message = "با موفقیت انجام شد", Item = Result });
                    }
                }
                catch (Exception ex)
                {
                    var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                    return Ok(new { Status = false, Message = $"Error Code :{code}" });
                }
            }
            return Ok(new { Status = false, Message = "پارامتر ارسالی خالی می باشد" });
        }


        [HttpPost]
        public IActionResult PreUpload(PreUploadModel model)
        {
            try
            {
                Dictionary<string, string> keyValuePairs = new();
                string uploaderid = Guid.NewGuid().ToString().Replace("-", "");
                string otp = $"DANA{Tools.RandomCode(8)}";
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var file = _fileRepository.FindOne(c => c.Sha256.Equals(model.Sha256));
                var UserInfo = new JWTUtility(_Configuration).ValidateToken(token);
                if (!string.IsNullOrEmpty(UserInfo) && file != null)
                {
                    var File = InsertFile(uploaderid,string.Empty, otp, file.FileId, model.FileName, file.Size, file.Sha256, file.Extension, file.Sha1, file.Md5);
                    var log = Tools.Initialog(ServiceName.Update, $"File uploaded to insecure database successfully: {otp}");
                    InitialCatalogue(uploaderid,File, otp, UploadGateway.Web, UserInfo, UserInfo);
                    _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded to insecure database successfully: {file.OTP}");
                    string tempPath = Path.Combine(_directory, File.OTP + ".temp");
                    System.IO.File.WriteAllText(tempPath, File.FileId);
                    return Ok(new { status = true, otp = File.OTP });
                }
                else if (file != null)
                {
                    var File = InsertFile(uploaderid,string.Empty,string.Empty,file.FileId, model.FileName, file.Size, file.Sha256, file.Extension, file.Sha1, file.Md5);
                    var log = Tools.Initialog(ServiceName.Update, $"File uploaded to insecure database successfully: {File.FileId}");
                    string uploaderId = InitialCatalogue(uploaderid,File,otp,UploadGateway.Web,null,null);
                    _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded to insecure database successfully: {file.FileId}");
                    keyValuePairs.Add("FileId", file.FileId);
                    keyValuePairs.Add("UploaderId", uploaderId);
                    return Ok(new { status = true, Item = keyValuePairs });
                }
                else
                {
                    return Ok(new { Status = false, Message = "فایل وجود ندارد" });
                }
            }
            catch (Exception ex)
            {
                var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                return Ok(new { Status = false, Message = $"Error Code :{code}" });
            }
        }


        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 904857600)]
        [RequestSizeLimit(250 * 1024 * 1024)]
        public IActionResult test(IFormFile file)
        {
            string path = Path.Combine(_directory, file.Name);

            EncryptionTools encryptionTools = new EncryptionTools();
            var unsecuredStorage = _Configuration.GetValue<string>("Storage:Unsecured");
            var PassEncrypt = _Configuration.GetValue<string>("PassEncrypt");
            var passwordBytes = Tools.ConvertStringToByteArray(PassEncrypt);

            byte[] bytesToBeEncrypted = Tools.ConvertFormFileToByteArray(file);

            if (bytesToBeEncrypted.Length < 10)
                return Ok("shit");


            byte[] encryptedBytes = null;

            string saltString = "--Aria.Ardin.in--";
            byte[] saltBytes = Encoding.UTF8.GetBytes(saltString);

            using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;

                var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                AES.Key = key.GetBytes(AES.KeySize / 8);
                AES.IV = key.GetBytes(AES.BlockSize / 8);

                AES.Mode = CipherMode.CBC;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }


            return Ok(new { bytesToBeEncrypted = bytesToBeEncrypted.Length, encryptedBytes = encryptedBytes.Length });
        }


        [HttpPost]
        public IActionResult FileGlobal(FileGlobal file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var stream = new MemoryStream(file.File);
                    IFormFile formFile = new FormFile(stream, 0, file.File.Length, file.Name, file.Name);
                    var mimetype = Tools.ReadFile(formFile);
                    Dictionary<string, string> Result = new File().Upload(string.Empty, _Configuration, _fileRepository, file.File, file.Name, "File uploaded to insecure database successfully", ServiceName.FileGlobal, false, mimetype, UploadGateway.Telegram, null, null);
                    _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded to insecure database successfully: {Result.Where(c => c.Key.Equals("FileId")).FirstOrDefault()}");
                    var result = Result.Where(c => c.Key.Equals("FileId")).FirstOrDefault();
                    return Ok(new { Status = true, Message = "با موفقیت انجام شد", Item = result.Value });
                }
                catch (Exception ex)
                {
                    var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                    return Ok(new { Status = false, Message = $"Error Code :{code}" });
                }
            }
            return Ok(new { Status = false, Message = "پارامتر ارسالی خالی می باشد" });
        }



        [HttpPost]
        [Authorize(Policy = "GlobalUploadPolicy")]
        public IActionResult Global(UploadGlobal uploadGlobal)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Dictionary<string, string> result = new();
                    if (!string.IsNullOrEmpty(uploadGlobal.Uploader.FirstName))
                    {
                        bool isAuthenticated = false;
                        var userExist = _userRepository.FindOne(a => a.PhoneNumber == uploadGlobal.Uploader.PhoneNumber && a.Status == true);
                        if (userExist != null)
                        {
                            isAuthenticated = true;
                            userExist.TelegramId = uploadGlobal.Uploader.TelegramId;
                            _userRepository.ReplaceOne(userExist);

                            result = PrepareGlobalFile(uploadGlobal);
                            var catalouge = result.Where(c => c.Key.Equals("FileId")).FirstOrDefault();
                            return Ok(new { Status = true, Message = "با موفقیت انجام شد", Item = catalouge.Value });
                        }
                        else
                        {
                            var userid = (string)HttpContext.Items["DUserId"];
                            DUser user = new()
                            {
                                PhoneNumber = uploadGlobal.Uploader.PhoneNumber,
                                Email = uploadGlobal.Uploader.Email,
                                Firstname = uploadGlobal.Uploader.FirstName,
                                Lastname = uploadGlobal.Uploader.LastName,
                                IsAdmin = false,
                                Status = true,
                                InsertDateTime = DateTime.Now,
                                UpdateDateTime = DateTime.Now,
                                InsertUserId = userid,
                                UpdateUserId = userid,
                            };
                            _userRepository.InsertOne(user);
                        }
                    }
                    result = PrepareGlobalFile(uploadGlobal);
                    var Result = result.Where(c => c.Key.Equals("FileId")).FirstOrDefault();
                    return Ok(new { Status = true, Message = "با موفقیت انجام شد", Item = Result.Value });
                }
                catch (Exception ex)
                {
                    var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                    return Ok(new { Status = false, Message = $"Error Code :{code}" });
                }
            }
            return Ok(new { Status = false, Message = "پارامتر ارسالی خالی می باشد" });
        }


        private Dictionary<string, string> PrepareGlobalFile(UploadGlobal uploadGlobal)
        {
            try
            {
                var stream = new MemoryStream(uploadGlobal.File.File);
                IFormFile formFile = new FormFile(stream, 0, uploadGlobal.File.File.Length, uploadGlobal.File.Name, uploadGlobal.File.Name);
                var mimetype = Tools.ReadFile(formFile);
                Dictionary<string, string> Result = new File().Upload(uploadGlobal.Uploader.UserId, _Configuration, _fileRepository, uploadGlobal.File.File, uploadGlobal.File.Name, $"File uploaded to insecure database successfully", ServiceName.FileGlobal,true, mimetype, uploadGlobal.UploadGateway, uploadGlobal.Uploader.Email, uploadGlobal.Uploader.PhoneNumber);
                _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"File uploaded to insecure database successfully: {Result}");
                return Result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpPost]
        public IActionResult Send(SenderModel model)
        {
            try
            {
                var log = Tools.Initialog(ServiceName.Send, $"OTP Send: {model.Email} {model.PhoneNumber}");
                string path = Path.Combine(_directory, model.FileId + ".Catalogue");
                string fileContents = System.IO.File.ReadAllText(path);
                CatalogueBO catalogue = System.Text.Json.JsonSerializer.Deserialize<CatalogueBO>(fileContents);
                ResultSenderModel ResultModel = new ResultSenderModel();

                if (!string.IsNullOrEmpty(model.Email))
                    ResultModel = _emailSender.Send(model.Email);
                else
                    ResultModel = _smsSender.Send(model.PhoneNumber);

                var uploader = catalogue.Uploaders.Where(c => c.UploaderId.Equals(model.UploaderId)).FirstOrDefault();
                if (uploader != null)
                {
                    catalogue.Uploaders.Remove(uploader);
                    uploader.VerifyCode = ResultModel.code;
                    catalogue.Uploaders.Add(uploader);
                }
                catalogue.Logs ??= new();
                catalogue.Logs.Add(log);
                var jsonModel = System.Text.Json.JsonSerializer.Serialize(catalogue);
                System.IO.File.WriteAllText(path, jsonModel);
                _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"OTP Send: {model.Email} {model.PhoneNumber} ");
                return Ok(new { Status = ResultModel.status, Message = ResultModel.message });
            }
            catch (Exception ex)
            {
                var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                return Ok(new { Status = false, Message = $"Error Code :{code}" });
            }
        }


        [HttpPost]
        public IActionResult Verify(VerifyModel model)
        {
            try
            {
                string otp = $"DANA{Tools.RandomCode(8)}";
                var log = Tools.Initialog(ServiceName.Verify, $"The number or email is confirmed: {model.PhoneNumber} {model.Email}");
                string pathCata = Path.Combine(_directory, model.FileId + ".Catalogue");
                string fileContents = System.IO.File.ReadAllText(pathCata);
                CatalogueBO catalogue = System.Text.Json.JsonSerializer.Deserialize<CatalogueBO>(fileContents);
                var uploader = catalogue.Uploaders.Where(c => c.UploaderId.Equals(model.UploaderId)).FirstOrDefault();
                if (uploader != null && model.ActiveCode == catalogue.Uploaders.LastOrDefault().VerifyCode)
                {
                    catalogue.Uploaders.Remove(uploader);
                    uploader.Email = model.Email;
                    uploader.PhoneNumber = model.PhoneNumber;
                    uploader.OTP = otp;
                    catalogue.Uploaders.Add(uploader);
                    catalogue.Logs ??= new();
                    catalogue.Logs.Add(log);
                    var jsonModel = System.Text.Json.JsonSerializer.Serialize(catalogue);
                    System.IO.File.WriteAllText(pathCata, jsonModel);
                    var token = new JWTUtility(_Configuration).GenerateNewToken(model.Email,model.PhoneNumber);
                    var file = _fileRepository.FindOne(c => c.FileId == model.FileId && c.UploaderId == model.UploaderId);
                    if (string.IsNullOrEmpty(file.OTP))
                    {
                        file.OTP = otp;
                        _fileRepository.ReplaceOne(file);
                    }
                    string tempPath = Path.Combine(_directory, otp + ".temp");
                    System.IO.File.WriteAllText(tempPath, model.FileId);
                    _LogError.AddLog(ServiceType.Upload, Severity.Information, "system", $"The number or email is confirmed: {model.PhoneNumber} {model.Email}");
                    return Ok(new { Status = true, Message = "عملیات با موفقیت انجام شد", Item = file.OTP, Token = token });
                }
                return Ok(new { Status = false, Message = "کد ارسالی صحیح نیست" });
            }
            catch (Exception ex)
            {
                var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                return Ok(new { Status = false, Message = $"Error Code :{code}" });
            }
        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult List(ListModel model)
        {
            try
            {
                string Key = _Configuration.GetValue<string>("KeyToken");
                var GridifyQueryFile = new GridifyQuery()
                {
                    Filter = $"Status ={true}{(string.IsNullOrEmpty(model.GridifyQuery.Filter) ? null : $",{model.GridifyQuery.Filter}")}",
                    Page = model.GridifyQuery.Page,
                    PageSize = model.GridifyQuery.PageSize,
                    OrderBy = "InsertDateTime desc"
                };

                if (model.Token.Equals(Key))
                {
                    var Files = _fileRepository.List(GridifyQueryFile);
                    if (Files.Count == 0)
                        return Ok(new { Status = false, Message = "فایلی وجود ندارد" });

                    return Ok(new { Status = true, Item = Files });
                }
                return Ok(new { Status = false, Message = "توکن وارد شده نادرست است" });
            }
            catch (Exception ex)
            {
                var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                return Ok(new { Status = false, Message = $"Error Code :{code}" });
            }
        }


        [HttpGet]
        public IActionResult FileInfo(string otp)
        {
            try
            {
                if (!string.IsNullOrEmpty(otp))
                {
                    FileInfoModel FileInfo = new FileInfoModel();

                    var file = _fileRepository.FindOne(c => c.Status == true && c.OTP == otp);
                    if (file == null)
                        return Ok(new { Status = false, Message = "فایل وجود ندارد" });

                    FileInfo.MimeType = file.MimeType;
                    FileInfo.Name = file.Name;
                    FileInfo.Size = file.Size;
                    FileInfo.Extension = file.Extension;
                    FileInfo.Sha1 = file.Sha1;
                    FileInfo.Sha256 = file.Sha256;
                    FileInfo.Md5 = file.Md5;
                    FileInfo.FileId = file.FileId;
                    FileInfo.MimeType = file.MimeType;

                    return Ok(new { Status = true, Item = FileInfo });
                }
                return Ok(new { Status = false, Message = "otp نمی تواند خالی باشد" });
            }
            catch (Exception ex)
            {
                var code = _ILogError.AddLog(ServiceType.Upload, Severity.Error, ex, "userid");
                return Ok(new { Status = false, Message = $"Error Code :{code}" });
            }
        }


        private string InitialCatalogue(string uploaderid,DFile file, string otp, UploadGateway uploadGateway, string email, string phonenumber)
        {
            
            var Uploader = Tools.InsertUploader(uploaderid, string.Empty, string.Empty, string.Empty, phonenumber, email, otp, uploadGateway);
            var log = Tools.Initialog(ServiceName.PreUpload, $"File uploaded to insecure database successfully: {file.FileId}");

            string path = Path.Combine(_directory, file.FileId + ".Catalogue");
            string fileContents = System.IO.File.ReadAllText(path);
            CatalogueBO catalogue = System.Text.Json.JsonSerializer.Deserialize<CatalogueBO>(fileContents);
            catalogue.Logs ??= new();
            catalogue.Logs.Add(log);
            catalogue.Uploaders ??= new();
            catalogue.Uploaders.Add(Uploader);
            var jsonModel = System.Text.Json.JsonSerializer.Serialize(catalogue);
            System.IO.File.WriteAllText(path,jsonModel);
            return Uploader.UploaderId;
        }


        private DFile InsertFile(string uploaderid,string userid, string otp, string fileid, string filename, long size, string sha256, string extension, string sha1, string md5)
        {
            DFile file = new DFile();
            file.OTP = otp;
            file.Sha1 = sha1;
            file.UploaderId = uploaderid;
            file.Md5 = md5;
            file.Sha256 = sha256;
            file.UserId = userid;
            file.Extension = extension;
            file.FileId = fileid;
            file.Name = filename;
            file.Size = size;
            file.MimeType = string.Empty;
            file.InsertDateTime = DateTime.Now;
            file.UpdateDateTime = DateTime.Now;
            file.InsertUserId = userid;
            file.UpdateUserId = userid;
            file.Status = true;
            _fileRepository.InsertOne(file);
            return file;
        }
    }
}
