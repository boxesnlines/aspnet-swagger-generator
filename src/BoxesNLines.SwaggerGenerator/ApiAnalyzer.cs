using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace BoxesNLines.SwaggerGenerator;

/// <summary>
/// Analyzes an ASP.NET Web API project and returns a <see cref="WebApi"/> object containing OpenApi docs and Swagger JSON.
/// </summary>
public class ApiAnalyzer
{
    public ApiAnalyzer()
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
    }

    /// <summary>
    /// Analyze a project to retrieve OpenApi docs and Swagger JSON.
    /// </summary>
    /// <param name="projectPath">Path to an ASP.NET Web API project</param>
    /// <returns>WebApi object containing results of analysis.</returns>
    public WebApi AnalyzeProject(string projectPath)
    {
        string builtDllPath = BuildProjectAndGetDllPath(projectPath);
        Assembly apiAssembly = Assembly.LoadFrom(builtDllPath);
        OpenApiDocument openApiDocument = GetOpenApiFromAssembly(apiAssembly, null, "TestWebApi");

        return new(openApiDocument);
    }

    /// <summary>
    /// Build a project and return the path of the built DLL.
    /// Note that we build with the Debug profile for the purposes of this library.
    /// </summary>
    /// <param name="projectPath"></param>
    /// <returns></returns>
    private string BuildProjectAndGetDllPath(string projectPath)
    {
        ProjectCollection projectCollection = new ProjectCollection();

        Dictionary<string, string> globalProperties = new Dictionary<string, string>
        {
            ["Configuration"] = "Debug"
        };

        Project project = new Project(projectPath, globalProperties, null, projectCollection);

        string buildTargetPath = project.GetPropertyValue("TargetPath");
        string builtDllPath = Path.GetFullPath(Path.Combine(project.DirectoryPath, buildTargetPath));

        ProjectInstance projectInstance = project.CreateProjectInstance();
        BuildParameters buildParameters = new BuildParameters(projectCollection);
        BuildRequestData request = new BuildRequestData(projectInstance, ["Build"]);

        BuildResult _ = BuildManager.DefaultBuildManager.Build(buildParameters, request);

        return builtDllPath;
    }

    /// <summary>
    /// Analyze the built assembly and retrieve an <see cref="OpenApiDocument"/> containing the details of all API actions.
    /// </summary>
    /// <param name="apiAssembly"></param>
    /// <param name="openApiInfo"></param>
    /// <param name="documentName"></param>
    /// <param name="host"></param>
    /// <param name="basePath"></param>
    /// <returns></returns>
    private OpenApiDocument GetOpenApiFromAssembly(Assembly apiAssembly, OpenApiInfo? openApiInfo = null, string documentName = "v1", string? host = null, string? basePath = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers()
            .AddApplicationPart(apiAssembly);

        openApiInfo ??= new OpenApiInfo { Title = documentName ?? "API", Version = "1.0" };
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(documentName, openApiInfo);
        });
        
        WebApplication app = builder.Build();
        
        // Get OpenAPI document from Swashbuckle
        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        OpenApiDocument document = swaggerProvider.GetSwagger(documentName, host, basePath);
        
        return document;
    }
}