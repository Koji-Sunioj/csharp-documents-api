using users;
using utils;
using dbfiles;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Text;
using System.Web;
using database;
using Newtonsoft.Json;
using static System.Console;
using Microsoft.AspNetCore.Mvc;


namespace DocumentApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : Controller
{

    DataBase database = new DataBase();

    [HttpPost()]
    public string CreateUser(Dictionary<string,string> body){
        string message = "";
        bool validBody = body.ContainsKey("user") && body.ContainsKey("password");
        if (validBody){
            bool isCreated = database.CreateDBUser(body["user"],body["password"]);
            message = "user successfully created";   
        }
        else {
            message = "malformed body in request";
        }
        string returnObject = JsonConvert.SerializeObject(new {message=message});
        return returnObject;
    }

    [HttpPost("sign-in")]
    public Dictionary<string,string> SignIn(Dictionary<string,string> body){
        Dictionary<string,string> response = new Dictionary<string,string>();
        bool validBody = body.ContainsKey("user") && body.ContainsKey("password");
        if (validBody){
            string userType = database.VerifyDBUser(body["user"],body["password"]);

            switch(userType){
                case "user":
                case "admin":
                    string token = Utils.CreateToken(DataBase.SignKey,body["user"],userType,DataBase.Host);
                    response["token"] = token;
                break;
                default:
                    response["message"] = "cannot verify username and password";
                    break;
            }
        }

        else {
            response["message"] = "missing parameters from body";
        }
        return response;
    }

    [HttpPost("validate")]
    public Dictionary<string,string> Validate(){
        Dictionary<string,string> response = new Dictionary<string,string>();
        bool hasHeaders = Request.Headers.ContainsKey("Authorization");
        if (hasHeaders) {
            try {
                string token = Request.Headers["Authorization"].ToString().Split(" ")[1].Trim();
                Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
                response["message"] = "valid user";
            }
            catch {
                response["message"] = "could not verify creds";
            }      
    }
    else {
        response["message"] = "missing authentication header";
    }

        return response;
    }

}
