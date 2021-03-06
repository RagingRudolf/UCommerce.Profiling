﻿using System.Data.Common;
using System.Data.SqlClient;
using StackExchange.Profiling.Data;

namespace RagingRudolf.UCommerce.Profiling.NHibernate
{
	/// <summary>
	/// Based on https://github.com/MRCollective/MiniProfiler.NHibernate
	/// </summary>
	public class ProfiledSqlDbCommand : ProfiledDbCommand
	{
		private readonly SqlCommand _sqlCommand;

		public ProfiledSqlDbCommand(DbCommand command, IDbProfiler profiler)
			: base(command, null, profiler)
		{
			_sqlCommand = (SqlCommand)command;
		}

		public SqlCommand SqlCommand
		{
			get { return _sqlCommand; }
		}
	}
}