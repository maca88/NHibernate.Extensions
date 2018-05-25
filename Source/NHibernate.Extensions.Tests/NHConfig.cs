using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.Helpers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Extensions.Tests;
using NHibernate.Tool.hbm2ddl;
using Environment = NHibernate.Cfg.Environment;


namespace T4FluentNH.Tests
{
    public class AutomappingConfiguration : DefaultAutomappingConfiguration
    {
        public override bool ShouldMap(Type type)
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
            var modelAssembly = typeof(NHConfig).Assembly;
            var configuration = Configuration = new Configuration();
            configuration.SetProperty(Environment.GenerateStatistics, "true");
            configuration.SetProperty(Environment.UseSqlComments, "true");
            configuration.SetProperty(Environment.ShowSql, "true");
            configuration.Configure();  //configure from the web.config
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
                    var mappingsDirecotry = Path.Combine(Directory.GetCurrentDirectory(), "Mappings");
                    if (!Directory.Exists(mappingsDirecotry))
                        Directory.CreateDirectory(mappingsDirecotry);
                    m.AutoMappings.ExportTo(mappingsDirecotry);
                    m.FluentMappings.ExportTo(mappingsDirecotry);
                });
            
            SessionFactory = fluentConfig.BuildSessionFactory();
            var schema = new SchemaExport(configuration);
            schema.Drop(false, true);
            schema.Create(false, true);

#if HIBERNATINGRHINOS
            HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
#endif
        }

        public static ISession OpenSession()
        {
            ISession session = SessionFactory.OpenSession();
            return session;
        }
    }
}
