using System;
using System.Collections.Generic;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.Helpers;
using FluentNHibernate.Conventions.Inspections;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Metadata;
using NHibernate.Tool.hbm2ddl;
using UCommerce.EntitiesV2;
using UCommerce.EntitiesV2.Listeners;
using UCommerce.Extensions;
using UCommerce.Infrastructure;
using UCommerce.Infrastructure.Configuration;
using UCommerce.Security;

namespace RagingRudolf.UCommerce.Profiling.NHibernate
{
	/// <summary>
	/// Based on decompile UCommerce assembly.
	/// </summary>
	public class SessionProvider : ISessionProvider, ICacheProvider
	{
		private static object _padLock = new object();
		private readonly IUserService _userService;
		private readonly IEnumerable<IContainsNHibernateMappingsTag> _mapAssemblyTags;
		internal ISession _session;
		internal IStatelessSession _statelessSession;
		private static bool _rebuildSchema;
		internal static ISessionFactory _factory;
		internal static bool _inTest;

		private CommerceConfigurationProvider CommerceConfigurationProvider { get; set; }

		public SessionProvider()
			: this(ObjectFactory.Instance.Resolve<CommerceConfigurationProvider>(), ObjectFactory.Instance.Resolve<IUserService>(), ObjectFactory.Instance.ResolveAll<IContainsNHibernateMappingsTag>())
		{
		}

		public SessionProvider(
			CommerceConfigurationProvider commerceConfiguration, 
			IUserService userService, 
			IEnumerable<IContainsNHibernateMappingsTag> mapAssemblyTags)
		{
			_userService = userService;
			_mapAssemblyTags = mapAssemblyTags;
			CommerceConfigurationProvider = commerceConfiguration;
		}

		public ISession GetSession()
		{
			if (_factory == null)
			{
				lock (_padLock)
				{
					if (_factory == null)
						_factory = CreateSessionFactory(CommerceConfigurationProvider.GetRuntimeConfiguration().EnableCache, CommerceConfigurationProvider.GetRuntimeConfiguration().CacheProvider);
				}
			}

			if (_session == null || !_session.IsOpen)
			{
				lock (_padLock)
				{
					if (_session != null)
					{
						if (_session.IsOpen)
							return _session;
					}
					_session = _factory.OpenSession();
				}
			}
		
			return _session;
		}

		public IStatelessSession GetStatelessSession()
		{
			if (_factory == null)
				_factory = CreateSessionFactory(CommerceConfigurationProvider.GetRuntimeConfiguration().EnableCache, CommerceConfigurationProvider.GetRuntimeConfiguration().CacheProvider);

			return _statelessSession ?? (_statelessSession = _factory.OpenStatelessSession());
		}

		public void RebuildSchema()
		{
			_rebuildSchema = true;
			
			lock (_padLock)
			{
				if (!_rebuildSchema)
					return;

				CreateSessionFactory(CommerceConfigurationProvider.GetRuntimeConfiguration().EnableCache, CommerceConfigurationProvider.GetRuntimeConfiguration().CacheProvider);
				_rebuildSchema = false;
			}
		}

		public void ClearCache()
		{
			var session = GetSession();
			var sessionFactory = session.SessionFactory;
			sessionFactory.EvictQueries();

			foreach (KeyValuePair<string, IClassMetadata> keyValuePair in sessionFactory.GetAllClassMetadata())
				session.Evict(keyValuePair);

			foreach (KeyValuePair<string, ICollectionMetadata> keyValuePair in sessionFactory.GetAllCollectionMetadata())
				session.Evict(keyValuePair);
		}

		internal ISessionFactory CreateSessionFactory(bool enableCache, string cacheProvider)
		{
			return Fluently
				.Configure()
				.Database(MsSqlConfiguration
					.MsSql2008.Raw("connection.isolation", "ReadUncommitted")
					.ConnectionString(this.CommerceConfigurationProvider.GetRuntimeConfiguration().ConnectionString)
					.Driver<MiniProfilerSql2008ClientDriver>()
					.UseReflectionOptimizer()
					.AdoNetBatchSize(200)
					)
				.Cache(c =>
				{
					if (!enableCache)
						return;

					c.UseQueryCache().UseSecondLevelCache().ProviderClass(cacheProvider);
				})
				.Mappings(m =>
				{
					FluentMappingsContainerExtensions
						.AddFromTaggedAssemblies(m.FluentMappings, _mapAssemblyTags)
						.Conventions.Add(Table.Is(x => "uCommerce_" + x.EntityType.Name))
						.Conventions.Add(PrimaryKey.Name.Is(FormatPrimaryKey))
						.Conventions.Add(ForeignKey.Format(FormatForeignKey))
						.Conventions.AddFromAssemblyOf<IdPropertyConvention>();

					m.HbmMappings.AddFromAssemblyOf<SessionProvider>();
				})
				.ExposeConfiguration(BuildSchema)
				.BuildSessionFactory();
		}

		private static string FormatPrimaryKey(IIdentityInspector arg)
		{
			return arg.EntityType.Name + "Id";
		}

		private static string FormatForeignKey(FluentNHibernate.Member property, Type type)
		{
			return type.Name + "Id";
		}

		private void BuildSchema(Configuration obj)
		{
			ApplyEventListeners(obj);
			
			if (!_rebuildSchema)
				return;
			
			Console.WriteLine("Rebuilding schema...");
			new SchemaExport(obj).Create(false, true);
			Console.WriteLine("Done rebuilding schema!");
		}

		internal void ApplyEventListeners(Configuration obj)
		{
			obj.SetListener(ListenerType.Delete, new DeleteEventListenerAggragator(new List<IEntityDeleteEventListener>()
			{
				new SoftDeleteEventListener(),
				new ProductDeleteEventListener(),
				new OrderLineDeleteEventListener()
			}));
			obj.SetListener(ListenerType.PreInsert, (object)this.GetInsertUpdateEventListener());
			obj.SetListener(ListenerType.PreUpdate, (object)this.GetInsertUpdateEventListener());
		}

		private AuditEventListener GetInsertUpdateEventListener()
		{
			if (_inTest)
				return new TestableAuditEventListener();

			return new AuditEventListener(_userService);
		}

		public void Dispose()
		{
			if (_session != null)
				_session.Dispose();
			if (_statelessSession == null)
				return;
			_statelessSession.Dispose();
		}
	}
}