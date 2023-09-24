using dbfiles;

using System;
using System.Web;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Data.SqlClient;
using static System.Console;

namespace database;

class DataBase{

    public static SqlConnection Connection {get;set;}
    public static string RootDir = Directory.GetCurrentDirectory();

    public void LoadEnv(){
        string[] fileEntries = Directory.GetFiles(DataBase.RootDir);
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

    public List<DbFile> GetDBFiles(string host){
        DataBase.Connection.Open();
        SqlCommand tableScan = new SqlCommand($"select file_id,name,created from files;",DataBase.Connection);
        SqlDataReader reader;
        reader = tableScan.ExecuteReader();
        List<DbFile> files = new List<DbFile>();
        while (reader.Read())
        {
            DbFile file = new DbFile(){Id=reader.GetInt16(0),Name=reader.GetString(1),Created=reader.GetDateTime(2),
            Path=$"{host}/documents/{reader.GetString(1)}"};
            files.Add(file);
        }
        DataBase.Connection.Close();
        return files;
    } 

    public DbFile GetDBFile(int fileId,string host){
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
            file.Path=Uri.EscapeUriString($"{host}/documents/{reader.GetString(1)}") ;
        }
        DataBase.Connection.Close();
        return file;
    }
}