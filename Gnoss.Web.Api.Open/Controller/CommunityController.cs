using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    /// <summary>
    /// Use it to query / create / modify / delete communities
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public partial class CommunityController : ControlApiGnossBase
    {

        public CommunityController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {

        }

        /// <summary>
        /// Gets the basic information of a community
        /// </summary>
        /// <returns>Community info</returns>
        [HttpGet, Route("get-community-information")]
        public CommunityInfoModel GetCommunity(string community_short_name = null, Guid? community_ID = null)
        {
            Guid proyID = Guid.Empty;

            try
            {
                CommunityInfoModel comunidad = new CommunityInfoModel();
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

                if (community_ID.HasValue && !community_ID.Value.Equals(Guid.Empty))
                {
                    proyID = community_ID.Value;
                }
                else
                {
                    proyID = proyCL.ObtenerProyectoIDPorNombreCorto(community_short_name);
                }

                if (!proyID.Equals(Guid.Empty))
                {
                    AD.EntityModel.Models.ProyectoDS.Proyecto filaProyecto = proyCL.ObtenerProyectoPorID(proyID).ListaProyecto[0];

                    comunidad.name = filaProyecto.Nombre;
                    comunidad.short_name = filaProyecto.NombreCorto;
                    comunidad.description = filaProyecto.Descripcion;
                    comunidad.type = ((TipoProyecto)filaProyecto.TipoProyecto).ToFriendlyString();
                    comunidad.access_type = filaProyecto.TipoAcceso;

                    TesauroCL tesCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                    DataWrapperTesauro tesDW = tesCL.ObtenerTesauroDeProyecto(proyID);
                    comunidad.categories = new List<Guid>();

                    foreach (AD.EntityModel.Models.Tesauro.CategoriaTesauro filaCategoria in tesDW.ListaCategoriaTesauro)
                    {
                        comunidad.categories.Add(filaCategoria.CategoriaTesauroID);
                    }

                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    comunidad.users = proyCN.ObtenerUsuarioIDMiembrosProyecto(proyID);
                }

                return comunidad;
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, $"community_short_name = {community_short_name} community_id = {proyID}");
                throw;
            }
        }

        /// <summary>
        /// Gets the identifier of a community by its short name
        /// </summary>
        /// <param name="community_short_name">Short name of the community</param>
        /// <returns>Returns the identifier of the community</returns>
        /// <example>GET get-community-id</example>
        [HttpGet, Route("get-community-id")]
        public Guid ObtenerProyectoIDPorNombreCorto(string community_short_name)
        {
            if (!string.IsNullOrEmpty(community_short_name))
            {
                ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid proyectoID = pry.ObtenerProyectoIDPorNombre(community_short_name);
                pry.Dispose();
                if (!proyectoID.Equals(Guid.Empty))
                {
                    return proyectoID;
                }
                else
                {
                    throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The community short name can not be empty", HttpStatusCode.BadRequest);
            }
        }

    }
}
