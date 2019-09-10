using System;
using System.Text;
using log4net;

namespace MarukoLib.Logging
{

    public class Logger
    {

        private readonly ILog _log;

        public Logger(Type type) => _log = LogManager.GetLogger(type);

        public static Logger GetLogger(Type type) => new Logger(type);

        public static string BuildMessage(string method, params object[] parameters)
        {
            var stringBuilder = new StringBuilder(128);
            stringBuilder.Append(method).Append(": ");
            if (parameters != null)
                for (var i = 0; i < parameters.Length; i++)
                    stringBuilder.Append(i % 2 == 0 ? (i == 0 ? " " : ", ") : ":").Append(parameters[i]);
            return stringBuilder.ToString();
        }

        public void Debug(string method, params object[] parameters) => _log.Debug(BuildMessage(BuildMessage(method, parameters)));

        public void Debug(string method, Exception e, params object[] parameters) => _log.Debug(BuildMessage(BuildMessage(method, parameters)), e);

        public void Info(string method, params object[] parameters) => _log.Info(BuildMessage(method, parameters));

        public void Info(string method, Exception e, params object[] parameters) => _log.Info(BuildMessage(BuildMessage(method, parameters)), e);

        public void Warn(string method, params object[] parameters) => _log.Warn(BuildMessage(method, parameters));

        public void Warn(string method, Exception e, params object[] parameters) => _log.Warn(BuildMessage(BuildMessage(method, parameters)), e);

        public void Error(string method, params object[] parameters) => _log.Error(BuildMessage(method, parameters));

        public void Error(string method, Exception e, params object[] parameters) => _log.Error(BuildMessage(BuildMessage(method, parameters)), e);

        public void Fatal(string method, params object[] parameters) => _log.Fatal(BuildMessage(method, parameters));

        public void Fatal(string method, Exception e, params object[] parameters) => _log.Fatal(BuildMessage(BuildMessage(method, parameters)), e);

    }

}
