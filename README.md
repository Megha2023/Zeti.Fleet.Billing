Zeti Fleet Billing Solution
This project is to calculate billing for fleet vehicles based on odometer readings and other parameters.
---
Core Components
1. BillingFunction
•	The entry point for the Azure Function that processes billing requests.
•	Accepts HTTP POST requests with billing details.
•	Validates the request using BillingValidator.
•	Calculates the total bill using BillingService.
•	Formats the response to JSON). // yet to be explored more to use Formatter factory pattern
•	File: Zeti.Fleet.Billing/BillingFunction.cs // copilot used for some request formatting and deserializing
• Looging generated using CoPilot
---
2. BillingService
•	The core service responsible for calculating the total bill for a customer.
•	Fetches odometer readings using HttpClient using the configurable API url with specific vehicles and dates.
• Made the costperMile configurable 
•	Calculates the total miles traveled and applies the cost per mile. // CoPilot used in converting meters to miles
•	Handles scenarios like missing odometer data or HTTP failures.
•	File: Zeti.Fleet.Billing/Services/BillingService.cs
---
3. BillingValidator
•	Validates the incoming billing request to ensure all required fields are present and valid.
•	Ensures Customer is not empty.
•	Validates that the Vehicles list is not empty.
•	Checks that the StartDate is earlier than the EndDate.
•	File: Zeti.Fleet.Billing/Validator/BillingValidator.cs
---
5. Configuration
•	Centralized configuration for the application.
•	Stores the cost per mile and the URL for fetching odometer readings.
•	Configured via appsettings.json.
•	File: Zeti.Fleet.Billing/appsettings.json / to run locally create a local.settings.json file with settings like below,
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "Billing": {
    "CostPerMile": 0.207,
    "readingUrl": "https://funczetiinterviewtest.azurewebsites.net/api/vehicles/history/"
  }
}
6. Unit Tests
•	Tests for BillingFunction, BillingService, and BillingValidator. // CoPilot has been useful in some stream deserialization and httpMessagehandler mock set up
•	Mocks dependencies like HttpClient and ILogger for isolated testing.
•	Uses FluentAssertions for expressive assertions.
• BillingValidator - used copilot for generating tests here

8. Program.cs
• uses dependency Injection to create all the relevant objects

Run the Azure function locally with the above local settings
use tool like postman with body data like below. can also use rest.http file to create and push sample API calls 
  POST http://localhost:7224/api/bill-customer-05052025
   {
     "Customer": "Bob's Taxi",
     "Vehicles": [ "CBDH789" , "86532AZE"],
     "StartDate": "2021-02-29T00:00:00Z",
     "EndDate": "2021-02-28T23:59:00Z"
   }
