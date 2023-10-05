
using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Util.General;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;

using System.IO;
using System.Threading.Tasks;
namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Middlewares
{

    public class GnossMiddleware
    {
        private IHostingEnvironment mEnv;
        private readonly RequestDelegate _next;

        public GnossMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            _next = next;
            mEnv = env;
        }

        public async Task Invoke(HttpContext context, LoggingService loggingService)
        {
            Application_BeginRequest(loggingService);
            await _next(context);            
            Application_EndRequest(loggingService);
        }




        protected void Application_BeginRequest(LoggingService pLoggingService)
        {
            pLoggingService.AgregarEntrada("TiemposMVC_Application_BeginRequest_INICIO");                        
            LoggingService.TiempoMinPeticion = 1;
            pLoggingService.AgregarEntrada("TiemposMVC_Application_BeginRequest_FIN");
        }

        protected void Application_EndRequest(LoggingService pLoggingService)
        {
            pLoggingService.AgregarEntrada("Comienza Application_EndRequest");

            try
            {
                pLoggingService.AgregarEntrada("Cierro conexiones");

                Es.Riam.Gnoss.Recursos.ControladorConexiones.CerrarConexiones();

                pLoggingService.AgregarEntrada("TiemposMVC_Application_EndRequest");

                pLoggingService.GuardarTraza(ObtenerRutaTraza());
            }
            catch (Exception) { }
        }

        protected string ObtenerRutaTraza()
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "trazas");

            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
            ruta = Path.Combine(ruta, $"traza_{DateTime.Now.ToString("yyyy-MM-dd")}.txt");

            return ruta;
        }

    }
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGnossMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GnossMiddleware>();
        }
    }
}