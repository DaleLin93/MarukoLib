namespace MarukoLib.Lang.Filters
{

    public interface IFilter<T>
    {

        T Apply(T input);

    }

}
