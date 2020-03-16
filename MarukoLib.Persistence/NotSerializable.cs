using System;

namespace MarukoLib.Persistence
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class NotSerializableAttribute : Attribute { }

}
