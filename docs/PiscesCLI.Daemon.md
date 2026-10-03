# PiscesCLI.Daemon

A lightweight local HTTP daemon that exposes two preconfigured Gemini clients (**translator** and **explainer**) on `localhost`. Its JSON contract matches the "KISS Translator" custom-API format (`text`, `from`, `to` → `text`, `src`), so browser translation extensions can use it as a backend.

## Showcase Disclaimer

This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License. It is provided as-is: **no technical support, no SLA, and no pull requests are accepted.**

## Architecture & Principles

- **Long-running process vs. one-shot CLI.** Unlike `ConsoleApp`, which runs a command and exits, the daemon loads configuration once and serves requests continuously using `System.Net.HttpListener` (no ASP.NET dependency).
- **Route → client mapping.** Each endpoint is bound to its own `Client`/config, so persona is set purely by `system_prompt` in TOML.
  - `/translate` → `translator.toml`
  - `/explain` → `explainer.toml`
- **Shared throttling.** One `SemaphoreSlim` and one `SlidingWindowRateLimiter`, built from the translator's `ConcurrencyLimit` / `RpmLimit`, guard all requests.
- **Request isolation.** Each request runs in `Task.Run`; errors become JSON error bodies (`{ "text": "...", "src": "error" }`) with status 404 / 429 / 500.
- **Fail-fast startup.** `DaemonHost.StartSucceeded` is false if either config is missing, and `Program` exits.

## Features Breakdown

- `Program` – entry point; listens on port **11435**.
- `DaemonHost` – config loading, listener loop, routing, error responses.
- Records: `KissRequest(text, from, to)`, `KissResponse(text, src)`; the response `src` is `"auto"`.
- Empty or whitespace text returns an empty response without calling the API.
- Case-insensitive JSON deserialization.
- Routing matches the exact path and query string (`PathAndQuery`).

## Usage Guide

Place `translator.toml` and `explainer.toml` (same schema as `client_config.toml`, see `PiscesCLI.Core`) beside the executable, then run the daemon:

```powershell
dotnet run --project PiscesCLI.Daemon
```

Call it:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:11435/translate `
  -ContentType 'application/json' `
  -Body '{"text":"Hello world","from":"auto","to":"zh-CN"}'
# => @{ text = "..."; src = "auto" }
```

Example translator prompt (`translator.toml`):

```toml
api_key = "AIza..."
model = "3.5flash"
system_prompt = "Translate the user's text into Simplified Chinese. Output only the translation."
```

Note: the `to` language field is parsed but not used; target language comes from the system prompt.
