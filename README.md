# dotnet-graphql-service

This project uses GraphQL API for product data, using HotChocolate. It exposes a `/graphql` endpoint for querying and mutating complex product data.

## Prerequisites
- .NET 8 or 9 SDK

## Running the Project

1. Restore dependencies and build:
   ```sh
   dotnet build
   ```
2. Run the API:
   ```sh
   dotnet run
   ```
   The server will start at `http://localhost:5284` (see terminal output for the exact port).

3. Open the GraphQL Playground in your browser:
   - Visit: [http://localhost:5284/graphql](http://localhost:5284/graphql)

## Example Queries


### 1. Get a Sample Red Hat Product (with parameters)

You can now pass parameters to the `product` query to get different product details. For example, to get the default sample:

**GraphQL Query:**
```graphql
{
  product(input: { }) {
    id
    name
    version
    releaseDate
    supportedArchitectures
    lifecycle { start end }
    features { name enabled }
    subscriptions { type price supportLevel }
  }
}
```

To get a different product by ID:
```graphql
{
  product(input: { id: "RHEL-9-001" }) {
    id
    name
    version
    releaseDate
    supportedArchitectures
    lifecycle { start end }
    features { name enabled }
    subscriptions { type price supportLevel }
  }
}
```

**Response (for id: "RHEL-9-001"):**
```json
{
  "data": {
    "product": {
      "id": "RHEL-9-001",
      "name": "Red Hat Enterprise Linux 9",
      "version": "9.0",
      "releaseDate": "2025-01-01",
      "supportedArchitectures": ["x86_64", "aarch64"],
      "lifecycle": { "start": "2025-01-01", "end": "2035-01-01" },
      "features": [
        { "name": "SELinux", "enabled": true },
        { "name": "Podman", "enabled": true }
      ],
      "subscriptions": [
        { "type": "Standard", "price": 399.0, "supportLevel": "24x7" }
      ]
    }
  }
}
```


### 2. POST a JSON Payload (Mutation)

The mutation manipulates the product before returning it. For example, it appends "-ECHOED" to the product name and increments the major version number.

**GraphQL Mutation Example:**
```graphql
mutation {
  product(input: {
    id: "RHEL-9-001"
    name: "Red Hat Enterprise Linux 9"
    version: "9.0"
    releaseDate: "2025-01-01"
    supportedArchitectures: ["x86_64", "aarch64"]
    lifecycle: { start: "2025-01-01", end: "2035-01-01" }
    features: [
      { name: "SELinux", enabled: true }
      { name: "Podman", enabled: true }
    ]
    subscriptions: [
      { type: "Standard", price: 399.0, supportLevel: "24x7" }
    ]
  }) {
    id
    name
    version
  }
}
```

**Response:**
```json
{
  "data": {
    "echoProduct": {
      "id": "RHEL-9-001",
      "name": "Red Hat Enterprise Linux 9-ECHOED",
      "version": "10.0"
    }
  }
}
```

## Notes
- The schema and types are defined in `Program.cs`.
- To add more queries or mutations, extend the GraphQL types in `Program.cs`.

---

For more on HotChocolate GraphQL: https://chillicream.com/docs/hotchocolate
