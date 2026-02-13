---
# Cartouche v1
title: "csharp-holons — C# SDK for Organic Programming"
author:
  name: "B. ALTER"
  copyright: "© 2026 Benoit Pereira da Silva"
created: 2026-02-12
revised: 2026-02-13
lang: en-US
access:
  humans: true
  agents: false
status: draft
---
# csharp-holons

**C# SDK for Organic Programming** — transport, serve, identity,
and Holon-RPC client utilities for building holons in C#.

## Test

```bash
dotnet test
```

## API surface

| Type | Description |
|------|-------------|
| `Transport` | `ParseUri(uri)`, `Listen(uri)`, `Scheme(uri)` |
| `Serve` | `ParseFlags(args)` |
| `IdentityParser` | `ParseHolon(path)` |
| `HolonRPCClient` | `ConnectAsync(url)`, `InvokeAsync(method, params)`, `Register(method, handler)`, `CloseAsync()` |

## Transport support

| Scheme | Support |
|--------|---------|
| `tcp://<host>:<port>` | Bound listener (`Transport.TransportListener.Tcp`) |
| `unix://<path>` | Native runtime listener + dial (`Transport.TransportListener.Unix`, `Transport.DialUnix`) |
| `stdio://` | Listener marker (`Transport.TransportListener.Stdio`) |
| `mem://` | Native in-process listener + dial (`Transport.TransportListener.Mem`, `Transport.MemDial`) |
| `ws://<host>:<port>` | Listener metadata (`Transport.TransportListener.Ws`) |
| `wss://<host>:<port>` | Listener metadata (`Transport.TransportListener.Ws`) |

## Parity Notes vs Go Reference

Implemented parity:

- URI parsing and listener dispatch semantics
- Native runtime listener for `tcp://`
- Native runtime listener + dial for `unix://`
- Native in-process listener + dial for `mem://`
- Holon-RPC client protocol support over `ws://` / `wss://` (JSON-RPC 2.0, heartbeat, reconnect)
- Standard serve flag parsing
- HOLON identity parsing

Not currently achievable in this minimal C# core (justified gaps):

- `stdio://` runtime listener:
  - gRPC .NET does not provide an official stdio transport equivalent to Go `net.Listener`.
- `ws://` / `wss://` runtime listener parity:
  - No official WebSocket server transport for standard gRPC HTTP/2 framing in the core stack.
  - Exposed as metadata only.
- Full gRPC transport parity (`Dial("tcp://...")`, `Dial("stdio://...")`, `Listen("stdio://...")`, and `Serve.Run()` wiring):
  - `Grpc.Net` does not expose an official stdio transport equivalent to Go `net.Listener`.
  - A complete `serve.Run()` equivalent requires additional reflection/signal/runtime orchestration not yet included in this SDK core.
