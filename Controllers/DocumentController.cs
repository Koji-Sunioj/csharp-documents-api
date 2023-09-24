using dbfiles;
using System.Net;
using System.Text;
using System.Web;
using System.IO;
using database;
using Newtonsoft.Json;
using static System.Console;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

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
        string host = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        List<DbFile> files = database.GetDBFiles(host);
        Dictionary<string,List<DbFile>> response = new Dictionary<string,List<DbFile>>();
        response["files"] = files;
        string returnObject = JsonConvert.SerializeObject(response);
        return returnObject;
    }

    [HttpGet("files/{fileName}")]
    public dynamic GetFile(int fileName)
    {
        string host = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        DbFile file = database.GetDBFile(fileName,host);
        dynamic response = null;
        switch(Request.Query["type"])
        {
            case "metadata":
                Dictionary<string, DbFile> responseObject = new Dictionary<string,DbFile>();
                responseObject["file"] = file;
                response = JsonConvert.SerializeObject(responseObject);
                break;
            case "content":
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(file.Path, out contentType);

                Stream fileStream  = new System.IO.FileStream(file.Path, FileMode.Open);
                response = File(fileStream, contentType);
                break;

        }      
        return response;
    }

    [HttpPost("files")]
    public string SaveFiles()
    { 
        
        
        
        
       /*  response["file"] = file; */
        string json = JsonConvert.SerializeObject(new
        {
            message = "api is workings",

        }); 

        return json;
    }
}
