using System;
using System.Collections.Generic;
using AppOptics.Instrumentation;

namespace Solarwinds.TracingWrapper
{
    public static class TracingHelper
    {
        public static void StartTrace(string traceName)
        {
            StartTraceEvent startEvent = Trace.StartTrace(traceName);
            startEvent.Report();
        }

        public static void EndTrace(string traceName)
        {
            EndTraceEvent endEvent = Trace.EndTrace(traceName);
            endEvent.Report();
        }

        public static bool WaitTillReady(TimeSpan timeout)
        {
            var isAgentReady = Trace.WaitUntilAgentReady((int)timeout.TotalMilliseconds, out int statusCode);

            //if (!agentReady)
            //{
            //    System.Console.WriteLine("Agent isn't ready. Status code returned: {0}", statusCode);
            //}

            return isAgentReady;
        }

        /// <summary>
        /// Creates the span wrapper for database operations
        /// </summary>
        /// <param name="spanName">Name of the span to be reported</param>
        /// <param name="query">The sql query</param>
        /// <param name="backendFlavor">Backend type - e.g. MySQL, ClickHouse, MSSQL, etc.</param>
        /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
        /// <param name="additionalInfo">Optional additional info. Eg. name of the database (under 'Database' key), stack trace etc.</param>
        public static IDisposable BeginQuerySpan(
            string spanName,
            string query,
            string backendFlavor,
            string remoteHost,
            IDictionary<string, object> additionalInfo = null)
        {
            return new SqlSpan(spanName, query, backendFlavor, remoteHost, additionalInfo);
        }

        /// <summary>
        /// Creates the span wrapper for Cache operations
        /// </summary>
        /// <param name="spanName">Name of the span to be reported</param>
        /// <param name="operation">The cache operation - e.g. get, set etc.</param>
        /// <param name="cacheFlavor">Cache type - e.g. Memcache, Redis etc.</param>
        /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
        /// <param name="additionalInfo">Optional additional info. Eg. hit flag (under 'KVHit' key), cache key etc.</param>
        public static IDisposable BeginCacheSpan(
            string spanName,
            string operation,
            string cacheFlavor,
            string remoteHost,
            IDictionary<string, object> additionalInfo = null)
        {
            return new CacheSpan(spanName, operation, cacheFlavor, remoteHost, additionalInfo);
        }

        /// <summary>
        /// Creates the span wrapper for RPC operations
        /// </summary>
        /// <param name="spanName">Name of the span to be reported</param>
        /// <param name="remoteProtocol">The protocol - e.g. grpc, wcf etc.</param>
        /// <param name="controller">The operation handler</param>
        /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
        /// <param name="additionalInfo">Optional additional info. Eg. hit flag (under 'KVHit' key), cache key etc.</param>
        public static IDisposable BeginRpcSpan(
            string spanName,
            string remoteProtocol,
            string controller,
            string remoteHost,
            IDictionary<string, object> additionalInfo = null)
        {
            return new RpcSpan(spanName, remoteProtocol, controller, remoteHost, additionalInfo);
        }

        private abstract class DisposableSpanBase : IDisposable
        {
            private readonly string _spanName;

            protected DisposableSpanBase(string spanName)
            {
                _spanName = spanName;
            }

            public void Dispose()
            {
                ExitTraceEvent evt = Trace.CreateExitEvent(_spanName);
                evt.Report();
            }
        }

        private class SqlSpan : DisposableSpanBase
        {
            /// <summary>
            /// Creates the span wrapper for database operations
            /// </summary>
            /// <param name="spanName">Name of the span to be reported</param>
            /// <param name="query">The sql query</param>
            /// <param name="backendFlavor">Backend type - e.g. MySQL, ClickHouse, MSSQL, etc.</param>
            /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
            /// <param name="additionalInfo">Optional additional info. Eg. name of the database (under 'Database' key), stack trace etc.</param>
            public SqlSpan(
                string spanName,
                string query,
                string backendFlavor,
                string remoteHost,
                IDictionary<string, object> additionalInfo = null)
                : base(spanName)
            {
                EntryTraceEvent evt = Trace.CreateEntryEvent(spanName);
                evt.AddInfo("Spec", "query");
                evt.AddInfo("Query", SanitizeQuery(query));
                evt.AddInfo("Flavor", backendFlavor);
                evt.AddInfo("RemoteHost", remoteHost);
                if (additionalInfo != null)
                {
                    evt.AddInfo(additionalInfo);
                }

                evt.Report();
            }

            //sql sanitization like:
            //https://github.com/appoptics/appoptics-apm-go/blob/7dee89d09f0c7fde721a30618e5c0fec7929290e/v1/ao/internal/reporter/sql_sanitizer.go#L358
            private static string SanitizeQuery(string query)
            {
                return query;
            }
        }

        private class CacheSpan : DisposableSpanBase
        {
            /// <summary>
            /// Creates the span wrapper for cache operations
            /// </summary>
            /// <param name="spanName">Name of the span to be reported</param>
            /// <param name="operation">The cache operation - e.g. get, set etc.</param>
            /// <param name="cacheFlavor">Cache type - e.g. Memcache, Redis etc.</param>
            /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
            /// <param name="additionalInfo">Optional additional info. Eg. hit flag (under 'KVHit' key), cache key etc.</param>
            public CacheSpan(
                string spanName,
                string operation,
                string cacheFlavor,
                string remoteHost,
                IDictionary<string, object> additionalInfo = null)
                : base(spanName)
            {
                EntryTraceEvent evt = Trace.CreateEntryEvent(spanName);
                evt.AddInfo("Spec", "cache");
                evt.AddInfo("KVOp", operation);
                evt.AddInfo("Flavor", cacheFlavor);
                evt.AddInfo("RemoteHost", remoteHost);
                if (additionalInfo != null)
                {
                    evt.AddInfo(additionalInfo);
                }

                evt.Report();
            }
        }

        private class RpcSpan : DisposableSpanBase
        {
            /// <summary>
            /// Creates the span wrapper for rpc operations
            /// </summary>
            /// <param name="spanName">Name of the span to be reported</param>
            /// <param name="remoteProtocol">The protocol - e.g. grpc, wcf etc.</param>
            /// <param name="controller">The operation handler</param>
            /// <param name="remoteHost">The remote host of this operation - basically the DB host</param>
            /// <param name="additionalInfo">Optional additional info. Eg. hit flag (under 'KVHit' key), cache key etc.</param>
            public RpcSpan(
                string spanName,
                string remoteProtocol,
                string controller,
                string remoteHost,
                IDictionary<string, object> additionalInfo = null)
                : base(spanName)
            {
                EntryTraceEvent evt = Trace.CreateEntryEvent(spanName);
                evt.AddInfo("Spec", "rsc");
                evt.AddInfo("IsService", true);
                evt.AddInfo("RemoteProtocol", remoteProtocol);
                evt.AddInfo("RemoteController", controller);
                evt.AddInfo("RemoteHost", remoteHost);
                if (additionalInfo != null)
                {
                    evt.AddInfo(additionalInfo);
                }

                evt.Report();
            }
        }
    }
}
