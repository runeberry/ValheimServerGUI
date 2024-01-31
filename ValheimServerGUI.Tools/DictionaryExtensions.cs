using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Tools
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns a new dictionary where values become keys and vice-versa.
        /// Probably throws an exception if any values are duplicated (i.e. would create a duplicate key).
        /// </summary>
        public static Dictionary<TValue, TKey> Invert<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }
    }
}
