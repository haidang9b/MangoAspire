var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres");

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var productdb = postgres.AddDatabase("productdb");
var orderdb = postgres.AddDatabase("orderdb");
var coupondb = postgres.AddDatabase("coupondb");
var identitydb = postgres.AddDatabase("identitydb");

var identity = builder.AddProject<Projects.Identity_API>("identity-app")
    .WaitFor(identitydb)
    .WithReference(identitydb);

var products = builder.AddProject<Projects.Products_API>("products-api")
    .WaitFor(productdb)
    .WithReference(productdb);

builder.AddProject<Projects.Coupons_API>("coupons-api")
    .WaitFor(coupondb)
    .WithReference(coupondb);

builder.Build().Run();
