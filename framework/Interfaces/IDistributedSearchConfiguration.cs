namespace Mercury.Interfaces
{
    using Azure.Core;

    public interface IDistributedSearchConfiguration
    {
        string EventHubName { get; }

        string EventHubNamespaceName { get; }

        string EventHubTopicNameBusinessDataUpdates { get; }

        string EventHubTopicNameRequests { get; }

        string EventHubTopicNameResponses { get; }

        string EventHubConnectionString { get; }

        // Snapshot Storage
        string BusinessDataSnapshotAccountName { get; }

        string BusinessDataSnapshotContainerName { get; }

        // Storage Offload
        string StorageOffloadAccountName { get; }

        string StorageOffloadContainerName { get; }

        TokenCredential AADServicePrincipal { get; }
    }
}
