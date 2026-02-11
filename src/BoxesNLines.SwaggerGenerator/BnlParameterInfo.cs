using System.Reflection;

namespace BoxesNLines.SwaggerGenerator;

public class BnlParameterInfo(ParameterInfo? parameterInfo)
{
    public ParameterInfo ParameterInfo { get; } = parameterInfo;

    public bool HasAttribute(string attributeName)
    {
        foreach (Attribute attribute in ParameterInfo.GetCustomAttributes())
        {
            if (attribute.GetType().Name.Equals(attributeName, StringComparison.Ordinal)) return true;
        }
        return false;
    }
    
    public bool IsNullable()
    {
        if (ParameterInfo.ParameterType.IsValueType)
        {
            return false;
        }
        else return Nullable.GetUnderlyingType(ParameterInfo.ParameterType) != null;
    }

    public bool IsBodyParameter()
    {
        if (HasAttribute("FromBodyAttribute")) return true;
        // Body parameters can also be implicit if they are complex types that aren't explicitly noted otherwise
        else if (!HasAttribute("FromRouteAttribute")
                 && !HasAttribute("FromHeaderAttribute")
                 && !HasAttribute("FromQueryAttribute")
                 && ParameterInfo.ParameterType.IsClass
                 && ParameterInfo.ParameterType != typeof(string))
        {
            return true;
        }
        return false;
    }
    
    public static implicit operator BnlParameterInfo(ParameterInfo parameterInfo)
    {
        return new BnlParameterInfo(parameterInfo);
    }
}