using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Crossplatform_2_smirnova
{
    public class AuthOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int LifetimeInHours { get; set; } = 24;
        public string SigningKey { get; set; } = string.Empty;

        public SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    }
}