# PiscesCLI.Batch

Library for the **asynchronous Gemini Batch API** workflow: build a JSONL request file, upload it (and any referenced files), create a server-side batch job, and persist local job metadata so work can be resumed.

## Showcase Disclaimer

This is a **read-only showcase** of internal tooling from **Everblooming Lab (Catmint Works)**, published under the MIT License. It is provided as-is: **no technical support, no SLA, and no pull requests are accepted.**

## Architecture & Principles

- **Submit now, fetch later.** Batch jobs complete on the server; this library submits and records state, while `Client.FetchBatchJobAsync` (in `Core`) downloads results and converts them to CSV.
- **Resumable via a job file.** A `Job` is serialized to `cache/batch_job_<name>_<timestamp>_object.json` after each stage (and in `finally`). `ContinueFileBatchJobAsync` reloads it, re-checks / re-uploads files, and resubmits.
- **Domain model.**
  - `File` – local path, MIME type and server metadata (`ServerFileName`, `Uri`, `State`, `Error`).
  - `Jsonl : File` – the generated request file; writes one request per line (`key` = track id such as `<job>_prompt_<i>` / `<job>_file_<i>`).
  - `Job` – local name, server name / state, file list (the `Jsonl` is not serialized).
- **Same throttling model as Bulk.** A semaphore plus `SlidingWindowRateLimiter` built from `ClientConfig` guards uploads and file-state polling.
- **State polling.** Files must reach `FileState.Active`; polling every 10 s with a 1-minute timeout per file.

## Features Breakdown

`BatchRunner`:
- `SubmitBatchJobTextAsync(client, promptsFile, jobName?)` – one prompt per line → JSONL → upload → create job.
- `SubmitBatchJobFileAsync(client, dir, jobName?)` – uploads each file in a directory, builds a JSONL referencing `fileData.fileUri`, creates the job.
- `ContinueFileBatchJobAsync(client, jobFilePath)` – resume from a saved job file.

`Jsonl` generates requests with the client's system prompt, max output tokens, response MIME type and `BLOCK_NONE` safety settings. A default job name `batch_<yyMMdd_HHmmss>` is used when none is given.

## Usage Guide

```csharp
using PiscesCLI.Batch;
using PiscesCLI.Core;

CoreBootstrapper.GetClientConfig("client_config.toml", out var cfg);
var client = new Client(cfg!);

await BatchRunner.SubmitBatchJobTextAsync(client, "prompts.txt", "my_job");
await BatchRunner.SubmitBatchJobFileAsync(client, "./docs", "doc_job");

// Resume after a failed upload
await BatchRunner.ContinueFileBatchJobAsync(client, "cache/batch_job_doc_job_<ts>_object.json");

// Later: download output JSONL + CSV into output/
await client.FetchBatchJobAsync("batches/xxxxxxxx");
```

CLI equivalents: `batch-text`, `batch-file`, `fetch` (see `PiscesCLI.ConsoleApp`). Resume is available through the library API only.
