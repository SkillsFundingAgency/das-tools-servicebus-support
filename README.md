# das-tools-servicebus-support


## MSI auth with Visual Studio 2019

We were not able to get MSI auth working with Microsoft Account and had to create Azure Active Directory account instead. 

- In azure portal go to Users and Create a new user.
- Give user access to the Service Bus
  - Service Bus Namespace > Access control (IAM) > Add role assignment  
    - Role - Azure Service Bus Data Receiver
    - Select - user name
- Configure user in Visual Studio 
  - Tools > Options > Azure Service Authentication > Account Selection

Issue details:
https://github.com/Azure/azure-sdk-for-net/issues/8627
