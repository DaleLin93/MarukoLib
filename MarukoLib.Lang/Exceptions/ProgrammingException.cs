using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace MarukoLib.Lang.Exceptions
{

    public class ProgrammingException : Exception
    {

        public ProgrammingException(string message) : base(message) { }

        public ProgrammingException(string message, Exception innerException) : base(message, innerException) { }

        protected ProgrammingException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context) { }

    }

}
