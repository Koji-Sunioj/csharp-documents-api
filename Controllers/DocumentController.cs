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
public class DocumentController : Controller 
{

    DataBase database = new DataBase();

 
    [HttpGet("files")]
    public IActionResult GetFiles([FromHeader] Header header)
    {
        string token = header.Authorization.Split(" ")[1].Trim();
        Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
        List<DbFile> files = database.GetDBFiles(jwtPayload["user"],jwtPayload["role"]);
        Dictionary<string,List<DbFile>> response = new Dictionary<string,List<DbFile>>();
        response["files"] = files;
        string returnObject = JsonConvert.SerializeObject(response);
        return Ok(returnObject);
    }

    [HttpGet("files/{fileId}/metadata")]
    public dynamic GetFileMetaData([FromHeader] Header header,int fileId)
    {
        string token = header.Authorization.Split(" ")[1].Trim();
        Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
        DbFile file = database.GetDBFile(fileId,jwtPayload["user"],jwtPayload["role"]);
        if (file == null) {
            return StatusCode(401);
        }
        else {
            Dictionary<string, DbFile> response = new Dictionary<string,DbFile>();
            response["file"] = file;
            string responseObject = JsonConvert.SerializeObject(response);
            return Ok(responseObject);
        }
    }

    [HttpGet("files/{fileName}/content")]
    public dynamic GetFileContent([FromHeader] Header header,string fileName)
    {

        string token = header.Authorization.Split(" ")[1].Trim();
        dynamic response = null;
        Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
        bool releaseFile = jwtPayload["role"] == "user" ? 
            database.CanAccessFile(jwtPayload["user"],fileName) : true;
     
        if (releaseFile) {
            string path = $"{Directory.GetCurrentDirectory()}/documents/{fileName}";
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(path, out contentType);
            Stream fileStream  = new System.IO.FileStream(path, FileMode.Open);
            response = File(fileStream, contentType);
        }
        
        else {
            Dictionary<string,string> rejection = new Dictionary<string,string>();
            rejection["message"] = "you are not authorized to view this file" ;
            response = rejection;
        }

        return response;
    }


    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    public async Task<string> SaveFile([FromHeader] Header header)
    { 
        string message = "";
        IFormFileCollection files = Request.Form.Files;
        string[] fileTypes = files.Select(file=>file.ContentType).ToArray();
        bool isOnlyPdf = fileTypes.All(file => file.Contains("pdf"));
        string fileCommand = isOnlyPdf && files.Count() == 1 ? "save" : "reject";

        switch(fileCommand) {
            case "save":
                IFormFile targetFile = files[0];
                string rootDir = $"{Directory.GetCurrentDirectory()}/documents";
                string[] currentDocs = Directory.GetFiles(rootDir);
                bool fileExists = currentDocs.Any(file=>file.Contains(targetFile.FileName));
                string token = header.Authorization.Split(" ")[1].Trim();
                Dictionary<string,string> jwtPayload = Utils.CheckToken(DataBase.SignKey,DataBase.Host,token);
                bool shouldSave = jwtPayload["role"] == "user" ? 
                    database.CanAccessFile(jwtPayload["user"],targetFile.FileName) || !fileExists : true;
                if (shouldSave) {
                    FileStream fileStream  = System.IO.File.Create($"{rootDir}/{targetFile.FileName}");
                    targetFile.CopyTo(fileStream);
                    fileStream.Dispose();
                    if (!fileExists) {
                        database.CreateDBFile(targetFile.FileName,jwtPayload["user"]);
                    }
                    message = $"file {targetFile.FileName} created";
                }

                else {
                    message = "no authorization to modify existing file";
                }
                break;
            case "reject":
                message = "can only accept pdfs, one file at a time";
                break;
        }
        
        
        string json = JsonConvert.SerializeObject(new{ message = message}); 
        
        return json;
    }
}


public class Header{
    [FromHeader]
    public string Authorization { get; set; }
}