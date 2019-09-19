namespace MarukoLib.Lang
{

    public delegate void Consumer<in TIn>(TIn t);

    public delegate TOut Supplier<out TOut>();

    public delegate TOut Function<in TIn, out TOut>(TIn t);

    public delegate TOut BiFunction<in TIn1, in TIn2, out TOut>(TIn1 t1, TIn2 t2);

    public delegate bool BiPredicate<in TIn1, in TIn2>(TIn1 t1, TIn2 t2);

    public delegate T UnaryOperator<T>(T input);

    public delegate T BinaryOperator<T>(T t1, T t2);

}
