using DSTV3.Common.BO.Common;
using DSTV3.Common.Utility;
using DSTV3.UploadInterface.Api.Models.UploadModels;
using System.Text;
using System.Text.Json;

namespace DSTV3.UploadInterface.Api.Utilities.Sender.SMS
{
    public class SMSSender : ISmsSender
    {
        private readonly IConfiguration _configuration;
        public SMSSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public ResultSenderModel Send(string phonenumber)
        {
            var Code =  Tools.ActiveCode();
            string message = SmsTemplate(Code);
            string url = _configuration.GetValue<string>("BaseUrl:SMSService");
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
            SmsModel smsModel = new SmsModel()
            {
                Number = phonenumber,
                Message = message.ToString(),
            };
            var request = httpClient.PostAsJsonAsync(url, smsModel);
            var response = request.Result.Content.ReadAsStringAsync().Result;
            ResultSenderModel Result = JsonSerializer.Deserialize<ResultSenderModel>(response);
            ResultSenderModel resultSenderModel = new ResultSenderModel();
            resultSenderModel.message = Result.message;
            resultSenderModel.status = Result.status;
            resultSenderModel.code = Code;
            return resultSenderModel;
        }

        private string SmsTemplate(string code)
        {
           
            StringBuilder message = new StringBuilder();
            message.AppendLine("انتقال امن فایل دانا");
            message.AppendLine(string.Empty);
            message.AppendLine("کد احراز هویت شما: " + code);
            message.AppendLine(string.Empty);
            message.AppendLine("Code: " + code);
            message.AppendLine(string.Empty);
            return message.ToString();  
        }
    }
    
}
