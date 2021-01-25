using BlazorProducts.Server.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using BlazorContacts.Server.Services;
namespace BlazorContacts.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .CreateDbIfNotExists()
                .BuildIndexIfNotExists()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
