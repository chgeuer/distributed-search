// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
namespace Mercury.Credentials
{
    using System;
    using System.IO;
    using Azure.Core;
    using Azure.Identity;
    using Mercury.Interfaces;
    using Newtonsoft.Json;

    public class DemoCredential : IDistributedSearchConfiguration
    {
        private readonly dynamic data = ReadCredData();

        public string EventHubName { get => this.ProjectConfig.eventHubName; }

        public string EntityPath { get => this.ProjectConfig.entityPath; }

        public string EventHubNamespaceName { get => this.EntityPath; }

        public string EventHubTopicNameRequests { get => this.ProjectConfig.topicNames.requests; }

        public string EventHubTopicNameBusinessDataUpdates { get => this.ProjectConfig.topicNames.businessdataupdates; }

        public string EventHubTopicNameResponses { get => this.ProjectConfig.topicNames.responses; }

        public string EventHubConnectionString { get => this.ProjectConfig.sharedAccessConnectionStringRoot; }

        public string EventHubCaptureStorageAccountConnectionString { get => this.ProjectConfig.capture.storageConnectionString; }

        public string EventHubCaptureStorageAccountContainerName { get => this.ProjectConfig.capture.containerName; }

        public string StorageOffloadAccountName { get => this.ProjectConfig.storageOffload.storageAccountName; }

        public string BusinessDataSnapshotAccountName { get => this.StorageOffloadAccountName; }

        public string BusinessDataSnapshotContainerName { get => this.ProjectConfig.snapshotStorageContainer; }

        public string StorageOffloadContainerNameRequests { get => this.ProjectConfig.storageOffload.containerName.requests; }

        public string StorageOffloadContainerNameResponses { get => this.ProjectConfig.storageOffload.containerName.responses; }

        public TokenCredential AADServicePrincipal
        {
            get => new ClientSecretCredential(
                tenantId: (string)this.data.azure.tenantId,
                clientId: (string)this.ProjectConfig.servicePrincipal.clientId,
                clientSecret: (string)this.ProjectConfig.servicePrincipal.clientSecret);
        }

        private dynamic ProjectConfig { get => this.data.flightbooking; }

        private static dynamic ReadCredData()
        {
            return JsonConvert.DeserializeObject<dynamic>(
                File.ReadAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "creds.json")));
        }
    }
}