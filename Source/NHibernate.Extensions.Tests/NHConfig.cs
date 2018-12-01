using System;
using System.IO;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Conventions.Helpers;
using log4net;
using log4net.Config;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Environment = NHibernate.Cfg.Environment;


namespace NHibernate.Extensions.Tests
{
    public class AutomappingConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(System.Type type)
        {
            return base.ShouldMap(type) && typeof(Entity).IsAssignableFrom(type);
        }
    }

    public static class NHConfig
    {
        public static readonly ISessionFactory SessionFactory;

        public static readonly Configuration Configuration;

        static NHConfig()
        {
            XmlConfigurator.Configure(LogManager.GetRepository(typeof(NHConfig).Assembly));
#if DEBUG
            HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
#endif

            var modelAssembly = typeof(NHConfig).Assembly;
            var configuration = Configuration = new Configuration();
            configuration.SetProperty(Environment.GenerateStatistics, "true");
            configuration.SetProperty(Environment.UseSqlComments, "true");
            configuration.SetProperty(Environment.ShowSql, "true");
            configuration.Configure();  // Configure from the hibernate.cfg.config

            // We have to replace |DataDirectory| as it is not supported on .NET Core
            var connString = configuration.Properties[Environment.ConnectionString];
            configuration.Properties[Environment.ConnectionString] =
                connString.Replace("|DataDirectory|",
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..")));

            var fluentConfig = Fluently.Configure(configuration);
            var autoPestModel = AutoMap
                .Assemblies(new AutomappingConfiguration(), new[] { modelAssembly })
                .UseOverridesFromAssembly(modelAssembly)
                .IgnoreBase<Entity>()
                .Conventions.Add<CascadeConvention>()
                .Conventions.Add(PrimaryKey.Name.Is(o => "Id"))
                .Conventions.Add(ForeignKey.EndsWith("Id"));
            fluentConfig
                .Diagnostics(o => o.Enable(true))
                .Mappings(m =>
                {
                    m.HbmMappings.AddFromAssembly(modelAssembly);
                    m.AutoMappings.Add(autoPestModel);
#if DEBUG
                    var mappingsDirecotry = Path.Combine(Directory.GetCurrentDirectory(), "Mappings");
                    if (!Directory.Exists(mappingsDirecotry))
                        Directory.CreateDirectory(mappingsDirecotry);
                    m.AutoMappings.ExportTo(mappingsDirecotry);
                    m.FluentMappings.ExportTo(mappingsDirecotry);
#endif
                });

            SessionFactory = fluentConfig.BuildSessionFactory();

            var schema = new SchemaExport(configuration);
            schema.Drop(false, true);
            schema.Create(false, true);
        }

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
    }
}
