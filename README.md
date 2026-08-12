# 🥭 MangoAspire

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire-Orchestration-orange?logo=dotnet)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**MangoAspire** is a modern, high-performance microservices-based e-commerce platform built using the latest **.NET 10** features and **.NET Aspire** for cloud-native orchestration.

---

## 🏗️ Architecture Overview

The project follows a microservices architecture, leveraging .NET Aspire to simplify service discovery, configuration, and orchestration.

> [!NOTE]
> For a detailed deep-dive into the system design, see [**Architecture Documentation**](docs/ARCHITECTURE.md).

- **Orchestration**: .NET Aspire AppHost manages service lifecycles and dependencies.
- **Gateway**: [YARP Reverse Proxy](https://microsoft.github.io/reverse-proxy/) for centralized request routing to all backend services.
- **Communication**: Synchronous REST APIs and Asynchronous EventBus.
- **Data Persistence**: Dedicated PostgreSQL databases for each microservice.
- **Security**: Centralized identity management using Duende IdentityServer.
- **Data Synchronization**: Change Data Capture (CDC) with Debezium.
- **Frontend Variety**: Choice of a traditional **ASP.NET Core MVC** or a modern **React SPA**.

---

## 🛠️ Tech Stack

- **Framework**: [.NET 10.0](https://dotnet.microsoft.com/)
- **Orchestration**: [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- **Frontend**: [React 18](https://reactjs.org/) (Vite) / [ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview)
- **Database**: [PostgreSQL](https://www.postgresql.org/) with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **Identity**: [Duende IdentityServer](https://duendesoftware.com/products/identityserver)
- **Messaging**: [RabbitMQ](https://www.rabbitmq.com/) (Default) / [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/)
- **AI Integration**: [Semantic Kernel](https://github.com/microsoft/semantic-kernel) (ChatAgent) with OpenAI support.
- **Gateway**: [YARP Reverse Proxy](https://microsoft.github.io/reverse-proxy/)
- **Patterns**: MediatR (CQRS), FluentValidation, Result Pattern, Vertical Slice Architecture
- **Observability**: OpenTelemetry (Metrics, Tracing, Logging) → Aspire Dashboard + [Grafana / Prometheus / Loki](docs/OBSERVABILITY.md)
- **CDC**: [Debezium](https://debezium.io/)
- **Containerization**: Optimized **Alpine-based** Docker images for reduced size and enhanced security.

---

## 📂 Project Structure

```text
MangoAspire/
├── src/
│   ├── Mango.AppHost/           # .NET Aspire Orchestrator
│   ├── Mango.ServiceDefaults/   # Common service configurations (Resilience, OTEL, etc.)
│   ├── Gateway/
│   │   └── Mango.Gateway        # YARP Reverse Proxy Gateway
│   ├── Services/                # Microservices
│   │   ├── Identity.API         # Duende IdentityServer
│   │   ├── Products.API         # Product Catalog Service
│   │   ├── Coupons.API          # Promotions Service
│   │   ├── Orders.API           # Order Management Service
│   │   ├── ShoppingCart.API     # Shopping Cart Service
│   │   ├── Payments.API         # Payment Mock Service
│   │   ├── ChatAgent.App        # AI Assistant Service (Semantic Kernel)
│   │   └── Mango.Orchestrators  # Saga Orchestrators
│   ├── Shared/                  # Shared libraries (Mango.Core, Mango.Infrastructure)
│   ├── EventBus/                # Message Bus abstraction
│   ├── UI/                      # Frontend applications
│   │   ├── Mango.Web            # MVC Web App (Classic)
│   │   └── mango-ui             # Modern React SPA (Vite + TypeScript)
├── docs/                        # Human documentation
│   ├── ARCHITECTURE.md          # Global System Architecture
│   └── ...                      # Feature-specific guides
├── .agent/                      # AI agent harness (see .agent/README.md)
│   ├── rules/ skills/ workflows/  # Normative guidance, skills, slash-command workflows
│   ├── memory/                  # Project memory (index.md links out to domain files)
│   ├── tickets/                 # Per-ticket state, notes and blueprints
│   ├── tools/                   # MCP servers, dev scripts, harness generation
│   └── ui/board.html            # Generated ticket board — open it in a browser
└── Directory.Packages.props     # Centralized NuGet versioning
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (required for Aspire container resources like PostgreSQL)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (version 17.12 or higher recommended) or [VS Code](https://code.visualstudio.com/)

### Running the Application

1. **Clone the repository**:
   ```bash
   git clone https://github.com/haidang9b/MangoAspire.git
   cd MangoAspire
   ```

2. **Run via Aspire AppHost**:
   - Set `Mango.AppHost` as the startup project.
   - Press `F5` or run:
     ```bash
     dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj
     ```

3. **Run via Docker Compose**:
   - Use the root-level `launchSettings.json` profiles to debug via Docker Compose.
   - Run:
     ```bash
     docker-compose up -d
     ```

4. **Access the Dashboard**:
   Once running, the .NET Aspire Dashboard URL will be printed in the console. Open it to monitor services, logs, and traces.

5. **Access Grafana** (optional):
   Logs and metrics are also shipped to a Grafana + Prometheus + Loki stack at
   <http://localhost:3000>, which keeps history across restarts. Turn it off with
   `--UseGrafanaStack false`. See [Observability](docs/OBSERVABILITY.md).

---

## 📘 Documentation

For detailed information on project-specific setups:
- [React UI Documentation](src/UI/mango-ui/README.md)
- [NuGet Package Management Guide](docs/PACKAGE_MANAGEMENT.md)
- [API Project Structure & Architecture](.agent/API_PROJECT_STRUCTURE.md)
- [Coding Conventions & Standards](.agent/CODING_CONVENTIONS.md)
- [Agent Harness Overview](.agent/README.md)
- [Project Memory Index](.agent/memory/index.md)
- [Ticket Board](.agent/ui/board.html) — generated by `pwsh ./scripts/update-ticket-board.ps1`
- [Checkout Saga Workflow](docs/CHECKOUT_SAGA.md)
- [Event Bus Usage & Switching](docs/EVENT_BUS.md)
- [Change Data Capture (CDC)](docs/CDC.md)
- [Observability: Grafana, Prometheus & Loki](docs/OBSERVABILITY.md)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
