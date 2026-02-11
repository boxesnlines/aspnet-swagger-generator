using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace BoxesNLines.SwaggerGenerator;

/// <summary>
/// Maps System.Type to OpenAPI schemas and registers complex types in the document's Components.Schemas.
/// </summary>
public sealed class OpenApiSchemaGenerator
{
    private readonly OpenApiDocument _document;
    private readonly IDictionary<string, IOpenApiSchema> _schemas;
    private readonly HashSet<string> _building = new(StringComparer.OrdinalIgnoreCase);

    public OpenApiSchemaGenerator(OpenApiDocument document)
    {
        _document = document;
        _document.Components ??= new OpenApiComponents();
        _document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        _schemas = _document.Components.Schemas;
    }

    /// <summary>
    /// Gets or creates an OpenAPI schema for the given type (for use in parameters, request body, or response).
    /// </summary>
    public IOpenApiSchema GetOrCreateSchema(Type type)
    {
        if (type == null!)
            return new OpenApiSchema { Type = JsonSchemaType.String };

        type = UnwrapNullable(type);
        type = UnwrapTask(type);

        if (TryCreatePrimitiveSchema(type, out OpenApiSchema primitive))
            return primitive;

        if (TryCreateArraySchema(type, out OpenApiSchema arraySchema))
            return arraySchema;

        if (type.IsEnum)
            return new OpenApiSchema { Type = JsonSchemaType.String };

        if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
        {
            string schemaId = GetSchemaId(type);
            if (_schemas.TryGetValue(schemaId, out _))
                return CreateSchemaRef(schemaId);

            if (_building.Contains(schemaId))
                return CreateSchemaRef(schemaId);

            _building.Add(schemaId);
            OpenApiSchema schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
            };

            HashSet<string> required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.GetMethod == null || !prop.GetMethod.IsPublic)
                    continue;
                Type propType = UnwrapNullable(prop.PropertyType);
                schema.Properties[prop.Name] = GetOrCreateSchema(propType);
                if (!IsNullable(prop.PropertyType))
                    required.Add(prop.Name);
            }

            if (required.Count > 0)
                schema.Required = required;

            _schemas[schemaId] = schema;
            _building.Remove(schemaId);
            return CreateSchemaRef(schemaId);
        }

        return new OpenApiSchema { Type = JsonSchemaType.Object };
    }

    private IOpenApiSchema CreateSchemaRef(string schemaId)
    {
        return new OpenApiSchemaReference(schemaId, _document, string.Empty);
    }

    /// <summary>
    /// Gets a schema for a method return type (unwraps Task/Task{T}, ActionResult{T}, etc.).
    /// </summary>
    public IOpenApiSchema? GetResponseSchema(Type returnType)
    {
        if (returnType == null!)
            return null;

        Type unwrapped = UnwrapTask(returnType);
        if (unwrapped == typeof(void))
            return null;

        if (unwrapped.IsGenericType && unwrapped.GetGenericTypeDefinition() == typeof(ActionResult<>))
            unwrapped = unwrapped.GetGenericArguments()[0];

        if (unwrapped == typeof(IActionResult) || unwrapped == typeof(ActionResult))
            return null;

        return GetOrCreateSchema(unwrapped);
    }

    private static Type UnwrapNullable(Type type)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        return underlying ?? type;
    }

    private static Type UnwrapTask(Type type)
    {
        if (type == typeof(Task))
            return typeof(void);
        if (type.IsGenericType)
        {
            Type def = type.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(ValueTask<>))
                return type.GetGenericArguments()[0];
        }
        return type;
    }

    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true;
        return Nullable.GetUnderlyingType(type) != null;
    }

    private static string GetSchemaId(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.Replace(".", "_", StringComparison.Ordinal).Replace("+", "_", StringComparison.Ordinal);
    }

    private bool TryCreatePrimitiveSchema(Type type, out OpenApiSchema schema)
    {
        schema = new OpenApiSchema();
        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTimeOffset))
        {
            schema.Type = JsonSchemaType.String;
            if (type == typeof(Guid)) schema.Format = "uuid";
            if (type == typeof(DateTimeOffset)) schema.Format = "date-time";
            return true;
        }
        if (type == typeof(bool)) { schema.Type = JsonSchemaType.Boolean; return true; }
        if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
        { schema.Type = JsonSchemaType.Integer; schema.Format = "int32"; return true; }
        if (type == typeof(long)) { schema.Type = JsonSchemaType.Integer; schema.Format = "int64"; return true; }
        if (type == typeof(float)) { schema.Type = JsonSchemaType.Number; schema.Format = "float"; return true; }
        if (type == typeof(double) || type == typeof(decimal)) { schema.Type = JsonSchemaType.Number; schema.Format = type == typeof(decimal) ? "decimal" : "double"; return true; }
        if (type == typeof(DateTime)) { schema.Type = JsonSchemaType.String; schema.Format = "date-time"; return true; }
        if (type == typeof(DateOnly)) { schema.Type = JsonSchemaType.String; schema.Format = "date"; return true; }
        if (type == typeof(TimeOnly)) { schema.Type = JsonSchemaType.String; schema.Format = "time"; return true; }
        return false;
    }

    private bool TryCreateArraySchema(Type type, out OpenApiSchema schema)
    {
        schema = new OpenApiSchema();
        Type? elementType = null;
        if (type.IsArray)
            elementType = type.GetElementType();
        else if (type.IsGenericType)
        {
            Type def = type.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(IList<>) || def == typeof(List<>) ||
                def == typeof(ICollection<>) || def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>))
                elementType = type.GetGenericArguments()[0];
        }
        if (elementType == null)
            return false;
        schema.Type = JsonSchemaType.Array;
        schema.Items = GetOrCreateSchema(elementType!);
        return true;
    }
}
