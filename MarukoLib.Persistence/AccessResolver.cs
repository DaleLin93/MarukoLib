namespace MarukoLib.Persistence
{

    public interface IAccessResolver
    {

        IValueAccessor Resolve(object obj);

    }

}
