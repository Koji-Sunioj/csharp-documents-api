using dbfiles;
using users;
using BC = BCrypt.Net.BCrypt;
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

class DataBase {

    public static SqlConnection Connection {get;set;}
    public static string Host{get;set;}
    public static string SignKey {get;set;}

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
        
        string[] parameters = {"Host","Password","Database","User","Key"};
        if (parameters.All(parameter => envFiles.ContainsKey(parameter)))
        {
            string connectionString = 
                $"Data Source=({envFiles["Host"]});Initial Catalog={envFiles["Database"]};"+
                $"User ID={envFiles["User"]};Password={envFiles["Password"]}";
            WriteLine(connectionString);
            DataBase.Connection = new SqlConnection(connectionString); 
            DataBase.SignKey = envFiles["Key"];
        }
    }

    public string EncodeURL(string fileName){
        string path = $"{DataBase.Host}/Document/files/{HttpUtility.UrlPathEncode(fileName)}/content";
        return path;
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

    public List<DbFile> GetDBFiles(string userName,string role){
        DataBase.Connection.Open();
        string command = "select files.file_id,files.name,files.created from files";
        string endCommand = role == "admin" ? 
            $"{command};" : $"{command} join users on users.user_id = files.owner where users.name='{userName}';";
 
        SqlCommand tableScan = new SqlCommand(endCommand,DataBase.Connection);
        SqlDataReader reader;
        reader = tableScan.ExecuteReader();
        List<DbFile> files = new List<DbFile>();
        while (reader.Read())
        {
            DbFile file = new DbFile(){Id=reader.GetInt16(0),Name=reader.GetString(1),Created=reader.GetDateTime(2),
            Path=EncodeURL(reader.GetString(1))};
            files.Add(file);
        }
        DataBase.Connection.Close();
        return files;
    } 

    public bool CanAccessFile(string userName,string fileName) {
        DataBase.Connection.Open();
        bool sameUser = false;
        string command = $"select users.name from users join files on files.owner = users.user_id where files.name='{fileName}';";
        SqlCommand select = new SqlCommand(command,DataBase.Connection);
        SqlDataReader reader;
        reader = select.ExecuteReader();
        if (reader.HasRows) {
            while (reader.Read()) {
                WriteLine(reader.GetString(0));
                sameUser=reader.GetString(0) == userName;
            }
        }
        DataBase.Connection.Close();
        return sameUser;

    }

    public DbFile GetDBFile(int fileId,string userName,string role){
        DataBase.Connection.Open();
        string command = $"select files.file_id,files.name,files.created from files";
        string endCommand = role == "admin" ? 
             $"{command} where files.file_id={fileId};" : $"{command} join users on users.user_id = files.owner where files.file_id={fileId} and users.name='{userName}';";
        SqlCommand select = new SqlCommand(endCommand,DataBase.Connection);
        SqlDataReader reader;
        reader = select.ExecuteReader();
        DbFile file = new DbFile();
       
        if (reader.HasRows){
            while (reader.Read()) {
                file.Id=reader.GetInt16(0);
                file.Name=reader.GetString(1);
                file.Created=reader.GetDateTime(2);
                file.Path=EncodeURL(reader.GetString(1));
            }
        }
        
        else {
            file = null;
        }
        DataBase.Connection.Close();
        return file;
    }

    public void CreateDBFile(string fileName, string userName) {
        int userId = 0;
        DataBase.Connection.Open();
        string getUserId = $"select users.user_id from users where users.name='{userName}'";
        SqlCommand selectUserId = new SqlCommand(getUserId,DataBase.Connection);
        SqlDataReader reader;
        reader = selectUserId.ExecuteReader();
        while (reader.Read()) {
             userId = reader.GetInt16(0);
        }
        DataBase.Connection.Close();
        if (userId > 0) {
            DataBase.Connection.Open();
            string command = $"insert into files (name,owner) values ('{fileName}',{userId});";
            SqlCommand insertCommand = new SqlCommand(command,DataBase.Connection);
            insertCommand.ExecuteNonQuery();
        }
        DataBase.Connection.Close();
    }

    public bool CreateDBUser(string userName,string password){
        string pwHash =  BC.HashPassword(password);
        string command = "insert into users (name,role,password) values ('{0}','{1}','{2}')";
        string role = userName == "koji" ? "admin" : "user";
        DataBase.Connection.Open();
        SqlCommand insertCommand = new SqlCommand(String.Format(command,userName,role,pwHash),DataBase.Connection);
        int rowsCommitted = insertCommand.ExecuteNonQuery();
        DataBase.Connection.Close();
        return rowsCommitted ==1;
    }

    public string VerifyDBUser(string userName,string password){
        User user = new User();
        string role = "non-user";
        DataBase.Connection.Open();
        SqlCommand selectUser = new SqlCommand($"select password,role from users where name='{userName}';",DataBase.Connection);
        SqlDataReader reader;
        reader = selectUser.ExecuteReader();
        while (reader.Read()) {
             role = BC.Verify(password,reader.GetString(0)) ? reader.GetString(1) : role;
        }
        DataBase.Connection.Close();
        return role;
    }
}