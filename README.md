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

**C# SDK for Organic Programming** — transport, serve, and identity
utilities for building holons in C#.

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

## Transport support

| Scheme | Support |
|--------|---------|
| `tcp://<host>:<port>` | Bound listener (`Transport.TransportListener.Tcp`) |
| `unix://<path>` | Parsed; runtime binding requires Unix-domain capable gRPC stack |
| `stdio://` | Listener marker (`Transport.TransportListener.Stdio`) |
| `mem://` | Listener marker (`Transport.TransportListener.Mem`) |
| `ws://<host>:<port>` | Listener metadata (`Transport.TransportListener.Ws`) |
| `wss://<host>:<port>` | Listener metadata (`Transport.TransportListener.Ws`) |

## Parity Notes vs Go Reference

Implemented parity:

- URI parsing and listener dispatch semantics
- Native runtime listener for `tcp://`
- Standard serve flag parsing
- HOLON identity parsing

Not currently achievable in this minimal C# core (justified gaps):

- `unix://` native runtime binding:
  - Requires a Unix-domain capable gRPC runtime server stack beyond this minimal transport surface.
- `stdio://` and `mem://` runtime listeners:
  - gRPC .NET does not provide official stdio/memory transports equivalent to Go `net.Listener` abstractions.
- `ws://` / `wss://` runtime listener parity:
  - No official WebSocket server transport for standard gRPC HTTP/2 framing in the core stack.
  - Exposed as metadata only.
- Transport-agnostic gRPC client helpers (`Dial`, `DialStdio`, `DialMem`, `DialWebSocket`):
  - Requires a dedicated .NET gRPC adapter layer that is not yet included.
