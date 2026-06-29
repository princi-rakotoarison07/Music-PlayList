using System.IO;
using Microsoft.Extensions.Configuration;

namespace desktop_server_app.Config
{
    public static class AppConfig
    {
        public static IConfiguration Root { get; }

        static AppConfig()
        {
            Root = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
    }
}