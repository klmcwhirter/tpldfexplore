using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using tpldfexplore.Command;
using tpldfexplore.Service;

namespace tpldfexplore
{
    public class Startup : IDisposable
    {
        IConfiguration Configuration { get; set; }
        ILoggerFactory LoggerFactory { get; set; }

        IContainer Container { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            Configuration = configBuilder.Build();

            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog(new NLogProviderOptions
                {
                    CaptureMessageTemplates = false,
                    CaptureMessageProperties = false
                });
                builder.AddDebug();
            });
            services.AddAutofac();
            services.AddOptions();
            services.Configure<ProgramOptions>(opts =>
            {
                opts.DoLog = Configuration.GetValue<bool>("doLog", false);
                opts.MaxDegreeOfParallelism = Configuration.GetValue<int>("maxDegreeOfParallelism", Environment.ProcessorCount);
                opts.ReadChunkSize = Configuration.GetValue<int>("readChunkSize", 100);
            });
        }

        public void ConfigureContainer(ContainerBuilder builder, string name, int? iterations)
        {
            // Add things to the Autofac ContainerBuilder.

            builder.RegisterType<Int2WordsService>().AsSelf();
            if (iterations.HasValue)
            {
                builder.RegisterCommands(name, iterations.Value);
            }
            else
            {
                builder.RegisterCommands(name);
            }

            Container = builder.Build();
        }

        public void Configure(string name, int? iterations)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            if (Container == null)
            {
                var builder = new ContainerBuilder();
                builder.Populate(services);
                ConfigureContainer(builder, name, iterations);
            }

            // ServiceProvider = new AutofacServiceProvider(Container);
        }

        internal ICommand GetCommand()
        {
            var cmd = Container.Resolve<ICommand>();
            return cmd;
        }

        internal ProgramOptions GetOptions()
        {
            var options = Container.Resolve<IOptions<ProgramOptions>>();
            return options?.Value;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    Container.Dispose();
                    Container = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Startup()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}