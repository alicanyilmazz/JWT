using AuthServer.Core.Configuration;
using AuthServer.Core.Dtos;
using AuthServer.Core.Models;
using AuthServer.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SharedLibrary.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Service.Services
{
    internal class TokenService : ITokenService
    {
        private readonly UserManager<UserApp> _userManager;
        private readonly CustomTokenOption _tokenOption;
        public TokenService(UserManager<UserApp> userManager, IOptions<CustomTokenOption> options)
        {
            _userManager = userManager;
            _tokenOption = options.Value;
        }

        private string CreateRefreshToken()
        {
            var numberByte = new Byte[32];
            using var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(numberByte);
            return Convert.ToBase64String(numberByte);
        }

        private IEnumerable<Claim> GetClaims(UserApp userApp, List<string> audience)
        {
            var userList = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userApp.Id),
                new Claim(JwtRegisteredClaimNames.Email, userApp.Email), // you can use ' new Claim(ClaimTypes.Email, userApp.Email), ' instead of this line
                new Claim(ClaimTypes.Name,userApp.UserName), 
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            userList.AddRange(audience.Select(x => new Claim(JwtRegisteredClaimNames.Aud, x)));

            return userList;
        }

        private IEnumerable<Claim> GetClaims()
        {

        }
        public TokenDto CreateToken(UserApp userApp)
        {
            throw new NotImplementedException();
        }

        public ClientTokenDto CreateTokenByClient(Client client)
        {
            throw new NotImplementedException();
        }
    }
}
