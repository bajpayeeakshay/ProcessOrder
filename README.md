# Process Order 
##### Choice Of Solution: Console APP

I have opted to develop a Console Application due to the simplicity of running it locally without additional installations. However, if given more time, I would have implemented a Blob Trigger, which would have required the assessors/reviewers to install additional dependencies like Azurite/Azure Storage Explorer for local execution.

# Project

The Code represents a Console Application that Processes Orders. 

## Program.cs
Configures Application using ConfigurationBuilder to read settings from JSON Files. 
Configures Services for Dependency Injection using ServiceCollection. 
### Settings
#### AppSettings.json
Consists of AppSettings like 
- FilePath where the data needs to be read from
- URL of the Order Management System where the XML needs to be sent
- Email Id of Account Manager (would have more sense to keep it in the db)
- XML File Path: Path where the xml file should be generated. 
- SMTP Details to Configure SMTP Server to Send Notification


    `{
      "appSettings": {
        "filePath": "path of .sdf file",
        "orderManagementSystemUrl": "url of order management system", 
        "accountManagerEmail": "email id of account manager", 
        "xmlFilePath": "path where you want the xml file to be generated."
      },
      "smtp": {
        "smtpServer": "your-smtp-server",
        "smtpUserName": "your-smtp-user-name",
        "smtpPassword": "your-smtp-password", 
        "smtpEmail":  "your-smtp-email"
      }
    }`

#### FileFormatSettings.Json
Ideally this information must be stored in a database, but for the ease of implementation I have rendered it from a JSON file. This File contains the Settings Regarding the fields of the .SDF file i.e. their Start Character and their Length. 

## OrderProcessor.cs (IOrderProcessor)
Responsible for processing order asynchronously. It is responsible for 
- Reading data from the File 
- Handling Exception and Logging Error 
- Validating File Type
- Send Email Notification to the Account Manager using SMTP Settings. 
- Sending XML to the HttpClient Configured. 

## ProcessOrderService.cs (IProcessOrderService)
Contains the following BusinessLogic
- *IsFileValid*: Responsible for Validating if the File received is an Order file i.e. FileIdentifierType is 'ORD'
- *GetOrderFromDataAsync*: 
	- Responsible for mapping data received from the file into Order and OrderItem models using the FileFormatSettings Data received from the JSON file. 
	-  Also responsible for Validating the OrderItem by using *ValidateOrderItem* method 
	- Responsible for *UpdatingStockForItem* at the ERP
- *ValidateOrderItem*: 
	- Responsible for getting Item from ERP using *GetItemForSupplier* and then performing following operations: 
		- If Unit Price don't match the consider Unit Price Received from ERP and Send Notification to the AccountManager Email
		- If Requested Qty is not available then abort the Order and Send Notification to the Account Manager Email
- *CreateXmlForOrderData*: 
	- Responsible for Serializing the OrderModel into stringified XML. 

### Assumptions:
- Created ***ProcessERP(IProcessERP)*** as dummy service to work with ProcessOrderService to retrieve data using ***GetItemForSupplier*** and update stock for item using ***UpdatingStockForItem***
- Another Assumption that has been made is regarding *OrderManagementSystemUrl*. The OrderManagementUrl to which the XML file needs to be send is currently being considered as it would be correct and the exception handling on the method responsible for sending it has been done with the sole purpose that the application will / should not give any error and the xml file can be created at the designated path in appSettings.json

## EmailNotifier(IEmailNotifier): 
Responsible for setting up SMTP client and sending email information to the Account Manager. 

## Unit Testing: 
Nunit along with Moq has been used to implement unit tests for the application. 

## Logging: 
Serilog has been used to logging purposes and for the ease of verifying the assignment, a File Log has been implemented. 
You can find the log file in the following place: '\bin\Debug\net8.0\Logs'
