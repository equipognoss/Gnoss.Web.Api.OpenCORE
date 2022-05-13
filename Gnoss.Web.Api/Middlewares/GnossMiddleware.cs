
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

        public async Task Invoke(HttpContext context, LoggingService loggingService, GnossCache gnossCache, VirtuosoAD virtuosoAD, IServicesUtilVirtuosoAndReplication pServicesUtilVirtuosoAndReplication)
        {
            Application_BeginRequest(loggingService);
            Application_PreRequestHandlerExecute(loggingService, context, gnossCache, virtuosoAD, pServicesUtilVirtuosoAndReplication);
            await _next(context);            
            Application_PostRequestHandlerExecute(context, loggingService, virtuosoAD, gnossCache, pServicesUtilVirtuosoAndReplication);
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
            string ruta = Path.Combine(mEnv.ContentRootPath, "trazas");

            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }

            ruta += "\\traza_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

            return ruta;
        }


        protected void Application_PreRequestHandlerExecute(LoggingService pLoggingService, HttpContext pHttpContext, GnossCache pGnossCache, VirtuosoAD pVirtuosoAD, IServicesUtilVirtuosoAndReplication pServicesUtilVirtuosoAndReplication)
        {
            pLoggingService.AgregarEntrada("Application_PreRequestHandlerExecute_INICIO");
            if (pHttpContext.Request.Headers != null && !string.IsNullOrEmpty(pHttpContext.Request.Headers["X-Request-ID"]))
            {
                string tokenAfinidadPeticion = pHttpContext.Request.Headers["X-Request-ID"];
                string conexionAfinidadVirtuosoCache = (string)pGnossCache.ObtenerObjetoDeCache("conexionAfinidadVirtuoso_" + tokenAfinidadPeticion);

                if (!string.IsNullOrEmpty(conexionAfinidadVirtuosoCache))
                {
                    pServicesUtilVirtuosoAndReplication.ConexionAfinidadVirtuoso = conexionAfinidadVirtuosoCache;
                }
                else if (!string.IsNullOrEmpty(tokenAfinidadPeticion) && tokenAfinidadPeticion.StartsWith("from-web_"))
                {
                    string conexionAfinidadVirtuoso = tokenAfinidadPeticion.Replace("from-web_", "");
                    pGnossCache.AgregarObjetoCache("conexionAfinidadVirtuoso_" + tokenAfinidadPeticion, conexionAfinidadVirtuoso, 60);
                    pServicesUtilVirtuosoAndReplication.ConexionAfinidadVirtuoso = conexionAfinidadVirtuoso;
                }
            }
            pLoggingService.AgregarEntrada("Application_PreRequestHandlerExecute_FIN");
        }

        protected void Application_PostRequestHandlerExecute(HttpContext pHttpContext, LoggingService pLoggingService, VirtuosoAD pVirtuosoAD, GnossCache pGnossCache, IServicesUtilVirtuosoAndReplication pServicesUtilVirtuosoAndReplication)
        {
            pLoggingService.AgregarEntrada("Application_PostRequestHandlerExecute_INICIO");
            string conexionAfinidadVirtuoso = pServicesUtilVirtuosoAndReplication.ConexionAfinidadVirtuoso;
            if (!string.IsNullOrEmpty(conexionAfinidadVirtuoso))
            {
                DateTime fechaFinAfinidadVirtuoso = pVirtuosoAD.FechaFinAfinidad;

                if (fechaFinAfinidadVirtuoso > DateTime.Now)
                {
                    if (pHttpContext.Request.Headers != null && !string.IsNullOrEmpty(pHttpContext.Request.Headers["X-Request-ID"]))
                    {
                        string tokenAfinidadPeticion = pHttpContext.Request.Headers["X-Request-ID"];
                        pGnossCache.AgregarObjetoCache("conexionAfinidadVirtuoso_" + tokenAfinidadPeticion, conexionAfinidadVirtuoso, 60);
                    }
                }
            }
            pLoggingService.AgregarEntrada("Application_PostRequestHandlerExecute_FIN");
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