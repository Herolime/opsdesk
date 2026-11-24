# OpsDesk

Enterprise IT & Operations Service Desk platform built with **.NET 10**, **Clean Architecture**, **DDD**, **CQRS (MediatR)**, **Dapper + SQLite**, and **MudBlazor** (`Obsidian & Emerald` theme).

---

## Architecture Overview

```mermaid
flowchart TD
    Web["OpsDesk.Web (Interactive Blazor + MudBlazor)"] --> App["OpsDesk.Application (MediatR CQRS + FluentValidation)"]
    Api["OpsDesk.Api (ASP.NET Core REST + OpenAPI)"] --> App
    Api --> Data["OpsDesk.Data (Dapper + SQLite)"]
    Web --> Data
    Data --> App
    App --> Domain["OpsDesk.Domain (DDD Aggregates, Value Objects, Events)"]
    Tests["OpsDesk.IntegrationTests (xUnit + WebApplicationFactory)"] --> Api
```

### Key Patterns & Design Highlights
- **Domain-Driven Design (`OpsDesk.Domain`)**:
  - `ServiceTicket` aggregate root enforcing lifecycle invariants (status transitions, specialist assignment, internal notes, technical reviews, and cross-team escalations).
  - Immutable Value Objects: `TicketId` (`OPS-2041`), `RequesterInfo`, and `SlaWindow` (business-day SLA calculation: `Urgent` = 1 day, `High` = 3 days, `Standard` = 7 days).
  - Strongly typed Domain Events (`TicketSubmittedEvent`, `TicketAssignedEvent`, `TicketStatusChangedEvent`, `TicketEscalatedEvent`).
- **CQRS & Validation Pipeline (`OpsDesk.Application`)**:
  - Command/Query separation via **MediatR**.
  - Cross-cutting `ValidationBehavior<TRequest, TResponse>` powered by **FluentValidation**.
- **Split Read/Write Persistence (`OpsDesk.Data`)**:
  - Aggregate write persistence (`TicketRepository`) paired with optimized Dapper read projections (`TicketQueryRepository`) over SQLite.
  - Automated schema initialization and demo seed data (`SchemaInitializer`).
- **MudBlazor UI (`OpsDesk.Web`)**:
  - Dark-mode **Obsidian & Emerald** `MudTheme` (`#0F172A` surface, `#10B981` primary accent, `ROUND_EIGHT` geometry).
  - Live **Persona Switcher** (`Operations Lead`, `Triage Lead`, `Specialist`, `Requester`, `System Admin`) in the top bar.
  - 9 operational views: Overview Dashboard, My Requests, Submit Service Request (with live SLA & routing preview), Split-Pane Triage Queue, Ticket Detail Workspace, All Tickets Directory, Reports & SLA Analytics, Admin & Role Management, and Notification Center.
- **End-to-End Integration Testing (`OpsDesk.IntegrationTests`)**:
  - Isolated SQLite `WebApplicationFactory<Program>` tests covering the full ticket lifecycle from submission through SLA calculation, triage, assignment, collaboration, technical review, escalation, resolution, and analytics reporting.

---

## UI Design (Google Stitch)

The UI was designed in **Google Stitch** using the **OpsDesk — Obsidian & Emerald** design system (`#0F172A` dark slate surface, `#10B981` emerald primary accent, `Geist` / `Inter` typography, `ROUND_EIGHT` geometry):

- **Stitch Project**: [OpsDesk (`projects/12928265875133280844`)](https://stitch.withgoogle.com/projects/12928265875133280844)
- **Design Manifest**: [`.stitch/opsdesk-metadata.json`](.stitch/opsdesk-metadata.json)

| # | Screen | Blazor Route | Stitch Screen |
|---|---|---|---|
| 1 | Operations Overview Dashboard | `/` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=e9a6ac8fb02a43ad81f1bb60aed60010) |
| 2 | My Requests — Requester Portal | `/my-requests` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=b84dce9dff5d4f5e9d7f252b9c97aec9) |
| 3 | Submit Service Request | `/submit` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=70812d88ca05450e8a17a301b2c5ea38) |
| 4 | Triage Queue & Assignment | `/triage` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=06d26d5ba20e4df38a45a85080ed5d91) |
| 5 | Ticket Detail & Collaboration Workspace | `/tickets/{id}` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=b62e87c1e9714e0ca7984f62fa9a008b) |
| 6 | All Tickets Directory | `/tickets` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=5d7b21c8d1844c7c8f950b5f9003e676) |
| 7 | Reports & SLA Analytics | `/reports` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=8495a52d210945de8adaad6893d3f11b) |
| 8 | Admin & Role Management | `/admin/roles` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=7a30863fafcf478cafd25f0a080d0221) |
| 9 | Notification Center | `/notifications` | [View in Stitch](https://stitch.withgoogle.com/projects/12928265875133280844?screen=1ac96bbb186544d590a6149715fb7ed0) |

---

## Web UI Screenshots

### Operations Overview Dashboard
![Operations Overview Dashboard](docs/screenshots/overview.png)

### Submit Service Request
![Submit Service Request](docs/screenshots/submit-request.png)

### Triage Queue & Assignment
![Triage Queue and Assignment](docs/screenshots/triage-queue.png)

### Ticket Detail & Collaboration Workspace
![Ticket Detail Workspace](docs/screenshots/ticket-detail.png)

### Reports & SLA Analytics
![Reports and SLA Analytics](docs/screenshots/reports-sla.png)

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the Blazor Web App (Standalone with Demo Seed Data)
```bash
dotnet run --project src/OpsDesk.Web
```

### Run the REST API & Swagger UI
```bash
dotnet run --project src/OpsDesk.Api
```

### Run Integration Tests
```bash
dotnet test OpsDesk.slnx
```
