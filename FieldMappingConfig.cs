using System.Text.Json;
using Newtonsoft.Json.Linq;

// ===== Dynamic Field Mapping Configuration =====

public class FieldMappingConfig
{
    public Dictionary<string, EntityMapping> Mappings { get; set; } = new();
    
    public static FieldMappingConfig LoadFromJson(string jsonContent)
    {
        return JsonSerializer.Deserialize<FieldMappingConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new FieldMappingConfig();
    }
}

public class EntityMapping
{
    public string TargetType { get; set; } = string.Empty;
    public Dictionary<string, FieldMapping> Fields { get; set; } = new();
    public List<ComputedField> ComputedFields { get; set; } = new();
}

public class FieldMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public string? TransformationRule { get; set; }
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? ValidationRule { get; set; }
}

public class ComputedField
{
    public string FieldName { get; set; } = string.Empty;
    public string ComputationRule { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public List<string> DependentFields { get; set; } = new();
}

// ===== Dynamic Field Mapper Service =====

public interface IFieldMapper
{
    T MapEntity<T>(dynamic sourceEntity, string mappingKey) where T : new();
    void LoadMappingConfiguration(string configPath);
    void RegisterTransformationFunction(string name, Func<object?, object?> function);
    void RegisterComputationFunction(string name, Func<dynamic, object?> function);
}

public class DynamicFieldMapper : IFieldMapper
{
    private FieldMappingConfig _config = new();
    private readonly Dictionary<string, Func<object?, object?>> _transformationFunctions = new();
    private readonly Dictionary<string, Func<dynamic, object?>> _computationFunctions = new();

    public DynamicFieldMapper()
    {
        RegisterDefaultTransformations();
        RegisterDefaultComputations();
    }

    public void LoadMappingConfiguration(string configPath)
    {
        var jsonContent = File.ReadAllText(configPath);
        _config = FieldMappingConfig.LoadFromJson(jsonContent);
    }

    public void RegisterTransformationFunction(string name, Func<object?, object?> function)
    {
        _transformationFunctions[name] = function;
    }

    public void RegisterComputationFunction(string name, Func<dynamic, object?> function)
    {
        _computationFunctions[name] = function;
    }

