# Titanium — Communication Intelligence & Evidence Management Platform

Titanium is a Clean Architecture Windows desktop platform for legal, compliance, and corporate communication intelligence. It integrates with Microsoft 365 Outlook to extract atomic statements, normalize claims, detect contradictions, and reconstruct event narratives.

## Architecture

- **Clean Architecture Modular Monolith**:
  - `Titanium.Domain`
  - `Titanium.Contracts`
  - `Titanium.Application`
  - `Titanium.Infrastructure`
  - `Titanium.Integrations.M365`
  - `Titanium.Intelligence`
  - `Titanium.Workers`
  - `Titanium.Desktop` (WPF MVVM with `CommunityToolkit.Mvvm` and `Microsoft.Extensions.Hosting`)

## Getting Started

```powershell
dotnet restore
dotnet build Titanium.sln
dotnet test Titanium.sln
```
