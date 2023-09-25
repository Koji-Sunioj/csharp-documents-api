using utils;
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

 
    [HttpGet("files")]
    public string GetFiles([FromHeader] Header header)
    {
        string token = header.Authorization.Split(" ")[1].Trim();
        Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
        List<DbFile> files = database.GetDBFiles(jwtPayload["user"],jwtPayload["role"]);
        Dictionary<string,List<DbFile>> response = new Dictionary<string,List<DbFile>>();
        response["files"] = files;
        string returnObject = JsonConvert.SerializeObject(response);
        return returnObject;
    }

    [HttpGet("files/{fileId}/metadata")]
    public dynamic GetFileMetaData([FromHeader] Header header,int fileId)
    {
        string token = header.Authorization.Split(" ")[1].Trim();
        Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
        DbFile file = database.GetDBFile(fileId,jwtPayload["user"],jwtPayload["role"]);
        dynamic response = null;
        if (file == null) {
            Dictionary<string,string> something = new Dictionary<string,string>();
            something["message"] = "invalid creds";
            response =something;
        }
        else {
            Dictionary<string, DbFile> responseObject = new Dictionary<string,DbFile>();
            responseObject["file"] = file;
            response = JsonConvert.SerializeObject(responseObject);
        }
        return response;
    }

    [HttpGet("files/{fileName}/content")]
    public dynamic GetFileContent(string fileName)
    {
        string path = $"{Directory.GetCurrentDirectory()}/documents/{fileName}";
        string contentType;
        new FileExtensionContentTypeProvider().TryGetContentType(path, out contentType);
        Stream fileStream  = new System.IO.FileStream(path, FileMode.Open);
        FileStreamResult response = File(fileStream, contentType);
        return response;
    }


    [HttpPost("files")]
    public async Task<string> SaveFile()
    { 
        string message = "";

        if (Request.ContentType.Contains("multipart/form-data"))
        {
            IFormFileCollection files = Request.Form.Files;
            string[] fileTypes = files.Select(file=>file.ContentType).ToArray();
            bool isOnlyPdf = fileTypes.All(file => file.Contains("pdf"));
            string fileCommand = isOnlyPdf && files.Count() == 1 ? "save" : "reject";

            switch(fileCommand){
                case "save":
                    string rootDir = $"{Directory.GetCurrentDirectory()}/documents";
                    string[] currentDocs = Directory.GetFiles(rootDir);
                    IFormFile targetFile = files[0];
                    bool fileExists = currentDocs.Any(file=>file.Contains(targetFile.FileName));
                    FileStream fileStream  = System.IO.File.Create($"{rootDir}/{targetFile.FileName}");
                    targetFile.CopyTo(fileStream);
                    fileStream.Dispose();
                    if (!fileExists)
                    {
                        bool created = database.CreateDBFile(targetFile.FileName);
                    }
                    message = $"file {targetFile.FileName} created";
                    break;
                case "reject":
                    message = "can only accept pdfs, one file at a time";
                    break;
            }
        }
        else
        {
             message = "can only accept multipart";
        }
        
        string json = JsonConvert.SerializeObject(new{ message = message}); 
        return json;
    }
}


public class Header{
    [FromHeader]
    public string Authorization { get; set; }
}