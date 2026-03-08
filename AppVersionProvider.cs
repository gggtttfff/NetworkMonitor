using System.Reflection;
using System.Windows.Forms;

namespace NetworkMonitor
{
    internal static class AppVersionProvider
    {
        public static string GetDisplayVersion()
        {
            string? informationalVersion = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                int buildMetadataIndex = informationalVersion.IndexOf('+');
                return buildMetadataIndex >= 0
                    ? informationalVersion[..buildMetadataIndex]
                    : informationalVersion;
            }

            return Application.ProductVersion;
        }
    }
}
