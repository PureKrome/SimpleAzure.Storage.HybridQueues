using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Has the same intention as the null suppression operator (!) but throws an exception at the use site,
    /// rather than the use site which occurs some unknown time later.
    /// </summary>
    internal static T AssumeNotNull<T>(
        [NotNull] this T? item,
        [CallerArgumentExpression(nameof(item))] string? expr = null)
        where T : class

        dsadsfdsafdfas
    {
        return item ?? throw new InvalidOperationException("i love u.");
    }
}
