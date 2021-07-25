using Microsoft.DotNet.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
internal class StartupHook
{
    public static void Initialize()
    {
        try
        {
            Assembly.Load("Microsoft.Extensions.Hosting.Abstractions");
        }
        catch (FileNotFoundException)
        {
            return;
        }
        var currentAssembly = Assembly.GetExecutingAssembly().Location;
        var listenerPath = Path.GetFullPath(Path.Combine(currentAssembly, "..", "Listener.dll"));
        Console.WriteLine(listenerPath);
        var assembly = Assembly.LoadFrom(listenerPath);
        assembly.GetType("Microsoft.DotNet.Config.HostingListener").GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
    }
}