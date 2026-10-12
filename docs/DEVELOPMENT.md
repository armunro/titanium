# Developer Guide

## Prerequisites

- .NET 9 / 10 SDK
- Windows OS (for WPF Desktop presentation)
- JetBrains Rider or Visual Studio 2022 / 2026

## Building and Running

1. Clone repository.
2. Build solution:
   ```powershell
   dotnet build Titanium.sln
   ```
3. Run tests:
   ```powershell
   dotnet test Titanium.sln
   ```
4. Launch WPF Desktop app:
   ```powershell
   dotnet run --project src/Titanium.Desktop/Titanium.Desktop.csproj
   ```
