# SignalR Example

This example demonstrates real-time communication using SignalR with a chat hub.

## Projects

- **SignalRServer**: ASP.NET Core server with SignalR hub
- **SignalRClient**: Console application that connects to the SignalR hub

## Features

- Real-time bidirectional communication
- Send messages to all clients
- Broadcast messages to other clients
- Connection/disconnection notifications

## Running the Example

### Terminal 1 - Start the Server:
```bash
cd SignalRServer
dotnet run
```

Server will run on `http://localhost:5001` with hub endpoint at `/chatHub`

### Terminal 2 - Run the Client:
```bash
cd SignalRClient
dotnet run
```

The client will connect to the hub and send real-time messages.

## Hub Methods

- `SendMessage(user, message)` - Send message to all clients
- `BroadcastMessage(message)` - Broadcast to all other clients
- `OnConnectedAsync()` - Handle client connection
- `OnDisconnectedAsync()` - Handle client disconnection
