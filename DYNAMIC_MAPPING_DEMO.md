# 🚀 Dynamic Field Mapping System

## ✨ **What Changed: From Hardcoded to Dynamic**

### **BEFORE (Hardcoded):**
```csharp
private Person MapCharacterToPerson(dynamic character)
{
    return new Person
    {
        Id = (int)character.id,                    // ❌ Hardcoded
        FullName = (string)character.name,         // ❌ Hardcoded  
        LifeStatus = MapLifeStatus((string)character.status), // ❌ Hardcoded
        // ... 100+ lines of hardcoded mapping logic
    };
}
```

### **AFTER (Dynamic & Configurable):**
```csharp
// 🎉 ONE line does it all!
return _fieldMapper.MapEntity<Person>(character, "character_to_person");
```

## 🏗️ **Dynamic Mapping Architecture**

### **1. Configuration-Driven Mapping (`field-mappings.json`)**
```json
{
  "mappings": {
    "character_to_person": {
      "targetType": "Person",
      "fields": {
        "name": {
          "sourceField": "name",
          "targetField": "FullName",
          "dataType": "string",
          "transformationRule": "default_if_empty"
        },
        "status": {
          "sourceField": "status", 
          "targetField": "LifeStatus",
          "transformationRule": "status_to_lifestatus"
        }
      },
      "computedFields": [
        {
          "fieldName": "IsMainCharacter",
          "computationRule": "is_main_character"
        }
      ]
    }
  }
}
```

### **2. Transformation Functions Registry**
```csharp
// ✅ Extensible transformation rules
_transformationFunctions["status_to_lifestatus"] = value => value?.ToString()?.ToLower() switch
{
    "alive" => LifeStatus.Alive,
    "dead" => LifeStatus.Dead,
    _ => LifeStatus.Unknown
};

_transformationFunctions["extract_season"] = value =>
{
    var episodeCode = value?.ToString();
    return episodeCode?.StartsWith("S") ? $"Season {episodeCode.Substring(1, 2)}" : "Unknown";
};
```

### **3. Computed Fields Engine**
```csharp
// ✅ Business logic as reusable functions
_computationFunctions["is_main_character"] = (dynamic entity) =>
{
    var name = entity?.name?.ToString() ?? "";
    return name.Contains("Rick") || name.Contains("Morty");
};
```

## 🎯 **Schema Transformation Examples**

### **Character → Person Mapping**
| Rick & Morty API | Our API | Transformation |
|------------------|---------|----------------|
| `name` | `fullName` | Direct mapping |
| `status` | `lifeStatus` | Enum conversion |
| `species` | `race` | Field rename |
| `type` | `personalityType` | Default "Standard" if empty |
| `origin.name` | `homeWorld.title` | Nested object mapping |
| **N/A** | `isMainCharacter` | **🧠 COMPUTED FIELD** |

### **Location → World Mapping**
| Rick & Morty API | Our API | Transformation |
|------------------|---------|----------------|
| `name` | `title` | Field rename |
| `type` | `classification` | Field rename |
| `dimension` | `reality` | Field rename |
| `residents.length` | `inhabitantCount` | **🔢 Array count transformation** |

### **Episode → StoryArc Mapping**
| Rick & Morty API | Our API | Transformation |
|------------------|---------|----------------|
| `name` | `title` | Direct mapping |
| `air_date` | `releaseDate` | DateTime parsing |
| `episode` ("S01E01") | `season` ("Season 1") | **📅 Regex extraction** |
| `characters.length` | `characterCount` | Array count |

## 🛠️ **How to Modify Mappings (NO CODE CHANGES!)**

### **1. Add New Field Mapping**
Edit `field-mappings.json`:
```json
{
  "newField": {
    "sourceField": "external_api_field",
    "targetField": "MyNewProperty", 
    "transformationRule": "my_custom_transform"
  }
}
```

### **2. Register Custom Transformation**
```csharp
mapper.RegisterTransformationFunction("my_custom_transform", value => 
    value?.ToString()?.ToUpper() ?? "DEFAULT");
```

### **3. Add Computed Field**
```json
{
  "computedFields": [
    {
      "fieldName": "MyComputedField",
      "computationRule": "calculate_score",
      "dependentFields": ["episodes", "status"]
    }
  ]
}
```

## 🚀 **Live Demo Commands**

```bash
# Test Person mapping (Character → Person)
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ person(id: 1) { fullName lifeStatus race isMainCharacter } }"}'

# Test World mapping (Location → World)  
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ world(id: 1) { title classification reality inhabitantCount } }"}'

# Test StoryArc mapping (Episode → StoryArc)
curl -X POST http://localhost:5284/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ storyArcs { title season episodeCode characterCount } }"}'
```

## ✅ **Benefits of Dynamic Mapping**

1. **🔧 Configuration-Driven**: Change mappings without recompiling
2. **🔄 Reusable**: Same transformation functions across different entities  
3. **🧠 Smart**: Computed fields with business logic
4. **📊 Type-Safe**: Strong typing with automatic conversion
5. **🛡️ Resilient**: Error handling with default values
6. **🚀 Extensible**: Easy to add new transformations
7. **🎯 Maintainable**: Clear separation of mapping logic

## 🔥 **Advanced Features**

- **Nested Object Mapping**: `origin.name` → `homeWorld.title`
- **Array Transformations**: `residents.length` → `inhabitantCount`
- **Conditional Logic**: Default values when fields are missing
- **Custom Validation**: Validation rules per field
- **Multiple Data Types**: String, DateTime, Enum, Int, Bool support
- **Dynamic Property Access**: Works with any dynamic object structure

This system transforms your GraphQL service from **rigid hardcoded mappings** to a **flexible, configuration-driven architecture** that can adapt to schema changes without code deployment! 🎉
