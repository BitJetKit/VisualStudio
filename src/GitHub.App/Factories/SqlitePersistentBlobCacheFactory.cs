﻿using Akavache;
using Akavache.Sqlite3;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitHub.Extensions;
using Serilog;

namespace GitHub.Factories
{
    [Export(typeof(IBlobCacheFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SqlitePersistentBlobCacheFactory : IBlobCacheFactory
    {
        Dictionary<string, IBlobCache> cache = new Dictionary<string, IBlobCache>();

        public IBlobCache CreateBlobCache(string path)
        {
            Guard.ArgumentNotEmptyString(path, nameof(path));
            if (cache.ContainsKey(path))
                return cache[path];

            try
            {
                var c = new SQLitePersistentBlobCache(path);
                cache.Add(path, c);
                return c;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error while creating SQLitePersistentBlobCache for {path}.", path);
                return new InMemoryBlobCache();
            }
        }

        bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                Task.Run(() =>
                {
                    foreach (var c in cache.Values)
                    {
                        c.Dispose();
                        c.Shutdown.Wait();
                    }
                }).Wait(500);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}