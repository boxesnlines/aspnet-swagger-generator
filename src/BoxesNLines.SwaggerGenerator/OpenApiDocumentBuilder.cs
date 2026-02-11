using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;

namespace BoxesNLines.SwaggerGenerator;

public class OpenApiDocumentBuilder
{
    public OpenApiDocument Build(IReadOnlyList<ActionDescriptor> actions, OpenApiInfo? openApiInfo = null)
    {
        // Set up the initial document
        openApiInfo ??= new OpenApiInfo { Title = "API", Version = "1.0" };
        OpenApiDocument document = new OpenApiDocument
        {
            Info = openApiInfo,
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents { Schemas = new Dictionary<string, IOpenApiSchema>() }
        };

        // Prepare the schema generator (handles the complex process of generating the correct schema)
        OpenApiSchemaGenerator schemaGenerator = new(document);

        // Loop through all actions and assemble the document
        foreach (ActionDescriptor action in actions)
        {
            if (action is ControllerActionDescriptor controllerActionDescriptor)
            {
                string controllerName = controllerActionDescriptor.ControllerName;
                string actionName = controllerActionDescriptor.ActionName;
                string? routeTemplate = controllerActionDescriptor.AttributeRouteInfo?.Template;
                Type? returnType = controllerActionDescriptor.MethodInfo?.ReturnType;

                string path = "/";
                if (routeTemplate != null) // Use route template if we have it (currently the default and most common)
                {
                    path = routeTemplate.Replace("[controller]", controllerName).Replace("[action]", actionName);
                }
                else // No route template so use controller and action name to determin the path
                {
                    path = $"/{controllerName}/{actionName}";
                }

                IReadOnlySet<string> routeParameters = GetRouteParameterNames(path); // Parameters can appear within the route, so extract those

                // Extract the HTTP methods used by the action
                IReadOnlyList<string> httpMethods = GetHttpMethods(action);

                foreach (string method in httpMethods)
                {
                    HttpMethod httpMethod = new HttpMethod(method);

                    // Add path to document
                    if (!document.Paths.TryGetValue(path, out IOpenApiPathItem? pathItem))
                    {
                        pathItem = new OpenApiPathItem();
                        document.Paths[path] = pathItem;
                    }

                    IOpenApiSchema? responseSchema = returnType != null ? schemaGenerator.GetResponseSchema(returnType) : null;
                    OpenApiResponse okResponse = new OpenApiResponse
                    {
                        Description = "Success",
                        Content = new Dictionary<string, IOpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType { Schema = responseSchema }
                        }
                    };

                    OpenApiOperation operation = new OpenApiOperation
                    {
                        OperationId = $"{controllerName}_{actionName}",
                        Responses = new OpenApiResponses { ["200"] = okResponse }
                    };


                    // Handle path/querystring parameters
                    List<IOpenApiParameter> parameters = new List<IOpenApiParameter>();
                    foreach (ParameterDescriptor param in action.Parameters)
                    {
                        if (param is ControllerParameterDescriptor controllerParameterDescriptor)
                        {
                            BnlParameterInfo bnlParameterInfo = controllerParameterDescriptor.ParameterInfo;

                            ParameterLocation? paramLocation = null; // Default to null (request body)
                            if (!bnlParameterInfo.HasAttribute("FromBodyAttribute"))
                            {
                                if (bnlParameterInfo.HasAttribute("FromRouteAttribute") || (bnlParameterInfo.ParameterInfo.Name != null && routeParameters.Contains(bnlParameterInfo.ParameterInfo.Name))) paramLocation = ParameterLocation.Path;
                                else if (bnlParameterInfo.HasAttribute("FromQueryAttribute")) paramLocation = ParameterLocation.Query;
                                else if (bnlParameterInfo.HasAttribute("FromHeaderAttribute")) paramLocation = ParameterLocation.Header;
                                else if (!bnlParameterInfo.ParameterInfo.ParameterType.IsClass || bnlParameterInfo.ParameterInfo.ParameterType == typeof(string) || httpMethod == HttpMethod.Get) paramLocation = ParameterLocation.Query;
                            }

                            if (paramLocation != null) // May be null for body parameters
                            {
                                parameters.Add(new OpenApiParameter
                                {
                                    Name = bnlParameterInfo.ParameterInfo.Name ?? "param",
                                    In = paramLocation.Value,
                                    Required = paramLocation == ParameterLocation.Path || !bnlParameterInfo.IsNullable(),
                                    Schema = schemaGenerator.GetOrCreateSchema(bnlParameterInfo.ParameterInfo.ParameterType),
                                });
                            }
                        }
                    }

                    // Handle body parameters
                    // Note: It's technically possible to pass a body with other verbs, but you're not supposed to so we won't support it
                    // If you did that you knew what you were doing.
                    if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put || httpMethod == HttpMethod.Put)
                    {
                        foreach (ParameterDescriptor param in action.Parameters)
                        {
                            if (param is ControllerParameterDescriptor controllerParameterDescriptor)
                            {
                                BnlParameterInfo bnlParameterInfo = controllerParameterDescriptor.ParameterInfo;
                                if (bnlParameterInfo.IsBodyParameter())
                                {
                                    operation.RequestBody = new OpenApiRequestBody
                                    {
                                        Content = new Dictionary<string, IOpenApiMediaType>
                                        {
                                            ["application/json"] = new OpenApiMediaType
                                            {
                                                Schema = schemaGenerator.GetOrCreateSchema(bnlParameterInfo.ParameterInfo.ParameterType)
                                            }
                                        }
                                    };
                                }
                            }
                        }
                    }
                    
                    // Set HTTP method if any operations exist
                    if (pathItem.Operations != null) pathItem.Operations[httpMethod] = operation;
                }
            }
        }

        return document;
    }

    public static IReadOnlySet<string> GetRouteParameterNames(string path)
    {
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int i = 0;
        while (i < path.Length)
        {
            int start = path.IndexOf('{', i);
            if (start < 0) break;
            int end = path.IndexOf('}', start + 1);
            if (end < 0) break;
            string name = path.Substring(start + 1, end - start - 1).Trim();
            if (name.Length > 0)
                set.Add(name);
            i = end + 1;
        }

        return set;
    }

    public static IReadOnlyList<string> GetHttpMethods(ActionDescriptor action)
    {
        if (action.ActionConstraints != null)
        {
            foreach (IActionConstraintMetadata constraint in action.ActionConstraints)
            {
                if (constraint is HttpMethodActionConstraint methodConstraint)
                    return methodConstraint.HttpMethods.ToList();
            }
        }

        foreach (object metadata in action.EndpointMetadata)
        {
            if (metadata is HttpMethodMetadata httpMethodMetadata) return httpMethodMetadata.HttpMethods.ToList();
        }

        return [];
    }
}