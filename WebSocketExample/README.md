# WebSocket Example

This example demonstrates low-level WebSocket communication with echo functionality.

## Projects

- **WebSocketServer**: ASP.NET Core server with WebSocket endpoint
- **WebSocketClient**: Console application that connects via WebSocket

## Features

- Full-duplex communication
- Text message support
- Echo server functionality
- Connection management

## Running the Example

### Terminal 1 - Start the Server:
```bash
cd WebSocketServer
dotnet run
```

Server will run on `http://localhost:5002` with WebSocket endpoint at `/ws`

### Terminal 2 - Run the Client:
```bash
cd WebSocketClient
dotnet run
```

The client will connect and send messages that the server echoes back.

## WebSocket Endpoint

- `ws://localhost:5002/ws` - WebSocket connection endpoint

The server receives text messages and echoes them back with "Echo: " prefix.
