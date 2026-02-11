using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace BoxesNLines.SwaggerGenerator;

public class BnlActionDescriptor(ControllerActionDescriptor actionDescriptor)
{
    public ControllerActionDescriptor ActionDescriptor { get; } = actionDescriptor;
    public string RouteTemplate => ActionDescriptor.AttributeRouteInfo?.Template ?? $"{ActionDescriptor.ControllerName}/{ActionDescriptor.ActionName}";

    public List<string> HttpMethods
    {
        get
        {
            if (ActionDescriptor.ActionConstraints != null)
            {
                foreach (IActionConstraintMetadata constraint in ActionDescriptor.ActionConstraints)
                {
                    if (constraint is HttpMethodActionConstraint methodConstraint) return methodConstraint.HttpMethods.ToList();
                }
            }

            foreach (var metadata in ActionDescriptor.EndpointMetadata)
            {
                if (metadata is HttpMethodMetadata httpMethodMetadata) return httpMethodMetadata.HttpMethods.ToList();
            }

            return [];
        }
    }
}