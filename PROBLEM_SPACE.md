# DataOnQ - Problem Space
Mobile devices are not always connected to the internet, in the Public Safety and Law Enforcement industry wi-fi and cellular connected devices become a bigger concern. In our evidence management product there is almost never wi-fi access and multiple floors underground cellular service is typically unavailable. 

# Limited Connectivity
In our Problem Space the only way to get a device connected to the internet is via an Ethernet Dock or sharing internet via Bluetooth or USB connection to the device. 

All of this requires a robust offline solution that acts as an offline first app with common synchronization routines to keep the data between the database and devices current.

# Backend Server
In our scenario the backend server is not a typical modern Web API that returns  JSON payload. Our offline synchronization library integrates with a WCF (Windows Communication Foundation) Service using a SOAP (Simple Object Access Protocol) XML payload. 

# Real World Workflow
In our scenario whenever the device is active and is in use not connected to the dock there is no internet. The app will continue to function as if they are connected to the internet but they will never know the difference. Placing the device into a connected cradle shares the latest information and changes with the server as well as downloads new information to the device.

1. Device is connected and is up to date
2. User removes device
3. User performs disconnected actions throughout their job
4. User places device in conencted dock, the device will synchronize automatically with the connected server

## Why Is This Model Useful
This model is very useful for designing an offline capable app because it is a worst-case scenario for many modern connected mobile apps. A typical user will never be in the workflows that our system is designed for, thus creating a very reslient data access layer to new apps that operate in more connected environments.