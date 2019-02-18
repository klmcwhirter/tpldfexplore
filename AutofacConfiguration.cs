using System;
using Autofac;
using tpldfexplore.Command;
using tpldfexplore.Service;

namespace tpldfexplore
{
    public static class AutofacConfiguration
    {
        static IContainer Container { get; set; }

        internal static IContainer RegisterTypes(string name)
        {
            if (Container == null)
            {
                var builder = new ContainerBuilder();

                builder.RegisterType<Int2WordsService>().AsSelf();
                builder.RegisterCommands(name);

                Container = builder.Build();
            }

            return Container;
        }

        internal static ICommand GetCommand()
        {
            var cmd = Container.Resolve<ICommand>();
            return cmd;
        }
    }
}