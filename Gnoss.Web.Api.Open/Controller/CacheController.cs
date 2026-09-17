using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Identidad;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers;
using Es.Riam.Interfaces.InterfacesOpen;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace Gnoss.Web.Api.Open.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class CacheController : ControlApiGnossBase
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory mLoggerFactory;

        public CacheController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, IAvailableServices availableServices, ILogger<CacheController> logger, ILoggerFactory loggerFactory) : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication, availableServices, logger, loggerFactory)
        {
            _logger = logger;
            mLoggerFactory = loggerFactory;
        }

        [HttpPost]
        [Route("invalidar-caches-locales")]
        public IActionResult InvalidarCachesLocales(string pPersonaID, string login)
        {
            try
            {
                mGnossCache.VersionarCacheLocal(ProyectoAD.MetaProyecto);
                using IdentidadCL gnossCacheCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCL>(), mLoggerFactory);
                
                if (!string.IsNullOrEmpty(pPersonaID))
                {
                    string cadena = $"IdentidadActual_{pPersonaID}";
                    gnossCacheCL.InvalidarCacheQueContengaCadena(cadena);
                }
                else
                {
                    using UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                    Guid usuarioId = usuarioCN.ObtenerFilaUsuarioPorLoginOEmail(login).UsuarioID;
                    string cadena = $"IdentidadActual_{usuarioId}";
                    gnossCacheCL.InvalidarCacheQueContengaCadena(cadena);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, _logger);
            }

            return Ok();
        }
    }
}
