#!/bin/bash

#
# Tweak line #7 (prefix) with something different
#
# Repro that I somehow can create an event hub with capture pointing to an immutable container,
# but once I try to get an SAS for the Event Hub, it blows off:
#
# "BlobContainer: This operation is not permitted as the blob is immutable due to a policy."

export prefix="chgpdnata"
export rg_name="dnata"
export location="westeurope"
export eventhub_namespace_name="${prefix}"
export eventhub_name="${prefix}"
export storage_account_name="${prefix}"
export storage_container_name="requests"

az group create \
    --resource-group "${rg_name}" \
    --location "${location}"

az storage account create \
    --resource-group "${rg_name}" \
    --location "${location}" \
    --name "${storage_account_name}" \
    --kind "StorageV2" \
    --sku "Standard_RAGRS"

az storage container create \
    --account-name "${storage_account_name}" \
    --name "${storage_container_name}" \
    --public-access "off"

az storage container immutability-policy create \
    --resource-group "${rg_name}" \
    --account-name "${storage_account_name}" \
    --container-name "${storage_container_name}" \
    --period 365

az eventhubs namespace create \
    --resource-group "${rg_name}" \
    --location "${location}" \
    --name "${eventhub_namespace_name}" \
    --enable-kafka true \
    --capacity 1 \
    --sku Standard

az eventhubs eventhub create \
    --resource-group "${rg_name}" \
    --namespace-name "${eventhub_namespace_name}" \
    --name "${eventhub_name}" \
    --message-retention 4 \
    --partition-count 1 \
    --enable-capture true \
    --destination-name="EventHubArchive.AzureBlockBlob" \
    --archive-name-format "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}" \
    --capture-interval 60 \
    --capture-size-limit 524288000 \
    --storage-account "${storage_account_name}" \
    --blob-container "${storage_container_name}"

#
# This will fail
#
az eventhubs eventhub authorization-rule create \
    --resource-group "${rg_name}" \
    --namespace-name "${eventhub_namespace_name}" \
    --eventhub-name "${eventhub_name}" \
    --name "webapi" \
    --rights Listen Send

az ad app list

servicePrincipalDisplayName="kafkaclient"
principal="$( az ad sp list --display-name "${servicePrincipalDisplayName}" | jq ".[0]" )"
appId="$( echo "${principal}" | jq -r ".appId" )"
objectId="$( echo "${principal}" | jq -r ".objectId" )"

eventHubId="$(az eventhubs eventhub show \
    --resource-group "${rg_name}" \
    --namespace-name "${eventhub_namespace_name}" \
    --name "${eventhub_name}" \
    | jq -r ".id" )"

receiverRole="$( az role definition list \
    --name "Azure Event Hubs Data Receiver" \
    | jq -r ".[0].name" )"

senderRole="$( az role definition list \
    --name "Azure Event Hubs Data Sender" \
    | jq -r ".[0].name" )"

ownerRole="$( az role definition list \
    --name "Azure Event Hubs Data Owner" \
    | jq -r ".[0].name" )"

# https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-event-hubs-data-owner
az role assignment create \
    --assignee-principal-type ServicePrincipal \
    --assignee-object-id "${objectId}" \
    --scope "${eventHubId}" \
    --role "${receiverRole}"

az role assignment create \
    --assignee-principal-type ServicePrincipal \
    --assignee-object-id "${objectId}" \
    --scope "${eventHubId}" \
    --role "${senderRole}"

#az eventhubs eventhub authorization-rule keys list \
#    --resource-group "${rg_name}" \
#    --namespace-name "${eventhub_namespace_name}" \
#    --eventhub-name "${eventhub_name}" \
#    --name "webapi" \
#    | jq -r ".primaryConnectionString"
