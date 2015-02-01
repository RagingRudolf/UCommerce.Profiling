using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Engine;

namespace RagingRudolf.UCommerce.Profiling.NHibernate
{
	/// <summary>
	/// Based on https://github.com/MRCollective/MiniProfiler.NHibernate
	/// </summary>
	internal class ProfiledSqlClientBatchingBatcherFactory : SqlClientBatchingBatcherFactory
	{
		public override IBatcher CreateBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
		{
			return new ProfiledSqlClientBatchingBatcher(connectionManager, interceptor);
		}
	}
}