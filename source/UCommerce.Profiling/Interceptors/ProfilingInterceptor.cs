using Castle.DynamicProxy;
using StackExchange.Profiling;

namespace RagingRudolf.UCommerce.Profiling.Interceptors
{
	public class ProfilingInterceptor : IInterceptor
	{
		public void Intercept(IInvocation invocation)
		{
			var stepName = string.Format("{0}.{1}", invocation.TargetType.Name, invocation.MethodInvocationTarget.Name);

			using (MiniProfiler.Current.Step(stepName))
			{
				invocation.Proceed();
			}
		}
	}
}