using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi;

namespace BoxesNLines.SwaggerGenerator;

public record WebApi(ActionDescriptorCollection ActionDescriptorCollection, OpenApiDocument OpenApiDocument)
{
    public string Swagger
    {
        get
        {
            return OpenApiDocument.SerializeAsync(OpenApiSpecVersion.OpenApi3_0, OpenApiConstants.Json).GetAwaiter().GetResult();
        }
    }
}