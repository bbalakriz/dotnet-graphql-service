# 🚀 Developer Guide: Implementing Your Own GraphQL API with Schema Transformation

## 📋 **Prerequisites**
- .NET 9.0 SDK
- Basic understanding of GraphQL
- Target GraphQL API endpoint to integrate with

---

## 🛠️ **Step 1: Project Setup**

### **1.1 Clone and Setup Base Project**
```bash
# Clone the base project
git clone [your-repo-url] my-graphql-service
cd my-graphql-service

# Test the base implementation
dotnet build
dotnet run

# Verify it's working with Rick & Morty API
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ person(id: 1) { fullName lifeStatus } }"}'
```

### **1.2 Clean Up for Your Implementation**
```bash
# Keep the core files, modify them for your API
# Core files to modify:
# - Program.cs (replace Rick & Morty with your API)
# - field-mappings.json (replace with your schema mappings)
# - DYNAMIC_MAPPING_DEMO.md (optional: update documentation)
```

---

## 🎯 **Step 2: Define Your Target Schema**

### **2.1 Design Your Exposed GraphQL Schema**
Create your target entity models in `Program.cs`:

```csharp
// Example: If integrating with Pokemon API
public class PokemonCharacter
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryType { get; set; } = string.Empty;
    public string SecondaryType { get; set; } = string.Empty;
    public int PowerLevel { get; set; }
    public bool IsLegendary { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public Region? HomeRegion { get; set; }
    public IEnumerable<Ability> Abilities { get; set; } = Array.Empty<Ability>();
}

public class Region
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PokemonCount { get; set; }
}

public class Ability
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
}

public enum PokemonType
{
    Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison,
    Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy
}
```

---

## 🔄 **Step 3: Configure External API Integration**

### **3.1 Update GraphQL Client Configuration**
In `Program.cs`, replace the Rick & Morty API client:

```csharp
// Replace this section:
builder.Services.AddSingleton(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var options = new GraphQLHttpClientOptions
    {
        EndPoint = new Uri("https://your-target-api.com/graphql")  // 🔄 CHANGE THIS
    };
    return new GraphQLHttpClient(options, new NewtonsoftJsonSerializer(), httpClient);
});
```

### **3.2 Create Your Service Interface**
```csharp
public interface IYourApiService
{
    Task<PokemonCharacter?> GetPokemonAsync(int id);
    Task<IEnumerable<PokemonCharacter>> GetPokemonsAsync(string? name);
    Task<Region?> GetRegionAsync(int id);
    Task<IEnumerable<Ability>> GetAbilitiesAsync(string? name);
}
```

### **3.3 Implement Your Service**
```csharp
public class YourApiService : IYourApiService
{
    private readonly GraphQLHttpClient _client;
    private readonly IFieldMapper _fieldMapper;

    public YourApiService(GraphQLHttpClient client, IFieldMapper fieldMapper)
    {
        _client = client;
        _fieldMapper = fieldMapper;
    }

    public async Task<PokemonCharacter?> GetPokemonAsync(int id)
    {
        var query = @"
            query GetPokemon($id: ID!) {
                pokemon(id: $id) {
                    id
                    name
                    types {
                        type {
                            name
                        }
                    }
                    stats {
                        base_stat
                        stat {
                            name
                        }
                    }
                    sprites {
                        front_default
                    }
                    abilities {
                        ability {
                            name
                            effect_entries {
                                effect
                                language {
                                    name
                                }
                            }
                        }
                        is_hidden
                    }
                    species {
                        is_legendary
                    }
                }
            }";

        var request = new GraphQL.GraphQLRequest
        {
            Query = query,
            Variables = new { id = id.ToString() }
        };

        try
        {
            var response = await _client.SendQueryAsync<dynamic>(request);
            if (response.Data?.pokemon == null) return null;

            return _fieldMapper.MapEntity<PokemonCharacter>(response.Data.pokemon, "pokemon_mapping");
        }
        catch
        {
            return null;
        }
    }

    // Implement other methods...
}
```

---

## 📝 **Step 4: Configure Field Mappings**

### **4.1 Replace field-mappings.json**
Create your own mapping configuration:

