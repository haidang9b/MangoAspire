var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres");

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    .WithDataVolume();
var productdb = postgres.AddDatabase("productdb");

var products = builder.AddProject<Projects.Mango_Services_Products>("mango-services-products")
    .WaitFor(productdb)
    .WithReference(productdb);

builder.Build().Run();