    public T MapEntity<T>(dynamic sourceEntity, string mappingKey) where T : new()
    {
        if (!_config.Mappings.TryGetValue(mappingKey, out var entityMapping))
        {
            throw new InvalidOperationException($"No mapping configuration found for key: {mappingKey}");
        }

        var targetEntity = new T();
        var targetType = typeof(T);

        // Map regular fields
        foreach (var fieldMapping in entityMapping.Fields.Values)
        {
            try
            {
                var sourceValue = GetNestedValue(sourceEntity, fieldMapping.SourceField);
                var transformedValue = ApplyTransformation(sourceValue, fieldMapping);
                
                var targetProperty = targetType.GetProperty(fieldMapping.TargetField);
                if (targetProperty != null && targetProperty.CanWrite)
                {
                    var convertedValue = ConvertValue(transformedValue, targetProperty.PropertyType);
                    targetProperty.SetValue(targetEntity, convertedValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error mapping field {fieldMapping.SourceField} -> {fieldMapping.TargetField}: {ex.Message}");
                
                // Apply default value if mapping fails and field is not required
                if (!fieldMapping.IsRequired && fieldMapping.DefaultValue != null)
                {
                    var targetProperty = targetType.GetProperty(fieldMapping.TargetField);
                    if (targetProperty != null && targetProperty.CanWrite)
                    {
                        var convertedDefault = ConvertValue(fieldMapping.DefaultValue, targetProperty.PropertyType);
                        targetProperty.SetValue(targetEntity, convertedDefault);
                    }
                }
            }
        }

        // Apply computed fields
        foreach (var computedField in entityMapping.ComputedFields)
        {
            try
            {
                if (_computationFunctions.TryGetValue(computedField.ComputationRule, out var computeFunc))
                {
                    var computedValue = computeFunc(sourceEntity);
                    var targetProperty = targetType.GetProperty(computedField.FieldName);
                    if (targetProperty != null && targetProperty.CanWrite)
                    {
                        var convertedValue = ConvertValue(computedValue, targetProperty.PropertyType);
                        targetProperty.SetValue(targetEntity, convertedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing field {computedField.FieldName}: {ex.Message}");
            }
        }

        return targetEntity;
    }

    private object? GetNestedValue(dynamic obj, string path)
    {
        var parts = path.Split('.');
        object? current = obj;
        
        foreach (var part in parts)
        {
            if (current == null) return null;
            current = GetPropertyValue(current, part);
        }
        
        return current;
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj == null) return null;
        
        // Handle Newtonsoft dynamic objects
        if (obj is Newtonsoft.Json.Linq.JObject jObj)
        {
            return jObj[propertyName]?.ToObject<object>();
        }
        
        // Handle dynamic expandos
        if (obj is IDictionary<string, object> dict)
        {
            return dict.TryGetValue(propertyName, out var value) ? value : null;
        }
        
        // Handle regular objects via reflection
        try
        {
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(obj);
            }
            
            // Try dynamic property access for dynamic objects
            if (obj is System.Dynamic.DynamicObject || obj.GetType().Name.Contains("Dynamic"))
            {
                dynamic dynObj = obj;
                try
                {
                    return ((IDictionary<string, object>)dynObj)[propertyName];
                }
                catch
                {
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing property {propertyName} on {obj.GetType()}: {ex.Message}");
        }
        
        return null;
    }

    private object? ApplyTransformation(object? value, FieldMapping mapping)
    {
        if (value == null) return mapping.DefaultValue;
        
        if (!string.IsNullOrEmpty(mapping.TransformationRule) && 
            _transformationFunctions.TryGetValue(mapping.TransformationRule, out var transformFunc))
        {
            return transformFunc(value);
        }
        
        return value;
    }

    private object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        
        var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        if (actualTargetType.IsEnum)
        {
            if (value is string stringValue)
            {
                return Enum.Parse(actualTargetType, stringValue, true);
            }
        }
        
        if (actualTargetType == typeof(DateTime) && value is string dateString)
        {
            return DateTime.TryParse(dateString, out var date) ? date : DateTime.MinValue;
        }
        
        return Convert.ChangeType(value, actualTargetType);
    }

    private void RegisterDefaultTransformations()
    {
        // Status transformations
        _transformationFunctions["status_to_lifestatus"] = value => value?.ToString()?.ToLower() switch
        {
            "alive" => LifeStatus.Alive,
            "dead" => LifeStatus.Dead,
            _ => LifeStatus.Unknown
        };

        // Gender transformations
        _transformationFunctions["gender_mapping"] = value => value?.ToString()?.ToLower() switch
        {
            "male" => Gender.Male,
            "female" => Gender.Female,
            "genderless" => Gender.Genderless,
            _ => Gender.Unknown
        };

        // String defaults
        _transformationFunctions["default_if_empty"] = value => 
            string.IsNullOrEmpty(value?.ToString()) ? "Standard" : value?.ToString();

        // Episode season extraction
        _transformationFunctions["extract_season"] = value =>
        {
            var episodeCode = value?.ToString();
            if (string.IsNullOrEmpty(episodeCode)) return "Unknown";
            
            if (episodeCode.StartsWith("S") && episodeCode.Contains("E"))
            {
                var parts = episodeCode.Split('E');
                var seasonPart = parts[0].Substring(1);
                return $"Season {int.Parse(seasonPart)}";
            }
            return "Unknown";
        };

        // Array count
        _transformationFunctions["array_count"] = value =>
        {
            if (value is IEnumerable<object> enumerable)
                return enumerable.Count();
            return 0;
        };
    }

    private void RegisterDefaultComputations()
    {
        // Check if character is main character
        _computationFunctions["is_main_character"] = (dynamic entity) =>
        {
            var name = entity?.name?.ToString() ?? "";
            return name.Contains("Rick") || name.Contains("Morty") || 
                   name.Contains("Summer") || name.Contains("Beth") || name.Contains("Jerry");
        };

        // Generate full display name
        _computationFunctions["generate_display_name"] = (dynamic entity) =>
        {
            var name = entity?.name?.ToString() ?? "";
            var species = entity?.species?.ToString() ?? "";
            return $"{name} ({species})";
        };

        // Calculate character importance score
        _computationFunctions["importance_score"] = (dynamic entity) =>
        {
            var episodeCount = entity?.episode?.Count ?? 0;
            var isMain = entity?.name?.ToString()?.Contains("Rick") == true || 
                        entity?.name?.ToString()?.Contains("Morty") == true;
            return isMain ? episodeCount * 2 : episodeCount;
        };
    }
}
