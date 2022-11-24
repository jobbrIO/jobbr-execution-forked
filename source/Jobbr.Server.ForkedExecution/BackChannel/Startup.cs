using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Jobbr.ComponentModel.Registration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    internal class Startup
    {
        /// <summary>
        /// The dependency resolver from the JobbrServer which needs to be passed through the OWIN stack to WebAPI.
        /// </summary>
        private readonly IJobbrServiceProvider dependencyResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// The dependency resolver.
        /// </param>
        public Startup(IJobbrServiceProvider serviceProvider)
        {
            dependencyResolver = serviceProvider;
        }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration
            {
                // Set the resolved to the service provider that gets injected when constructing this component
                DependencyResolver = new DependencyResolverAdapter(dependencyResolver),
            };

            // Add trace logger for exceptions
            config.Services.Add(typeof(IExceptionLogger), new TraceSourceExceptionLogger(new NullLoggerFactory()));

            // Controllers all have attributes
            config.MapHttpAttributeRoutes();

            // Serialization
            var jsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver(), NullValueHandling = NullValueHandling.Ignore };
            config.Formatters.JsonFormatter.SerializerSettings = jsonSerializerSettings;

            // Remove XML response format
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => string.Equals(t.MediaType, "application/xml", StringComparison.Ordinal));
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            // Finally attach WebApi to the pipeline with the given configuration
            app.UseWebApi(config);
        }
    }
}