```json
{
  "mappings": {
    "pokemon_mapping": {
      "targetType": "PokemonCharacter",
      "fields": {
        "id": {
          "sourceField": "id",
          "targetField": "Id",
          "dataType": "int",
          "isRequired": true
        },
        "name": {
          "sourceField": "name",
          "targetField": "DisplayName",
          "dataType": "string",
          "transformationRule": "capitalize_name",
          "isRequired": true
        },
        "primary_type": {
          "sourceField": "types[0].type.name",
          "targetField": "PrimaryType",
          "dataType": "string",
          "transformationRule": "capitalize_name"
        },
        "secondary_type": {
          "sourceField": "types[1].type.name",
          "targetField": "SecondaryType",
          "dataType": "string",
          "transformationRule": "capitalize_name",
          "defaultValue": "",
          "isRequired": false
        },
        "power": {
          "sourceField": "stats",
          "targetField": "PowerLevel",
          "dataType": "int",
          "transformationRule": "calculate_total_stats"
        },
        "image": {
          "sourceField": "sprites.front_default",
          "targetField": "ImageUrl",
          "dataType": "string",
          "defaultValue": ""
        },
        "legendary": {
          "sourceField": "species.is_legendary",
          "targetField": "IsLegendary",
          "dataType": "bool",
          "defaultValue": false
        }
      },
      "computedFields": [
        {
          "fieldName": "IsStarter",
          "computationRule": "check_starter_pokemon",
          "dataType": "bool",
          "dependentFields": ["id"]
        }
      ]
    },
    "region_mapping": {
      "targetType": "Region",
      "fields": {
        "id": {
          "sourceField": "id",
          "targetField": "Id",
          "dataType": "int"
        },
        "name": {
          "sourceField": "name",
          "targetField": "Name",
          "dataType": "string",
          "transformationRule": "capitalize_name"
        },
        "description": {
          "sourceField": "names[0].name",
          "targetField": "Description",
          "dataType": "string",
          "defaultValue": "No description available"
        },
        "pokemon_count": {
          "sourceField": "pokemon_entries",
          "targetField": "PokemonCount",
          "dataType": "int",
          "transformationRule": "array_count"
        }
      },
      "computedFields": []
    }
  }
}
```

### **4.2 Register Custom Transformation Functions**
In `FieldMappingConfig.cs`, add your transformations to `RegisterDefaultTransformations()`:

```csharp
private void RegisterDefaultTransformations()
{
    // Keep existing transformations or remove them...
    
    // Add your custom transformations
    _transformationFunctions["capitalize_name"] = value => 
    {
        var name = value?.ToString();
        return string.IsNullOrEmpty(name) ? name : 
               char.ToUpper(name[0]) + name.Substring(1).ToLower();
    };

    _transformationFunctions["calculate_total_stats"] = value =>
    {
        if (value is IEnumerable<object> stats)
        {
            int total = 0;
            foreach (dynamic stat in stats)
            {
                if (int.TryParse(stat?.base_stat?.ToString(), out int baseStat))
                    total += baseStat;
            }
            return total;
        }
        return 0;
    };

    // Array count (already exists, but shown for reference)
    _transformationFunctions["array_count"] = value =>
    {
        if (value is IEnumerable<object> enumerable)
            return enumerable.Count();
        return 0;
    };
}

private void RegisterDefaultComputations()
{
    // Add your computed field logic
    _computationFunctions["check_starter_pokemon"] = (dynamic entity) =>
    {
        var id = (int)(entity?.id ?? 0);
        // Starter Pokemon IDs (example logic)
        int[] starterIds = { 1, 4, 7, 25, 152, 155, 158 }; // Bulbasaur, Charmander, etc.
        return starterIds.Contains(id);
    };

    _computationFunctions["is_legendary_computed"] = (dynamic entity) =>
    {
        // More complex legendary check if needed
        var name = entity?.name?.ToString()?.ToLower() ?? "";
        string[] legendaryNames = { "mewtwo", "mew", "articuno", "zapdos", "moltres" };
        return legendaryNames.Any(legendary => name.Contains(legendary));
    };
}
```

---

## 🎨 **Step 5: Update GraphQL Query Types**

### **5.1 Update Query Class**
Replace the Query class in `Program.cs`:

