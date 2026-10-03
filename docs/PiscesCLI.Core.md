# PiscesCLI.Core

Shared foundation library of PiscesCLI. It wraps the Google Gemini SDK (`Google.GenAI`) behind a small `Client` type and provides configuration loading (TOML), model alias resolution, MIME-type lookup, request/response models and working-directory management. Every other project in the solution (`Bulk`, `Batch`, `Daemon`, `ConsoleApp`) depends on it.

## Showcase Disclaimer

This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License. It is provided as-is: **no technical support, no SLA, and no pull requests are accepted.**

## Architecture & Principles

- **Single facade over the SDK.** `Client` owns the only `GeminiClient` instance. Upper layers never touch the SDK directly, except for SDK types that flow through (`BatchJob`, `File`).
- **Three-layer configuration.**
  1. `CoreConfig` (`core_config.toml`) – global: default model, model alias map (`model_api_names`), extension→MIME map. Falls back to built-in defaults when the file is missing.
  2. `ClientConfig` (any `*.toml` path passed by the caller) – API key, model alias, token limit, MIME type, concurrency / RPM limits, system prompt.
  3. `ModelConfig` (`model_conf/<alias>.json` lookup in `ClientContext`) – per-model overrides such as thinking level.
  Model-specific config wins; otherwise the client config is used (see `Client.GetGenerateResponseAsync`).
- **Model aliases, not raw API names.** Users select short aliases (`pro`, `3.5flash`, …); `ClientContext.GetModelApiName` maps them to full API names and falls back to the default model.
- **Snake-case TOML** via `Tomlyn` with `JsonNamingPolicy.SnakeCaseLower` (`TomlConfigSerializer`).
- **Non-throwing boundaries.** `Client` catches SDK errors and reports them through `GenerationResponse.State/Error` or console output, so batch callers can continue.
- **Portable working directory.** All paths (`cache/`, `output/`, `model_conf/`, `core_config.toml`, `client_config.toml`) are rooted at `AppContext.BaseDirectory` (`AppPath`).

## Features Breakdown

| Component | Responsibility |
|---|---|
| `Client` | Creates the Gemini client; `TestConnectionAsync`, `TestGenerationAsync`, `GenerateFromCliPromptAsync`, `GetGenerateResponseAsync`; file upload / get; batch job create / fetch; converts batch result JSONL to CSV (CsvHelper). API key is masked in logs. |
| `ClientConfig` | Immutable record of client settings; default instance; builds `GenerateContentConfig` (safety settings, system instruction). Can write a commented default TOML. |
| `CoreConfig` | Default model, alias map, extension→MIME map (text, JSON/JSONL, PDF, images, video), safety settings (all categories `BlockNone`). |
| `ModelConfig` | Per-model overrides incl. `ThinkingLevel` (parsed to the SDK enum; invalid value → falls back). |
| `ClientContext` | Static registry started once (`Start`); model lookup, MIME lookup (`TryGetMimeType`, `GetMimeType`, `IsSupported`). |
| `CoreBootstrapper` | `GetClientConfig` – creates work directories, loads core config, starts `ClientContext`, loads client config. |
| `GenerationRequest` | Text prompt or inline binary data (MIME resolved from file name) plus a `TrackId`. |
| `GenerationResponse` | State machine (`NotStarted → WaitingResponse → Completed/Error`), text, error, `TokenCount`, elapsed time. |
| `AppPath`, `Tools`, `TomlConfigSerializer` | Path helpers (timestamped batch artifact names), y/n confirmation, TOML load/save. |

## Usage Guide

```csharp
using PiscesCLI.Core;

// 1. Load configs and start the core (creates cache/, output/, model_conf/)
if (!CoreBootstrapper.GetClientConfig("client_config.toml", out var cfg)) return;

// 2. Create the client
var client = new Client(cfg!);
if (!client.BootSucceeded) return;

// 3. Generate
var response = await client.GetGenerateResponseAsync(new GenerationRequest("Hello", trackId: 1));
if (response.State == GenerationResponse.EState.Completed)
	Console.WriteLine($"{response.Response} ({response.TokenCount?.Total} tokens, {response.TimeTaken.TotalSeconds:F2}s)");

// Inline binary data (type resolved from extension)
var req = new GenerationRequest(File.ReadAllBytes("a.png"), "a.png", trackId: 2);
```

Example `client_config.toml` (keys are snake_case):

```toml
api_key = "AIza..."
model = "3.5flash"
max_output_tokens = 65536
response_mime_type = "text/plain"
concurrency_limit = 2
rpm_limit = 10
system_prompt = ""
```
