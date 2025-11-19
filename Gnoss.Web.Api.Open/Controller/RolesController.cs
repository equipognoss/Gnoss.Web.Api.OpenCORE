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
using Gnoss.Web.Api.Open.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;

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
        public List<RolModelController> GetRolesCommunity(string community_short_name, Guid community_id)
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


        /// <summary>
        /// Add a new role to the community.
        /// </summary>
        /// <param name="community_short_name">Community short name (optional if community_id is provided)</param>
        /// <param name="community_id">Community ID (optional if community_short_name is provided)</param>
        /// <param name="pNombre">Role name</param>
        /// <param name="pDescripcion">Role description</param>
        /// <param name="pAmbito">Role scope</param>
        /// <param name="pPermisos">Community permissions (binary string)</param>
        /// <param name="pPermisosRecursos">Resource permissions (binary string)</param>
        /// <param name="pPermisosEcosistema">Ecosystem permissions (binary string)</param>
        /// <param name="pPermisosContenidos">Content permissions (binary string)</param>
        /// <param name="pPermisosRecursosSemanticos">Semantic resource permissions (JSON)</param>
        /// <returns>Created role information</returns>
        /// <example>POST roles/add-role-community</example>
        [HttpPost, Route("add-role-community")]
        public void AddRoleCommunity(ParamsRoleCommunity parameters)
        {
            try
            {
                // Validar que se proporcione al menos un identificador de comunidad
                bool tieneNombreCorto = !string.IsNullOrEmpty(parameters.community_short_name);
                bool tieneId = parameters.community_id.HasValue && !parameters.community_id.Value.Equals(Guid.Empty);

                if (!tieneNombreCorto && !tieneId)
                {
                    throw new GnossException("Debes proporcionar community_short_name o community_id", HttpStatusCode.BadRequest);
                }

                if (tieneNombreCorto && tieneId)
                {
                    throw new GnossException("Debes proporcionar solo uno de los parámetros: community_short_name o community_id, no ambos", HttpStatusCode.BadRequest);
                }

                // Validar nombre del rol
                if (string.IsNullOrWhiteSpace(parameters.pNombre))
                {
                    throw new GnossException("El nombre del rol es obligatorio", HttpStatusCode.BadRequest);
                }

                // Obtener el proyecto
                ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService,
                mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);

                Guid proyectoID;
                if (tieneId)
                {
                    proyectoID = parameters.community_id.Value;
                }
                else
                {
                    proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(parameters.community_short_name);
                }

                if (proyectoID == Guid.Empty)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se ha encontrado la comunidad especificada", HttpStatusCode.NotFound);
                }

                proyectoCN.Dispose();

                Guid nuevoRolID = Guid.NewGuid();
                GuardarRol(
                    nuevoRolID,
                    proyectoID,
                    parameters.pNombre,
                    parameters.pDescripcion,
                    parameters.pAmbito,
                    parameters.pPermisos,
                    parameters.pPermisosRecursos,
                    parameters.pPermisosEcosistema,
                    parameters.pPermisosContenidos,
                    parameters.pPermisosRecursosSemanticos);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error al crear rol en la comunidad");
                throw;
            }
        }

        /// <summary>
        /// Saves a role (create or update) and returns the role information
        /// </summary>
        /// <param name="pRolID">Role ID</param>
        /// <param name="pProyectoID">Project ID</param>
        /// <param name="pNombre">Role name</param>
        /// <param name="pDescripcion">Role description</param>
        /// <param name="pAmbito">Role scope</param>
        /// <param name="pPermisos">Community permissions (binary string)</param>
        /// <param name="pPermisosRecursos">Resource permissions (binary string)</param>
        /// <param name="pPermisosEcosistema">Ecosystem permissions (binary string)</param>
        /// <param name="pPermisosContenidos">Content permissions (binary string)</param>
        /// <param name="pPermisosRecursosSemanticos">Semantic resource permissions (JSON)</param>
        /// <returns>Created/updated role information</returns>
        [NonAction]
        public void GuardarRol(
            Guid pRolID,
            Guid pProyectoID,
            string pNombre,
            string pDescripcion,
            AmbitoRol pAmbito,
            PermisosDTO pPermisos,
            PermisosRecursosDTO pPermisosRecursos,
            PermisosEcosistemaDTO pPermisosEcosistema,
            PermisosContenidosDTO pPermisosContenidos,
            Dictionary<Guid, DiccionarioDePermisos> pPermisosRecursosSemanticos)
        {
            try
            {
                if (pRolID.Equals(ProyectoAD.RolAdministrador) || pRolID.Equals(ProyectoAD.RolAdministradorEcosistema))
                {
                    throw new GnossException("El rol de Administrador no puede ser modificado", HttpStatusCode.BadRequest);
                }

                if (!string.IsNullOrEmpty(pDescripcion))
                {
                    pDescripcion = UtilCadenas.EliminarHtmlDeTexto(HttpUtility.UrlDecode(pDescripcion));
                }
                ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService,
                mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);


                Rol rol = new Rol();
                if (pAmbito == AmbitoRol.Ecosistema)
                {
                    rol.ProyectoID = ProyectoAD.MetaProyecto;
                }
                else
                {
                    rol.ProyectoID = pProyectoID;
                }
                string nombreCorto = proyectoCN.ObtenerNombreCortoProyecto(pProyectoID);
                // Obtener el proyecto
                var proyecto = proyectoCN.ObtenerProyectoPorNombreCorto(nombreCorto);
                if (proyecto == null)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se ha encontrado el proyecto especificado", HttpStatusCode.NotFound);
                }

                rol.OrganizacionID = proyecto.OrganizacionID;
                rol.RolID = pRolID;
                rol.Nombre = pNombre;
                rol.Descripcion = pDescripcion;
                rol.Tipo = (short)pAmbito;
                rol.FechaModificacion = DateTime.Now;
                rol.PermisosAdministracion = pPermisos.ToUlong();
                rol.PermisosRecursos = pPermisosRecursos.ToUlong();
                rol.PermisosContenidos = pPermisosContenidos.ToUlong();
                rol.EsRolUsuario = ComprobarSiEsRolDeUsuario(pRolID, pProyectoID);


                ulong permisosSemanticos = UtilPermisos.ProcesarPermisosRecursosSemanticos(pPermisosRecursosSemanticos, pRolID, (short)pAmbito, mEntityContext);
                if (UtilPermisos.EcosistemaConPermisosSemanticos((short)pAmbito, permisosSemanticos))
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se puede crear un rol con ámbito de Ecosistema si tiene permisos relacionados con los recursos semánticos", HttpStatusCode.BadRequest);
                }

                proyectoCN.GuardarRolProyecto(rol);
                proyectoCN.Dispose();
            }
            catch (GnossException)
            {
                throw;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Ha habido un error al guardar los datos del rol");
                throw new GnossException("Ha habido un error al guardar los cambios", HttpStatusCode.InternalServerError);
            }
        }


        [NonAction]
        private bool ComprobarSiEsRolDeUsuario(Guid pRolID, Guid pIdProyecto)
        {
            bool esRolUsuario = false;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Rol rolUsuario = proyectoCN.ObtenerRolUsuario(pIdProyecto);
            if (rolUsuario != null && rolUsuario.RolID.Equals(pRolID))
            {
                esRolUsuario = true;
            }
            proyectoCN.Dispose();

            return esRolUsuario;
        }



        /// <summary>
        /// Update the role within a community.
        /// </summary>
        /// <param name="community_short_name">Community short name (optional if community_id is provided)</param>
        /// <param name="community_id">Community ID (optional if community_short_name is provided)</param>
        /// <param name="pNombre">Role name</param>
        /// <param name="pDescripcion">Role description</param>
        /// <param name="pAmbito">Role scope</param>
        /// <param name="pPermisos">Community permissions (binary string)</param>
        /// <param name="pPermisosRecursos">Resource permissions (binary string)</param>
        /// <param name="pPermisosEcosistema">Ecosystem permissions (binary string)</param>
        /// <param name="pPermisosContenidos">Content permissions (binary string)</param>
        /// <param name="pPermisosRecursosSemanticos">Semantic resource permissions (JSON)</param>
        /// <returns>Created role information</returns>
        /// <example>POST roles/set-role-community</example>
        [HttpPost, Route("set-role-community")]
        public void SetRoleCommunity(ParamsRoleCommunity parameters)
        {
            try
            {
                // Validar que se proporcione al menos un identificador de comunidad
                bool tieneNombreCorto = !string.IsNullOrEmpty(parameters?.community_short_name);
                bool tieneId = parameters?.community_id != null && parameters.community_id != Guid.Empty;


                if (!tieneNombreCorto && !tieneId)
                {
                    throw new GnossException("Debes proporcionar community_short_name o community_id", HttpStatusCode.BadRequest);
                }

                if (tieneNombreCorto && tieneId)
                {
                    throw new GnossException("Debes proporcionar solo uno de los parámetros: community_short_name o community_id, no ambos", HttpStatusCode.BadRequest);
                }

                // Validar nombre del rol
                if (string.IsNullOrWhiteSpace(parameters.pNombre))
                {
                    throw new GnossException("El nombre del rol es obligatorio", HttpStatusCode.BadRequest);
                }

                // Obtener el proyecto
                ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService,
                mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);

                Guid proyectoID;
                if (tieneId)
                {
                    proyectoID = parameters.community_id.Value;
                }
                else
                {
                    proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(parameters.community_short_name);
                }

                if (proyectoID == Guid.Empty)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se ha encontrado la comunidad especificada", HttpStatusCode.NotFound);
                }

                Guid rolID;
                rolID = parameters.rol_id.Value;
                if (rolID == Guid.Empty)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("Se debe proporcionar el id del rol", HttpStatusCode.NotFound);
                }

                List<Rol> rolesProyecto = proyectoCN.ObtenerRolesDeProyecto(proyectoID);
                Rol rolAsignar = rolesProyecto.FirstOrDefault(r => r.RolID == rolID);

                if (parameters.pAmbito == AmbitoRol.Ecosistema)
                {

                }


                if (rolAsignar == null)
                {
                    proyectoCN.Dispose();
                    mLoggingService.GuardarLogError($"El rol: {rolID} no existe en el proyecto {proyectoID}", mLogger);
                    return;
                }
                proyectoCN.Dispose();

                GuardarRol(
                    rolID,
                    proyectoID,
                    parameters.pNombre,
                    parameters.pDescripcion,
                    parameters.pAmbito,
                    parameters.pPermisos,
                    parameters.pPermisosRecursos,
                    parameters.pPermisosEcosistema,
                    parameters.pPermisosContenidos,
                    parameters.pPermisosRecursosSemanticos);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error al crear rol en la comunidad");
                throw;
            }
        }

        /// <summary>
        /// Delete a role from a community.
        /// </summary>
        /// <param name="community_id">Community ID (optional if community_short_name is provided)</param>
        /// <param name="community_short_name">Community short name (optional if community_id is provided)</param>
        /// <param name="rol_id">Role ID to delete</param>
        /// <returns></returns>
        /// <example>Delete roles/delete-role-community</example>
        [HttpDelete, Route("delete-role-community")]
        public void DeleteRoleCommunity(Guid community_id, string community_short_name, Guid rol_id)
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

                if (rol_id.Equals(Guid.Empty))
                {
                    throw new GnossException("Debes proporcionar el rol_id a eliminar", HttpStatusCode.BadRequest);
                }

                ProyectoCN proyectoCN = new ProyectoCN(
                    mEntityContext,
                    mLoggingService,
                    mConfigService,
                    mServicesUtilVirtuosoAndReplication,
                    mLoggerFactory.CreateLogger<ProyectoCN>(),
                    mLoggerFactory
                );

                Guid proyectoID;
                if (tieneId)
                {
                    proyectoID = community_id;
                }
                else
                {
                    proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(community_short_name);
                }

                if (proyectoID == Guid.Empty)
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se ha encontrado la comunidad especificada", HttpStatusCode.NotFound);
                }

                // Validar que el rol exista
                List<Rol> rolesProyecto = proyectoCN.ObtenerRolesDeProyecto(proyectoID);
                Rol rolEliminar = rolesProyecto.FirstOrDefault(r => r.RolID.Equals(rol_id));

                if (rolEliminar == null)
                {
                    proyectoCN.Dispose();
                    mLogger.LogInformation($"El rol {rol_id} no existe en la comunidad {proyectoID}");
                    return;
                }

                if (rol_id.Equals(ProyectoAD.RolAdministrador) || rol_id.Equals(ProyectoAD.RolAdministradorEcosistema))
                {
                    proyectoCN.Dispose();
                    throw new GnossException("No se puede eliminar el rol de Administrador o Administrador de Ecosistema", HttpStatusCode.BadRequest);
                }

                proyectoCN.EliminarRolDeProyecto(rolEliminar.RolID);
                proyectoCN.Dispose();

                mLogger.LogInformation($"Rol {rol_id} eliminado correctamente del proyecto {proyectoID}");
            }
            catch (GnossException)
            {
                throw;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Error al eliminar el rol {rol_id}");
                throw new GnossException($"Error interno del servidor: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }


        #endregion
    }
}
