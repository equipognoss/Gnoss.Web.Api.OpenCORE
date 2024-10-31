using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Identidad;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Microsoft.AspNetCore.Mvc;
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
        protected GnossCache mGnossCache;
        public CacheController(GnossCache gnossCache, EntityContext entityContext, LoggingService loggingService, RedisCacheWrapper redisCacheWrapper, ConfigService configService, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication) 
        {
            _entityContext = entityContext;
            _loggingService = loggingService;
            _configService = configService;
            _redisCacheWrapper = redisCacheWrapper;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mGnossCache = gnossCache;
        }
        [HttpPost]
        [Route("invalidar-caches-locales")]
        public IActionResult InvalidarCachesLocales([FromForm] string pPersonaID)
        {
            mGnossCache.VersionarCacheLocal(ProyectoAD.MetaProyecto);
            IdentidadCL gnossCacheCL = new IdentidadCL(_entityContext, _loggingService, _redisCacheWrapper, _configService, mServicesUtilVirtuosoAndReplication);
            if (!string.IsNullOrEmpty(pPersonaID))
            { 
                string cadena = $"IdentidadActual_{pPersonaID}";
                gnossCacheCL.InvalidarCacheQueContengaCadena(cadena);
            }
            return Ok();
        }
    }
}
