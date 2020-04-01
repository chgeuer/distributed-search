// Copyright (c) Microsoft Corporation. Licensed under the MIT License.
namespace Credentials
{
    using System;
    using System.IO;
    using Azure.Core;
    using Azure.Identity;
    using Newtonsoft.Json;

    public static class DemoCredential
    {
        public static string EventHubName { get => ProjectConfig.eventHubName; }

        public static string EntityPath { get => ProjectConfig.entityPath; }

        public static string EventHubNamespaceName { get => EntityPath; }

        public static string EventHubTopicNameRequests { get => ProjectConfig.topicNames.requests; }

        public static string EventHubTopicNameBusinessDataUpdates { get => "businessdataupdates"; }

        public static string EventHubTopicNameResponses { get => ProjectConfig.topicNames.responses; }

        public static string EventHubConnectionString { get => ProjectConfig.sharedAccessConnectionStringRoot; }

        public static string EventHubCaptureStorageAccountConnectionString { get => ProjectConfig.capture.storageConnectionString; }

        public static string EventHubCaptureStorageAccountContainerName { get => ProjectConfig.capture.containerName; }

        public static string StorageOffloadAccountName { get => ProjectConfig.storageOffload.storageAccountName; }

        public static string BusinessDataSnapshotAccountName { get => StorageOffloadAccountName; }

        public static string BusinessDataSnapshotContainerName { get => "snapshots"; }

        public static string StorageOffloadContainerNameRequests { get => ProjectConfig.storageOffload.containerName.requests; }

        public static string StorageOffloadContainerNameResponses { get => ProjectConfig.storageOffload.containerName.responses; }

        public static TokenCredential AADServicePrincipal
        {
            get => new ClientSecretCredential(
                tenantId: (string)Data.azure.tenantId,
                clientId: (string)ProjectConfig.servicePrincipal.clientId,
                clientSecret: (string)ProjectConfig.servicePrincipal.clientSecret);
        }

        private static dynamic ProjectConfig { get => Data.flightbooking; }

        private static readonly dynamic Data = ReadCredData();

        private static dynamic ReadCredData()
        {
            return JsonConvert.DeserializeObject<dynamic>(
                File.ReadAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "creds.json")));
        }
    }
}