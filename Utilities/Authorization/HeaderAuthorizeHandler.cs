using Microsoft.AspNetCore.Authorization;

public class HeaderAuthorizeHandler : AuthorizationHandler<HeaderAuthorizeRequirement>
{
    private readonly IHttpContextAccessor contextAccessor;
    private readonly IConfiguration configuration;

    public HeaderAuthorizeHandler(IHttpContextAccessor contextAccessor, IConfiguration configuration)
    {
        this.contextAccessor = contextAccessor;
        this.configuration = configuration;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HeaderAuthorizeRequirement requirement)
    {
        var httpRequest = contextAccessor.HttpContext!.Request;
        if (!httpRequest.Headers[requirement.GlobalUploadToken].Any())
        {
            context.Fail();
            return Task.CompletedTask;
        }
        if (configuration["GlobalUploadToken"] == httpRequest.Headers["X-Upload-Token"])
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}