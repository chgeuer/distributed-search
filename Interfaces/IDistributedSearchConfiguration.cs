namespace Mercury.Interfaces
{
    using Azure.Core;

    public interface IDistributedSearchConfiguration
    {
        string EventHubName { get; }

        string EntityPath { get; }

        string EventHubNamespaceName { get; }

        string EventHubTopicNameRequests { get; }

        string EventHubTopicNameBusinessDataUpdates { get; }

        string EventHubTopicNameResponses { get; }

        string EventHubConnectionString { get; }

        string EventHubCaptureStorageAccountConnectionString { get; }

        string EventHubCaptureStorageAccountContainerName { get; }

        string StorageOffloadAccountName { get; }

        string BusinessDataSnapshotAccountName { get; }

        string BusinessDataSnapshotContainerName { get; }

        string StorageOffloadContainerNameRequests { get; }

        string StorageOffloadContainerNameResponses { get; }

        TokenCredential AADServicePrincipal { get; }
    }
}
