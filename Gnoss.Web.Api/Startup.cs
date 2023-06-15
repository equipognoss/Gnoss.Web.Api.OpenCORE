using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.RabbitMQ;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Util.Seguridad;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Middlewares;
using Es.Riam.Interfaces.InterfacesOpen;
using Es.Riam.Open;
using Es.Riam.OpenReplication;
using Es.Riam.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gnoss.Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            Configuration = configuration;
            mEnvironment = environment;
            
        }

        public IConfiguration Configuration { get; }
        public Microsoft.AspNetCore.Hosting.IHostingEnvironment mEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            EscribirLogTiempos("Application_Start Inicio");
            Conexion.ServicioWeb = true;
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
            services.AddHttpContextAccessor();
            services.AddScoped(typeof(UtilTelemetry));
            services.AddScoped(typeof(Usuario));
            services.AddScoped(typeof(UtilPeticion));
            services.AddScoped(typeof(Conexion));
            services.AddScoped(typeof(UtilGeneral));
            services.AddScoped(typeof(LoggingService));
            services.AddScoped(typeof(RedisCacheWrapper));
            services.AddScoped(typeof(Configuracion));
            services.AddScoped(typeof(GnossCache)); 
            services.AddScoped(typeof(VirtuosoAD));
            services.AddScoped(typeof(UtilServicios));
            services.AddScoped<IUtilServicioIntegracionContinua, UtilServicioIntegracionContinuaOpen>();
            services.AddScoped<IServicesUtilVirtuosoAndReplication, ServicesVirtuosoAndBidirectionalReplicationOpen>();
            string bdType = "";
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            if (environmentVariables.Contains("connectionType"))
            {
                bdType = environmentVariables["connectionType"] as string;
            }
            else
            {
                bdType = Configuration.GetConnectionString("connectionType");
            }
            if (bdType.Equals("2"))
            {
                services.AddScoped(typeof(DbContextOptions<EntityContext>));
                services.AddScoped(typeof(DbContextOptions<EntityContextBASE>));
            }
            services.AddSingleton(typeof(ConfigService));
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddMvc();
            string acid = "";
            if (environmentVariables.Contains("acid"))
            {
                acid = environmentVariables["acid"] as string;
            }
            else
            {
                acid = Configuration.GetConnectionString("acid");
            }
            string baseConnection = "";
            if (environmentVariables.Contains("base"))
            {
                baseConnection = environmentVariables["base"] as string;
            }
            else
            {
                baseConnection = Configuration.GetConnectionString("base");
            }
            
            if (bdType.Equals("0"))
            {
                services.AddDbContext<EntityContext>(options =>
                        options.UseSqlServer(acid)
                        );
                services.AddDbContext<EntityContextBASE>(options =>
                        options.UseSqlServer(baseConnection)

                        );
            }
            else if (bdType.Equals("2"))
            {
                services.AddDbContext<EntityContext, EntityContextPostgres>(opt =>
                {
                    var builder = new NpgsqlDbContextOptionsBuilder(opt);
                    builder.SetPostgresVersion(new Version(9, 6));
                    opt.UseNpgsql(acid);

                });
                services.AddDbContext<EntityContextBASE, EntityContextBASEPostgres>(opt =>
                {
                    var builder = new NpgsqlDbContextOptionsBuilder(opt);
                    builder.SetPostgresVersion(new Version(9, 6));
                    opt.UseNpgsql(baseConnection);

                });
            }
            var sp = services.BuildServiceProvider();

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


            // Resolve the services from the service provider
            var configService = sp.GetService<ConfigService>();
            configService.ObtenerProcesarStringGrafo();

            int hilos = configService.ObtenerHilosAplicacion();
            if(hilos != 0)
            {
                System.Threading.ThreadPool.GetMinThreads(out int x, out int y);
                System.Threading.ThreadPool.SetMinThreads(hilos, y);
            }

            

            string configLogStash = configService.ObtenerLogStashConnection();
            if (!string.IsNullOrEmpty(configLogStash))
            {
                LoggingService.InicializarLogstash(configLogStash);
            }
            var entity = sp.GetService<EntityContext>();
            LoggingService.RUTA_DIRECTORIO_ERROR = Path.Combine(mEnvironment.ContentRootPath, "logs");

            //TODO Javier AD.BaseAD.LeerConfiguracionConexion(mGestorParametrosAplicacion.ListaConfiguracionBBDD.Where(confiBBDD=> confiBBDD.TipoConexion.Equals((short)TipoConexion.SQLServer)).ToList());

            //TODO Javier BaseCL.LeerConfiguracionCache(mGestorParametrosAplicacion.ListaConfiguracionBBDD.Where(confiBBDD => confiBBDD.TipoConexion.Equals((short)TipoConexion.Redis)).ToList());

            //TODO Javier AD.BaseAD.LeerConfiguracionConexion(mGestorParametrosAplicacion.ListaConfiguracionBBDD.Where(confiBBDD => confiBBDD.TipoConexion.Equals((short)TipoConexion.Virtuoso)).ToList());

            //TODO Javier AD.BaseAD.LeerConfiguracionConexion(mGestorParametrosAplicacion.ListaConfiguracionBBDD.Where(confiBBDD => confiBBDD.TipoConexion.Equals((short)TipoConexion.Virtuoso_HA_PROXY)).ToList());

            //List<ConfiguracionBBDD> confsBBDD = mGestorParametrosAplicacion.ListaConfiguracionBBDD.Where(confiBBDD => confiBBDD.TipoConexion.Equals((short)TipoConexion.RabbitMQ)).ToList();
            //foreach (ConfiguracionBBDD confBBDD in confsBBDD)
            //{
            //    //TODO Javier RabbitMQClient.LeerConfiguracionConexion(confBBDD.Conexion, confBBDD.NombreConexion, confBBDD.LecturaPermitida, confBBDD.DatosExtra);
            //}


            RabbitMQClient.ClientName = $"API_V3_{mEnvironment.EnvironmentName}";

            EstablecerDominioCache(entity);

            CargarIdiomasPlataforma(configService);

            ConfigurarApplicationInsights(configService);

            //Configuro la caché de lectura
            ConfigurarParametros(configService);

            EscribirLogTiempos("Application_Start Fin");
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gnoss.Web.Api", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Servers = new List<OpenApiServer>
                      {
                        new OpenApiServer { Url = $"/apiv3"},
                        new OpenApiServer { Url = $"/" }
                      });
            });
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Gnoss.Web.Api v1"));
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseGnossMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigurarParametros(ConfigService configService)
        {

            string nodoTiempos = configService.ObtenerTiempos();
            if (!string.IsNullOrEmpty(nodoTiempos))
            {
                ControlApiGnossBase.TomarTiempos = string.Compare(nodoTiempos, "true", true) == 0;
            }

            string nodoTrazas = configService.ObtenerTrazaHabilitada();
            if (!string.IsNullOrEmpty(nodoTrazas))
            {
                LoggingService.TrazaHabilitada = string.Compare(nodoTrazas, "true", true) == 0;
            }

            string nodoDatosPeticion = configService.ObtenerGuardarDatosPeticion();
            if (!string.IsNullOrEmpty(nodoDatosPeticion))
            {
                ControlApiGnossBase.GuardarDatosPeticion = string.Compare(nodoDatosPeticion, "true", true) == 0;
            }
        }

        /// <summary>
        /// Establece el dominio de la cache.
        /// </summary>
        private void EstablecerDominioCache(EntityContext entity)
        {
            string dominio = entity.ParametroAplicacion.Where(parametroApp => parametroApp.Parametro.Equals("UrlIntragnoss")).FirstOrDefault().Valor;

            dominio = dominio.Replace("http://", "").Replace("https://", "").Replace("www.", "");

            if (dominio[dominio.Length - 1] == '/')
            {
                dominio = dominio.Substring(0, dominio.Length - 1);
            }

            BaseCL.DominioEstatico = dominio;
        }
        private void CargarIdiomasPlataforma(ConfigService configService)
        {

            configService.ObtenerListaIdiomas().FirstOrDefault();
        }

        public void EscribirLogTiempos(string pMensaje)
        {
            try
            {
                string directorio = Path.Combine(mEnvironment.ContentRootPath, "logs");
                System.IO.Directory.CreateDirectory(directorio);
                string rutaFichero = directorio + "\\logTiempos_Global_apiRecursos_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";

                //Añado el error al fichero
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(rutaFichero, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine(pMensaje + " " + DateTime.Now.ToString());
                }
            }
            catch (Exception) { }
        }

        private void ConfigurarApplicationInsights(ConfigService configService)
        {
            string valor = configService.ObtenerImplementationKeyApiV3();

            if (!string.IsNullOrEmpty(valor))
            {
                Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey = valor.ToLower();
            }

            if (UtilTelemetry.EstaConfiguradaTelemetria)
            {
                //Configuración de los logs

                string ubicacionLogs = configService.ObtenerUbicacionLogsApiV3();

                int valorInt = 0;
                if (int.TryParse(ubicacionLogs, out valorInt))
                {
                    if (Enum.IsDefined(typeof(UtilTelemetry.UbicacionLogsYTrazas), valorInt))
                    {
                        LoggingService.UBICACIONLOGS = (UtilTelemetry.UbicacionLogsYTrazas)valorInt;
                    }
                }


                //Configuración de las trazas

                string ubicacionTrazas = configService.ObtenerUbicacionTrazasApiV3();

                int valorInt2 = 0;
                if (int.TryParse(ubicacionTrazas, out valorInt2))
                {
                    if (Enum.IsDefined(typeof(UtilTelemetry.UbicacionLogsYTrazas), valorInt2))
                    {
                        LoggingService.UBICACIONTRAZA = (UtilTelemetry.UbicacionLogsYTrazas)valorInt2;
                    }
                }

            }

        }
    }
}
