using Microsoft.AspNetCore.Authorization;

public class HeaderAuthorizeRequirement : IAuthorizationRequirement
{
    public HeaderAuthorizeRequirement(string globalUploadToken)
    {
        GlobalUploadToken = globalUploadToken;
    }

    public string GlobalUploadToken { get; }
}