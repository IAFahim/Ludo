# REST API Example

This example demonstrates a simple REST API with CRUD operations using ASP.NET Core minimal APIs.

## Projects

- **RestServer**: ASP.NET Core Web API server with message endpoints
- **RestClient**: Console application that consumes the REST API

## Features

- GET all messages
- GET message by ID
- POST new message
- PUT update message
- DELETE message

## Running the Example

### Terminal 1 - Start the Server:
```bash
cd RestServer
dotnet run
```

Server will run on `http://localhost:5000`

### Terminal 2 - Run the Client:
```bash
cd RestClient
dotnet run
```

The client will perform various CRUD operations and display the results.

## API Endpoints

- `GET /api/messages` - Get all messages
- `GET /api/messages/{id}` - Get message by ID
- `POST /api/messages` - Create new message
- `PUT /api/messages/{id}` - Update message
- `DELETE /api/messages/{id}` - Delete message