```csharp
public class Query
{
    public async Task<PokemonCharacter?> GetPokemonAsync(int id, [Service] IYourApiService service)
        => await service.GetPokemonAsync(id);

    public async Task<IEnumerable<PokemonCharacter>> GetPokemonsAsync(string? name, [Service] IYourApiService service)
        => await service.GetPokemonsAsync(name);

    public async Task<Region?> GetRegionAsync(int id, [Service] IYourApiService service)
        => await service.GetRegionAsync(id);

    public async Task<IEnumerable<Ability>> GetAbilitiesAsync(string? name, [Service] IYourApiService service)
        => await service.GetAbilitiesAsync(name);
}
```

### **5.2 Update Service Registration**
In `Program.cs`:

```csharp
// Register your new service
builder.Services.AddScoped<IYourApiService, YourApiService>();

// Update GraphQL server configuration
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();
```

---

## 🧪 **Step 6: Testing Your Implementation**

### **6.1 Build and Run**
```bash
dotnet build
dotnet run
```

### **6.2 Test Your Endpoints**
```bash
# Test Pokemon query
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ getPokemon(id: 1) { displayName primaryType powerLevel isLegendary isStarter } }"}'

# Test Region query  
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ getRegion(id: 1) { name description pokemonCount } }"}'

# Check schema
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ __schema { types { name } } }"}'
```

---

## 📚 **Step 7: Advanced Customizations**

### **7.1 Add More Complex Transformations**
```csharp
// In FieldMappingConfig.cs
_transformationFunctions["extract_english_description"] = value =>
{
    if (value is IEnumerable<object> descriptions)
    {
        foreach (dynamic desc in descriptions)
        {
            if (desc?.language?.name?.ToString() == "en")
                return desc?.flavor_text?.ToString();
        }
    }
    return "No description available";
};
```

### **7.2 Handle Nested Object Mappings**
```json
{
  "abilities": {
    "sourceField": "abilities",
    "targetField": "Abilities",
    "dataType": "Ability[]",
    "transformationRule": "map_abilities_array"
  }
}
```

### **7.3 Add Validation Rules**
```json
{
  "name": {
    "sourceField": "name",
    "targetField": "DisplayName",
    "validationRule": "not_empty",
    "defaultValue": "Unknown Pokemon"
  }
}
```

---

## ✅ **Step 8: Final Checklist**

- [ ] External API endpoint configured
- [ ] Target schema models defined
- [ ] Field mappings configuration created
- [ ] Custom transformation functions registered
- [ ] Service implementation completed
- [ ] Query types updated
- [ ] Dependency injection configured
- [ ] Testing completed
- [ ] Documentation updated

---

## 🚀 **Your GraphQL Transformation Service is Ready!**

You now have a **fully configurable, schema-agnostic GraphQL transformation service** that can:

✅ **Call any external GraphQL API**  
✅ **Transform response schemas dynamically**  
✅ **Add computed fields with business logic**  
✅ **Handle complex data transformations**  
✅ **Scale to multiple API integrations**

The entire field mapping system is **configuration-driven** - you can modify schema transformations by simply editing `field-mappings.json` without touching any code! 🎉

---

## 📖 **Need Help?**

- Check `DYNAMIC_MAPPING_DEMO.md` for more examples
- Review the `FieldMappingConfig.cs` for advanced transformation options
- Test with the provided curl commands
- Start simple and gradually add complexity

**Happy coding!** 🚀

---

## 🔧 **Quick Reference**

### **Key Files:**
- `Program.cs` - Main application logic and schema definitions
- `FieldMappingConfig.cs` - Dynamic mapping engine
- `field-mappings.json` - Schema transformation configuration
- `DEVELOPER_GUIDE.md` - This documentation

### **One-Line Schema Transformation:**
```csharp
return _fieldMapper.MapEntity<YourTargetType>(sourceData, "your_mapping_key");
```

### **Configuration Pattern:**
```json
{
  "mappings": {
    "your_mapping_key": {
      "fields": { "source": "target" },
      "computedFields": [{ "fieldName": "computed", "computationRule": "rule" }]
    }
  }
}
```

That's it! You're ready to build your own GraphQL transformation service! 🎯
