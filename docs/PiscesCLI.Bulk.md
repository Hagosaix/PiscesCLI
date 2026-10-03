# PiscesCLI.Bulk

Library for **real-time bulk generation**: it fires many individual Gemini requests (one per text line or per local file) in parallel under concurrency and requests-per-minute limits, then writes all results to a single CSV.

## Showcase Disclaimer

This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License. It is provided as-is: **no technical support, no SLA, and no pull requests are accepted.**

## Architecture & Principles

- **Template-method pattern.** `BulkTask` (abstract) owns scheduling, throttling, state tracking and CSV export. Subclasses only implement `GetGenerationRequest()`:
  - `TextTask` – one request per prompt string.
  - `InlineDataTask` – one request per file, sent as inline bytes.
- **Two-level throttling**, driven by `ClientConfig`:
  - `SemaphoreSlim(ConcurrencyLimit)` caps in-flight requests.
  - `SlidingWindowRateLimiter` (1-minute window, 6 segments, unbounded queue, oldest first) enforces `RpmLimit`.
- **Partial-failure tolerant.** Every request becomes a `GenerationResponse`; the task ends as `Completed`, `CompletedWithError` or `Error` (`BulkTask.EState`).
- **Stable correlation.** The array index is used as `TrackId` and written to the CSV.
- **Factory-delegate entry point.** `BulkRunner.StartBulkTask` receives a `Func<string, BulkTask?>`, so input loading is decoupled from execution.

## Features Breakdown

- `BulkRunner`
  - `StartBulkTask(client, path, factory)` – run and, on success, write `output/bulk_task_<yyMMdd_HHmmss>_output.csv`.
  - `GetBulkTaskTextPrompt(path)` – reads a text file, one prompt per line.
  - `GetBulkTaskInlineData(dir)` – reads every file in a directory as bytes.
- `BulkTask` – `StartTask`, `WriteToCsv`, `State`.
- CSV columns: `TrackID, TimeTaken, TokenInput, TokenOutput, TokenTotal, Response` (the error message replaces the response for failed items).
- Requests whose file extension has no known MIME type are skipped (`CreateRequestSuccessful == false`).

## Usage Guide

```csharp
using PiscesCLI.Bulk;
using PiscesCLI.Core;

CoreBootstrapper.GetClientConfig("client_config.toml", out var cfg);
var client = new Client(cfg!);

// One prompt per line
await BulkRunner.StartBulkTask(client, "prompts.txt", BulkRunner.GetBulkTaskTextPrompt);

// Every file in a folder (images, PDFs, ...)
await BulkRunner.StartBulkTask(client, "./images", BulkRunner.GetBulkTaskInlineData);
```

Custom task:

```csharp
public class MyTask(string[] items) : BulkTask
{
	protected override IEnumerable<GenerationRequest> GetGenerationRequest() =>
		items.Select((s, i) => new GenerationRequest(s, i));
}
```

Via the CLI: `bulk-text <clientCfg> <promptFile>` and `bulk-file <clientCfg> <dir>` (see `PiscesCLI.ConsoleApp`).
