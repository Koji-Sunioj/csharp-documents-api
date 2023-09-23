using dbfiles;
using System.Text;
using System.Web;
using System.IO;
using database;
using Newtonsoft.Json;
using static System.Console;
using Microsoft.AspNetCore.Mvc;

namespace DocumentApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : Controller //Controller
{

    DataBase database = new DataBase();
    [HttpGet("test")]
    public  string Get()
    {
        string json = JsonConvert.SerializeObject(new
        {
            message = "api is workings",

        }); 
        
        return json;
    }


    [HttpGet("files")]
    public string GetFiles()
    {
        List<DbFile> files = database.GetFiles();
        Dictionary<string,List<DbFile>> response = new Dictionary<string,List<DbFile>>();
        response["files"] = files;
        string returnObject = JsonConvert.SerializeObject(response);
        return returnObject;
    }

    [HttpGet("files/{fileName}")]
    public IActionResult GetFile(string fileName)
    {
        string shit = "/home/koji/Desktop/csharp/DocumentApi/documents/test.jpeg";
        Stream fileStream  = new System.IO.FileStream(shit, FileMode.Open);
        return File(fileStream, "image/jpeg");
    }

    [HttpPost("files")]
    public string SaveFiles()
    { 
        string json = JsonConvert.SerializeObject(new
        {
            message = "api is workings",

        }); 

        return json;
    }
}
