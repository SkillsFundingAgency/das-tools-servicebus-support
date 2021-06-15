# ServiceBus Support Utility

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_apis/build/status/das-tools-servicebus-support?branchName=master)](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_build/latest?definitionId=2281&branchName=master)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)
[![Jira Project](https://img.shields.io/badge/Jira-Project-blue)](https://skillsfundingagency.atlassian.net/secure/RapidBoard.jspa?rapidView=564&projectKey=QUAL)
[![Confluence Project](https://img.shields.io/badge/Confluence-Project-blue)](https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/2049115342/Azure+Service+Bus+Error+Queue+Management)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-admin-service&metric=alert_status)](https://sonarcloud.io/dashboard?id=SkillsFundingAgency_das-tools-servicebus-support)

The ServiceBus Support Utility is an Azure ServiceBus Queue management tool that allows you to manage messages that have moved to error queues without having to resort to managing each message individually.

1. Utilises Azure Active Directory for Authentication
1. Automatically enumerates error queues within the Azure Service Bus namespace
1. Messages can be retrieved per queue
1. Retrieved messages can be:
    - Aborted - all retrieved messages will be placed back on the queue they were received from
    - Replayed - messages will be moved back onto the original processing queue so that they can be processed again
    - Deleted - messages will be removed and will be no longer available for processing

## How It Works

The ServiceBus Utility is a combination of Website and background processor that enumerates Azure Service Bus queues within a namespace using the error queue naming convention and presents them to the user as a selectable list that allows the messages on a queue to be retrieved for investigation. Once a queue has been selected the website will receive the messages from the error queue and place them into a CosmosDB under the exclusive possession of the logged in user. Once the messages have been moved into the CosmosDB the background processor will ensure that those messages are held for a maximum sliding time period of 24 hours. If messages are still present after this period expires the background process will move them back to the error queue automatically so that they aren't held indefinitely.

Depending on the action performed by the user the messages will follow one of three paths. In the event that the user Aborts the process, the messages are moved back to the error queue they came from, if the user replays the messages they will be placed back onto the "processing queue" they were on prior to ending up in the error queue and will be removed from the CosmosDB. If the user deletes the messages then they will be removed from the CosmosDB and will be gone forever.

## 🚀 Installation

### Pre-Requisites

* A clone of this repository
* A code editor that supports Azure functions and .NetCore 3.1
* A CosmosDB instance or emulator
* An Azure Service Bus instance
* An Azure Active Directory account with the appropriate roles as per the [config](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-tools-servicebus-support/SFA.DAS.Tools.Servicebus.Support.json)
* The [das-audit](https://github.com/SkillsFundingAgency/das-audit) API available either running locally or accessible in an Azure tenancy    

### Config

This utility uses the standard Apprenticeship Service configuration. All configuration can be found in the [das-employer-config repository](https://github.com/SkillsFundingAgency/das-employer-config).

* A connection string for either the Apprenticeship Services ASB namespace or a namespace you own for development
* A CosmosDB connection string for either the Apprenticeship Service instance CosmosDB or a CosmosDB you own for development (you can use the emulator)
* Configure the [das-audit](https://github.com/SkillsFundingAgency/das-audit) project as per [das-employer-config](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-audit/SFA.DAS.AuditApiClient.json)
* Add an appsettings.Development.json file
    * Add your connection strings for CosmosDB and ASB to the relevant sections of the file
* The CosmosDB will be created automatically if it does not already exist and the credentials you are connected with have the appropriate rights within the Azure tenant otherwise it will need to be created manually using the details in the config below under `CosmosDbSettings`.

AppSettings.Development.json file
```json
{
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;",
    "ConfigNames": "SFA.DAS.Tools.Servicebus.Support,SFA.DAS.AuditApiClient",
    "EnvironmentName": "LOCAL",
    "Version": "1.0",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "",
    "AzureAdConfiguration": {
      "ClientId": "",
      "ClientSecret": "",
      "Authority": ""
    }
  }  
```

Azure Table Storage config

Row Key: SFA.DAS.Tools.Servicebus.Support_1.0

Partition Key: LOCAL

Data:

```json
{
  "BaseUrl": "localhost:5001",
  "UserIdentitySettings":{
    "RequiredRole": "Servicebus Admin", 
    "UserSessionExpiryHours": 24,
    "UserRefreshSessionIntervalMinutes": 5,
    "NameClaim": "name"
  },
  "ServiceBusSettings":{
    "ServiceBusConnectionString": "",
    "QueueSelectionRegex": "[-,_]+error",
    "PeekMessageBatchSize": 10,
    "MaxRetrievalSize": 250,
    "ErrorQueueRegex": "[-,_]error[s]*$",
    "RedactPatterns": [
      "(.*SharedAccessKey=)([\\s\\S]+=)(.*)"
    ]
  },
  "CosmosDbSettings":{
    "Url": "",
    "AuthKey": "",
    "DatabaseName": "QueueExplorer",
    "CollectionName": "Session",
    "Throughput": 400,
    "DefaultCosmosOperationTimeout": 55,
    "DefaultCosmosInterimRequestTimeout": 2
  }
}
```

Row Key: SFA.DAS.AuditApiClient_1.0

Partition Key: LOCAL

Data:
```json
{
	"ApiBaseUrl": "",
	"ClientId": "",
	"ClientSecret": "",
	"IdentifierUri": "",
	"Tenant": "",
	"DatabaseConnectionString": "",
	"ServiceBusConnectionString": ""
}
```

<details><summary><b>Show AAD instructions</b></summary>

Creating an AAD Account in an Azure tenant:

* In azure portal go to Users and Create a new user
* Give user access to the Service Bus
    * Service Bus Namespace > Access control (IAM) > Add role assignment  
        * Role - Azure Service Bus Data Receiver (and potentially Sender if you need to populate the queue in the first place)
        * Select - user name
* To configure the user in Visual Studio
    * Navigate to Tools > Options > Azure Service Authentication > Account Selection
    * Add the credentials of the user from above

</details>

## 🔗 External Dependencies

* This utility uses the [das-audit](https://github.com/SkillsFundingAgency/das-audit) Api to log changes

## Technologies

* .NetCore 3.1
* Azure Functions V3
* CosmosDB
* REDIS
* NLog
* Azure Table Storage
* NUnit
* Moq
* FluentAssertions

## 🐛 Known Issues

There are no issues known at this time