using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Storage;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> apiKey = builder.AddParameter("apiKey", secret: true);
IResourceBuilder<ParameterResource> customDomain =
    builder.AddParameter("customDomain", "go.richardmurillo.com", publishValueAsDefault: true);

// The Container Apps managed certificate for customDomain (see appsettings.json). Deploying with an empty value
// leaves the hostname attached but unbound, which is only correct before the certificate has been issued.
IResourceBuilder<ParameterResource> certificateName = builder.AddParameter("certificateName");

// The managed dashboard is an always-on component; skip it to keep the environment at scale-to-zero cost.
builder.AddAzureContainerAppEnvironment("aca")
    .WithDashboard(false);

IResourceBuilder<AzureTableStorageResource> tables = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .ConfigureInfrastructure(infra =>
    {
        StorageAccount account = infra.GetProvisionableResources().OfType<StorageAccount>().Single();
        account.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
    })
    .AddTables("tables");

// The readiness probe also registers /health as the Aspire health check that WaitFor and the dashboard use.
IResourceBuilder<ProjectResource> web = builder.AddProject<Projects.UrlShortener_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpProbe(ProbeType.Readiness, "/health", periodSeconds: 10, timeoutSeconds: 5, failureThreshold: 3)
    .WithHttpProbe(ProbeType.Liveness, "/alive", periodSeconds: 30, timeoutSeconds: 3, failureThreshold: 3)
    .WithReference(tables)
    .WaitFor(tables)
    .WithEnvironment("Shortener__ApiKey", apiKey)
    .WithEnvironment("Shortener__HomeUrl", "https://richardmurillo.com")
    .PublishAsAzureContainerApp((_, app) =>
    {
        app.Template.Scale.MinReplicas = 0;
        app.Template.Scale.MaxReplicas = 1;
        app.Template.Containers[0].Value!.Resources = new AppContainerResources
        {
            Cpu = 0.25,
            Memory = "0.5Gi",
        };
        app.ConfigureCustomDomain(customDomain, certificateName);
    });

ReferenceExpression baseUrl = builder.ExecutionContext.IsPublishMode
    ? ReferenceExpression.Create($"https://{customDomain}")
    : ReferenceExpression.Create($"{web.GetEndpoint("http")}");
web.WithEnvironment("Shortener__BaseUrl", baseUrl);

await builder.Build().RunAsync().ConfigureAwait(false);
