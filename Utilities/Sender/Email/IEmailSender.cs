using DSTV3.UploadInterface.Api.Models.UploadModels;

namespace DSTV3.UploadInterface.Api.Utilities.Sender.Email
{
    public interface IEmailSender
    {
        ResultSenderModel Send(string email);
    }
}
