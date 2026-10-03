# Contributing to PiscesCLI

Thank you for your interest in PiscesCLI and the engineering work of Everblooming Lab (Catmint Works)! 

We are thrilled to share our internal Gemini AI toolchain (CLI, Bulk/Batch processing workflows, and HTTP Daemon) with the broader developer community. Because our primary focus as a boutique studio is on shipping our own core projects, we have specific guidelines regarding how we interact with this public repository.

Please read the following guidelines before interacting with this repository.

## 🚫 Pull Requests (Not Accepted)

**Please do not submit Pull Requests.** 

This public repository operates strictly in a **"Showcase / Window" mode**. It is maintained separately as a read-only mirror of our internal development environment. Because our active development happens in a private, internal pipeline, we cannot integrate external code changes directly into this public repository. 

Any Pull Requests submitted to this repository will be politely ignored and automatically closed. If you have a specific feature you want to add (e.g., supporting a different LLM API provider, adding new CLI commands, or altering the Daemon contract), we highly encourage you to fork the repository instead (see below).

## 🐛 Issues: Bug Reports & Suggestions

We welcome friendly bug reports, architectural suggestions, and general feedback via the GitHub Issues tab! 

However, please understand that this software is provided strictly "as-is." **We do not guarantee that we will read, reply to, or implement any suggestions or fixes.** If an issue aligns with our internal development roadmaps, we may port the fix from our internal codebase to this public repository at our own discretion, but there is absolutely no promised timeline or SLA.

## ❓ Usage Questions & Technical Support

You are welcome to open an issue to ask questions about configuration scaffolding (`.toml`), Batch job tracking, or Daemon deployment. 

However, **we expect users to be entirely self-reliant.** We do not provide technical support. Please be aware that:
* **Out of Scope:** We will not provide support or troubleshooting for Google GenAI API quota limits, region restrictions, network routing/proxy setup, or API key management.
* We do not guarantee a response to any questions.
* Issues may be left unanswered for extended periods.
* Issues may be closed without reason or explanation.
* Our engineering team may simply not have the bandwidth to review incoming queries.

## 🍴 Forking is Highly Encouraged!

PiscesCLI is released under the **MIT License**. 

You are completely free—and warmly encouraged—to fork this repository, modify the code to fit your specific project needs (such as adapting the HTTP Daemon for other browser extensions, or rewriting the Bulk workers), and maintain your own version of these tools. You do not need our permission to build amazing things with this codebase.

Thank you for understanding our constraints, and we hope you find our internal toolchain useful!
