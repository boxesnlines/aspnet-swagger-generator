using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BoxesNLines.SwaggerGenerator;

public class ApiAnalyzer
{
    public ApiAnalyzer()
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
    }

    public WebApi AnalyzeProject(string projectPath)
    {
        string builtDllPath = BuildProjectAndGetDllPath(projectPath);
        Assembly apiAssembly = Assembly.LoadFrom(builtDllPath);
        ActionDescriptorCollection actions = GetEndpointsFromAssembly(apiAssembly);

        OpenApiDocument openApiDocument = GetOpenApiFromActionDescriptors(actions, Path.GetFileNameWithoutExtension(builtDllPath));

        return new(actions, openApiDocument);
    }

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

    private ActionDescriptorCollection GetEndpointsFromAssembly(Assembly apiAssembly)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers()
            .AddApplicationPart(apiAssembly);

        WebApplication app = builder.Build();

        IActionDescriptorCollectionProvider provider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();

        return provider.ActionDescriptors;
    }

    private OpenApiDocument GetOpenApiFromActionDescriptors(ActionDescriptorCollection actionDescriptorCollection, string title)
    {
        OpenApiDocumentBuilder builder = new OpenApiDocumentBuilder();
        OpenApiDocument document = builder.Build(actionDescriptorCollection.Items, new OpenApiInfo
        {
            Title = title,
            Version = "1.0"
        });
        return document;
    }
}