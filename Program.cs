using DSTV3.Common.Utility.ErrorHandling;
using DSTV3.UploadInterface.Api.Models.MongoDbModels;
using DSTV3.UploadInterface.Api.MongoGenericRepository;
using DSTV3.UploadInterface.Api.Utilities.Sender.Email;
using DSTV3.UploadInterface.Api.Utilities.Sender.SMS;
using ElmahCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
#region Register Services
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoDbSettings>(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);
builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<IAuthorizationHandler, HeaderAuthorizeHandler>();
builder.Services.AddScoped<IError, Error>();
#endregion

#region Add Authorization Policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GlobalUploadPolicy", policy =>
    {
        policy.Requirements.Add(new HeaderAuthorizeRequirement("X-Upload-Token"));
    });
});
#endregion

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

#region Add Swagger To Services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "0.8.64",
        Title = "DSTV3.UploadInterface.Api",
        Description = "",
        Contact = new OpenApiContact
        {
            Name = "DSTV3.UploadInterface.Api",
            Email = "Kahkeshan@gmail.com",
            Url = new Uri("http://www.Sample.ir/")
        }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                           new string[] {}
                    }
                });

    //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    //c.IncludeXmlComments(xmlPath);
});
#endregion

#region Register Services
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ISmsSender, SMSSender>();
builder.Services.AddScoped<IError, Error>();
#endregion

#region Configure JWT Authentication
var secretKey = builder.Configuration.GetValue<string>("TokenKey");
var key = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});
#endregion

#region Add Cors To Services
builder.Services.AddCors(o => o.AddPolicy
            ("APIPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader();
            }));
#endregion

#region Elmah
builder.Services.AddElmah();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DSTV3.UploadInterface.Api v1"));
app.UseCors("APIPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseElmah();
app.MapControllers();
app.Run();


