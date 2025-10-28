using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Crossplatform_2_smirnova
{
    public static class AuthOptions
    {
        public static string Issuer => "BookingSystem";
        public static string Audience => "BookingClients";
        public static int LifetimeInHours => 24; 

        public static SecurityKey SigningKey =>
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes("superSecretKeyMustBeLoooooongAndLonger123"));

        public static string GenerateToken(int userId, string role)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userId.ToString()),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, role)
            };

            var jwt = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                notBefore: now,
                expires: now.AddHours(LifetimeInHours),
                claims: claims,
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
