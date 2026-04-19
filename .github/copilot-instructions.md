# Copilot Instructions for QiviLabel

## Project Overview

- This repository contains `QiviLabel`, a cross-platform desktop app built with Avalonia on .NET 10.
- Main app project: `BarcodeApp/`
- Test project: `BarcodeApp.Tests/`
- Solution file: `BarcodeApp.sln`

## Tech Stack

- C# / .NET 10
- Avalonia UI 12
- MVVM via CommunityToolkit.Mvvm
- CsvHelper and ClosedXML for import
- xUnit for tests

## Architecture and Conventions

- Root namespace is `Qivisoft.BarcodeApp`.
- Follow MVVM separation:
  - `Views/` for XAML and UI code-behind
  - `ViewModels/` for app logic/state
  - `Services/` for import/export/business services
  - `Models/` for DTOs/options/settings
- Keep business logic out of code-behind when possible.
- Prefer small, focused methods and explicit naming.

## UI/UX Rules

- UI text is in Polish and should remain Polish unless explicitly requested otherwise.
- Main window title is `QiviLabel`.
- Export should remain disabled when there are no valid rows to export.
- Keep the configuration section compact/responsive (currently collapsible) to preserve table visibility on smaller screens.

## Barcode and ZPL Rules

- Supported barcode types:
  - EAN-13
  - EAN-8
  - UPC-A
  - Code 128
- Validation must match selected symbology.
- ZPL payload formatting must match symbology behavior already implemented:
  - EAN-13: send 12 data digits to `^BE`
  - EAN-8: send 7 data digits to `^B8`
  - UPC-A: send 11 data digits to `^BU`
  - Code 128: send full content to `^BC`
- Do not reintroduce direct printer host/port send flow unless explicitly requested.

## Settings and Profiles

- Printer/label settings are persisted.
- Multiple printer profiles are supported and should remain backward-compatible with legacy settings fields.
- Keep profile actions deterministic (add/remove/select/reset/presets).

## Testing Requirements

- Add/update tests for behavior changes.
- Prefer deterministic tests (use in-memory settings stores for ViewModel tests when possible).
- Run before finalizing changes:

```bash
dotnet build BarcodeApp.sln -c Debug
dotnet test BarcodeApp.sln --configuration Debug
```

## GitHub Actions

- Manual packaging workflow exists at `.github/workflows/manual-build-package.yml`.
- It is manually triggered and builds selected target artifacts.
- Keep workflow_dispatch behavior intact unless explicitly asked to change release process.

## Editing Safety

- Avoid broad refactors unless needed for the task.
- Preserve existing file/folder names unless requested.
- Keep changes minimal and aligned with current code style.
