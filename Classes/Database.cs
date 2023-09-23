using dbfiles;

using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Data.SqlClient;
using static System.Console;

namespace database;

class DataBase{

    public static SqlConnection Connection {get;set;}

    public void LoadEnv(){
        string root = Directory.GetCurrentDirectory();
        string[] fileEntries = Directory.GetFiles(root);
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

    public List<DbFile> GetFiles(){
        DataBase.Connection.Open();
        SqlCommand tableScan = new SqlCommand($"select file_id,name,created from files;",DataBase.Connection);
        SqlDataReader reader;
        reader = tableScan.ExecuteReader();
        List<DbFile> files = new List<DbFile>();
        while (reader.Read())
        {
            DbFile file = new DbFile(){Id=reader.GetInt16(0),Name=reader.GetString(1),Created=reader.GetDateTime(2)};
            files.Add(file);
        }
        DataBase.Connection.Close();
        return files;
    } 
}