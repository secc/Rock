using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Rock.Web.Cache
{
    public class RockCache : IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {
        // object used for locking
        private static object s_initLock = new object();

        // singleton instance of RockMemoryCache
        private static RockCache s_defaultCache;

        // static instance of the cache provider
        private static IRockCacheProvider s_cacheProvider;

        // variable to determine if caching is enabled
        private bool _isCachingDisabled = false;

        /// <summary>
        /// Gets the default.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        public static RockCache Instance
        {
            get
            {
                if ( s_defaultCache == null )
                {
                    lock (s_initLock )
                    {
                        if ( s_defaultCache == null )
                        {
                            s_defaultCache = new RockCache();
                        }
                    }
                }

                return s_defaultCache;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RockCache"/> class.
        /// </summary>
        public RockCache()
        {
            // Check to see if caching has been disabled
            _isCachingDisabled = ConfigurationManager.AppSettings["DisableCaching"].AsBoolean();

            LoadCacheProvider();


        }

        /// <summary>
        /// Loads the cache provider.
        /// </summary>
        private static void LoadCacheProvider()
        {
            try
            {
                string cacheProviderSettings = ConfigurationManager.AppSettings["CacheProvider"];

                string[] providerSettings = cacheProviderSettings.Split( ',' );

                if ( providerSettings.Count() != 2 )
                {
                    s_cacheProvider = DefaultProvider();
                }

                Assembly cacheProviderAssembly = Assembly.Load( providerSettings[1] );
                if ( cacheProviderAssembly != null )
                {
                    Type cacheProviderType = cacheProviderAssembly.GetType( providerSettings[0] );
                    if ( cacheProviderType == null )
                    {
                        cacheProviderType = cacheProviderAssembly.GetType( providerSettings[1] + "." + providerSettings[0] );
                    }

                    if ( cacheProviderType != null )
                    {
                        /*var property = cacheProviderType.GetProperty( "Instance" );

                        if ( property != null )
                        {
                            s_cacheProvider = (IRockCacheProvider)property.GetValue( null );
                        }
                        else
                        {
                            s_cacheProvider = DefaultProvider();
                        }*/
                        s_cacheProvider = ( IRockCacheProvider )Activator.CreateInstance( cacheProviderType );

                    }
                    else
                    {
                        s_cacheProvider = DefaultProvider();
                    }
                }
                else
                {
                    s_cacheProvider = DefaultProvider();
                }
            }
            catch ( Exception ex )
            {
                s_cacheProvider = DefaultProvider();
            }
        }

        /// <summary>
        /// Defaults the provider.
        /// </summary>
        /// <returns></returns>
        private static IRockCacheProvider DefaultProvider()
        {
            // todo consider throwing an exception or sending a message..?

            return new RockMemoryCache();
        }

        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="regionName">Name of the region.</param>
        /// <returns></returns>
        public object Get( string key, string regionName = null )
        {
            if ( _isCachingDisabled )
            {
                return null;
            }

            return s_cacheProvider.Get( key, regionName );
        }

        /// <summary>
        /// Determines whether [contains] [the specified key].
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="regionName">Name of the region.</param>
        /// <returns></returns>
        public bool Contains( string key, string regionName = null )
        {
            return s_cacheProvider.Contains( key, regionName );
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="regionName">Name of the region.</param>
        /// <returns></returns>
        public object Remove( string key, string regionName = null )
        {
            return s_cacheProvider.Remove( key, regionName );
        }

        /// <summary>
        /// Sets the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        public void Set( CacheItem item, CacheItemPolicy policy )
        {
            s_cacheProvider.Set( item, policy );
        }

        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="policy">The policy.</param>
        /// <param name="regionName">Name of the region.</param>
        public void Set( string key, object value, CacheItemPolicy policy, string regionName = null )
        {
            s_cacheProvider.Set( key, value, policy, regionName );
        }

        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="absoluteExpiration">The absolute expiration.</param>
        /// <param name="regionName">Name of the region.</param>
        public void Set( string key, object value, DateTimeOffset absoluteExpiration, string regionName = null )
        {
            s_cacheProvider.Set( key, value, absoluteExpiration, regionName );
        }

        public object this[string key]
        {
            get
            {
                if ( _isCachingDisabled )
                {
                    return null;
                }

                return s_cacheProvider[key];
            }
            set
            {
                s_cacheProvider[key] = value;
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public static void Clear()
        {
            lock ( s_initLock )
            {
                if ( s_defaultCache != null )
                {
                    s_cacheProvider.Dispose();
                    s_defaultCache = null;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return s_cacheProvider.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
