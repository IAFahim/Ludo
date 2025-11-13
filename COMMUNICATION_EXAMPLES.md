# Communication Examples - REST, SignalR, and WebSocket

This repository contains three separate solutions demonstrating different communication patterns in .NET:

## 1. REST API Example (`RestExample/`)

Traditional HTTP REST API with CRUD operations.

**Use Cases:**
- Standard web APIs
- CRUD operations
- Stateless request/response patterns
- Mobile app backends

**Pros:**
- Simple and widely understood
- Stateless and scalable
- Works with any HTTP client
- Easy to cache

**Cons:**
- Request/response only (no push notifications)
- Higher overhead for frequent updates
- No real-time capabilities

**Running:**
```bash
cd RestExample
# Terminal 1
cd RestServer && dotnet run
# Terminal 2
cd RestClient && dotnet run
```

## 2. SignalR Example (`SignalRExample/`)

Real-time communication using SignalR with automatic transport fallback.

**Use Cases:**
- Chat applications
- Live dashboards
- Real-time notifications
- Collaborative editing
- Live sports scores

**Pros:**
- Real-time bidirectional communication
- Automatic transport selection (WebSocket, Server-Sent Events, Long Polling)
- Built-in connection management and reconnection
- Strongly-typed hub methods
- Easy to scale with backplane

**Cons:**
- More complex than REST
- Requires SignalR client library
- Stateful connections

**Running:**
```bash
cd SignalRExample
# Terminal 1
cd SignalRServer && dotnet run
# Terminal 2
cd SignalRClient && dotnet run
```

## 3. WebSocket Example (`WebSocketExample/`)

Low-level WebSocket communication with full control.

**Use Cases:**
- Gaming
- Financial trading platforms
- IoT device communication
- Custom protocols
- High-performance real-time apps

**Pros:**
- Full-duplex communication
- Low overhead and latency
- Direct control over messages
- Binary and text support
- Works across firewalls (port 80/443)

**Cons:**
- Lower-level API (more code required)
- No automatic reconnection
- Manual connection management
- No built-in RPC pattern

**Running:**
```bash
cd WebSocketExample
# Terminal 1
cd WebSocketServer && dotnet run
# Terminal 2
cd WebSocketClient && dotnet run
```

## Comparison Summary

| Feature | REST | SignalR | WebSocket |
|---------|------|---------|-----------|
| **Communication** | Request/Response | Bidirectional | Bidirectional |
| **Real-time** | No | Yes | Yes |
| **Connection** | Stateless | Stateful | Stateful |
| **Complexity** | Low | Medium | Medium-High |
| **Overhead** | Higher | Medium | Lower |
| **Browser Support** | Universal | Universal | Modern browsers |
| **Fallback** | N/A | Automatic | Manual |

## Requirements

- .NET 9.0 SDK or later
- Terminal/Command Prompt

## Architecture

Each solution contains:
- **Server**: ASP.NET Core application exposing the API/Hub/WebSocket endpoint
- **Client**: Console application demonstrating how to consume the service

All servers run on different ports to avoid conflicts:
- REST: `http://localhost:5000`
- SignalR: `http://localhost:5001`
- WebSocket: `http://localhost:5002`
