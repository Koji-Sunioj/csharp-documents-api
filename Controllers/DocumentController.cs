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
    public string GetFiles()
    {
        List<DbFile> files = database.GetDBFiles();
        Dictionary<string,List<DbFile>> response = new Dictionary<string,List<DbFile>>();
        response["files"] = files;
        string returnObject = JsonConvert.SerializeObject(response);
        return returnObject;
    }

    [HttpGet("files/{fileId}")]
    public dynamic GetFile(int fileId)
    {
        DbFile file = database.GetDBFile(fileId);
        dynamic response = null;
        switch(Request.Query["type"]){
            case "metadata":
                Dictionary<string, DbFile> responseObject = new Dictionary<string,DbFile>();
                responseObject["file"] = file;
                response = JsonConvert.SerializeObject(responseObject);
                break;
            case "content":
                string path = $"{Directory.GetCurrentDirectory()}/documents/{file.Name}";
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(path, out contentType);
                Stream fileStream  = new System.IO.FileStream(path, FileMode.Open);
                response = File(fileStream, contentType);
                break;

        }      
        return response;
    }

    [HttpPost("files")]
    public async Task<string> SaveFiles()
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
                    string[] currentDocs = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/documents");
                    IFormFile targetFile = files[0];
                    bool fileExists = currentDocs.Any(file=>file.Contains(targetFile.FileName));
                    FileStream fileStream  = System.IO.File.Create($"{Directory.GetCurrentDirectory()}/documents/{targetFile.FileName}");
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
