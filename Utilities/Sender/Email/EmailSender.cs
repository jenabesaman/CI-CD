using DSTV3.Common.BO.Common;
using DSTV3.Common.Utility;
using DSTV3.UploadInterface.Api.Models.UploadModels;
using System.Text;
using System.Text.Json;

namespace DSTV3.UploadInterface.Api.Utilities.Sender.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public ResultSenderModel Send(string email)
        {
            var Code = Tools.ActiveCode();
            string message = EmailTemplate(Code);
            string url = _configuration.GetValue<string>("BaseUrl:EmailService");
            string pass = _configuration.GetValue<string>("SenderApiKey");
            //#region BypassLocalSSL
            //HttpClientHandler clientHandler = new()
            //{
            //    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            //};
            //#endregion
            //HttpClient httpClient = new HttpClient(clientHandler);

            DRequestHandler httpClient = new();
            httpClient.DefaultRequestHeaders.Add("CredentialToken",pass);
            EmailModel emailmodel = new EmailModel()
            {
                To = email,
                Subject = "انتقال امن فایل دانا",
                Message = message.ToString(),
            };
            var request = httpClient.PostAsJsonAsync(url, emailmodel);
            var response = request.Result.Content.ReadAsStringAsync().Result;
            ResultSenderModel Result = JsonSerializer.Deserialize<ResultSenderModel>(response);
            ResultSenderModel resultSenderModel = new ResultSenderModel();
            resultSenderModel.message = Result.message;
            resultSenderModel.status = Result.status;
            resultSenderModel.code = Code;
            return resultSenderModel;
        }

        private string EmailTemplate(string code)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("کد احراز هویت شما:" + code);
            message.AppendLine(string.Empty);
            message.AppendLine("کد احراز هویت برای سامانه انتقال امن فایل دانا می باشد لطفا آنرا در اختیار دیگران قرار ندهید.");
            message.AppendLine(string.Empty);
            message.AppendLine("اگر این در خواست از سمت شما نبوده است آن را نادیده بگیرید.");
            message.AppendLine(string.Empty);
            return message.ToString();
        }
    }
}
