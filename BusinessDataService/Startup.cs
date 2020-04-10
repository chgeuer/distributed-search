namespace Mercury.BusinessDataService
{
    using System;
    using Azure.Storage.Blobs;
    using Fashion;
    using Mercury.BusinessDataPump;
    using Mercury.Credentials;
    using Mercury.Interfaces;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Fashion.BusinessData;
    using static Mercury.Fundamentals.Types;

    public class Startup
    {
        public IConfiguration Configuration { get; }

        private readonly IDistributedSearchConfiguration demoCredential;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.demoCredential = new DemoCredential();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => this.GetCurrentBusinessData<FashionBusinessData, FashionBusinessDataUpdate>(
                newFashionBusinessData, FashionExtensions.ApplyFashionUpdate));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }

        internal Func<BusinessData<TBusinessData>> GetCurrentBusinessData<TBusinessData, TBusinessDataUpdate>(
            Func<TBusinessData> createEmptyBusinessData,
            Func<TBusinessData, TBusinessDataUpdate, TBusinessData> applyUpdate)
        {
            var businessDataUpdates = new BusinessDataPump<TBusinessData, TBusinessDataUpdate>(
                demoCredential: this.demoCredential,
                createEmptyBusinessData: createEmptyBusinessData,
                applyUpdate: applyUpdate,
                snapshotContainerClient: new BlobContainerClient(
                    blobContainerUri: new Uri($"https://{this.demoCredential.BusinessDataSnapshotAccountName}.blob.core.windows.net/{this.demoCredential.BusinessDataSnapshotContainerName}/"),
                    credential: this.demoCredential.AADServicePrincipal));

            businessDataUpdates.StartUpdateProcess().Wait();
            return () => businessDataUpdates.BusinessData;
        }

    }
}
