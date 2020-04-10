namespace Mercury.BusinessDataService
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    public class BusinessDataServiceProgram
    {
        public static void Main(string[] args)
        {
            Console.Title = "Business Data Service";

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<BusinessDataStartup>();
                });
    }
}