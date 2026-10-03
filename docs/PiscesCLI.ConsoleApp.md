# PiscesCLI.ConsoleApp

The command-line front end of PiscesCLI. It uses `ConsoleAppFramework` to expose connection tests, interactive generation, configuration scaffolding, and the Bulk / Batch workflows as sub-commands.

## Showcase Disclaimer

This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License. It is provided as-is: **no technical support, no SLA, and no pull requests are accepted.**

## Architecture & Principles

- **Thin orchestration layer.** Commands contain no business logic; each one validates its input path, calls `CoreBootstrapper.GetClientConfig`, builds a `Client`, checks `BootSucceeded`, and delegates to `Core`, `Bulk` or `Batch`.
- **Commands grouped by class** and registered in `Program` with `app.Add<T>()`:
  - `PiscesCommandCore` – connectivity and generation
  - `PiscesCommandConfig` – config scaffolding
  - `PiscesCommandBulkBatch` – bulk and batch jobs
- **Client config is an explicit argument**, so one binary can serve multiple accounts or personas.
- **Interactive safety.** Config creation asks for `y/n` confirmation before writing files.

## Features Breakdown

| Command | Arguments | Description |
|---|---|---|
| `connect` | `[clientCfg]` | List models to verify API access. |
| `test-gene` | `[clientCfg]` | Send a test prompt and verify the expected reply. |
| `generate` / `gene` | `[clientCfg]` | Interactive prompt generation (`Client.GenerateFromCliPromptAsync`). |
| `create-configs` / `create-conf` | – | Offer to write default `core_config.toml`, `client_config.toml`, `model_conf/default_model_config.toml` (with comment headers). |
| `bulk-text` | `clientCfg promptFile` | Parallel real-time requests, one per line → CSV. |
| `bulk-file` | `clientCfg dir` | Parallel requests, one per file (inline data) → CSV. |
| `batch-text` | `clientCfg promptFile [jobName]` | Submit a Batch API job from text prompts. |
| `batch-file` | `clientCfg dir [jobName]` | Upload files and submit a Batch API job. |
| `fetch` | `clientCfg jobServerName` | Download a finished batch job to JSONL and CSV in `output/`. |

## Usage Guide

```powershell
# 1. Scaffold configuration, then edit client_config.toml (set api_key, model)
PiscesCLI create-configs

# 2. Verify
PiscesCLI connect client_config.toml
PiscesCLI test-gene client_config.toml

# 3. Interactive generation
PiscesCLI gene client_config.toml

# 4. Bulk (real time)
PiscesCLI bulk-text client_config.toml prompts.txt
PiscesCLI bulk-file client_config.toml ./images

# 5. Batch (asynchronous)
PiscesCLI batch-text client_config.toml prompts.txt my_job
PiscesCLI batch-file client_config.toml ./docs doc_job
PiscesCLI fetch client_config.toml batches/xxxxxxxx
```

Output files go to `output/` and intermediate artifacts (JSONL uploads, job files) to `cache/` next to the executable. Executable name may differ depending on build settings.
