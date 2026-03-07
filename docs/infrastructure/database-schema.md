# Infrastructure & Database Schema

The infrastructure logic centers heavily around cloud-native orchestration managed locally through **.NET Aspire**.
For production, the Aspire Manifest guarantees smooth Azure Developer CLI (`azd`) deployments across AKS/Container Apps.

## Database-per-Service
The system explicitly abides by the database-per-service pattern. Sharing relational databases across service boundaries is strictly prohibited.
All relational stores are backed by **PostgreSQL** and modeled with Entity Framework Core (EF Core) via Code-First migrations.

- **`identitydb`**: Schema dedicated to user accounts, roles, claims, and OAuth grants.
- **`productdb`**: Master schema for Products and Catalog Types.
- **`shoppingcartdb`**: Extremely transient database for storing cart session data.
- **`orderdb`**: Master transaction log managing order states.
- **`coupondb`**: Stores static promotional data.

## Change Data Capture
To circumvent direct database joins, the `ShoppingCart` service consumes a local read-model of product details (name, price, imageUrl). This model is populated via **Debezium**, which monitors the `Products` database's write-ahead log. When a product changes, Debezium publishes an event to RabbitMQ, eventually updating the downstream cart schema.

## Event Broker
RabbitMQ is the default local event broker, abstracted heavily through a core Event Bus interfaces (e.g., `IMessageBus` or MassTransit configuration).
This allows decoupled microservices to coordinate complex long-running operations (like submitting an order and fulfilling payment) fully asynchronously.
