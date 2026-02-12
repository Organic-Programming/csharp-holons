---
# Cartouche v1
title: "csharp-holons — C# SDK for Organic Programming"
author:
  name: "B. ALTER"
  copyright: "© 2026 Benoit Pereira da Silva"
created: 2026-02-12
revised: 2026-02-12
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
