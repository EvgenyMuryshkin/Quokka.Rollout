using System;
using System.Diagnostics;

namespace Quokka.Rollout
{
    public class NugetClient
    {
        public static void Push(string packageLocation, string APIKey)
        {
            var proc = Process.Start(new ProcessStartInfo()
            {
                FileName = @"dotnet",
                Arguments = $"nuget push \"{packageLocation}\" --api-key {APIKey} --source https://api.nuget.org/v3/index.json",
                UseShellExecute = false
            });

            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception($"Build failed");
        }
    }
}
