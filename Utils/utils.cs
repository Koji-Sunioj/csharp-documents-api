using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace utils;

public static class Utils {

    public static string CreateToken(string signKey,string userName,string role,string host) {

        System.Byte[] keyBytes = Encoding.UTF8.GetBytes(signKey);
        SymmetricSecurityKey symmetricKey = new SymmetricSecurityKey(keyBytes);
        SigningCredentials signingCredentials = new SigningCredentials(symmetricKey,SecurityAlgorithms.HmacSha256);
        List<Claim> claims = new List<Claim>()
        {
            new Claim("sub",userName),
            new Claim("role",role)
        };
        JwtSecurityToken token = new JwtSecurityToken(
            claims: claims,
            audience: host,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: signingCredentials);
        
        string rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        return rawToken;
    }

    public static Dictionary<string,string> CheckToken(string signKey,string host,string token){
       
        Dictionary<string,string> jwtPayload = new Dictionary<string,string>();
        System.Byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signKey);
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidAudience = host,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
        };
    
        tokenHandler.ValidateToken(token,validationParameters,out SecurityToken securityToken);
    
        JwtSecurityToken jwtToken = (JwtSecurityToken)securityToken;
        jwtPayload["user"] = jwtToken.Payload["sub"].ToString();
        jwtPayload["role"] = jwtToken.Payload["role"].ToString();
        
        return jwtPayload;

    }
}