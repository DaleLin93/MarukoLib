using System.Windows;

namespace MarukoLib.Persistence
{

    public interface IAccessResolver
    {

        IValueAccessor Resolve(object obj);

    }

}
