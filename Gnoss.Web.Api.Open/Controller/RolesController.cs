using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models.Roles;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.MVC.Models.Administracion;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers;
using Es.Riam.Interfaces.InterfacesOpen;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gnoss.Web.Api.Open.Controller
{
    /// <summary>
    /// Use it to query / create / modify / delete roles
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class RolesController : ControlApiGnossBase
    {
        private ILogger mLogger;
        private ILoggerFactory mLoggerFactory;

        public RolesController(EntityContext entityContext, LoggingService loggingService, ConfigService configService,
            IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD,
            EntityContextBASE entityContextBASE, GnossCache gnossCache,
            IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication,
            IAvailableServices availableServices, ILogger<ControlApiGnossBase> logger, ILoggerFactory loggerFactory)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper,
                virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication, availableServices, logger, loggerFactory)
        {
            mLogger = logger;
            mLoggerFactory = loggerFactory;
        }

        #region Métodos Públicos

        /// <summary>
        /// Gets all roles available for a community, including ecosystem roles.
        /// </summary>
        /// <param name="community_short_name">Community short name (optional if community_id is provided)</param>
        /// <param name="community_id">Community ID (optional if community_short_name is provided)</param>
        /// <returns>List with roles (role ID, role name, permissions list)</returns>
        /// <example>GET roles/get-roles-community?community_short_name=mycommunity</example>
        /// <example>GET roles/get-roles-community?community_id=12345678-1234-1234-1234-123456789abc</example>
        [HttpGet, Route("get-roles-community")]
        public List<RolModelController> GetRolesComunidad(string community_short_name, Guid community_id)
        {
            try
            {
                bool tieneNombreCorto = !string.IsNullOrEmpty(community_short_name);
                bool tieneId = !community_id.Equals(Guid.Empty);

                if (!tieneNombreCorto && !tieneId)
                {
                    throw new GnossException("Debes proporcionar community_short_name o community_id", HttpStatusCode.BadRequest);
                }

                if (tieneNombreCorto && tieneId)
                {
                    throw new GnossException("Debes proporcionar solo uno de los parámetros: community_short_name o community_id, no ambos", HttpStatusCode.BadRequest);
                }

                List<RolModelController> listaRoles = new List<RolModelController>();
                ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService,
                    mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);

                Guid proyectoID;
                string nombreCorto;

                if (tieneId)
                {
                    proyectoID = community_id;
                    nombreCorto = proyectoCN.ObtenerNombreCortoProyecto(proyectoID);
                }
                else
                {
                    nombreCorto = community_short_name;
                    proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(community_short_name);
                }

                if (proyectoID == Guid.Empty)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se ha encontrado la comunidad especificada", HttpStatusCode.NotFound);
                }

                Dictionary<string, Guid> ontologiasDict = proyectoCN.ObtenerOntologiasConIDPorNombreCortoProy(nombreCorto);

                List<RolEcosistema> rolesEcosistema = proyectoCN.ObtenerRolesAdministracionEcosistema();
                foreach (RolEcosistema rolEcosistema in rolesEcosistema)
                {
                    RolModelController rolModel = new RolModelController();
                    rolModel.RolID = rolEcosistema.RolID;
                    rolModel.Nombre = rolEcosistema.Nombre;
                    rolModel.ListaPermisos = UtilPermisos.CargarListaDePermisosDeRolEcosistema(rolEcosistema.RolID, mEntityContext);

                    listaRoles.Add(rolModel);
                }

                List<Rol> roles = proyectoCN.ObtenerRolesDeProyecto(proyectoID);
                foreach (Rol rol in roles)
                {
                    RolModelController rolModel = new RolModelController();
                    rolModel.RolID = rol.RolID;
                    rolModel.Nombre = HttpUtility.HtmlDecode(rol.Nombre);
                    rolModel.ListaPermisos = UtilPermisos.CargarListaDePermisosDeRolComunidad(rol.RolID, mEntityContext);

                    listaRoles.Add(rolModel);
                }

                proyectoCN.Dispose();

                return listaRoles;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error al obtener roles de la comunidad");
                throw new GnossException($"Error interno del servidor: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}