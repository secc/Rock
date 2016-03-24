using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Rock.Web.Cache
{
    interface IRockCacheProvider: IEnumerable<KeyValuePair<string, object>>, IDisposable
    {
        object Get( string key, string regionName = null );

        bool Contains( string key, string regionName = null );

        object Remove( string key, string regionName = null );

        void Set( CacheItem item, CacheItemPolicy policy );

        void Set( string key, object value, CacheItemPolicy policy, string regionName = null );

        void Set( string key, object value, DateTimeOffset absoluteExpiration, string regionName = null );

        object this[string key] { get; set; }

        //bool Add( CacheItem item, CacheItemPolicy policy );
    }
}
