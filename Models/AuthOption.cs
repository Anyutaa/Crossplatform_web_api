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

    }
}
