using System.Reflection;
using System.Security.Claims;

namespace Microsoft.Azure.Functions.Worker;

public static class FunctionContextExtensions
{
    internal static MethodInfo GetTargetFunctionMethod(this FunctionContext context)
    {
        var entryPoint = context.FunctionDefinition.EntryPoint;

        var assemblyPath = context.FunctionDefinition.PathToAssembly;
        var assembly = Assembly.LoadFrom(assemblyPath);
        var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
        var type = assembly.GetType(typeName);
        var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
        var method = type.GetMethod(methodName);
        return method;
    }

    public static ClaimsPrincipal GetUser(this FunctionContext context)
    {
        if (context?.Items["User"] is not null)
            return (context?.Items["User"]! as ClaimsPrincipal)!;
        else
            return new ClaimsPrincipal();
    }
}