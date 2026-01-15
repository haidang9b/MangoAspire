var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres");

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    .WithDataVolume();

var productdb = postgres.AddDatabase("productdb");
var orderdb = postgres.AddDatabase("orderdb");

var products = builder.AddProject<Projects.Products_API>("products-api")
    .WaitFor(productdb)
    .WithReference(productdb);

builder.AddProject<Projects.Coupons_API>("coupons-api")
    .WaitFor(orderdb)
    .WithReference(orderdb);

builder.Build().Run();
