using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DSTV3.UploadInterface.Api.Utilities
{
    public class JWTUtility
    {
        private readonly IConfiguration _configuration;
        public JWTUtility(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string ValidateToken(string token)
        {
            if (token == null)
                return string.Empty;


            var tokenHandler = new JwtSecurityTokenHandler();
            var Key = Encoding.ASCII.GetBytes(_configuration.GetValue<string>("TokenKey"));
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var Email = jwtToken.Claims.FirstOrDefault(x => x.Type == "DEmail").Value;
                var Phonenumber = jwtToken.Claims.FirstOrDefault(x => x.Type == "DMobile").Value;
                return Email + Phonenumber;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public string GenerateNewToken(string email, string phonenumber)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration.GetValue<string>("TokenKey"));
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("DEmail", email),
                        new Claim("DMobile", phonenumber),
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
