# das-tools-servicebus-support Project

das-tools-servicebus-support is a website for managing errors on Azure ServiceBus Queues.

1. Once logged in, the application will automatically enumerate the queues within the ASB namespace and check them against a Regular Expression before displaying the to the user.
2. The user can then select a queue and the application will receive all messages from the queue and insert them into the CosmosDB
3. The user can then:
  * Abort the operation and all messages will be placed back on the queue they were received from
  * Replay selected messages, this will add the messages back onto the original processing queue so that they can be actioned again
  * Delete selected messages; this will delete them from the CosmosDB and they will be gone forever

## Pre-Requisites

* Visual Studio 2019, Visual Studio Code etc
* A CosmosDB instance or emulator
* Azure Service Bus instance
* Azure Active Directory account
* das-audit - configured using SFA.DAS.AuditApiClient_1.0 entry in table storage 

## Usage

* Add an appsettings.Development.json file
* Add your connection strings for CosmosDB and ASB to the relevant sections

* Create an AAD account:
  * In azure portal go to Users and Create a new user.
  * Give user access to the Service Bus
      * Service Bus Namespace > Access control (IAM) > Add role assignment  
        * Role - Azure Service Bus Data Receiver (and potentially Sender if you need to populate the queue in the first place)
        * Select - user name
  * Configure user in Visual Studio
      * Tools > Options > Azure Service Authentication > Account Selection

## License
[MIT](https://choosealicense.com/licenses/mit/)
