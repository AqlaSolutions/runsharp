namespace TriAxis.RunSharp
{
    public delegate void RunSharpAction();
    public delegate void RunSharpAction<T>(T arg);
    public delegate T RunSharpFunc<out T>();
    public delegate T RunSharpFunc<in T1, out T>(T1 arg);
}