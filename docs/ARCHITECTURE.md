# Architecture Documentation

## Overview

**MangoAspire** is a cloud-native, microservices-based e-commerce platform built on **.NET 10** and orchestrated by **.NET Aspire**. It demonstrates a modern, scalable architecture using industry-standard patterns like CQRS, Event-Driven Architecture, and Vertical Slices.

## High-Level Architecture

The system is composed of several autonomous microservices, each with its own database, communicating asynchronously via an Event Bus/Message Broker.

```mermaid
graph TD
    subgraph "Frontend Applications"
        Web[Mango.Web (MVC)]
        UI[mango-ui (React SPA)]
    end

    subgraph "Gateway / Orchestrator"
        AppHost[Mango.AppHost (.NET Aspire)]
        Gateway[Mango.Gateway (YARP Proxy)]
    end

    subgraph "Core Microservices"
        Identity[Identity.API]
        Product[Products.API]
        Cart[ShoppingCart.API]
        Order[Orders.API]
        Coupon[Coupons.API]
        Payment[Payments.API]
        Chat[ChatAgent.App (AI)]
    end

    subgraph "Infrastructure"
        RabbitMQ[RabbitMQ / Azure Service Bus]
        Postgres[(PostgreSQL)]
        Debezium[Debezium (CDC)]
    end

    Web --> Gateway
    UI --> Gateway
    
    Gateway --> Identity
    Gateway --> Product
    Gateway --> Cart
    Gateway --> Order
    Gateway --> Coupon
    Gateway --> Chat

    Product --> Postgres
    Cart --> Postgres
    Order --> Postgres
    Coupon --> Postgres
    Identity --> Postgres
    Payment --> Postgres
    Chat --> Postgres

    Product -.->|CDC Events| Debezium
    Debezium -.->|Publish| RabbitMQ
    
    Order -.->|Integration Events| RabbitMQ
    Payment -.->|Integration Events| RabbitMQ
    Cart -.->|Integration Events| RabbitMQ

    RabbitMQ -.->|Consume| Order
    RabbitMQ -.->|Consume| Cart
    RabbitMQ -.->|Consume| Payment
    RabbitMQ -.->|Consume| Product
```

## Key Components

### 1. Identity & Security
- **Duende IdentityServer**: Centralized authentication and authorization.
- **OpenID Connect (OIDC)**: Used for secure communication between the frontends and microservices.
- **Token-Based Auth**: Bearer tokens handling access control.

### 2. Event-Driven Communication
- **RabbitMQ**: Default message broker for local development.
- **Azure Service Bus**: Production-ready alternative (configurable via `AppHost`).
- **Integration Events**: Used for cross-service communication (e.g., `OrderCreated`, `PaymentSucceeded`).

### 3. Data Synchronization (CDC)
- **Debezium**: Captures row-level changes in the `Products` database.
- **Real-Time Sync**: Updates the `ShoppingCart` service's read model to ensure product prices and names are always current without direct service-to-service calls.

### 4. Database Strategy
- **Database-per-Service**: Each microservice owns its data and schema.
- **PostgreSQL**: The primary relational database engine.
- **Entity Framework Core**: ORM for data access, using Code-First migrations.

### 5. AI Integration
- **ChatAgent.App**: A dedicated service for AI-powered interactions, utilizing **Semantic Kernel** to provide intelligent responses to user queries and conversation history persistence.

### 6. Observability
- **OpenTelemetry**: Built-in logging, metrics, and distributed tracing.
- **Aspire Dashboard**: Centralized view of all service health, logs, and traces.

## Service Breakdown

| Service | Responsibility | Database |
| :--- | :--- | :--- |
| **Identity.API** | AuthN/AuthZ, user management | `identitydb` |
| **Products.API** | Product catalog management | `productdb` |
| **ShoppingCart.API** | User cart & items | `shoppingcartdb` |
| **Coupons.API** | Discount codes & promotions | `coupondb` |
| **Orders.API** | Order lifecycle management | `orderdb` |
| **Payments.API** | Payment processing simulation | `N/A` (Stateless) |
| **ChatAgent.App** | AI assistant for user queries | `chatagent` |
| **Mango.Orchestrators** | Complex transaction management (Sagas) | `mango-orchestrators` |

## Project Structure (Vertical Slice)

Services follow the **Vertical Slice Architecture**, grouping code by feature (e.g., `CreateOrder`, `GetProduct`) rather than technical layers. This ensures high cohesion and low coupling.
