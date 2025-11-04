using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdisBackupCompare
{
    public static class Extensions
    {
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> dictionaryToAdd)
        {
            foreach (var kvp in dictionaryToAdd)
            {
                if (!source.ContainsKey(kvp.Key))
                    source.Add(kvp.Key, kvp.Value);
            }
        }

    }
}
