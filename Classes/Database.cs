using dbfiles;

using System;
using System.Web;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;
using System.Data.SqlClient;
using static System.Console;

namespace database;

class DataBase{

    public static SqlConnection Connection {get;set;}
    public static string Host{get;set;}

    public void LoadEnv(){
        string[] fileEntries = Directory.GetFiles(Directory.GetCurrentDirectory());
        string envFile = Array.Find(fileEntries, file => file.Contains(".env")); 
        string[] envValues = File.ReadAllLines(envFile);
        Dictionary<string,string> envFiles = new Dictionary<string,string>();
        TextInfo textinfo = new CultureInfo("en-GB", false).TextInfo;
        foreach (string line in envValues)
        {
            string[] lineValues = line.Split("=");
            string key = textinfo.ToTitleCase(lineValues[0].ToLower());
            string value = lineValues[1];
            envFiles[key] = value;
        }
        
        string[] parameters = {"Host","Password","Database","User"};
        if (parameters.All(parameter => envFiles.ContainsKey(parameter)))
        {
            string connectionString = 
                $"Data Source=({envFiles["Host"]});Initial Catalog={envFiles["Database"]};"+
                $"User ID={envFiles["User"]};Password={envFiles["Password"]}";
            WriteLine(connectionString);
            DataBase.Connection = new SqlConnection(connectionString); 
        }
    }

    public void LoadHost(){
        string settings = "./Properties/launchSettings.json";
        var json = File.ReadAllText(settings);   
        Dictionary<string, object>   mydictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        string[] lines = mydictionary["profiles"].ToString().Split(
            new string[] { Environment.NewLine },
            StringSplitOptions.None
        );
        string value = Array.Find(lines,element => element.Contains("http://localhost"));
        string regexPattern = @"http://localhost:[0-9]+";
        Regex regex = new Regex(regexPattern);
        Match match = regex.Match(value);
        DataBase.Host = value.Substring(match.Index,match.Length).Trim();    
    }

    public List<DbFile> GetDBFiles(){
        DataBase.Connection.Open();
        SqlCommand tableScan = new SqlCommand($"select file_id,name,created from files;",DataBase.Connection);
        SqlDataReader reader;
        reader = tableScan.ExecuteReader();
        List<DbFile> files = new List<DbFile>();
        while (reader.Read())
        {
            DbFile file = new DbFile(){Id=reader.GetInt16(0),Name=reader.GetString(1),Created=reader.GetDateTime(2),
            Path=$"{DataBase.Host}/Document/files/{reader.GetInt16(0)}?type=content"};
            files.Add(file);
        }
        DataBase.Connection.Close();
        return files;
    } 

    public DbFile GetDBFile(int fileId){
        DataBase.Connection.Open();
        SqlCommand tableScan = new SqlCommand($"select file_id,name,created from files where file_id={fileId};",DataBase.Connection);
        SqlDataReader reader;
        reader = tableScan.ExecuteReader();
        DbFile file = new DbFile();
        while (reader.Read())
        {
            file.Id=reader.GetInt16(0);
            file.Name=reader.GetString(1);
            file.Created=reader.GetDateTime(2);
            file.Path=$"{DataBase.Host}/Document/files/{reader.GetInt16(0)}?type=content";
        }
        DataBase.Connection.Close();
        return file;
    }

    public bool CreateDBFile(string fileName){
        DataBase.Connection.Open();
        SqlCommand insertCommand = new SqlCommand($"insert into files (name) values ('{fileName}');",DataBase.Connection);
        int rowsCommitted = insertCommand.ExecuteNonQuery();
        DataBase.Connection.Close();
        return rowsCommitted ==1;
    }
}