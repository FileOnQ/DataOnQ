# DataOnQ - Problem Space
Mobile devices are not always connected to the internet despite the abundance of wireless networks. The Public Safety and Law Enforcement sectors are investing in Wi-Fi and cellular connected devices for all their personnel. Evidence management in law enforcement agencies requires mobile devices to be used in environments without Wi-Fi access and often multiple floors underground where cellular service is typically unavailable.

# Limited Connectivity
The only way to get a device connected to the internet in our problem space is via an Ethernet dock or sharing internet via Bluetooth or USB connection to the device.

All these factors necessitate a robust offline solution that acts as an offline first app with common synchronization routines to keep the data between the database and devices current.

# Backend Server
Our offline synchronization library integrates with a WCF (Windows Communication Foundation) service using a SOAP (Simple Object Access Protocol) XML payload rather than a typical modern Web API that returns a JSON payload.

# Real World Workflow
The devices are not connected to their docks when they are in use, so there is no internet connectivity. The app will continue to function as if the device is connected to the internet but the users will never know the difference. Placing the device into a connected dock shares the latest information and changes with the server as well as downloads new information to the device.

1. Device is connected and is up to date
2. User removes device
3. User performs disconnected actions throughout their job
4. User places device in conencted dock, the device will synchronize automatically with the connected server

## Why Is This Model Useful
This model is very useful for designing an offline capable app because it is a worst-case scenario for many modern connected mobile apps. A typical user will never be in the workflows that our system is designed for, thus creating a very reslient data access layer for new apps that operate in more connected environments.