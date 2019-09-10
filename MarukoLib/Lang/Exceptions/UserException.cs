using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace MarukoLib.Lang.Exceptions
{

    public class UserException : Exception
    {

        public UserException(string message) : base(message) { }

        public UserException(string message, Exception innerException) : base(message, innerException) { }

        protected UserException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context) { }

    }

}
