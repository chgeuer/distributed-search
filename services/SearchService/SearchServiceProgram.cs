namespace Mercury.Services.SearchService
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    public class SearchServiceProgram
    {
        public static void Main(string[] args)
        {
            Console.Title = "Search Service";

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<SearchServiceStartup>();
                });
    }
}
