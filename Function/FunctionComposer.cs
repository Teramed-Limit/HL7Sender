namespace HL7Sender.Function;

public static class FunctionalExtensions
{
    public static Func<T1, TResult> Pipe<T1, TIntermediate, TResult>(
        this Func<T1, TIntermediate> function1,
        Func<TIntermediate, TResult> function2)
    {
        return arg => function2(function1(arg));
    }
}

public static class FunctionComposer
{
    public static Func<string, string> ComposeFunctions(Dictionary<string, Func<string, string>> functions,
        List<string> functionNames)
    {
        Func<string, string> composedFunction = str => str;

        foreach (string functionName in functionNames)
        {
            if (functions.TryGetValue(functionName, out Func<string, string> function))
            {
                composedFunction = composedFunction.Pipe(function);
            }
            else
            {
                throw new ArgumentException($"Function {functionName} not found.");
            }
        }

        return composedFunction;
    }
}