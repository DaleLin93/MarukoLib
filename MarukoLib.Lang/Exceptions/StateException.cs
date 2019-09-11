using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace MarukoLib.Lang.Exceptions
{
    public class StateException : Exception
    {
        public StateException() { }

        public StateException(string message) : base(message) { }

        public StateException(string message, Exception innerException) : base(message, innerException) { }

        protected StateException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
