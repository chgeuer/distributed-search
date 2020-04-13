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

        public string EventHubNamespaceName { get => this.ProjectConfig.eventHubName; }

        public string EventHubTopicNameRequests { get => this.ProjectConfig.topicNames.requests; }

        public string EventHubTopicNameResponses { get => this.ProjectConfig.topicNames.responses; }

        public string EventHubTopicNameBusinessDataUpdates { get => this.ProjectConfig.topicNames.businessdataupdates; }

        public string EventHubConnectionString { get => this.ProjectConfig.eventHubConnectionString; }

        public string StorageOffloadAccountName { get => this.ProjectConfig.storageOffload.storageAccountName; }

        public string StorageOffloadContainerName { get => this.ProjectConfig.storageOffload.containerName; }

        public string BusinessDataSnapshotAccountName { get => this.ProjectConfig.snapshotStorage.storageAccountName; }

        public string BusinessDataSnapshotContainerName { get => this.ProjectConfig.snapshotStorage.containerName; }

        public TokenCredential AADServicePrincipal
        {
            get => new ClientSecretCredential(
                tenantId: (string)this.ProjectConfig.servicePrincipal.tenantId,
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