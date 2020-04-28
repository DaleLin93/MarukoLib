using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using log4net;

namespace MarukoLib.Logging
{

    public class Logger
    {

        private sealed class NamedProvider
        {

            [NotNull] public readonly string Name;

            [NotNull] public readonly Delegate Provider;

            public NamedProvider([NotNull] string name, [NotNull] Delegate provider)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            }

            public object Value => Provider.DynamicInvoke();

        }

        [NotNull] public readonly ILog Log;

        [CanBeNull] private readonly IList<NamedProvider> _providers;

        public Logger([NotNull] Type type, params object[] parameters) 
            : this(LogManager.GetLogger(type ?? throw new ArgumentNullException(nameof(type))), parameters) { }

        public Logger([NotNull] ILog log, params object[] parameters)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            _providers = CreateProviders(parameters);
        }

        private static IList<NamedProvider> CreateProviders(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            var list = new List<NamedProvider>(parameters.Length / 2);
            for (var i = 0; i < parameters.Length / 2; i++)
            {
                var name = (string) parameters[i * 2];
                var provider = (Delegate) parameters[i * 2 + 1];
                list.Add(new NamedProvider(name, provider));
            }
            return list;
        }

        public static Logger GetLogger(Type type) => new Logger(type);

        public static string BuildMessage(string method, params object[] parameters) => BuildMessage(method, null, parameters);

        private static string BuildMessage(string method, IList<NamedProvider> providers, params object[] parameters)
        {
            var stringBuilder = new StringBuilder(128);
            stringBuilder.Append(method).Append(": ");
            var count = 0;
            if (providers != null)
                foreach (var provider in providers)
                    stringBuilder.Append(count++ == 0 ? " " : ", ").Append(provider.Name).Append(":").Append(provider.Value);
            if (parameters != null)
                for (var i = 0; i < parameters.Length; i++)
                    stringBuilder.Append(i % 2 == 0 ? (count++ == 0 ? " " : ", ") : ":").Append(parameters[i]);
            return stringBuilder.ToString();
        }

        public void Debug(string method, params object[] parameters)
        {
            if (Log.IsDebugEnabled)
                Log.Debug(BuildMessage(BuildMessage(method, _providers, parameters)));
        }

        public void Debug(string method, Exception e, params object[] parameters)
        {
            if (Log.IsDebugEnabled)
                Log.Debug(BuildMessage(BuildMessage(method, _providers, parameters)), e);
        }

        public void Info(string method, params object[] parameters)
        {
            if (Log.IsInfoEnabled)
                Log.Info(BuildMessage(method, _providers, parameters));
        }

        public void Info(string method, Exception e, params object[] parameters)
        {
            if (Log.IsInfoEnabled)
                Log.Info(BuildMessage(BuildMessage(method, _providers, parameters)), e);
        }

        public void Warn(string method, params object[] parameters)
        {
            if (Log.IsWarnEnabled)
                Log.Warn(BuildMessage(method, _providers, parameters));
        }

        public void Warn(string method, Exception e, params object[] parameters)
        {
            if (Log.IsWarnEnabled)
                Log.Warn(BuildMessage(BuildMessage(method, _providers, parameters)), e);
        }

        public void Error(string method, params object[] parameters)
        {
            if (Log.IsErrorEnabled)
                Log.Error(BuildMessage(method, _providers, parameters));
        }

        public void Error(string method, Exception e, params object[] parameters)
        {
            if (Log.IsErrorEnabled)
                Log.Error(BuildMessage(BuildMessage(method, _providers, parameters)), e);
        }

        public void Fatal(string method, params object[] parameters)
        {
            if (Log.IsFatalEnabled)
                Log.Fatal(BuildMessage(method, _providers, parameters));
        }

        public void Fatal(string method, Exception e, params object[] parameters)
        {
            if (Log.IsFatalEnabled)
                Log.Fatal(BuildMessage(BuildMessage(method, _providers, parameters)), e);
        }

    }

}
