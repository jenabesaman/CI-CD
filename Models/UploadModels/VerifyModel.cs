namespace DSTV3.UploadInterface.Api.Models.UploadModels
{
    public class VerifyModel
    {
        public string UploaderId { get; set; }
        public string ActiveCode { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FileId { get; set; }
        public string CaptchaCode { get; set; }
    }
}
