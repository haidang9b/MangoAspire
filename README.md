# 🥭 MangoAspire

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire-Orchestration-orange?logo=dotnet)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**MangoAspire** is a modern, high-performance microservices-based e-commerce platform built using the latest **.NET 10** features and **.NET Aspire** for cloud-native orchestration.

---

## 🏗️ Architecture Overview

The project follows a microservices architecture, leveraging .NET Aspire to simplify service discovery, configuration, and orchestration.

- **Orchestration**: .NET Aspire AppHost manages service lifecycles and dependencies.
- **Communication**: Synchronous REST APIs and Asynchronous EventBus (RabbitMQ / Azure Service Bus).
- **Data Persistence**: Dedicated PostgreSQL databases for each microservice to ensure loose coupling.
- **Security**: Centralized identity management using Duende IdentityServer.

---

## 🛠️ Tech Stack

- **Framework**: [.NET 10.0](https://dotnet.microsoft.com/)
- **Orchestration**: [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- **Database**: [PostgreSQL](https://www.postgresql.org/) with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **Identity**: [Duende IdentityServer](https://duendesoftware.com/products/identityserver)
- **Patterns**: MediatR (CQRS), FluentValidation, Result Pattern
- **Observability**: OpenTelemetry for Logging, Metrics, and Tracing
- **Package Management**: Central Package Management (CPM) via `Directory.Packages.props`

---

## 📂 Project Structure

```text
MangoAspire/
├── src/
│   ├── Mango.AppHost/           # .NET Aspire Orchestrator
│   ├── Mango.ServiceDefaults/   # Common service configurations (Resilience, OTEL, etc.)
│   ├── Services/                # Microservices
│   │   ├── Identity.API         # Identity and Access Management (OpenIddict)
│   │   ├── Products.API         # Product Catalog Service
│   │   ├── Coupons.API          # Promotions and Discount Service
│   │   ├── Orders.API           # Order Management Service
│   │   └── ShoppingCart.API     # Shopping Cart Service
│   ├── Shared/                  # Shared libraries (Mango.Core, Mango.Infrastructure)
│   ├── EventBus/                # Message Bus abstraction
│   ├── EventBus.RabbitMQ/       # RabbitMQ implementation
│   ├── EventBus.ServiceBus/     # Azure Service Bus implementation
│   └── UI/                      # Frontend applications
├── docs/                        # Project documentation
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

3. **Access the Dashboard**:
   Once running, the .NET Aspire Dashboard URL will be printed in the console. Open it to monitor services, logs, and traces.

---

## 📘 Documentation

For detailed information on project-specific setups:
- [NuGet Package Management Guide](docs/PACKAGE_MANAGEMENT.md)
- [API Project Structure & Architecture](.agent/API_PROJECT_STRUCTURE.md)
- [Coding Conventions & Standards](.agent/CODING_CONVENTIONS.md)
- [Event Bus Usage & Switching](docs/EVENT_BUS.md)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
