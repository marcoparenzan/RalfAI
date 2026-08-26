using System.Reflection;

namespace RalfAI;

public static class AppVersion
{
    public static string Current => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
}
