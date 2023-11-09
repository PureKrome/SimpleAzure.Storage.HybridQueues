namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

internal static class Helpers
{
    // REF: https://stackoverflow.com/a/38998370/30674
    /// <summary>
    /// This checks to see if the Type is a Simple Type (Primitive | string | decimal).
    /// </summary>
    /// <param name="type">The Type to check.</param>
    /// <remarks>To see the full list of .NET Primitive types: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table</remarks>
    /// <returns>True if this is a Simple Type or False if it is not.</returns>
    internal static bool IsASimpleType(this Type type) =>
        type.IsPrimitive ||
        type == typeof(string) ||
        type == typeof(decimal);
}
