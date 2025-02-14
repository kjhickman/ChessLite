using System.Numerics;
using System.Runtime.InteropServices;

namespace Zugfish.Engine.Extensions;

public static class DictionaryExtensions
{
    public static void IncrementOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
        where TValue : INumber<TValue>

    {
        ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (exists)
        {
            val!++;
        }

        val = TValue.One;
    }
}
