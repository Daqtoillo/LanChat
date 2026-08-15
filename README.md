# **LanChat Backend**

LanChat is a robust, scalable, real-time chat application backend built with .NET / C\# and heavily integrated with Microsoft Azure cloud services.  
This repository contains the server-side code, which consists of a high-performance ASP.NET Core Web API for handling core logic and real-time sockets, alongside an Azure Functions project for asynchronous background processing.

# 

# **Features**

* Real-Time Messaging: Powered by SignalR for instant message delivery and live typing indicators.  
* Secure by Design: Integrates Azure Key Vault to handle cryptographic operations and secure secret management.  
* Media Sharing: Users can upload images which are securely stored in Azure Blob Storage.  
* Automatic Image Optimization: Uses an event-driven Azure Function to automatically resize and optimize images upon upload.  
* Highly Scalable: Utilizes Azure Cosmos DB (NoSQL) for fast message retrieval and Redis Cache for rapid data access and SignalR backplane scaling.

# 

# **Tech Stack**

* Frameworks: .NET 8, ASP.NET Core Web API, Azure Functions  
* Real-time Communication: SignalR  
* Database: Azure Cosmos DB  
* Storage: Azure Blob Storage  
* Caching: Azure Cache for Redis  
* Security and Secrets: Azure Key Vault

# 

# **Project Structure**

The solution is divided into two primary projects.

1\. LanChat.Server (Main API and WebSocket Server)   
The core backend service handling client connections and business logic.

* Controllers folder: Exposes REST endpoints for HTTP operations (ChatController, ImagesController, CryptoController).  
* Hubs/ChatHub.cs: The SignalR hub managing real-time WebSocket connections and message broadcasting.  
* Services folder: Contains integrations with external Azure resources (CosmosDbService, BlobService, RedisCacheService, KeyVaultCryptoService).

2\. LanChat.Functions (Background Workers)   
A serverless worker project designed to run independently of the main API.

* ImageResizeTrigger.cs: An Azure Blob Storage trigger that listens for new image uploads and automatically processes them without blocking the main chat server.

# 

# **Getting Started**

Prerequisites: 

* .NET SDK installed.  
* Azure Functions Core Tools for running the functions locally.  
* Access to an Azure subscription or local emulators for Cosmos DB, Redis, and Azurite for Blob Storage.

Configuration Steps:  
Step 1\. Clone the repository to your local machine and navigate to the LanChat-main directory.  
Step 2\. Navigate to the LanChat.Server directory and configure your appsettings.Development.json file with your Azure connection strings (CosmosDb, RedisCache, BlobStorage, and KeyVault URI).  
Step 3\. Configure the LanChat.Functions/local.settings.json with the corresponding Blob Storage connection string.

Running the Application Locally:  
To start the API Server, navigate to the LanChat.Server folder, run the build command, and then run the start command using the .NET CLI.  
To start the Azure Functions, open a new terminal window, navigate to the LanChat.Functions folder, and start it using the functions core tools CLI.

# 

