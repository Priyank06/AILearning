# ADR-001: Blazor Server as the UI Framework

## Status
Retrospective

## Context
The application needed a web UI capable of streaming real-time progress updates as three concurrent AI agents run their analysis (which can take 1–3 minutes per request). The team is operating in a .NET-first ecosystem and wanted to avoid a separate JavaScript SPA build pipeline while still delivering a rich, reactive experience.

## Decision
Use **ASP.NET Core 8 Blazor Server** as the UI framework. Pages and components are written in Razor (C#), state lives on the server, and updates are pushed to the browser via a persistent **SignalR** circuit.

## Consequences
### Positive
- Full C# sharing between UI and service layer — no REST/JSON contract needed for internal calls.
- Real-time agent progress streamed naturally through the SignalR circuit without polling.
- Eliminated a separate SPA build pipeline, reducing toolchain complexity.
- Strongly-typed component parameters reduce runtime errors.
### Negative
- Every connected user holds a persistent SignalR circuit, increasing server memory footprint at scale.
- Long-running AI operations (up to 10 minutes) required extending the default Blazor circuit disconnect timeout.
- Server-side rendering means the app is not usable offline and is harder to CDN-cache.
- Debugging async UI state in Blazor Server can be more complex than a traditional SPA.
