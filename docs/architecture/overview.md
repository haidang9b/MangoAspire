# Architecture Overview

**MangoAspire** is a modern, high-performance microservices-based e-commerce platform built using the latest **.NET 10** features and **.NET Aspire** for cloud-native orchestration.

## High-Level Flow
The system is composed of several autonomous microservices, each managing its own data store. The services communicate synchronously via REST/gRPC and asynchronously via an Event Bus configured with RabbitMQ or Azure Service Bus.

- **Frontend Applications**: 
  - `mango-ui`: A modern React SPA built with Vite and TypeScript.
  - `Mango.Web`: A traditional ASP.NET Core MVC application.
- **Gateway**: `Mango.Gateway` uses YARP (Yet Another Reverse Proxy) to route incoming traffic to the backend microservices.
- **Orchestration**: `Mango.AppHost` is the .NET Aspire project responsible for spinning up the microservices, message brokers, and databases locally.

## Core Microservices
The backend features the following primary services:
- **Identity.API**: Handles AuthN/AuthZ using Duende IdentityServer.
- **OpenIdentity.App**: Alternative identity service utilizing **OpenIddict**, MVC controllers for the interactive OIDC flows, and Minimal APIs for administration.
- **Products.API**: Manages the product catalog.
- **ShoppingCart.API**: Manages user carts and items.
- **Orders.API**: Handles the order lifecycle.
- **Coupons.API**: Manages discount codes.
- **Payments.API**: Simulates payment processing.
- **ChatAgent.App**: An AI assistant powered by Semantic Kernel.

## Identity Provider Switch

The two identity providers are **mutually exclusive** — exactly one is registered and started per run. `Mango.AppHost` reads an `IdentityType` setting and resolves a single `identityRef` that every consumer is wired against:

| `IdentityType` | Project started | Database |
| :--- | :--- | :--- |
| `Duende` (default) | `Identity.API` | `identitydb` |
| `OpenIddict` | `OpenIdentity.App` | `openidentitydb` |

The value is read from `appsettings.json` (`IdentityType`), the `IdentityType` / `IDENTITY_TYPE` environment variables, or `--IdentityType` on the CLI. Any other value fails fast at startup with an `InvalidOperationException`.

Because only one provider runs, nothing downstream hardcodes an authority. `Mango.AppHost` resolves the selected provider's endpoint once and propagates it:

- Backend services receive `ServiceUrls__IdentityApp`, which becomes the JWT `Authority`.
- `Mango.Web` additionally receives `OpenIdConnect__Authority`.
- `mango-ui` receives `VITE_IDENTITY_URL`.
- Redirect and post-logout URIs are pushed into whichever provider is active, using that provider's own configuration shape (`OpenIddict__Clients__*` vs `IdentityServer__Clients__*`).

Two consequences worth knowing when switching providers:

- **Scope claim format differs.** Duende emits one `scope` claim per scope; OpenIddict emits a single space-delimited `scope` claim. Authorization policies split on whitespace so both formats satisfy the same policy — see [API & Endpoints](../api/endpoints.md).
- **Seed user IDs must match.** Both providers bind the shared `Mango.Core.Options.SeedUsersOptions` section, and each seed account carries a fixed `Id` that is issued as the OIDC `sub` claim. Carts and orders are keyed by that subject, so a mismatch between the two providers' configured IDs orphans existing user data across a toggle. `SeedUserOptions.Validate` fails fast on missing `Id`, `UserName`, `Password`, or `Role`.

## Caching

A shared `ICacheManager` abstraction (`Mango.Core.Caching`) backed by .NET `HybridCache` provides a single caching entry point across services. Registered with `services.AddCacheManager()`, it is an in-process (L1) cache today; registering an `IDistributedCache` such as Redis adds the L2 tier with no change to calling code. See [Backend Services](../backend/services.md#caching) for the contract and current consumers.

## Background Synchronization
A Change Data Capture (CDC) pipeline using **Debezium** captures and publishes changes from specific databases (like `Products` and `Identity`) to ensure eventual consistency across dependent read models without direct synchronous coupling.

*(For detailed logical diagrams, refer to `docs/ARCHITECTURE.md`.)*
