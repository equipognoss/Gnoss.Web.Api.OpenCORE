using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Identidad;
using Es.Riam.Gnoss.Elementos.Suscripcion;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Interfaces.InterfacesOpen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gnoss.Web.Api.Open.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class CacheController : Microsoft.AspNetCore.Mvc.Controller
    {
        private EntityContext _entityContext;
        private LoggingService _loggingService;
        private RedisCacheWrapper _redisCacheWrapper;
        private ConfigService _configService;
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;
        private IAvailableServices _availableServices;
        protected GnossCache mGnossCache;
        private ILogger mlogger;
        private ILoggerFactory mLoggerFactory;
        public CacheController(GnossCache gnossCache, EntityContext entityContext, LoggingService loggingService, RedisCacheWrapper redisCacheWrapper, ConfigService configService, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, IAvailableServices availableServices, ILogger<CacheController> logger, ILoggerFactory loggerFactory) 
        {
            _entityContext = entityContext;
            _loggingService = loggingService;
            _configService = configService;
            _redisCacheWrapper = redisCacheWrapper;
            _availableServices = availableServices;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mGnossCache = gnossCache;
            mlogger = logger;
            mLoggerFactory = loggerFactory;
        }
        [HttpPost]
        [Route("invalidar-caches-locales")]
        public IActionResult InvalidarCachesLocales(string pPersonaID, string login)
        {
            mGnossCache.VersionarCacheLocal(ProyectoAD.MetaProyecto);
            IdentidadCL gnossCacheCL = new IdentidadCL(_entityContext, _loggingService, _redisCacheWrapper, _configService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCL>(), mLoggerFactory);
            if (!string.IsNullOrEmpty(pPersonaID))
            { 
                string cadena = $"IdentidadActual_{pPersonaID}";
                gnossCacheCL.InvalidarCacheQueContengaCadena(cadena);
            }
            else
            {
                UsuarioCN usuarioCN = new UsuarioCN(_entityContext, _loggingService, _configService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                Guid usuarioId = usuarioCN.ObtenerFilaUsuarioPorLoginOEmail(login).UsuarioID;
                string cadena = $"IdentidadActual_{usuarioId}";
                gnossCacheCL.InvalidarCacheQueContengaCadena(cadena);
            }
                return Ok();
        }
    }
}
