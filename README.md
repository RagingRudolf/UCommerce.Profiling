# Profiling for uCommerce


uCommerce.Profiling is a code sample that helps you profiling services inside uCommerce itself. 
uCommerce.Profiling uses StackOverflow's MiniProfiler to archive this
accompanied by an interceptor written for Castle.Windsor. This library intents to help you
find out which of your components are running slow or causes uCommerce running slow. 

You will be able to see execution time and sql queries fired against the database.

Don't know what uCommerce is? 
uCommerce is a e-commerce platform build on .NET. 
You can find more information about it [here](http://www.ucommerce.net/ "uCommerce")

## Requirements

- .NET
- uCommerce and how to register components

## Install

- Compile the project against your specific uCommerce version, Castle.Windsor, NHibernate and FluentNHibernate.
- Register ProfilingInterceptor in uCommerce "customs.config".
- Register custom session provider instead of ucommerce's default one to get SQL queries shown as well.
- Register interceptor on the components you want to intercept (Examples inside of Core.config in uCommerce shows how that is done).
- Register ProfilingHandlerModule if you want a simple HttpModule for starting MiniProfiler on request start.

## Future

As of right now you have to manual configure an interceptor for those services you want to intercept. I will try to make a
facility to Castle.Windsor which will put an interceptor on all components when they are registered so it will be
even easier to profile.

Enjoy!