using System;
using RT.Util;

namespace KtaneWeb
{
    sealed class CacheEntry<T>
    {
        public T Value;
        public DateTime Expires;

        public CacheEntry(T value)
        {
            Value = value;
            Expires = DateTime.UtcNow + TimeSpan.FromHours(1 + Rnd.NextDouble());
        }
    }
}
