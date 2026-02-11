using Microsoft.OpenApi;

namespace BoxesNLines.SwaggerGenerator;

/// <summary>
/// Object representing an ASP.NET web API.
/// This is a metadata object that provides OpenAPI and Swagger JSON data.
/// </summary>
/// <param name="OpenApiDocument">Object representing an OpenAPI document that mirrors the API's actions.</param>
public record WebApi(OpenApiDocument OpenApiDocument)
{
    /// <summary>
    /// Swagger JSON generated from the <see cref="OpenApiDocument"/>.
    /// Note that this is a simple convenience method and will have the same results as directly serializing the OpenAPI document.
    /// </summary>
    public string Swagger => OpenApiDocument.SerializeAsync(OpenApiSpecVersion.OpenApi3_0, OpenApiConstants.Json).GetAwaiter().GetResult();
}