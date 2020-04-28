using System;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Windows
{

    public class ContextChangedEventArgs : EventArgs
    {

        public ContextChangedEventArgs(IReadonlyContext context) => Context = context;

        public IReadonlyContext Context { get; }

    }

}