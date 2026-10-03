
<div align="center">

# PiscesCLI

<img src="https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=c-sharp&logoColor=white" alt="C#" /> <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10.0" /> <img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License MIT" /> <img src="https://img.shields.io/badge/Status-Read_Only-red.svg?style=flat-square" alt="Read Only" />

</div>

<br/>

**PiscesCLI** is an internal C#-based command-line interface and background daemon toolchain created by **Everblooming Lab** (a DBA of Catmint Works LLC). 

Designed around the Google Gemini API (`Google.GenAI`), it provides a highly decoupled, rate-limited, and scalable framework for single-shot generation, high-concurrency real-time bulk processing, and asynchronous batch job management.

## ⚠️ Showcase Disclaimer (Read-Only Repository)

This repository operates in a **"Showcase / Window"** mode. It is a public mirror of our internal development environment, published to share our engineering practices with the broader developer community.

* **No Pull Requests:** We do not accept PRs. Active development occurs in our private pipeline.
* **No Technical Support:** This code is provided "as-is". We offer no SLA and do not guarantee responses to issues.
* **Experimental State:** Please note that this is an **early-stage internal tool (Pre-Alpha)**. It was rapidly prototyped to solve specific workflow bottlenecks for our team. As such, it is heavily optimized for "happy paths" and internal usage scenarios. 
* **Forking Encouraged:** You are highly encouraged to fork this repository and adapt the code for your own projects under the MIT License.

## 📦 Core Modules

PiscesCLI is structured into independent subsystems. Each module serves a specific operational paradigm, ranging from direct console interactions to local HTTP microservices.

| Module | Description |
|---|---|
| [**PiscesCLI.Core**](docs/PiscesCLI.Core.md) | The foundation library. Provides a unified facade over the Gemini SDK, dynamic TOML-based layered configuration (Core/Client/Model), model alias resolution, MIME-type mapping, and strict state tracking. |
| [**PiscesCLI.ConsoleApp**](docs/PiscesCLI.ConsoleApp.md) | The thin orchestration CLI front-end (powered by `ConsoleAppFramework`). Exposes interactive generation, connectivity tests, config scaffolding, and triggers for Bulk/Batch workflows. |
| [**PiscesCLI.Bulk**](docs/PiscesCLI.Bulk.md) | Real-time, parallel bulk generation engine. Processes large volumes of local text prompts or binary files (images/documents) concurrently, enforces strict RPM/Concurrency limits, and exports aggregated results to CSV. |
| [**PiscesCLI.Batch**](docs/PiscesCLI.Batch.md) | Asynchronous Gemini Batch API workflow manager. Automatically builds JSONL payloads, uploads assets, creates server-side jobs, and manages local state serialization to allow resuming interrupted batch sequences. |
| [**PiscesCLI.Daemon**](docs/PiscesCLI.Daemon.md) | A lightweight, long-running local HTTP server (`localhost:11435`). Exposes isolated endpoints (`/translate`, `/explain`) mapped to distinct AI personas. Integrates seamlessly with browser extensions like KISS Translator. |

## ⚙️ Architecture & Highlights

- **Multi-Tier Configuration:** Employs a three-layer fallback system via Snake-case TOML (`core_config.toml` -> `client_config.toml` -> `model_conf/<alias>.json`), allowing highly granular control over API keys, thinking levels, system prompts, and token limits.
- **Robust Throttling Pipeline:** Both `Bulk` and `Daemon` modules implement a unified dual-layer throttling mechanism: a `SemaphoreSlim` for concurrency capping and a `SlidingWindowRateLimiter` (unbounded queue, oldest first) to strictly enforce API RPM (Requests Per Minute) quotas.
- **Fault Tolerance:** Partial failures during bulk processing or batch uploads do not halt execution. Errors are encapsulated into `GenerationResponse` models and gracefully exported to CSV or logged, allowing for immediate resumption.
- **Data Agnostic Payload Resolution:** Automatically resolves MIME types via file extensions, converting local media (PDFs, images, videos) into valid inline byte payloads or server-side URIs dynamically.

## 🚀 Quick Usage (CLI)

```powershell
# 1. Scaffold default configurations
PiscesCLI create-configs

# 2. Interactive generation
PiscesCLI gene client_config.toml

# 3. High-concurrency bulk processing (Text -> CSV)
PiscesCLI bulk-text client_config.toml prompts.txt

# 4. Asynchronous Batch job submission (Directory files -> Cloud Batch)
PiscesCLI batch-file client_config.toml ./docs doc_processing_job

# 5. Fetch completed batch results
PiscesCLI fetch client_config.toml batches/xxxxxxxx
```

## 🏢 About Us

**Everblooming Lab** is the experimental engineering and toolchain division of **Catmint Works LLC**. We focus on building robust, scalable game architectures and rapid development utilities that power our interactive projects.

* **Website:** [catmintworks.com](https://catmintworks.com)
* **Contact:** lab@catmintworks.com / contact@catmintworks.com 

## ⚖️ Third-Party Acknowledgements

This project leverages several high-quality open-source libraries (e.g., `ConsoleAppFramework`, `Tomlyn`, `CsvHelper`). All third-party components are used in compliance with their respective licenses (MIT, Apache-2.0, BSD, etc.). 

Full license texts, copyright notices, and attributions for these dependencies are maintained in the `[THIRD-PARTY-LICENSES]` directory.

## 📄 License

Released under the [MIT License](LICENSE). 
© 2026 Catmint Works LLC. All rights reserved.

