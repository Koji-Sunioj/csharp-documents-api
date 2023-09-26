using users;
using utils;
using dbfiles;
using System.Net;
using System.Text;
using System.Web;
using database;
using Newtonsoft.Json;
using static System.Console;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DocumentApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : Controller
{

    DataBase database = new DataBase();

    [HttpPost()]
    public IActionResult CreateUser(JsonUser body){
        Dictionary<string,string> response = new Dictionary<string,string>();
        bool isCreated = database.CreateDBUser(body.user,body.password);
        response["message"] = isCreated ? "user successfully created" : "error creating user";   
        return isCreated ? Ok(response) : StatusCode(400,response);
    }

    [HttpPost("sign-in")]
    public IActionResult SignIn(JsonUser body) {
        string role = database.VerifyDBUser(body.user,body.password);
        switch(role) {
            case "user":
            case "admin":
                Dictionary<string,string> response = new Dictionary<string,string>();
                string token = Utils.CreateToken(DataBase.SignKey,body.user,role,DataBase.Host);
                response["token"] = token;
                return Ok(response);
            default:
                return StatusCode(401);     
        }
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromHeader] Utils.Header header){
        Dictionary<string,string> response = new Dictionary<string,string>();
        try {
            string token = header.Authorization.Split(" ")[1].Trim();
            Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
            response["message"] = "valid user";
            return Ok(response);
        }
        catch {
            return StatusCode(400); 
        }       
    }
}


public class JsonUser
{
    [StringLength(20, MinimumLength = 4)]
    [Required]
    public string? user { get; set; }

    [StringLength(20, MinimumLength = 3)]
    [Required]
    public string? password { get; set; }
}
