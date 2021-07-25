using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.DotNet.Config
{
    public sealed class HostingListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly Action<IHost> _configure;
        private static readonly AsyncLocal<HostingListener> _currentListener = new();
        private readonly IDisposable _subscription0;
        private IDisposable _subscription1;
        public static void Initialize()
        {
            var listener = new HostingListener(host =>
            {
                var config = host.Services.GetRequiredService<IConfiguration>();
                Console.WriteLine(((IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>()).GetDebugView());
                Environment.Exit(0);
            });
            AppDomain.CurrentDomain.DomainUnload += (_, _) => listener.Dispose();
        }
        public HostingListener(Action<IHost> configure)
        {
            _configure = configure;
            _subscription0 = DiagnosticListener.AllListeners.Subscribe(this);
            _currentListener.Value = this;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener value)
        {
            // Ignore events that aren't for this listener
            if (_currentListener.Value == this)
            {
                if (value.Name == "Microsoft.Extensions.Hosting")
                {
                    _subscription1 = value.Subscribe(this);
                }
            }
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (value.Key == "HostBuilt")
            {
                _configure?.Invoke((IHost)value.Value);
            }
        }

        public void Dispose()
        {
            // Undo this here just in case the code unwinds synchronously since that doesn't revert
            // the execution context to the original state. Only async methods do that on exit.
            _currentListener.Value = null;

            _subscription0.Dispose();
            _subscription1?.Dispose();
        }
    }

    public static class ConfigurationRootExtensions
    {
        public static string GetDebugView(this IConfigurationRoot root)
        {
            void RecurseChildren(
                StringBuilder stringBuilder,
                IEnumerable<IConfigurationSection> children,
                string indent)
            {
                foreach (IConfigurationSection child in children)
                {
                    (string Value, IConfigurationProvider Provider) valueAndProvider = GetValueAndProvider(root, child.Path);

                    if (valueAndProvider.Provider != null && valueAndProvider.Provider.GetType() != typeof(EnvironmentVariablesConfigurationProvider) )
                    {
                        stringBuilder
                            .Append(indent)
                            .Append(child.Key)
                            .Append('=')
                            .Append(valueAndProvider.Value)
                            .Append(" (")
                            .Append(valueAndProvider.Provider)
                            .AppendLine(")");
                    }
                    else
                    {
                        stringBuilder
                            .Append(indent)
                            .Append(child.Key)
                            .AppendLine(":");
                    }

                    RecurseChildren(stringBuilder, child.GetChildren(), indent + "  ");
                }
            }

            var builder = new StringBuilder();

            RecurseChildren(builder, root.GetChildren(), "");

            return builder.ToString();
        }

        private static (string Value, IConfigurationProvider Provider) GetValueAndProvider(
            IConfigurationRoot root,
            string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out string value))
                {
                    return (value, provider);
                }
            }

            return (null, null);
        }
    }
}
