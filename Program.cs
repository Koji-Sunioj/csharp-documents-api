using database;

using Microsoft.Extensions.FileProviders;
using static System.Console;


class Program
{
    static void Main(string[] args)
    {

        DataBase database = new DataBase();
        database.LoadEnv();

        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
     
        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();  
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider($"{Directory.GetCurrentDirectory()}/documents"),
            RequestPath = "/documents",
        });

        app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            FileProvider = new PhysicalFileProvider($"{Directory.GetCurrentDirectory()}/documents"),
            RequestPath = "/documents",
        });      

        app.UseAuthorization();

        app.MapControllers();

        app.Run();

   }
}