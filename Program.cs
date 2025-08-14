
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<RedHatProductQuery>()
    .AddMutationType<RedHatProductMutation>();

var app = builder.Build();

app.MapGraphQL();
app.Run();

// --- just creating sample types for testing purposes ---
public class RedHatProductQuery
{
    public RedHatProduct GetProduct(ProductQueryInput input)
    {
        // return different products based on input parameters
        if (input?.Id == "RHEL-9-001")
        {
            return new RedHatProduct
            {
                Id = "RHEL-9-001",
                Name = "Red Hat Enterprise Linux 9",
                Version = "9.0",
                ReleaseDate = "2025-01-01",
                SupportedArchitectures = new[] { "x86_64", "aarch64" },
                Lifecycle = new Lifecycle { Start = "2025-01-01", End = "2035-01-01" },
                Features = new[]
                {
                    new Feature { Name = "SELinux", Enabled = true },
                    new Feature { Name = "Podman", Enabled = true }
                },
                Subscriptions = new[]
                {
                    new Subscription { Type = "Standard", Price = 399.0, SupportLevel = "24x7" }
                }
            };
        }
        // default sample product
        return new RedHatProduct
        {
            Id = "RHEL-8-001",
            Name = "Red Hat Enterprise Linux",
            Version = "8.6",
            ReleaseDate = "2022-05-10",
            SupportedArchitectures = new[] { "x86_64", "aarch64", "ppc64le" },
            Lifecycle = new Lifecycle { Start = "2022-05-10", End = "2032-05-10" },
            Features = new[]
            {
                new Feature { Name = "SELinux", Enabled = true },
                new Feature { Name = "Cockpit", Enabled = true },
                new Feature { Name = "System Roles", Enabled = true }
            },
            Subscriptions = new[]
            {
                new Subscription { Type = "Standard", Price = 349.0, SupportLevel = "24x7" },
                new Subscription { Type = "Premium", Price = 699.0, SupportLevel = "24x7 with TAM" }
            }
        };
    }
}

public class ProductQueryInput
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
}

public class RedHatProduct
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string ReleaseDate { get; set; }
    public string[] SupportedArchitectures { get; set; }
    public Lifecycle Lifecycle { get; set; }
    public Feature[] Features { get; set; }
    public Subscription[] Subscriptions { get; set; }
}

public class Lifecycle
{
    public string Start { get; set; }
    public string End { get; set; }
}

public class Feature
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
}

public class Subscription
{
    public string Type { get; set; }
    public double Price { get; set; }
    public string SupportLevel { get; set; }
}

// adding a mutuation (POST) type definition
public class RedHatProductMutation
{
    public RedHatProduct Product(RedHatProduct input)
    {
        // update the product: append '-ECHOED' to name and increment version
        var newProduct = new RedHatProduct
        {
            Id = input.Id,
            Name = input.Name + "-ECHOED",
            Version = IncrementVersion(input.Version),
            ReleaseDate = input.ReleaseDate,
            SupportedArchitectures = input.SupportedArchitectures,
            Lifecycle = input.Lifecycle,
            Features = input.Features,
            Subscriptions = input.Subscriptions
        };
        return newProduct;
    }

    private string IncrementVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return version;
        var parts = version.Split('.');
        if (int.TryParse(parts[0], out int major))
        {
            major++;
            parts[0] = major.ToString();
            return string.Join('.', parts);
        }
        return version + ".1";
    }
}
