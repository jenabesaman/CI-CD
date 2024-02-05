using DSTV3.UploadInterface.Api.Models.UploadModels;

namespace DSTV3.UploadInterface.Api.Utilities.Sender.SMS
{
    public interface ISmsSender
    {
        ResultSenderModel Send(string phonenumber);
    }
}
