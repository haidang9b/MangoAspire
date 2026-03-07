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
- **Products.API**: Manages the product catalog.
- **ShoppingCart.API**: Manages user carts and items.
- **Orders.API**: Handles the order lifecycle.
- **Coupons.API**: Manages discount codes.
- **Payments.API**: Simulates payment processing.
- **ChatAgent.App**: An AI assistant powered by Semantic Kernel.

## Background Synchronization
A Change Data Capture (CDC) pipeline using **Debezium** captures and publishes changes from specific databases (like `Products` and `Identity`) to ensure eventual consistency across dependent read models without direct synchronous coupling.

*(For detailed logical diagrams, refer to the root `ARCHITECTURE.md` file.)*
