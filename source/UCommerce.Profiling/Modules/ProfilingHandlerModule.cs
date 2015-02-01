using System;
using System.Web;
using StackExchange.Profiling;

namespace RagingRudolf.UCommerce.Profiling.Modules
{
	public class ProfilingHandlerModule : IHttpModule
	{
		public void Init(HttpApplication context)
		{
			context.BeginRequest += BeginRequest;
			context.EndRequest += EndRequest;
		}

		private void EndRequest(object sender, EventArgs e)
		{
			MiniProfiler.Stop();
		}

		private void BeginRequest(object sender, EventArgs e)
		{

			MiniProfiler.Start();
		}

		public void Dispose()
		{
		}
	}
}