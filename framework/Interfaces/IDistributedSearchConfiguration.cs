namespace Mercury.Interfaces;

using Azure.Core;

public interface IDistributedSearchConfiguration
{
    string EventHubName { get; }

    string EventHubNamespaceName { get; }

    string EventHubTopicNameBusinessDataUpdates { get; }

    string ServiceBusTopicNameRequests { get; }

    string ProviderName { get; }

    string EventHubTopicNameResponses { get; }

    string EventHubConnectionString { get; }

    string ServiceBusConnectionString { get; }

    // Snapshot Storage
    string BusinessDataSnapshotAccountName { get; }

    string BusinessDataSnapshotContainerName { get; }

    TokenCredential AADServicePrincipal { get; }
}
