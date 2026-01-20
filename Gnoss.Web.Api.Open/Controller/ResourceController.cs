using BeetleX.Redis.Commands;
using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.Documentacion;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models.Carga;
using Es.Riam.Gnoss.AD.EntityModel.Models.Documentacion;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.AD.Facetado.Model;
using Es.Riam.Gnoss.AD.Identidad;
using Es.Riam.Gnoss.AD.Live;
using Es.Riam.Gnoss.AD.Parametro;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.RDF.Model;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Documentacion;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.Elementos;
using Es.Riam.Gnoss.Elementos.Comentario;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Elementos.ServiciosGenerales;
using Es.Riam.Gnoss.Elementos.Suscripcion;
using Es.Riam.Gnoss.Elementos.Tesauro;
using Es.Riam.Gnoss.Logica.BASE_BD;
using Es.Riam.Gnoss.Logica.Comentario;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.Identidad;
using Es.Riam.Gnoss.Logica.MVC;
using Es.Riam.Gnoss.Logica.Parametro;
using Es.Riam.Gnoss.Logica.RDF;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Logica.Voto;
using Es.Riam.Gnoss.RabbitMQ;
using Es.Riam.Gnoss.RabbitMQ.Models;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Servicios;
using Es.Riam.Gnoss.Traducciones.TraduccionTextos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.Controles.Documentacion;
using Es.Riam.Gnoss.Web.Controles.GeneradorPlantillasOWL;
using Es.Riam.Gnoss.Web.Controles.ServicioImagenesWrapper;
using Es.Riam.Gnoss.Web.MVC.Models;
using Es.Riam.Gnoss.Web.MVC.Models.CargaMasiva;
using Es.Riam.Gnoss.Web.MVC.Models.FicherosRecursos;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Es.Riam.Interfaces.InterfacesOpen;
using Es.Riam.Semantica.OWL;
using Es.Riam.Semantica.Plantillas;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    /// <summary>
    /// Use it to query / create / modify / delete resources
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ResourceController : ControlApiGnossBase
    {
        private ILogger mlogger;
        private ILoggerFactory mLoggerFactory;
        public ResourceController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, IAvailableServices availableServices, ILogger<ResourceController> logger, ILoggerFactory loggerFactory)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication, availableServices,logger,loggerFactory)
        {
            mlogger = logger;
            mLoggerFactory = loggerFactory;
        }

        #region Miembros

        /// <summary>
        /// It indicates whether the document is uploading has assigned an identifier or create a new one
        /// </summary>
        private bool mTieneDocumentoID = false;

        /// <summary>
        /// Loading resources ontology.
        /// </summary>
        private Ontologia mOntologia;

        /// <summary>
        /// Entity and generated controls list.
        /// </summary>
        private List<ElementoOntologia> mInstanciasPrincipales = null;

        /// <summary>
        /// Identifier of the main entity of a semantic form.
        /// </summary>
        private string mIDEntidadPrincipal;

        /// <summary>
        /// Already loaded ontologies.
        /// </summary>
        public volatile static Dictionary<Guid, KeyValuePair<Guid, Ontologia>> OntologiasCargadas = new Dictionary<Guid, KeyValuePair<Guid, Ontologia>>();

        /// <summary>
        /// Returns the service URL documentation links.
        /// </summary>
        private string mUrlServicioDocumentosLink;

        /// <summary>
        /// It indicates wheter the resource must be indexed.
        /// </summary>
        public static bool mIndexarRecursos = true;

        /// <summary>
        /// List with the identifiers of entities to delete.
        /// </summary>
        private List<string> mEntidadesABorrar;
        private Dictionary<string, string> mURIEntidadesABorrar;

        /// <summary>
        /// Url de la propiedad que debe contener al menos un idioma para realizar la busqueda por el mismo.
        /// </summary>
        private string mPropiedadIdiomaBusquedaComunidad;

        /// <summary>
        /// String separator for GoogleDrive resource identifier
        /// </summary>
        public const string ID_GOOGLE = "##idgoogle##";

        private static ConcurrentDictionary<Guid, DataWrapperFacetas> mFacetaDSPorProyectoID = new ConcurrentDictionary<Guid, DataWrapperFacetas>();

        private static ConcurrentDictionary<Guid, List<string>> mFacetasExternasPorProyectoID = new ConcurrentDictionary<Guid, List<string>>();

        private static ConcurrentDictionary<Guid, List<string>> mFacetasTextoInvariablePorProyectoID = new ConcurrentDictionary<Guid, List<string>>();


        #endregion

        #region Propiedades

        /// <summary>
        /// Obtiene la url del servicio de documentacion
        /// </summary>
        public string UrlServicioDocumentosLink
        {
            get
            {

                if (mUrlServicioDocumentosLink == null)
                {
                    if (!string.IsNullOrEmpty(UrlServicioInterno))
                    {
                        mUrlServicioDocumentosLink = string.Concat(UrlServicioInterno, "ServicioDocumentosLink.asmx");
                    }
                    else
                    {
                        mUrlServicioDocumentosLink = "";
                    }
                }
                return mUrlServicioDocumentosLink;
            }
        }

        /// <summary>
        /// It indicates whether the uploaded resources should go to the BASE or not.
        /// </summary>
        public bool AgregarColaBase
        {
            get
            {
                string param = mConfigService.ObtenerColaBase();
                return (string.IsNullOrEmpty(param) || param == "1");
            }
        }

        private List<string> ListaFacetasExternas
        {
            get
            {
                if (!mFacetasExternasPorProyectoID.ContainsKey(FilaProy.ProyectoID))
                {
                    DataWrapperFacetas facetaDW = ObtenerFacetasExternasDeProyecto();

                    List<string> facetaEntidadExterna = new List<string>();
                    foreach (var facetaExterna in facetaDW.ListaFacetaEntidadesExternas)
                    {
                        if (!facetaEntidadExterna.Contains(facetaExterna.EntidadID))
                        {
                            facetaEntidadExterna.Add(facetaExterna.EntidadID);
                        }
                    }

                    mFacetasExternasPorProyectoID.TryAdd(FilaProy.ProyectoID, facetaEntidadExterna);
                }
                return mFacetasExternasPorProyectoID[FilaProy.ProyectoID];
            }
        }

        private List<string> ListaFacetasTextoInvariable
        {
            get
            {
                if (!mFacetasTextoInvariablePorProyectoID.ContainsKey(FilaProy.ProyectoID))
                {
                    DataWrapperFacetas facetaDW = ObtenerFacetasExternasDeProyecto();

                    List<string> facetasInvariables = facetaDW.ListaFacetaObjetoConocimientoProyecto.Where(item => item.TipoPropiedad.Equals((short)TipoPropiedadFaceta.TextoInvariable)).Select(item => item.Faceta).ToList();

                    mFacetasTextoInvariablePorProyectoID.TryAdd(FilaProy.ProyectoID, facetasInvariables);
                }
                return mFacetasTextoInvariablePorProyectoID[FilaProy.ProyectoID];
            }
        }

        #endregion

        #region Métodos originales Api V2

        /// <summary>
        /// Gets the number of outstanding shares of the load of ontology in a community
        /// </summary>
        /// <param name="ontology_name">Name of the ontology</param>
        /// <param name="community_short_name">Short name of the community</param>
        /// <returns>Number of outstanding shares of the load of ontology in a community</returns>
        /// <example>GET resource/get-pending-actions</example>
        [HttpGet, Route("get-pending-actions")]
        public int PendingActions(string ontology_name, string community_short_name)
        {
            if (!string.IsNullOrEmpty(community_short_name) && !string.IsNullOrEmpty(ontology_name))
            {
                int contAccionesPendientes = -1;

                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                List<Guid> lista = proyCN.ObtenerProyectoIDOrganizacionIDPorNombreCorto(community_short_name);
                proyCN.Dispose();

                Guid organizacionID = Guid.Empty;
                Guid proyectoID = Guid.Empty;

                if (lista.Count > 0)
                {
                    organizacionID = lista[0];
                    proyectoID = lista[1];
                }

                if (proyectoID != Guid.Empty && organizacionID != Guid.Empty)
                {
                    return contAccionesPendientes;
                }
                else
                {
                    throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Gets the community short name by the resource_id
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <returns>String with the community short name</returns>
        /// <example>GET resource/get-community-short-name-by-resource_id?resource_id={resource_id}</example>
        [HttpGet, Route("get-community-short-name-by-resource_id")]
        public string GetCommunityShortNameByresourceID(Guid resource_id)
        {
            string salida = string.Empty;
            //Guid documentoID = (Guid)ComprobarParametros("resource_id", true, typeof(Guid));

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            Guid proyectoID = docCN.ObtenerProyectoIDPorDocumentoID(resource_id);
            docCN.Dispose();

            if (!proyectoID.Equals(Guid.Empty))
            {
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                string nombreCortoCom = proyCN.ObtenerNombreCortoProyecto(proyectoID);
                proyCN.Dispose();

                if (!string.IsNullOrEmpty(nombreCortoCom))
                {
                    salida = nombreCortoCom;
                }
                else
                {
                    salida = "The community where the resource was published on does not exist.";
                    throw new GnossException(salida, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                salida = "The resource " + resource_id + " does not exist.";
                throw new GnossException(salida, HttpStatusCode.BadRequest);
            }

            return salida;
        }

        /// <summary>
        /// Checks whether the user has permission on the resource editing
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="user_id">User identifier</param>
        /// <param name="community_id">Community identifier</param>
        /// <returns>True if the user has editing permission on the resource. False if not.</returns>
        [HttpGet, Route("get-user-editing-permission-on-resource")]
        public bool GetUserEditingPermissionOnResource(Guid resource_id, Guid user_id, Guid community_id,string login)
        {
            if (!user_id.Equals(Guid.Empty)) 
            {
                bool puedeEditar = false;

                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    mNombreCortoComunidad = proyCN.ObtenerNombreCortoProyecto(community_id);
                    proyCN.Dispose();

                    if (!string.IsNullOrEmpty(mNombreCortoComunidad))
                    {
                        UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                        if (usuCN.ExisteUsuarioEnBD(user_id))
                        {
                            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, user_id, true);

                            if (identidad != null)
                            {
                                puedeEditar = ComprobarPermisosEdicion(documento, identidad, Proyecto);
                            }
                            else
                            {
                                throw new GnossException("The user " + user_id + " does not participate in the community: " + mNombreCortoComunidad, HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The user " + user_id + " does not exist.", HttpStatusCode.BadRequest);
                        }

                        usuCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The parameter 'community_short_name' cannot be empty.", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }

                return puedeEditar;
            }
            else
            {
                bool puedeEditar = false;

                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    mNombreCortoComunidad = proyCN.ObtenerNombreCortoProyecto(community_id);
                    proyCN.Dispose();

                    if (!string.IsNullOrEmpty(mNombreCortoComunidad))
                    {
                        UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                        Guid usuarioId = usuCN.ObtenerFilaUsuarioPorLoginOEmail(login).UsuarioID;
                        if (usuCN.ExisteUsuarioEnBD(usuarioId))
                        {
                            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, usuarioId, true);

                            if (identidad != null)
                            {
                                puedeEditar = ComprobarPermisosEdicion(documento, identidad, Proyecto);
                            }
                            else
                            {
                                throw new GnossException("The user " + usuarioId + " does not participate in the community: " + mNombreCortoComunidad, HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The user " + usuarioId + " does not exist.", HttpStatusCode.BadRequest);
                        }

                        usuCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The parameter 'community_short_name' cannot be empty.", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }

                return puedeEditar;
            }
            
        }

        /// <summary>
        /// Checks whether the user has permission on the resource editing
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="user_id">User identifier</param>
        /// <param name="community_short_name">Community short name</param>
        /// <returns>True if the user has editing permission on the resource. False if not.</returns>
        /// <example>GET resource/get-user-editing-permission-on-resource-by-community-name?resource_id={resource_id}&amp;user_id={userID}&amp;community_short_name={community_short_name}</example>
        [HttpGet, Route("get-user-editing-permission-on-resource-by-community-name")]
        public bool GetUserEditingPermissionOnResourceByCommunityName(Guid resource_id, Guid user_id, string community_short_name, string login)
        {
            bool puedeEditar = false;
            if (string.IsNullOrEmpty(community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (!user_id.Equals(Guid.Empty))
            {
                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];

                    if (!string.IsNullOrEmpty(community_short_name))
                    {
                        mNombreCortoComunidad = community_short_name;

                        UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                        if (usuCN.ExisteUsuarioEnBD(user_id))
                        {
                            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, user_id, true);

                            if (identidad != null)
                            {
                                puedeEditar = ComprobarPermisosEdicion(documento, identidad, Proyecto);
                            }
                            else
                            {
                                throw new GnossException("The user does not participate in the community: " + community_short_name, HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The user " + user_id + " does not exist.", HttpStatusCode.BadRequest);
                        }

                        usuCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The parameter 'community_short_name' cannot be empty.", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }

                return puedeEditar;
            }
            else
            {
                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];

                    if (!string.IsNullOrEmpty(community_short_name))
                    {
                        mNombreCortoComunidad = community_short_name;

                        UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                        Guid usuarioId = usuCN.ObtenerFilaUsuarioPorLoginOEmail(login).UsuarioID;
                        if (usuCN.ExisteUsuarioEnBD(usuarioId))
                        {
                            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, usuarioId, true);

                            if (identidad != null)
                            {
                                puedeEditar = ComprobarPermisosEdicion(documento, identidad, Proyecto);
                            }
                            else
                            {
                                throw new GnossException("The user does not participate in the community: " + community_short_name, HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The user " + usuarioId + " does not exist.", HttpStatusCode.BadRequest);
                        }

                        usuCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The parameter 'community_short_name' cannot be empty.", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }

                return puedeEditar;
            }
        }

        /// <summary>
        /// Gets the related resources of a resource
        /// </summary>
        /// <param name="relatedResourcesParams">Related resource params</param>
        /// <returns>List of resource identifiers</returns>
        /// <example>GET resource/get-related-resources?resource_id={resource_id}</example>
        [HttpPost, Route("get-related-resources-from-list")]
        public Dictionary<Guid, List<Guid>> GetRelatedResourcesFromList(RelatedResourcesParams relatedResourcesParams)
        {
            Dictionary<Guid, List<Guid>> listaIds = new Dictionary<Guid, List<Guid>>();

            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
            Guid proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(relatedResourcesParams.community_short_name);

            if (!proyectoID.Equals(ProyectoAD.MetaProyecto) && EsAdministradorProyecto(UsuarioOAuth, proyectoID))
            {
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                DataWrapperDocumentacion docDW = docCN.ObtenerVinculacionesRecursos(relatedResourcesParams.resource_ids);

                foreach (AD.EntityModel.Models.Documentacion.DocumentoVincDoc filaVinc in docDW.ListaDocumentoVincDoc)
                {
                    if (!listaIds.ContainsKey(filaVinc.DocumentoID))
                    {
                        listaIds.Add(filaVinc.DocumentoID, new List<Guid>());
                    }
                    if (!listaIds[filaVinc.DocumentoID].Contains(filaVinc.DocumentoVincID))
                    {
                        listaIds[filaVinc.DocumentoID].Add(filaVinc.DocumentoVincID);
                    }
                }

            }
            else
            {
                string error = "Invalid Oauth. Insufficient permissions.";
                throw new GnossException(error, HttpStatusCode.BadRequest);
            }

            return listaIds;
        }

        /// <summary>
        /// Gets the related resources of a resource
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="community_short_name">Community short name</param>
        /// <returns>List of resource identifiers</returns>
        /// <example>GET resource/get-related-resources?resource_id={resource_id}</example>
        [HttpGet, Route("get-related-resources")]
        public List<Guid> GetRelatedResources(Guid resource_id, string community_short_name)
        {
            List<Guid> listaIds = new List<Guid>();

            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
            Guid proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(community_short_name);

            if (!proyectoID.Equals(ProyectoAD.MetaProyecto) && EsAdministradorProyecto(UsuarioOAuth, proyectoID))
            {
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                DataWrapperDocumentacion docDW = docCN.ObtenerVinculacionesRecurso(resource_id);

                foreach (AD.EntityModel.Models.Documentacion.DocumentoVincDoc filaVinc in docDW.ListaDocumentoVincDoc)
                {
                    if (!listaIds.Contains(filaVinc.DocumentoVincID) && !filaVinc.DocumentoVincID.Equals(resource_id))
                    {
                        listaIds.Add(filaVinc.DocumentoVincID);
                    }
                    else if (!listaIds.Contains(filaVinc.DocumentoID) && !filaVinc.DocumentoID.Equals(resource_id))
                    {
                        listaIds.Add(filaVinc.DocumentoID);
                    }
                }
            }
            else
            {
                string error = "Invalid Oauth. Insufficient permissions.";
                throw new GnossException(error, HttpStatusCode.BadRequest);
            }

            return listaIds;
        }

        /// <summary>
        /// Gets the community short name of the communities where the resource is published or shared on
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <returns>List of strings with the community short names</returns>
        /// <example>GET resource/get-communities-resource-shared?resource_id={resource_id}</example>
        [HttpGet, Route("get-communities-resource-shared")]
        public List<string> GetCommunitiesResourceShared(Guid resource_id)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                //Guid documentoID = (Guid)ComprobarParametros("resource_id", true, typeof(Guid));
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                List<Guid> listaDocs = new List<Guid>();
                listaDocs.Add(resource_id);
                Dictionary<Guid, List<string>> dicDocumentos = docCN.ObtenerProyectosDocumentos(listaDocs);
                docCN.Dispose();

                if (dicDocumentos.Count > 0)
                {
                    return dicDocumentos[resource_id];
                }
                else
                {
                    string error = "The resource " + resource_id + " does not exist.";
                    throw new GnossException(error, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                string error = "Invalid Oauth. Insufficient permissions.";
                throw new GnossException(error, HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Gets the readers or the readers groups (group names of both community and organizational) short name of the resource
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <returns>List of strings with the short names</returns>
        /// <example>GET resource/get-resource-readers?resource_id={resource_id}</example>
        [HttpGet, Route("get-resource-readers")]
        public KeyReaders GetResourceReaders(Guid resource_id)
        {
            //List<string> lectores = new List<string>();
            //Guid documentoID = (Guid)ComprobarParametros("resource_id", true, typeof(Guid));
            KeyReaders lectores = new KeyReaders();

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(resource_id);
            DataWrapperDocumentacion docDW = docCN.ObtenerLectoresYGruposLectoresDocumentos(listaDocs);
            docCN.Dispose();

            if (docDW.ListaNombrePerfil != null && docDW.ListaNombrePerfil.Count > 0)
            {
                lectores.readers = new List<string>();
            }

            foreach (AD.EntityModel.Models.Documentacion.NombrePerfil filaNombrePerfil in docDW.ListaNombrePerfil)
            {
                string nomLector = filaNombrePerfil.NombrePerfilAtributo;

                if (!lectores.readers.Contains(nomLector))
                {
                    lectores.readers.Add(nomLector);
                }
            }

            if (docDW.ListaNombreGrupo != null && docDW.ListaNombreGrupo.Count > 0)
            {
                lectores.reader_groups = new List<ReaderGroup>();
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupo filaNombreGrupo in docDW.ListaNombreGrupo)
            {
                string grupoLector = filaNombreGrupo.NombreGrupoAtributo;
                if (lectores.reader_groups.Find(grupo => grupo.group_short_name.Equals(grupoLector)) == null)
                {
                    ReaderGroup readerGr = new ReaderGroup();
                    readerGr.group_short_name = grupoLector;
                    lectores.reader_groups.Add(readerGr);
                }
            }

            if (docDW.ListaNombreGrupoOrg != null && docDW.ListaNombreGrupoOrg.Count > 0)
            {
                lectores.reader_groups = new List<ReaderGroup>();
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupoOrg filaNombreGrupoOrg in docDW.ListaNombreGrupoOrg)
            {
                string nombreOrg = filaNombreGrupoOrg.NombreOrganizacion;
                string grupoLector = filaNombreGrupoOrg.NombreGrupo;

                if (lectores.reader_groups.Find(grupo => grupo.group_short_name.Equals(grupoLector) && grupo.organization_short_name.Equals(nombreOrg)) == null)
                {
                    ReaderGroup readerGr = new ReaderGroup();
                    readerGr.group_short_name = grupoLector;
                    readerGr.organization_short_name = nombreOrg;
                    lectores.reader_groups.Add(readerGr);
                }
            }

            return lectores;
        }

        /// <summary>
        /// Gets the visibility of the resource
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <returns><see cref="ResourceVisibility">ResourceVisibility</see> with the visibility of the resource. Posible values: 
        ///     Open. All users can view the resource: 0;
        ///     Editors. Only editors can view the resource: 1;
        ///     Community Members: Only community members can view the resource: 2;
        ///     Specific: Specific users can view the resource: 3
        /// </returns>
        /// <example>GET recurso/get-visibility?resource_id={resource_id}</example>
        [HttpGet, Route("get-visibility")]
        public Models.ResourceVisibility GetResourceVisibility(Guid resource_id)
        {
            if (!resource_id.Equals(Guid.Empty))
            {
                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];

                    if (documento.FilaDocumentoWebVinBR.PrivadoEditores)
                    {
                        if (documento.ListaGruposLectores.Count > 0 || documento.ListaPerfilesLectores.Count > 0)
                        {
                            return Models.ResourceVisibility.specific;
                        }
                        else
                        {
                            return Models.ResourceVisibility.editors;
                        }
                    }
                    else if (documento.FilaDocumento.Visibilidad.Equals((short)VisibilidadDocumento.Todos))
                    {
                        return Models.ResourceVisibility.open;
                    }
                    else if (documento.FilaDocumento.Visibilidad.Equals((short)VisibilidadDocumento.MiembrosComunidad) || documento.FilaDocumento.Visibilidad.Equals((short)VisibilidadDocumento.PrivadoMiembrosComunidad))
                    {
                        return Models.ResourceVisibility.communitymembers;
                    }
                    else
                    {
                        return Models.ResourceVisibility.specific;
                    }
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The resource_id cannot be an empty guid.", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Unshare resource of a community
        /// </summary>
        /// <param name="parameters">
        /// resource_id = Resource identifier
        /// CommunityShortName = Community short name
        /// </param>
        /// <returns>True if the resource has been unshared. False if not.</returns>
        /// <example>POST resource/unshared-community-resource?resource_id={resource_id}&amp;community_short_name={community_short_name}</example>
        [HttpPost, Route("unshared-community-resource")]
        public bool UnsharedCommunityResource(UnsharedResourceParams parameters)
        {
            bool descompartido = false;

            //Guid documentoID = (Guid)ComprobarParametros("resource_id", true, typeof(Guid));
            //community_short_name = ComprobarParametros("community_short_name", true);
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyCN.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                mNombreCortoComunidad = parameters.community_short_name;

                GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];
                    GestionProyecto gestorProyecto = new GestionProyecto(proyCN.ObtenerProyectoPorID(proyectoID), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestionProyecto>(), mLoggerFactory);
                    gestorProyecto.CargarGestor();
                    Proyecto proyecto = gestorProyecto.ListaProyectos[proyectoID];
                    Guid brProyDescompartir = proyCN.ObtenerBaseRecursosProyectoPorProyectoID(proyectoID);
                    Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
                    bool tienePermisoEdicion = documento.TienePermisosEdicionIdentidad(identidad, null, proyecto, Guid.Empty, false);

                    if (EsAdministradorProyectoMyGnoss(UsuarioOAuth) || tienePermisoEdicion)
                    {
                        if (documento.BaseRecursos.Contains(brProyDescompartir))
                        {
                            gestorDoc.DesVincularDocumentoDeBaseRecursos(parameters.resource_id, brProyDescompartir, proyectoID, identidad.Clave);
                            documento.RecargarBasesRecursos();

                            //si no hay bases de recursos se elimina el recurso Lógicamente
                            if (documento.BaseRecursos.Count == 0)
                            {
                                gestorDoc.EliminarDocumentoLogicamente(documento);
                            }

                            List<Guid> listaProyectosActualizarNumRec = new List<Guid>();
                            if (documento.FilaDocumento.Borrador == false)
                            {
                                listaProyectosActualizarNumRec.Add(proyectoID);
                                #region Actualizar cola GnossLIVE

                                int tipo;
                                switch (documento.TipoDocumentacion)
                                {
                                    case TiposDocumentacion.Debate:
                                        tipo = (int)TipoLive.Debate;
                                        break;
                                    case TiposDocumentacion.Pregunta:
                                        tipo = (int)TipoLive.Pregunta;
                                        break;
                                    default:
                                        tipo = (int)TipoLive.Recurso;
                                        break;
                                }

                                string infoExtra = null;

                                if (proyectoID == ProyectoAD.MetaProyecto)
                                {
                                    infoExtra = identidad.IdentidadPersonalMyGNOSS.PerfilID.ToString();
                                }

                                ControladorDocumentacion.ActualizarGnossLive(proyectoID, parameters.resource_id, AccionLive.Eliminado, tipo, PrioridadLive.Alta, infoExtra, mAvailableServices);
                                ControladorDocumentacion.ActualizarGnossLive(proyectoID, documento.ObtenerPublicadorEnBR(brProyDescompartir), AccionLive.RecursoAgregado, (int)TipoLive.Miembro, PrioridadLive.Alta, mAvailableServices);

                                #endregion
                            }

                            documento.FilaDocumento.FechaModificacion = DateTime.Now;
                            Guardar(listaProyectosActualizarNumRec, gestorDoc, documento);
                            ControladorDocumentacion.EstablecePrivacidadRecursoEnMetaBuscador(documento, identidad, true);

                            if (documento.TipoDocumentacion == TiposDocumentacion.Semantico && documento.FilaDocumento.Eliminado && documento.FilaDocumento.ElementoVinculadoID.HasValue)
                            {                                
                                ControladorDocumentacion.BorrarRDFDeDocumentoEliminado(parameters.resource_id, documento.FilaDocumento.ElementoVinculadoID.Value, UrlIntragnoss, false, FilaProy.ProyectoID);
                            }

                            //Enviamos al base para eliminar la búsqueda
                            ControladorDocumentacion.EliminarRecursoModeloBaseSimple(parameters.resource_id, proyectoID, (short)documento.TipoDocumentacion, mAvailableServices);

                            try
                            {
                                //anular cache del documento
                                ControladorDocumentacion.BorrarCacheControlFichaRecursos(parameters.resource_id);

                                //borrar cache recursos
                                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                                facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(FilaProy.ProyectoID, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));
                            }
                            catch (Exception)
                            {
                            }
                            descompartido = true;
                        }
                        else
                        {
                            throw new GnossException("The resource " + parameters.resource_id + " is not shared on the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
                    }

                    proyCN.Dispose();
                }
                else
                {
                    throw new GnossException("The resource " + parameters.resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The community " + parameters.community_short_name + " does not exist.", HttpStatusCode.BadRequest);
            }

            return descompartido;
        }

        #endregion

        #region Métodos migrados Api SOAP

        /// <summary>
        /// Gets the short names of resource editors and editor groups.
        /// </summary>
        /// <param name="resource_id_list">resources identifiers list</param>
        /// <returns>KeyEditors list with the short names of editors and editor groups</returns>
        /// <example>POST resource/get-editors</example>
        [HttpPost, Route("get-editors")]
        public List<KeyEditors> GetEditors(List<Guid> resource_id_list)
        {
            Dictionary<string, List<string>> dicDocsIDListaEditoresDocumentos = new Dictionary<string, List<string>>();
            List<KeyEditors> documentosIDEditores = new List<KeyEditors>();

            //resource_id_list = (List<Guid>)ComprobarParametros("resource_id_list", true, typeof(List<Guid>));
            if (resource_id_list == null || resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion dataWrapperDocumentacion = docCN.ObtenerEditoresYGruposEditoresDocumentos(resource_id_list);
            docCN.Dispose();

            foreach (AD.EntityModel.Models.Documentacion.NombrePerfil filaNombrePerfil in dataWrapperDocumentacion.ListaNombrePerfil)
            {
                string nomEditor = filaNombrePerfil.NombrePerfilAtributo;
                Guid documentoID = filaNombrePerfil.DocumentoID;

                KeyEditors editor = documentosIDEditores.Find(doc => doc.resource_id.Equals(documentoID));
                if (editor == null)
                {
                    editor = new KeyEditors();
                    editor.resource_id = documentoID;
                    editor.editors = new List<string>();
                    documentosIDEditores.Add(editor);
                }

                if (editor.editors != null && !editor.editors.Contains(nomEditor))
                {
                    editor.editors.Add(nomEditor);
                }
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupo filaNombreGrupo in dataWrapperDocumentacion.ListaNombreGrupo)
            {
                string nomGrEditor = filaNombreGrupo.NombreGrupoAtributo;
                Guid documentoID = filaNombreGrupo.DocumentoID;

                KeyEditors grupoEditor = documentosIDEditores.Find(doc => doc.resource_id.Equals(documentoID));
                if (grupoEditor == null)
                {
                    grupoEditor = new KeyEditors();
                    grupoEditor.resource_id = documentoID;
                    grupoEditor.editor_groups = new List<EditorGroup>();
                    documentosIDEditores.Add(grupoEditor);
                }

                EditorGroup grupo = null;
                if (grupoEditor.editor_groups != null)
                {
                    grupo = grupoEditor.editor_groups.Find(gr => gr.group_short_name.Equals(nomGrEditor));
                }
                else
                {
                    grupoEditor.editor_groups = new List<EditorGroup>();
                    grupo = new EditorGroup();
                    grupo.group_short_name = nomGrEditor;
                }

                if (grupo != null && !grupoEditor.editor_groups.Contains(grupo))
                {
                    grupoEditor.editor_groups.Add(grupo);
                }
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupoOrg filaNombreGrupoOrg in dataWrapperDocumentacion.ListaNombreGrupoOrg)
            {
                string nombreOrg = filaNombreGrupoOrg.NombreOrganizacion;
                string nomGrEditor = filaNombreGrupoOrg.NombreGrupo;
                Guid documentoID = filaNombreGrupoOrg.DocumentoID;

                KeyEditors grupoOrgEditor = documentosIDEditores.Find(doc => doc.resource_id.Equals(documentoID));

                if (grupoOrgEditor == null)
                {
                    grupoOrgEditor = new KeyEditors();
                    grupoOrgEditor.resource_id = documentoID;
                    grupoOrgEditor.editor_groups = new List<EditorGroup>();
                    documentosIDEditores.Add(grupoOrgEditor);
                }

                EditorGroup grupoOrg = null;
                if (grupoOrgEditor.editor_groups != null)
                {
                    grupoOrg = grupoOrgEditor.editor_groups.Find(gr => !string.IsNullOrEmpty(gr.organization_short_name) && gr.organization_short_name.Equals(nombreOrg) && !string.IsNullOrEmpty(gr.group_short_name) && gr.group_short_name.Equals(nomGrEditor));
                }
                else
                {
                    grupoOrgEditor.editor_groups = new List<EditorGroup>();
                }

                if (grupoOrg == null)
                {
                    grupoOrg = new EditorGroup();
                    grupoOrg.group_short_name = nomGrEditor;
                    grupoOrg.organization_short_name = nombreOrg;
                }

                if (grupoOrgEditor != null && !grupoOrgEditor.editor_groups.Contains(grupoOrg))
                {
                    grupoOrgEditor.editor_groups.Add(grupoOrg);
                }
            }

            return documentosIDEditores;
        }

        /// <summary>
        /// Gets the resources download urls
        /// </summary>
        /// <param name="parameters">
        /// resource_id_list = resources identifiers list
        /// community_short_name = community short name
        /// </param>
        /// <returns>Resource.RequestGetDownloadUrl list with the existent resources download urls</returns>
        /// <example>POST resource/get-download-url</example>
        [HttpPost, Route("get-download-url")]
        public ResponseGetUrl[] GetDownloadUrl(GetDownloadUrlParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.resource_id_list == null || parameters.resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;
            List<ResponseGetUrl> listaUrlsDocumentos = new List<ResponseGetUrl>();

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            gestorDocumental.DataWrapperDocumentacion.ListaDocumento = gestorDocumental.DataWrapperDocumentacion.ListaDocumento.Union(docCN.ObtenerDocumentosPorIDSoloDocumento(parameters.resource_id_list)).ToList();
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

            foreach (Guid documentoID in parameters.resource_id_list)
            {
                string urlDocumento = string.Empty;

                if (gestorDocumental.ListaDocumentos.ContainsKey(documentoID))
                {
                    Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[documentoID];
                    if (documento.TipoDocumentacion == TiposDocumentacion.FicheroServidor || documento.TipoDocumentacion == TiposDocumentacion.Imagen)
                    {
                        string proyectoID = "";
                        string tipo = ControladorDocumentacion.ObtenerTipoEntidadAdjuntarDocumento(documento.TipoEntidadVinculada);
                        string extension = System.IO.Path.GetExtension(documento.NombreDocumento).ToLower();

                        if (documento.FilaDocumento.ProyectoID.HasValue)
                        {
                            proyectoID = "&proy=" + documento.FilaDocumento.ProyectoID.Value.ToString();
                        }
                        urlDocumento = FilaProy.URLPropia + "/VisualizarDocumento.aspx?tipo=" + tipo + "&org=" + documento.FilaDocumento.OrganizacionID + proyectoID + "&doc=" + documento.Clave + "&nombre=" + HttpUtility.UrlEncode(documento.NombreDocumento) + "&ext=" + extension + "&ID=" + identidad.Clave;

                        ResponseGetUrl claveValor = new ResponseGetUrl();
                        claveValor.resource_id = documentoID;
                        claveValor.url = urlDocumento;

                        listaUrlsDocumentos.Add(claveValor);
                    }
                }
            }

            return listaUrlsDocumentos.ToArray();
        }

        /// <summary>
        /// Gets the resources urls in the indicated language
        /// </summary>
        /// <param name="parameters">
        /// resource_id_list = resources identifiers Guid list 
        /// community_short_name = community short name string
        /// language = language code string
        /// </param>
        /// <returns>Resource.ResponseGetUrl list with the existent resources urls</returns>
        /// <example>POST resource/get-url</example>
        [HttpPost, Route("get-url")]
        public List<ResponseGetUrl> GetUrl(GetUrlParams parameters)
        {
            List<ResponseGetUrl> listaUrlsDocumentos = new List<ResponseGetUrl>();

            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(parameters.language))
            {
                throw new GnossException("The parameter 'language' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.language.Length > 2)
            {
                throw new GnossException("The parameter 'language' has a wrong format.", HttpStatusCode.BadRequest);
            }

            if (parameters.resource_id_list == null || parameters.resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            gestorDocumental.DataWrapperDocumentacion.ListaDocumento = gestorDocumental.DataWrapperDocumentacion.ListaDocumento.Union(docCN.ObtenerDocumentosPorIDSoloDocumento(parameters.resource_id_list)).ToList();
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            foreach (Guid documentoID in parameters.resource_id_list)
            {
                string urlDocumento = string.Empty;

                if (gestorDocumental.ListaDocumentos.ContainsKey(documentoID))
                {
                    UtilIdiomas = new UtilIdiomas(parameters.language, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mLoggerFactory.CreateLogger<UtilIdiomas>(), mLoggerFactory);
                    Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[documentoID];
                    urlDocumento = mControladorBase.UrlsSemanticas.GetURLBaseRecursosFicha(UrlIntragnoss, UtilIdiomas, parameters.community_short_name, null, documento, false);
                    ResponseGetUrl claveValor = new ResponseGetUrl();
                    claveValor.resource_id = documentoID;
                    claveValor.url = urlDocumento;

                    listaUrlsDocumentos.Add(claveValor);
                }
            }

            return listaUrlsDocumentos;
        }

        /// <summary>
        /// Sets the readers of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// visibility {0=open, 1=editors, 2=communitymembers, 3=specific} short that specifies the resource visibility. 3 if pass a readers_list
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can read the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/set-readers</example>
        [HttpPost, Route("set-readers")]
        public void SetReaders(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.readers_list != null && parameters.readers_list.Count > 0)
            {
                if (!string.IsNullOrEmpty(parameters.readers_list[0].user_short_name) && !string.IsNullOrEmpty(parameters.readers_list[0].organization_short_name))
                {
                    throw new GnossException("You can't send organization short name if you send -user short name", HttpStatusCode.BadRequest);
                }
                else if (!string.IsNullOrEmpty(parameters.readers_list[0].organization_short_name) && string.IsNullOrEmpty(parameters.readers_list[0].group_short_name))
                {
                    throw new GnossException("You should send group short name if you send organization short name", HttpStatusCode.BadRequest);
                }
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                if (parameters.visibility.Equals((short)Models.ResourceVisibility.open) || parameters.visibility.Equals((short)Models.ResourceVisibility.editors) || parameters.visibility.Equals((short)Models.ResourceVisibility.communitymembers) || (parameters.visibility.Equals((short)Models.ResourceVisibility.specific) && parameters.readers_list.Count > 0))
                {
                    EstablecerPrivacidadRecurso(documento, parameters.visibility, true);
                    ConfigurarLectores(documento, parameters.readers_list, parameters.visibility);

                    List<Guid> listaProyectosActualNumRec = new List<Guid>();
                    foreach (Guid baseRecurso in documento.BaseRecursos)
                    {
                        Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                        listaProyectosActualNumRec.Add(proyectoID);
                    }

                    Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                    #region Actualizamos la cache
                    try
                    {
                        //Borrar la caché de la ficha del recurso
                        DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                        documentacionCL.Dispose();

                        DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                        docCL.Dispose();
                    }
                    catch (Exception) { }
                    #endregion

                    #region Live

                    int tipo;
                    switch (documento.TipoDocumentacion)
                    {
                        case TiposDocumentacion.Debate:
                            tipo = (int)TipoLive.Debate;
                            break;
                        case TiposDocumentacion.Pregunta:
                            tipo = (int)TipoLive.Pregunta;
                            break;
                        default:
                            tipo = (int)TipoLive.Recurso;
                            break;
                    }

                    if (AgregarColaLive || parameters.publish_home)
                    {
                        ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, documento.Clave, AccionLive.Editado, tipo, false, "base", PrioridadLive.Baja, Constantes.PRIVACIDAD_CAMBIADA, mAvailableServices);
                        GuardarLogTiempos("Tras Actualizar Live");
                    }

                    #endregion

                    ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                    controDoc.mActualizarTodosProyectosCompartido = true;
                    controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
                }
            }
        }

        /// <summary>
        /// Set the readers of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// visibility {0=open, 1=editors, 2=communitymembers, 3=specific} short that specifies the resource visibility. 3 if pass a readers_list
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can read the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/add-readers</example>
        [HttpPost, Route("add-readers")]
        public void AddReaders(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                bool privadoEditores = documento.FilaDocumentoWebVinBR.PrivadoEditores;

                if (privadoEditores)
                {

                    ConfigurarLectoresAdd(documento, parameters.readers_list);

                    List<Guid> listaProyectosActualNumRec = new List<Guid>();
                    foreach (Guid baseRecurso in documento.BaseRecursos)
                    {
                        Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                        listaProyectosActualNumRec.Add(proyectoID);
                    }

                    Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                    #region Actualizamos la cache
                    try
                    {
                        //Borrar la caché de la ficha del recurso
                        DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                        documentacionCL.Dispose();

                        DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                        docCL.Dispose();
                    }
                    catch (Exception) { }
                    #endregion


                    ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                    controDoc.mActualizarTodosProyectosCompartido = true;
                    controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
                }
            }
        }


        /// <summary>
        /// Sets the readers of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// visibility {0=open, 1=editors, 2=communitymembers, 3=specific} short that specifies the resource visibility. 3 if pass a readers_list
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can read the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/add-readers</example>
        [HttpPost, Route("remove-readers")]
        public void RemoveReaders(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                bool privadoEditores = documento.FilaDocumentoWebVinBR.PrivadoEditores;

                if (privadoEditores)
                {
                    ConfigurarLectoresRemove(documento, parameters.readers_list);

                    List<Guid> listaProyectosActualNumRec = new List<Guid>();
                    foreach (Guid baseRecurso in documento.BaseRecursos)
                    {
                        Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                        listaProyectosActualNumRec.Add(proyectoID);
                    }

                    Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                    #region Actualizamos la cache
                    try
                    {
                        //Borrar la caché de la ficha del recurso
                        DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                        documentacionCL.Dispose();

                        DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                        docCL.Dispose();
                    }
                    catch (Exception) { }
                    #endregion


                    ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                    controDoc.mActualizarTodosProyectosCompartido = true;
                    controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
                }
            }
        }

        /// <summary>
        /// Sets the editors of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can edit the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/set-editors</example>
        [HttpPost, Route("set-editors")]
        public void SetEditors(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                EstablecerPrivacidadRecurso(documento, parameters.visibility, true);
                ConfigurarEditores(documento, parameters.readers_list);

                List<Guid> listaProyectosActualNumRec = new List<Guid>();
                foreach (Guid baseRecurso in documento.BaseRecursos)
                {
                    Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                    listaProyectosActualNumRec.Add(proyectoID);
                }

                Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                #region Actualizamos la cache
                try
                {
                    //Borrar la caché de la ficha del recurso
                    DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                    documentacionCL.Dispose();

                    DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                    docCL.Dispose();
                }
                catch (Exception) { }
                #endregion

                #region Live

                int tipo;
                switch (documento.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                if (AgregarColaLive || parameters.publish_home)
                {
                    ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, documento.Clave, AccionLive.Editado, tipo, "base", PrioridadLive.Baja, mAvailableServices);
                    GuardarLogTiempos("Tras Actualizar Live");
                }

                #endregion

                ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                controDoc.mActualizarTodosProyectosCompartido = true;
                controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
            }
        }

        /// <summary>
        /// Sets the editors of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can edit the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/add-editors</example>
        [HttpPost, Route("add-editors")]
        public void AddEditors(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                ConfigurarEditoresAdd(documento, parameters.readers_list);

                List<Guid> listaProyectosActualNumRec = new List<Guid>();
                foreach (Guid baseRecurso in documento.BaseRecursos)
                {
                    Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                    listaProyectosActualNumRec.Add(proyectoID);
                }

                Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                #region Actualizamos la cache
                try
                {
                    //Borrar la caché de la ficha del recurso
                    DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                    documentacionCL.Dispose();

                    DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                    docCL.Dispose();
                }
                catch (Exception) { }
                #endregion

                #region Live

                int tipo;
                switch (documento.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }


                #endregion

                ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                controDoc.mActualizarTodosProyectosCompartido = true;
                controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
            }
        }

        /// <summary>
        /// Sets the editors of the resuorce
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier Guid
        /// community_short_name = community short name string
        /// ReaderEditor list readers_list = list with the short names of users, community or organization groups, that can edit the resource
        /// {
        ///     user_short_name = user short name string
        ///     group_short_name = group short name string
        ///     organization_short_name = organization short name string
        /// }
        /// publish_home = indicates whether the home must be updated
        /// </param>
        /// <example>POST resource/add-editors</example>
        [HttpPost, Route("remove-editors")]
        public void RemoveEditors(SetReadersEditorsParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = CargarGestorDocumental(FilaProy);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDocumental.DataWrapperDocumentacion, true, true, null);
            gestorDocumental.CargarDocumentos(false);
            docCN.Dispose();

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }
            else
            {
                Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];
                Identidad identidad = CargarIdentidad(gestorDocumental, FilaProy, UsuarioOAuth, true);

                if (!EsAdministradorProyectoMyGnoss(UsuarioOAuth) && !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
                }

                ConfigurarEditoresRemove(documento, parameters.readers_list);

                List<Guid> listaProyectosActualNumRec = new List<Guid>();
                foreach (Guid baseRecurso in documento.BaseRecursos)
                {
                    Guid proyectoID = gestorDocumental.ObtenerProyectoID(baseRecurso);
                    listaProyectosActualNumRec.Add(proyectoID);
                }

                Guardar(listaProyectosActualNumRec, gestorDocumental, documento);

                #region Actualizamos la cache
                try
                {
                    //Borrar la caché de la ficha del recurso
                    DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    documentacionCL.BorrarControlFichaRecursos(documento.Clave);
                    documentacionCL.Dispose();

                    DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                    docCL.Dispose();
                }
                catch (Exception) { }
                #endregion

                #region Live

                int tipo;
                switch (documento.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                #endregion

                ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                controDoc.mActualizarTodosProyectosCompartido = true;
                controDoc.GuardarTriplesLectoresEditores(documento, Proyecto, mAvailableServices);
            }
        }

        /// <summary>
        /// Gets the email of the resources creators
        /// </summary>
        /// <param name="parameters">
        /// resource_id_list = resources identifiers list
        /// community_short_name = community short name
        /// </param>
        /// <returns>Dictionary with resource identifiers and email of the creators</returns>
        /// <example>POST resource/get-creator-email</example>
        [HttpPost, Route("get-creator-email")]
        public Dictionary<Guid, string> GetCreatorEmail(GetDownloadUrlParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.resource_id_list == null || parameters.resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            Dictionary<Guid, string> dicCreadores = docCN.ObtenerEmailCreadoresDocumentosID(parameters.resource_id_list);
            docCN.Dispose();

            return dicCreadores;
        }

        /// <summary>
        /// Gets the categories of the resources
        /// </summary>
        /// <param name="parameters">
        /// resource_id_list = resources identifiers list
        /// community_short_name = community short name
        /// </param>
        /// <returns>ResponseGetCategories list with the categories of the resources</returns>
        /// <example>POST resource/get-categories</example>
        [HttpPost, Route("get-categories")]
        public List<ResponseGetCategories> GetCategories(GetDownloadUrlParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The parameter 'community_short_name' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.resource_id_list == null || parameters.resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            List<ResponseGetCategories> listaDocsCats = new List<ResponseGetCategories>();

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion docDW = new DataWrapperDocumentacion();
            docCN.ObtenerDocumentosWebPorIDWEB(parameters.resource_id_list, docDW);
            Dictionary<Guid, List<string>> diccionario = new Dictionary<Guid, List<string>>();

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            List<Guid> orgIDproyID = proyCN.ObtenerProyectoIDOrganizacionIDPorNombreCorto(parameters.community_short_name);
            Guid proyectoID = Guid.Empty;

            if (orgIDproyID != null)
            {
                proyectoID = orgIDproyID[1];
            }
            else
            {
                throw new GnossException("The community short name '" + parameters.community_short_name + "' does not exist.", HttpStatusCode.BadRequest);
            }

            TesauroCL tesCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCL>(), mLoggerFactory);
            DataWrapperTesauro tesDW = tesCL.ObtenerTesauroDeProyecto(proyectoID);
            tesCL.Dispose();
            Guid baseRecursosID = docCN.ObtenerBaseRecursosIDProyecto(proyectoID);
            docCN.Dispose();

            GestionTesauro gestorTesauro = new GestionTesauro(tesDW, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestionTesauro>(), mLoggerFactory);

            foreach (AD.EntityModel.Models.Documentacion.DocumentoWebAgCatTesauro fila in docDW.ListaDocumentoWebAgCatTesauro.Where(baseRec => baseRec.BaseRecursosID.Equals(baseRecursosID)).ToList())
            {
                ResponseGetCategories docCatsObj = listaDocsCats.Find(doc => doc.resource_id.Equals(fila.DocumentoID));
                if (docCatsObj == null)
                {
                    docCatsObj = new ResponseGetCategories();
                    docCatsObj.resource_id = fila.DocumentoID;
                    docCatsObj.category_id_list = new List<ThesaurusCategory>();
                    listaDocsCats.Add(docCatsObj);
                }

                if (docCatsObj.category_id_list != null)
                {
                    ThesaurusCategory categoria = docCatsObj.category_id_list.Find(cat => cat.category_id.Equals(fila.CategoriaTesauroID));
                    if (categoria == null)
                    {
                        string nomCategoria = tesDW.ListaCategoriaTesauro.FirstOrDefault(catTes => catTes.TesauroID.Equals(fila.TesauroID) && catTes.CategoriaTesauroID.Equals(fila.CategoriaTesauroID)).Nombre;
                        categoria = new ThesaurusCategory();
                        categoria.category_id = fila.CategoriaTesauroID;

                        if (gestorTesauro.ListaCategoriasTesauro[fila.CategoriaTesauroID].Padre is CategoriaTesauro)
                        {
                            CategoriaTesauro categoriaPadre = (CategoriaTesauro)gestorTesauro.ListaCategoriasTesauro[fila.CategoriaTesauroID].Padre;
                            categoria.parent_category_id = categoriaPadre.Clave;
                        }

                        categoria.category_name = nomCategoria;
                        docCatsObj.category_id_list.Add(categoria);
                    }
                }
            }

            return listaDocsCats;
        }

        /// <summary>
        /// Gets the tags of the resources
        /// </summary>
        /// <param name="parameters">
        /// resource_id_list = resources identifiers list
        /// </param>
        /// <returns>ResponseGetTags list with the tags of the resources</returns>
        /// <example>POST resource/get-tags</example>
        [HttpPost, Route("get-tags")]
        public List<ResponseGetTags> GetTags(List<Guid> resource_id_list)
        {
            if (resource_id_list == null || resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            List<ResponseGetTags> listaEtiquetas = new List<ResponseGetTags>();
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion docDW = new DataWrapperDocumentacion();
            docDW.ListaDocumento = docDW.ListaDocumento.Union(docCN.ObtenerDocumentosPorIDSoloDocumento(resource_id_list)).ToList();
            GestorDocumental gestorDoc = new GestorDocumental(docDW, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
            docCN.Dispose();

            foreach (Guid docID in gestorDoc.ListaDocumentos.Keys)
            {
                ResponseGetTags docIDEtiquetas = new ResponseGetTags();
                docIDEtiquetas.resource_id = docID;

                string docTags = gestorDoc.ListaDocumentos[docID].Tags;
                if (string.IsNullOrEmpty(docTags))
                {
                    docIDEtiquetas.tags = new List<string>();
                }
                else
                {
                    docIDEtiquetas.tags = new List<string>();
                    foreach (string tag in docTags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        docIDEtiquetas.tags.Add(tag.Trim());
                    }
                }

                listaEtiquetas.Add(docIDEtiquetas);
            }

            gestorDoc.Dispose();
            return listaEtiquetas;
        }

        /// <summary>
        /// Gets the main image of the resources
        /// </summary>
        /// <param name="resource_id_list">List of resources identificators</param>
        /// <returns>List of ResponseGetMainImage with the path of the main image of the resources and their available sizes</returns>
        /// <example>POST resource/get-main-image</example>
        [HttpPost, Route("get-main-image")]
        public List<ResponseGetMainImage> GetMainImage(List<Guid> resource_id_list)
        {
            List<ResponseGetMainImage> respuesta = new List<ResponseGetMainImage>();

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            string[] listaDocsImagenes = docCN.ObtenerImagenesPrincipalesDocumentos(resource_id_list);
            docCN.Dispose();

            //NombreCategoriaDoc de la tabla documento
            foreach (string nombreCategoriaDoc in listaDocsImagenes)
            {
                string[] nombres = nombreCategoriaDoc.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                Guid docID = Guid.Empty;

                if (Guid.TryParse(nombres[0], out docID))
                {
                    ResponseGetMainImage imagen = new ResponseGetMainImage();
                    imagen.resource_id = docID;
                    imagen.sizes = new List<short>();

                    string[] tamañosYRuta = nombres[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string tamaño in tamañosYRuta)
                    {
                        short imgSize = -1;
                        if (short.TryParse(tamaño, out imgSize))
                        {
                            //es una parte de los tamaños
                            imagen.sizes.Add(imgSize);
                        }
                        else
                        {
                            //es la parte de la ruta
                            imagen.path = tamaño;
                        }
                    }
                    respuesta.Add(imagen);
                }
            }

            return respuesta;
        }

        /// <summary>
        /// Gets the rdf of the resource with a complex semanthic
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <returns>String with the rdf of the resource</returns>
        /// <example>GET resource/get-rdf?resource_id={resource_id}</example>
        [HttpGet, Route("get-rdf")]
        public ActionResult GetRDF(Guid resource_id)
        {

            string response = ObtenerRdfRecurso(resource_id);

            return Content(response, "text/html");
        }

        private string ObtenerRdfRecurso(Guid resource_id)
        {
            string rdfTexto = string.Empty;
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion dataWrapperDocumentacion = docCN.ObtenerDocumentoPorID(resource_id);
            string NamespaceOntologia = null;
            string UrlOntologia = null;

            if (dataWrapperDocumentacion != null && dataWrapperDocumentacion.ListaDocumento.Count > 0)
            {
                GestorDocumental gestorDocumental = new GestorDocumental(dataWrapperDocumentacion, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                Ontologia ontologia = null;

                if (gestorDocumental.ListaDocumentos.ContainsKey(resource_id))
                {
                    Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[resource_id];

                    if (!documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                    {
                        throw new GnossException("The resources with a basic semanthic has no Rdf description.", HttpStatusCode.BadRequest);
                    }

                    Guid ontrologiaID = Guid.Empty;
                    if (documento.FilaDocumento.ElementoVinculadoID.HasValue)
                    {
                        ontrologiaID = documento.FilaDocumento.ElementoVinculadoID.Value;
                    }
                    dataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(ontrologiaID));
                    gestorDocumental.CargarDocumentos(false);
                    docCN.Dispose();

                    //carga de la ontología
                    Dictionary<string, List<EstiloPlantilla>> mListaEstilos = new Dictionary<string, List<EstiloPlantilla>>();
                    byte[] arrayOnto = ControladorDocumentacion.ObtenerOntologia(ontrologiaID, out mListaEstilos, documento.FilaDocumento.ProyectoID.Value);

                    if (arrayOnto != null)
                    {
                        ontologia = new Ontologia(arrayOnto, true);
                        ontologia.EstilosPlantilla = mListaEstilos;

                        try
                        {
                            ontologia.LeerOntologia();
                        }
                        catch (Exception ex)
                        {
                            throw new GnossException("The ontology " + ontrologiaID + " is incorrect.", HttpStatusCode.BadRequest);
                        }
                    }

                    if (ontrologiaID != Guid.Empty)
                    {
                        string nombreOntologia = gestorDocumental.ListaDocumentos[ontrologiaID].FilaDocumento.Enlace;
                        NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
                        UrlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";
                        gestorDocumental.RdfDS = ControladorDocumentacion.ObtenerRDFDeBDRDF(resource_id, documento.FilaDocumento.ProyectoID.Value);

                        if (gestorDocumental.RdfDS.RdfDocumento.Count > 0)
                        {
                            rdfTexto = documento.RdfSemantico;

                            if (!rdfTexto.Contains(NamespaceOntologia + ":"))
                            {
                                try
                                {
                                    string namespaceGuardado = rdfTexto.Substring(0, rdfTexto.IndexOf("=\"" + UrlOntologia + "\""));
                                    namespaceGuardado = namespaceGuardado.Substring(namespaceGuardado.LastIndexOf("xmlns:") + 6);
                                    rdfTexto = rdfTexto.Replace("xmlns:" + namespaceGuardado + "=", "xmlns:" + NamespaceOntologia + "=");
                                    rdfTexto = rdfTexto.Replace(namespaceGuardado + ":", NamespaceOntologia + ":");
                                }
                                catch (Exception)
                                {//Que vaya a virtuoso
                                    rdfTexto = null;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(rdfTexto))
                        {
                            if (gestorDocumental.RdfDS.RdfDocumento.Count == 0)
                            {
                                gestorDocumental.RdfDS = null;
                            }

                            if (gestorDocumental.ListaDocumentos.ContainsKey(ontrologiaID))
                            {
                                MemoryStream buffer = new MemoryStream(ObtenerRDFDeVirtuosoControlCheckpoint(resource_id, gestorDocumental.ListaDocumentos[ontrologiaID].Enlace, UrlOntologia, NamespaceOntologia, ontologia, false));

                                if (buffer == null)
                                {
                                    throw new GnossException("The resource " + documento.Clave + " has no data at Virtuoso.", HttpStatusCode.BadRequest);
                                }

                                StreamReader reader = new StreamReader(buffer);
                                rdfTexto = reader.ReadToEnd();
                                reader.Close();
                                reader.Dispose();
                            }
                        }
                        else
                        {
                            return rdfTexto;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(rdfTexto))
                {
                    //se reescribe el rdf para asegurar el formato adecuado
                    GestionOWL gestorOWL = new GestionOWL();
                    gestorOWL.UrlOntologia = UrlOntologia;
                    gestorOWL.NamespaceOntologia = NamespaceOntologia;
                    GestionOWL.FicheroConfiguracionBD = "acid";
                    GestionOWL.URLIntragnoss = UrlIntragnoss;
                    List<ElementoOntologia> instanciasPrincipales = gestorOWL.LeerFicheroRDF(ontologia, rdfTexto, true);

                    //Guardado temporal del RDF:
                    //string nombreTemporal = Path.GetRandomFileName() + ".rdf";
                    //string rutaGuardar = Path.GetTempPath() + nombreTemporal;
                    Stream stream = gestorOWL.PasarOWL(null, ontologia, instanciasPrincipales, null, null);
                    stream.Position = 0; //al escribir el stream se queda en la última posición
                    rdfTexto = new StreamReader(stream).ReadToEnd();

                    //Lee fichero temporal
                    //rdfTexto = File.ReadAllText(rutaGuardar);

                    //Borrado del fichero temporal
                    //FileInfo fichRDF = new FileInfo(rutaGuardar);
                    //fichRDF.Delete();
                    stream = null;
                }

                gestorDocumental.Dispose();
            }
            else
            {
                throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
            }

            return rdfTexto;
        }

        /// <summary>
        /// Inserts the value in the graph
        /// </summary>
        /// <param name="parameters">
        /// graph = Graph identifier
        /// value = Value to insert in the graph
        /// </param>
        /// <example>POST resource/insert-attribute</example>
        [HttpPost, Route("insert-attribute")]
        public void InsertAttribute(InsertAttributeParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.graph) || string.IsNullOrEmpty(parameters.value))
            {
                throw new GnossException("Neither the graph nor the value can be null or empty", HttpStatusCode.BadRequest);
            }

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            facetadoCN.InsertarValorGrafo(parameters.graph.ToLower(), parameters.value, (short)PrioridadBase.ApiRecursos);
            facetadoCN.Dispose();
        }

		/// <summary>
		/// Gets the list of supported languages in the platform in BCP 47 format
		/// </summary>
		/// <returns>List of supported languages</returns>
		[HttpGet, Route("get-translation-languages")]
        public List<string> GetTranslationLanguages()
        {
            try
            {
				TranslationConfig config = UtilTraducciones.CrearTranslationConfig(mConfigService);
                ITranslationStrategy strategy = new TranslationStrategyFactory().CreateTranslationStrategy(config, TranslationProvider.Scia);
                TranslationService service = new TranslationService(strategy);
                LanguagesResponse response = service.GetAvailableLanguages();

                if (!response.Success)
                {
					mLoggingService.GuardarLogError(response.ErrorMessage, mlogger);
                    throw new GnossException($"Error attempting to get the list of languages: {response.ErrorMessage}", HttpStatusCode.InternalServerError);
                }

				return response.AvailableLanguajes;
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                throw new GnossException("There has been a problem with the translation service or it is not installed.", HttpStatusCode.ServiceUnavailable);
            }
        }

		/// <summary>
		/// Initiates an asynchronous translation process of the resource to the selected languages from the original language
		/// </summary>
		/// <param name="parameters">Resource identifier, Original language of the resource, List of language codes in BCP 47 format that the resource will be translated to, Community short name</param>
		/// <returns>Identifier of the async translation progress</returns>
		[HttpPost, Route("translate-resource")]
        public Guid TranslateResource(TranslateResourceParams parameters)
        {
            if (parameters.resource_id.Equals(Guid.Empty) || string.IsNullOrEmpty(parameters.original_language) || parameters.target_languages == null || parameters.target_languages.Count == 0 || string.IsNullOrEmpty(parameters.community_short_name))
            {
                throw new GnossException("The required parameters can not be empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;
            if (!ComprobarTienePermisoEdicionRecurso(parameters.resource_id))
            {
                throw new GnossException("The OAuth user does not have edit permissions on the resource.", HttpStatusCode.Unauthorized);
            }

			List<string> supportedLanguages = null;
			try
			{
				supportedLanguages = GetTranslationLanguages();
			}
			catch (Exception ex)
			{
				mLoggingService.GuardarLogError(ex, mlogger);
				throw new GnossException("There has been a problem with the translation service or it is not installed.", HttpStatusCode.ServiceUnavailable);
			}

            string error = UtilTraducciones.ComprobarIdiomasDisponibles(parameters.target_languages, supportedLanguages, mLoggingService, mlogger);
            if (!string.IsNullOrEmpty(error))
            {
				throw new GnossException($"Languages '{error}' are not supported. To check the list of supported languages use 'GetTranslationLanguages'", HttpStatusCode.BadRequest);
			}

			Guid translationID = Guid.NewGuid();
            try
            {
				if (mAvailableServices.CheckIfServiceIsAvailable(mAvailableServices.GetBackServiceCode(BackgroundService.TranslateService), ServiceType.Background))
				{
					TranslationRabbitModel translationModel = new TranslationRabbitModel
					{
						TranslationID = translationID,
						ResourceID = parameters.resource_id,
						PublishDate = DateTime.Now,
						OriginalLanguage = parameters.original_language,
						TargetLanguages = parameters.target_languages,
						UserID = UsuarioOAuth
					};

					using (RabbitMQClient rabbitMQ = new RabbitMQClient(RabbitMQClient.BD_SERVICIOS_WIN, "gnoss.translations.translation.exchange", mLoggingService, mConfigService, mLoggerFactory.CreateLogger<RabbitMQClient>(), mLoggerFactory, "gnoss.translations.translation.exchange", "topic"))
					{
						rabbitMQ.AgregarElementoAColaConReintentosExchange(JsonConvert.SerializeObject(translationModel));
					}
				}
			}
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                throw new GnossException($"There has been a problem with the translation service", HttpStatusCode.InternalServerError);
            }			
			
			return translationID;
		}

        /// <summary>
        /// Link a resource list to other resource
        /// </summary>
        /// <param name="parameters">
        /// resourceId = Resource to be linked by the resource list = Graph identifier
        /// community_short_name = Community short name
        /// resource_list_to_link = List of resources to link
        /// </param>
        /// <example>POST resource/link-resource</example>
        [HttpPost, Route("link-resource")]
        public void LinkResource(LinkedParams parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            //identidad
            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

            if (identidad == null)
            {
                throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion dataWrapperDocumentacion = new DataWrapperDocumentacion();
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, dataWrapperDocumentacion, true, false, null);
            dataWrapperDocumentacion.Merge(docCN.ObtenerVinculacionesRecurso(parameters.resource_id));
            docCN.Dispose();

            if (parameters.resoruce_list_to_link.Contains(parameters.resource_id))
            {
                throw new GnossException("The reource cannot be linked to itself.", HttpStatusCode.BadRequest);
            }
            else if (dataWrapperDocumentacion.ListaDocumento.First().Eliminado || !dataWrapperDocumentacion.ListaDocumento.First().UltimaVersion || dataWrapperDocumentacion.ListaDocumento.First().Borrador || dataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos.Where(doc => !doc.Eliminado).ToList().Count == 0)
            {
                throw new GnossException($"The resource {parameters.resource_id} was removed or is not the latest version.", HttpStatusCode.BadRequest);
            }
            else
            {
                foreach (Guid documentoID in parameters.resoruce_list_to_link)
                {
                    if (dataWrapperDocumentacion.ListaDocumentoVincDoc.Count(docVinDoc => docVinDoc.DocumentoID.Equals(documentoID) && docVinDoc.DocumentoVincID.Equals(parameters.resource_id)) > 0)
                    {
                        throw new GnossException($"The reource {documentoID} is already linked to the resource {parameters.resource_id}.", HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        gestorDoc.VincularDocumentos(documentoID, parameters.resource_id, identidad.Clave);

                        mEntityContext.SaveChanges();

                        try
                        {
                            DocumentacionCL docCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                            docCL.InvalidarVinculadosRecursoMVC(parameters.resource_id, FilaProy.ProyectoID);
                            docCL.InvalidarVinculadosRecursoMVC(documentoID, FilaProy.ProyectoID);
                            docCL.Dispose();
                        }
                        catch (Exception ex)
                        {
                            mLoggingService.GuardarLog($"{ex.Message}\\n{ex.StackTrace}",mlogger);
                            throw new GnossException($"{ex.Message}\\n{ex.StackTrace}", HttpStatusCode.InternalServerError);
                        }
                    }
                }

                // Actualizamos el base
                ControladorDocumentacion.ActualizarGnossLivePopularidad(FilaProy.ProyectoID, identidad.Clave, parameters.resource_id, AccionLive.VincularRecursoaRecurso, (int)TipoLive.Miembro, (int)TipoLive.Recurso, true, PrioridadLive.Alta, mAvailableServices);
            }

        }

        /// <summary>
        /// Logical delete of the resource
        /// </summary>
        /// <param name="parameters">Identificador del recurso a borrar
        /// resource_id = Resource identifier Guid
        /// community_short_name = community short name string
        /// end_of_charge = bool that marks the end of the charge
        /// charge_id = charge identifier string
        /// </param>
        /// <example>POST resource/delete</example>
        [HttpPost, Route("delete")]
        public void Delete(DeleteParams parameters)
        {
            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            mNombreCortoComunidad = parameters.community_short_name;
            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, FilaProy.ProyectoID.ToString(), mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            try
            {

                bool usarReplicacion = false;
                //documento
                GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                gestorDoc.CargarDocumentos(false);
                docCN.Dispose();

                if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
                {
                    throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
                }

                Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];

                if (documento.FilaDocumento.Eliminado)
                {
                    throw new GnossException("The resource " + parameters.resource_id + " has already been deleted", HttpStatusCode.BadRequest);
                }

                //identidad
                Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

                //si no existe la identidad o no tiene permisos de administración
                if (identidad == null || (!documento.EsEditoraIdentidad(identidad, true) && !Proyecto.EsAdministradorUsuario(UsuarioOAuth)))
                {
                    throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
                }

                int prioridad = (int)PrioridadBase.ApiRecursos;
                if (parameters.end_of_load)
                {
                    prioridad = (int)PrioridadBase.ApiRecursosBorrarCache;
                }

                #region Espacio BRs personales

                try
                {
                    double espacioArchivo = 0;

                    if (Proyecto.Clave == ProyectoAD.MetaProyecto)
                    {
                        if (documento.EsFicheroDigital)
                        {
                            if (documento.TipoDocumentacion == TiposDocumentacion.FicheroServidor)
                            {
                                GestionDocumental gd = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                                gd.Url = UrlServicioWebDocumentacion;
                                mLoggingService.AgregarEntrada("Obtiene espacio de la base de recursos del usuario");
                                if (identidad.IdentidadOrganizacion == null)
                                {
                                    espacioArchivo = gd.ObtenerEspacioDocumentoDeBaseRecursosUsuario(TipoEntidadVinculadaDocumentoTexto.BASE_RECURSOS, identidad.PersonaID.Value, documento.Clave, Path.GetExtension(documento.Enlace));
                                }
                                else
                                {
                                    Guid organizacionID = Guid.Empty;
                                    AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg perfilPersonaOrg = identidad.IdentidadOrganizacion.PerfilUsuario.FilaRelacionPerfil as AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg;

                                    if (perfilPersonaOrg != null)
                                    {
                                        organizacionID = perfilPersonaOrg.OrganizacionID;
                                    }
                                    espacioArchivo = gd.ObtenerEspacioDocumentoDeBaseRecursosOrganizacion(TipoEntidadVinculadaDocumentoTexto.BASE_RECURSOS, organizacionID, documento.Clave, Path.GetExtension(documento.Enlace));
                                }

                                mLoggingService.AgregarEntrada("obtenido");
                            }
                            else if (documento.TipoDocumentacion == TiposDocumentacion.Imagen)
                            {
                                ServicioImagenes sI = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                                sI.Url = UrlServicioImagenes;
                                mLoggingService.AgregarEntrada("Comprueba si imagen es documento personal");
                                if (identidad.IdentidadOrganizacion == null)
                                {
                                    espacioArchivo = sI.ObtenerEspacioImagenDocumentoPersonal(documento.Clave.ToString(), ".jpg", identidad.PersonaID.Value);
                                }
                                else
                                {
                                    Guid organizacionID = Guid.Empty;
                                    AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg perfilPersonaOrg = identidad.IdentidadOrganizacion.PerfilUsuario.FilaRelacionPerfil as AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg;

                                    if (perfilPersonaOrg != null)
                                    {
                                        organizacionID = perfilPersonaOrg.OrganizacionID;
                                    }
                                    espacioArchivo = sI.ObtenerEspacioImagenDocumentoOrganizacion(documento.Clave.ToString(), ".jpg", organizacionID);
                                }
                                mLoggingService.AgregarEntrada("comprobado");
                            }
                            else if (documento.TipoDocumentacion == TiposDocumentacion.Video)
                            {
                                mLoggingService.AgregarEntrada("comprueba si vídeo está en espacio personal");

                                ServicioVideos sV = new ServicioVideos(mConfigService, mLoggingService, mLoggerFactory.CreateLogger<ServicioVideos>(), mLoggerFactory);

                                if (identidad.IdentidadOrganizacion == null)
                                {
                                    espacioArchivo = sV.ObtenerEspacioVideoPersonal(documento.Clave, identidad.PersonaID.Value);
                                }
                                else
                                {
                                    Guid organizacionID = Guid.Empty;
                                    AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg perfilPersonaOrg = identidad.IdentidadOrganizacion.PerfilUsuario.FilaRelacionPerfil as AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg;

                                    if (perfilPersonaOrg != null)
                                    {
                                        organizacionID = perfilPersonaOrg.OrganizacionID;
                                    }

                                    espacioArchivo = sV.ObtenerEspacioVideoPersonal(documento.Clave, organizacionID);
                                }

                                mLoggingService.AgregarEntrada("comprobado");
                            }
                        }
                    }
                    if (espacioArchivo != 0)
                    {
                        documento.GestorDocumental.EspacioActualBaseRecursos = documento.GestorDocumental.EspacioActualBaseRecursos - espacioArchivo;

                        if (documento.GestorDocumental.EspacioActualBaseRecursos < 0)
                        {
                            documento.GestorDocumental.EspacioActualBaseRecursos = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, mlogger);
                }

                #endregion

                documentacionCN.IniciarTransaccion();
                List<Guid> listaProyectosActualNumRec = new List<Guid>();
                Dictionary<Guid, Guid> listaBRsConProyectos = new Dictionary<Guid, Guid>();

                if (documento.FilaDocumento.Borrador == false)
                {
                    if (documento.TipoDocumentacion != TiposDocumentacion.Hipervinculo)
                    {
                        foreach (Guid baseRecurso in documento.BaseRecursos)
                        {
                            Guid proyID = documento.GestorDocumental.ObtenerProyectoID(baseRecurso);
                            listaProyectosActualNumRec.Add(proyID);
                            listaBRsConProyectos.Add(baseRecurso, proyID);
                        }
                    }
                    else
                    {
                        listaProyectosActualNumRec.Add(FilaProy.ProyectoID);
                        listaBRsConProyectos.Add(documento.GestorDocumental.BaseRecursosIDActual, FilaProy.ProyectoID);
                    }
                }

                if (documento.TipoDocumentacion != TiposDocumentacion.Hipervinculo || documento.BaseRecursos.Count < 2)
                {
                    documento.GestorDocumental.EliminarDocumentoLogicamente(documento);
                }
                else
                {
                    documento.GestorDocumental.DesVincularDocumentoDeBaseRecursos(documento.Clave, documento.GestorDocumental.BaseRecursosIDActual, FilaProy.ProyectoID, identidad.Clave);
                }
                documento.FilaDocumento.FechaModificacion = DateTime.Now;

                documento.GestorDocumental.DesvincularDocumentoDeCategorias(documento, documento.CategoriasTesauro.Values.ToList(), documento.CreadorID, FilaProy.ProyectoID);

                mEntityContext.SaveChanges();

                string urlDoc = "";

                if (documento.TipoDocumentacion == TiposDocumentacion.Semantico)
                {
                    docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                    gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(documento.ElementoVinculadoID));
                    docCN.Dispose();
                    gestorDoc.CargarDocumentos(false);
                    urlDoc = UrlIntragnoss + gestorDoc.ListaDocumentos[documento.ElementoVinculadoID].Enlace;
                }

                if (documento.TipoDocumentacion == TiposDocumentacion.Semantico && documento.FilaDocumento.ElementoVinculadoID.HasValue)
                {
                    string infoExtra_Replicacion = "" + ObtenerInfoExtraBaseDocumento(parameters.resource_id, (short)TiposDocumentacion.Semantico, FilaProy.ProyectoID, (short)PrioridadBase.ApiRecursos, 1);
                    infoExtra_Replicacion += "|;|%|;|";
                    ControladorDocumentacion.BorrarRDFDeDocumentoEliminado(documento.Clave, documento.FilaDocumento.ElementoVinculadoID.Value, UrlIntragnoss, infoExtra_Replicacion, usarReplicacion, FilaProy.ProyectoID);
                }

                //Borramos la cache de las comunidades afectadas
                DocumentacionCL documentacionCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                List<Guid> listaProyectosActualYProyectosRelacionados = new List<Guid>();
                listaProyectosActualYProyectosRelacionados.AddRange(listaProyectosActualNumRec);
                foreach (Guid proy in listaProyectosActualNumRec)
                {
                    int tipoDoc = -1;
                    if (documento.TipoDocumentacion == TiposDocumentacion.Debate || documento.TipoDocumentacion == TiposDocumentacion.Pregunta)
                    {
                        tipoDoc = (int)documento.TipoDocumentacion;
                    }
                    documentacionCL.BorrarPrimerosRecursos(proy, tipoDoc);
                }

                //Borramos la cache de recursos relacionados de las comunidades afectadas, asi como todas las relacionadas
                //documentacionCL.BorrarRecursosRelacionados(listaProyectosActualYProyectosRelacionados);
                documentacionCL.Dispose();


                #region Actualizar cola GnossLIVE

                int tipo;
                switch (documento.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                foreach (Guid baseRecursosID in listaBRsConProyectos.Keys)
                {
                    string infoExtra = null;

                    if (listaBRsConProyectos[baseRecursosID] == ProyectoAD.MetaProyecto)
                    {
                        infoExtra = identidad.IdentidadPersonalMyGNOSS.PerfilID.ToString();
                    }

                    if (AgregarColaLive)
                    {
                        ControladorDocumentacion.ActualizarGnossLive(listaBRsConProyectos[baseRecursosID], documento.Clave, AccionLive.Eliminado, tipo, PrioridadLive.Alta, infoExtra, mAvailableServices);
                        ControladorDocumentacion.ActualizarGnossLive(listaBRsConProyectos[baseRecursosID], documento.ObtenerPublicadorEnBR(baseRecursosID), AccionLive.RecursoAgregado, (int)TipoLive.Miembro, PrioridadLive.Alta, mAvailableServices);
                    }

                    //if (AgregarColaSuscripciones)
                    //{
                    //ControladorSuscripciones.AgregarElementoCola(listaBRsConProyectos[baseRecursosID], documento.Clave, AccionLive.Eliminado, (TipoLive)tipo);
                    //}
                }

                #endregion

                //borrar cache recursos

                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(Proyecto.Clave, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));


                FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
                facetadoAD.IniciarTransaccion();

                facetadoCN.BorrarRecurso(identidad.PerfilID.ToString(), documento.Clave, prioridad);

                if (documento.TipoDocumentacion == TiposDocumentacion.Hipervinculo)
                {
                    ControladorDocumentacion.EstablecePrivacidadRecursoEnMetaBuscador(documento, identidad, true);
                }

                foreach (Guid proy in listaProyectosActualNumRec)
                {
                    if (proy.Equals(documento.ProyectoID))
                    {
                        facetadoCN.BorrarRecurso(proy.ToString(), documento.Clave, 0, "", false, true, true);
                    }

                    ControladorDocumentacion.EliminarRecursoModeloBaseSimple(parameters.resource_id, proy, documento.FilaDocumento.Tipo, null, null, (short)EstadosColaTags.EnEspera, (short)PrioridadBase.ApiRecursos, mAvailableServices);
                }

                documentacionCN.TerminarTransaccion(true);
                facetadoCN.TerminarTransaccion(true);

                if (documento.TipoDocumentacion == TiposDocumentacion.Semantico)
                {
                    ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                    servicioImagenes.Url = UrlServicioImagenes;

                    string directorio = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticasAntiguo + "\\" + parameters.resource_id.ToString();
                    servicioImagenes.BorrarImagenesDeRecurso(directorio);

                    directorio = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticas + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(parameters.resource_id);
                    servicioImagenes.BorrarImagenesDeRecurso(directorio);
                }

                ControladorDocumentacion.InsertarEnColaProcesarFicherosRecursosModificadosOEliminados(documento.Clave, TipoEventoProcesarFicherosRecursos.Borrado, mAvailableServices);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                documentacionCN.TerminarTransaccion(false);
                facetadoCN.TerminarTransaccion(false);

                if (documentacionCN != null)
                {
                    documentacionCN.Dispose();
                }
                if (facetadoCN != null)
                {
                    facetadoCN.Dispose();
                }

                throw;
            }
        }

        /// <summary>
        /// Persistent delete of the resource
        /// </summary>
        /// <param name="parameters">
        /// resource_id = Resource identifier Guid
        /// community_short_name = community short name string
        /// delete_attached = bool that indicates if the attached resources must be deleted
        /// end_of_charge = bool that marks the end of the charge
        /// </param>
        /// <example>POST resource/persistent-delete</example>
        [HttpPost, Route("persistent-delete")]
        public void PersistentDelete(PersistentDeleteParams parameters)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            try
            {
                mNombreCortoComunidad = parameters.community_short_name;

                //Identidad
                GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);

                Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, false);

                bool esAdministradorMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

                if (identidad == null && !esAdministradorMyGnoss)
                {
                    throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
                }

                //documento
                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                gestorDoc.CargarDocumentos(false);

                ControladorApiRecursos controlApi = new ControladorApiRecursos(mEntityContext, mLoggingService, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mEntityContextBASE, mGnossCache, mServicesUtilVirtuosoAndReplication, mAvailableServices, mLoggerFactory.CreateLogger<ControladorApiRecursos>(), mLoggerFactory);
                if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
                {
                    //Si esta eliminado, probar a eliminar del grafo de la ontologia y del grafo de busqueda
                    controlApi.BorradoGrafoOntologia(new List<Guid>() { parameters.resource_id }, FilaProy.ProyectoID, UrlIntragnoss);

                    //Borrar los grafos de búsqueda en los que están compartidos los recursos
                    controlApi.BorradoGrafoBusqueda(new List<Guid>() { parameters.resource_id }, FilaProy.ProyectoID);

                    throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
                }

                Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];

                if (!esAdministradorMyGnoss && (identidad == null || !documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false)))
                {
                    throw new GnossException("The OAuth user has no permission on resource deleting.", HttpStatusCode.BadRequest);
                }

                List<Guid> idsDocumento = new List<Guid>();
                idsDocumento.Add(documento.Clave);
                Guid ontologiaID = Guid.Empty;

                if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                {
                    ontologiaID = documento.ElementoVinculadoID;

                    if (ontologiaID.Equals(Guid.Empty))
                    {
                        throw new Exception("El documento no está vinculado a ninguna ontología");
                        throw new GnossException("The resource is not linked to an ontology.", HttpStatusCode.BadRequest);
                    }
                }
                facetadoCN.IniciarTransaccion();

                if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                {
                    //Borrado del grafo de ontología de los elementos de la lista obtenida
                    controlApi.BorradoGrafoOntologia(idsDocumento, ontologiaID, FilaProy.ProyectoID, UrlIntragnoss);
                }

                //Borrar los grafos de búsqueda en los que están compartidos los recursos
                controlApi.BorrarGrafoBusqueda(idsDocumento, documento.ProyectoID);

                if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                {
                    //Borrado de la BD rdf de los proyectos donde estén compartidos esos documentos
                    controlApi.BorradoRDF(idsDocumento);

                    // Esta parte se pasa a realizar en el servicio de Back ProcessFilesDeletedResources
                    //sólo se borrarán los adjuntos si se especifica, pueden querer borrar un recurso y no borrar sus adjuntos
                    /*if (parameters.delete_attached)
                    {
                        //Borrado de imágenes
                        controlApi.BorrarImagenRecursos(idsDocumento);

                        //Borrado de archivos, se pasa la ontologia como GuidEmpty para que no borre todos los documentos de la ontologia(esto lo hacía con la ruta antigua de documentos, que iba asociada a la ontología) y sólo borre los documentos del archivo(ruta nueva de documentos)
                        controlApi.BorrarArchivosDocumentosOntologia(idsDocumento, Guid.Empty);
                    }*/
                }
                else
                {
                    // Esta parte se pasa a realizar en el servicio de Back ProcessFilesDeletedResources
                    /*if (parameters.delete_attached)
                    {
                        try
                        {
                            if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Imagen))
                            {
                                //Borramos la imagen del servidor
                                string directorioImg = UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.DirectorioDocumentoFileSystem(documento.Clave) + "/" + documento.Clave + documento.Extension;
                                ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService);
                                servicioImagenes.Url = UrlServicioImagenes;
                                servicioImagenes.BorrarImagenDeDirectorio(directorioImg);
                            }

                            if (TieneGoogleDriveConfigurado)
                            {
                                try
                                {
                                    string nombreArchivo = documento.Enlace.Substring(0, documento.Enlace.LastIndexOf("."));
                                    // TODO Javier migrar reddes sociales 
                                    //OAuthGoogleDrive gdrive = new OAuthGoogleDrive();
                                    //id de google sin extension
                                    string googleID = nombreArchivo;
                                    string docID = googleID.Substring(googleID.LastIndexOf('#') + 1);

                                    //TODO Javier descomentar gdrive.EliminarDocumento(docID);
                                }
                                catch (Exception ex)
                                {
                                    mLoggingService.GuardarLogError(ex);
                                    throw new Exception("Archivos adjuntos incorrectos. GoogleDrive=" + TieneGoogleDriveConfigurado);
                                }
                            }
                            else
                            {
                                //Borramos el fichero del servidor
                                GestionDocumental gd = new GestionDocumental(mLoggingService, mConfigService);
                                gd.Url = UrlServicioWebDocumentacion;
                                TipoEntidadVinculadaDocumento tipoEntidaVinDoc = TipoEntidadVinculadaDocumento.Web;
                                string tipoEntidadTexto = ControladorDocumentacion.ObtenerTipoEntidadAdjuntarDocumento(tipoEntidaVinDoc);
                                gd.BorrarDocumento(tipoEntidadTexto, FilaProy.OrganizacionID, FilaProy.ProyectoID, documento.Clave, documento.Extension);
                            }
                        }
                        catch (Exception ex)
                        {
                            mLoggingService.GuardarLogError(ex);
                            EscribirLogTiempos(Guid.Empty);
                            throw ex;
                        }
                    }*/
                }

                //Borrar los documentos vinculados de la base ácida                
                controlApi.BorrarDocumentos(idsDocumento);
                facetadoCN.TerminarTransaccion(true);

                if (parameters.end_of_load)
                {
                    //Invalidamos caché del proyecto
                    ProyectoCL proyCL = new ProyectoCL("acid", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                    proyCL.InvalidarCacheQueContengaCadena(FilaProy.ProyectoID.ToString());
                    proyCL.Dispose();

                    //Invalidamos caché de resultados
                    FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                    facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(FilaProy.ProyectoID, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));
                    facetadoCL.Dispose();
                }

                ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, documento.Clave, AccionLive.Eliminado, (int)TipoLive.Recurso, "base", PrioridadLive.Baja, mAvailableServices);
                ControladorDocumentacion.InsertarEnColaProcesarFicherosRecursosModificadosOEliminados(documento.Clave, TipoEventoProcesarFicherosRecursos.BorradoPersistente, mAvailableServices);

                Proyecto.GestorProyectos.Dispose();
            }
            catch (Exception exception)
            {
                docCN.TerminarTransaccion(false);
                facetadoCN.TerminarTransaccion(false);
                throw exception;
            }
        }

        /// <summary>
        /// Checks whether the url exists in a resource of the community. (Searchs on the resource description)
        /// </summary>
        /// <param name="parameters">
        /// url = link to search in the community
        /// community_short_name = community short name string
        /// </param>
        /// <returns>True if the link exists in a resource of the community</returns>
        /// <example>POST resource/exists-url</example>
        [HttpPost, Route("exists-url")]
        public bool ExistsUrl([FromBody] ExistsUrlParams parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;
            DocumentacionCN docu = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            bool existe = docu.EstaEnlaceEnComunidad(parameters.url, FilaProy.ProyectoID);
            docu.Dispose();
            return existe;
        }

        /// <summary>
        /// Shares the resource in the destination community
        /// </summary>
        /// <param name="parameters">
        /// destination_community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// categories = categories guid list where the document is going to be shared to 
        /// </param>
        /// <example>POST resource/share</example>
        [HttpPost, Route("share")]
        public void Share(ShareParams parameters)
        {
            mNombreCortoComunidad = parameters.destination_communitiy_short_name;
            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = null;

            if (string.IsNullOrEmpty(parameters.publisher_email))
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, false);
            }
            else
            {
                PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
                Guid personaID = persCN.ObtenerPersonaPorEmail(parameters.publisher_email);
                Guid? userID = persCN.ObtenerUsuarioIDDePersonaID(personaID);
                identidad = CargarIdentidad(gestorDoc, FilaProy, userID.Value, false);
            }

            if (identidad == null)
            {
                throw new GnossException("The OAuth user has no participate on the community.", HttpStatusCode.BadRequest);
            }

            //documento
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            gestorDoc.CargarDocumentos(false);

            if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist.", HttpStatusCode.BadRequest);
            }
            AD.EntityModel.Models.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id].FilaDocumento;
            bool categoriesOk = true;

            if (documento.ElementoVinculadoID.HasValue && documento.Tipo.Equals((short)TiposDocumentacion.Semantico) && (parameters.categories == null || parameters.categories.Count == 0))
            {
                LectorXmlConfig lectorXmlConfig = new LectorXmlConfig(documento.ElementoVinculadoID.Value, FilaProy.ProyectoID, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mVirtuosoAD, mLoggerFactory.CreateLogger<LectorXmlConfig>(), mLoggerFactory);
                Dictionary<string, List<EstiloPlantilla>> xml = lectorXmlConfig.ObtenerConfiguracionXml();
                EstiloPlantillaConfigGen configGen = (EstiloPlantillaConfigGen)xml["[ConfiguracionGeneral]"].First();
                categoriesOk = configGen.CategorizacionTesauroGnossNoObligatoria;
                parameters.categories = new List<Guid>();
            }
            else if (parameters.categories == null || (parameters.categories != null && parameters.categories.Count == 0))
            {
                categoriesOk = false;
            }

            if (!categoriesOk)
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not have categories selected.", HttpStatusCode.BadRequest);
            }
            DocumentoWeb documentoEdicion = new DocumentoWeb(gestorDoc.ListaDocumentos[parameters.resource_id].FilaDocumento, gestorDoc);

            if (documentoEdicion.ListaProyectos.Contains(FilaProy.ProyectoID))
            {
                throw new GnossException("The resource " + parameters.resource_id + " is already shared in the community " + parameters.destination_communitiy_short_name + ".", HttpStatusCode.BadRequest);
            }

            //Comprobar si la comunidad permite el tipo de recurso:
            ComprobarUsuarioEnProyectoAdmiteTipoRecurso(identidad.Clave, identidad.IdentidadMyGNOSS.Clave, FilaProy.ProyectoID, UsuarioOAuth, documentoEdicion.TipoDocumentacion, documentoEdicion.ElementoVinculadoID, true);

            Dictionary<Guid, CategoriaTesauro> listaCategorias = new Dictionary<Guid, CategoriaTesauro>();
            foreach (Guid categoriaID in parameters.categories)
            {
                if (gestorDoc.GestorTesauro.ListaCategoriasTesauro.ContainsKey(categoriaID))
                {
                    CategoriaTesauro categoria = gestorDoc.GestorTesauro.ListaCategoriasTesauro[categoriaID];
                    listaCategorias.Add(categoria.Clave, categoria);
                }
            }

            gestorDoc.CompartirRecurso(FilaProy.ProyectoID.ToString(), listaCategorias, documentoEdicion, gestorDoc, identidad.Clave);

            List<Guid> listaProyActualizar = new List<Guid>();
            listaProyActualizar.Add(FilaProy.ProyectoID);

            Guardar(listaProyActualizar, gestorDoc, documentoEdicion);

            //ControladorDocumentacion.EstablecePrivacidadRecursoEnMetaBuscadorDesdeServicio(documentoEdicion, identidad, true, "acid");

            #region Actualizar cola GnossLIVE

            int tipo;
            switch (documentoEdicion.TipoDocumentacion)
            {
                case TiposDocumentacion.Debate:
                    tipo = (int)TipoLive.Debate;
                    break;
                case TiposDocumentacion.Pregunta:
                    tipo = (int)TipoLive.Pregunta;
                    break;
                default:
                    tipo = (int)TipoLive.Recurso;
                    break;
            }

            ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, documentoEdicion.Clave, AccionLive.Agregado, tipo, "base", PrioridadLive.Baja, mAvailableServices);

            ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, identidad.Clave, AccionLive.RecursoAgregado, (int)TipoLive.Miembro, "base", PrioridadLive.Baja, mAvailableServices);

            #endregion

            ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(documentoEdicion.Clave);
            controDoc.NotificarAgregarRecursosEnComunidad(listaDocs, FilaProy.ProyectoID, PrioridadBase.Baja, mAvailableServices);
        }


        /// <summary>
        /// Shares the resource in the destination community
        /// </summary>
        /// <param name="parameters">
        /// destination_community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// categories = categories guid list where the document is going to be shared to 
        /// </param>
        /// <example>POST resource/share</example>
        [HttpPost, Route("share-resources")]
        public void ShareResources(List<ShareParams> parameters)
        {
            List<Guid> documentosID = new List<Guid>();
            string publisherEmail;
            bool unaComunidad = ObtenerDocumentosIDShareParamsNombreComunidad(parameters, documentosID, out mNombreCortoComunidad, out publisherEmail);

            if (!unaComunidad)
            {
                throw new GnossException("The community where the resources are sharing must be unique ", HttpStatusCode.BadRequest);
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = null;

            if (string.IsNullOrEmpty(publisherEmail))
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, false);
            }
            else
            {
                PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
                Guid personaID = persCN.ObtenerPersonaPorEmail(publisherEmail);
                Guid? userID = persCN.ObtenerUsuarioIDDePersonaID(personaID);
                identidad = CargarIdentidad(gestorDoc, FilaProy, userID.Value, false);
            }

            if (identidad == null)
            {
                throw new GnossException("The OAuth user has no participate on the community.", HttpStatusCode.BadRequest);
            }

            //documento
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentosPorIDParaListadoDeAcciones(documentosID, FilaProy.ProyectoID, Guid.Empty));
            gestorDoc.CargarDocumentos(false);

            //Inicio variables
            List<Guid> listaProyActualizar = new List<Guid>();
            DocumentoWeb documentoEdicion = new DocumentoWeb(gestorDoc.ListaDocumentos[parameters[0].resource_id].FilaDocumento, gestorDoc);
            List<Guid> listaDocs = new List<Guid>();

            Guid proyectoOntologiasID = FilaProy.ProyectoID;
            bool identidadDeOtroProyecto = false;

            if (ParametroProyecto.ContainsKey(ParametroAD.ProyectoIDPatronOntologias))
            {
                proyectoOntologiasID = new Guid(ParametroProyecto[ParametroAD.ProyectoIDPatronOntologias]);
                identidadDeOtroProyecto = true;
            }
            else if (Proyecto.FilaProyecto.ProyectoSuperiorID.HasValue)
            {
                proyectoOntologiasID = Proyecto.FilaProyecto.ProyectoSuperiorID.Value;
                identidadDeOtroProyecto = true;
            }

            ProyectoCN proyCN = new ProyectoCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
            TipoRolUsuario tipoRolUsuario = proyCN.ObtenerRolUsuarioEnProyecto(FilaProy.ProyectoID, UsuarioOAuth);

            //Tiempos
            List<Guid> listaPermisosOntologias = proyCN.ObtenerOntologiasPermitidasIdentidadEnProyecto(identidad.Clave, identidad.IdentidadMyGNOSS.Clave, UsuarioOAuth, tipoRolUsuario, identidadDeOtroProyecto);
            List<TiposDocumentacion> tipoDocPermitidos = proyCN.ObtenerTiposDocumentosPermitidosUsuarioEnProyectoPorUsuID(proyectoOntologiasID, UsuarioOAuth);
            proyCN.Dispose();
            proyCL.Dispose();

            foreach (ShareParams param in parameters)
            {
                if (!gestorDoc.ListaDocumentos.ContainsKey(param.resource_id))
                {
                    mLoggingService.GuardarLogError("The resource " + param.resource_id + " does not exist.",mlogger);
                }
                else
                {
                    documentoEdicion = new DocumentoWeb(gestorDoc.ListaDocumentos[param.resource_id].FilaDocumento, gestorDoc);

                    if (documentoEdicion.ListaProyectos.Contains(FilaProy.ProyectoID))
                    {
                        mLoggingService.GuardarLogError("The resource " + param.resource_id + " is already shared in the community " + param.destination_communitiy_short_name + ".", mlogger);
                        //throw new GnossException("The resource " + param.resource_id + " is already shared in the community " + param.destination_communitiy_short_name + ".", HttpStatusCode.BadRequest);
                    }

                    //Comprobar si la comunidad permite el tipo de recurso:
                    ComprobarUsuarioEnProyectoAdmiteTipoRecurso(identidad.Clave, identidad.IdentidadMyGNOSS.Clave, FilaProy.ProyectoID, UsuarioOAuth, documentoEdicion.TipoDocumentacion, documentoEdicion.ElementoVinculadoID, true, listaPermisosOntologias, tipoDocPermitidos);

                    Dictionary<Guid, CategoriaTesauro> listaCategorias = new Dictionary<Guid, CategoriaTesauro>();
                    foreach (Guid categoriaID in param.categories)
                    {
                        if (gestorDoc.GestorTesauro.ListaCategoriasTesauro.ContainsKey(categoriaID))
                        {
                            CategoriaTesauro categoria = gestorDoc.GestorTesauro.ListaCategoriasTesauro[categoriaID];
                            listaCategorias.Add(categoria.Clave, categoria);
                        }
                    }

                    gestorDoc.CompartirRecurso(FilaProy.ProyectoID.ToString(), listaCategorias, documentoEdicion, gestorDoc, identidad.Clave);

                    if (!listaProyActualizar.Contains(FilaProy.ProyectoID))
                    {
                        listaProyActualizar.Add(FilaProy.ProyectoID);
                    }

                    listaDocs.Add(param.resource_id);
                }
            }

            Guardar(listaProyActualizar, gestorDoc, documentoEdicion);
            ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
            controDoc.NotificarAgregarRecursosEnComunidad(listaDocs, FilaProy.ProyectoID, PrioridadBase.Baja, mAvailableServices);
        }

        private bool ObtenerDocumentosIDShareParamsNombreComunidad(List<ShareParams> parameters, List<Guid> listaIds, out string pNombreComunidad, out string pPublisherEmail)
        {
            List<string> comunidades = new List<string>();
            bool unacomunidad = true;
            pPublisherEmail = null;
            pNombreComunidad = null;
            foreach (ShareParams shareParams in parameters)
            {
                if (!listaIds.Contains(shareParams.resource_id))
                {
                    listaIds.Add(shareParams.resource_id);
                }
                if (!comunidades.Contains(shareParams.destination_communitiy_short_name))
                {
                    comunidades.Add(shareParams.destination_communitiy_short_name);
                }
                if (pPublisherEmail == null)
                {
                    pPublisherEmail = shareParams.publisher_email;
                }
            }

            if (comunidades.Count > 1)
            {
                unacomunidad = false;
            }
            else
            {
                pNombreComunidad = comunidades.First();
            }

            return unacomunidad;
        }

        /// <summary>
        /// Sets the resource main image
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// path = relative path with the image name, image sizes available and '[IMGPrincipal]' mask
        /// </param>
        /// <example>POST resource/set-main-image</example>
        [HttpPost, Route("set-main-image")]
        public void SetMainImage(SetMainImageParams parameters)
        {
            string imagenRepresentanteDoc = string.Empty;
            mNombreCortoComunidad = parameters.community_short_name;

            if (string.IsNullOrEmpty(parameters.path))
            {
                throw new GnossException("The image path cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (!parameters.path.Contains("."))
            {
                throw new GnossException("The image path has not contains the image extension.", HttpStatusCode.BadRequest);
            }

            string especialID = parameters.path;
            string extensionArchivo = especialID.Substring(especialID.LastIndexOf("."));
            especialID = especialID.Substring(0, especialID.LastIndexOf("."));

            if (!especialID.Contains("[IMGPrincipal]"))
            {
                throw new GnossException("The image path has incorrect format. Mask '[IMGPrincipal]' not found.", HttpStatusCode.BadRequest);
            }

            especialID = especialID.Replace("[IMGPrincipal]", "");

            string tamañosImgs = especialID.Substring(1, especialID.IndexOf("]") - 1);

            if (tamañosImgs[tamañosImgs.Length - 1] != ',')
            {
                tamañosImgs += ",";
            }

            especialID = especialID.Substring(especialID.IndexOf("]") + 1);

            if (especialID.Contains("/"))
            {
                especialID = especialID.Substring(especialID.IndexOf("/") + 1);
            }

            imagenRepresentanteDoc = tamañosImgs + UtilArchivos.ContentImagenes + "/" + UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.ContentImagenesSemanticas + "/" + UtilArchivos.DirectorioDocumento(parameters.resource_id) + "/" + especialID + extensionArchivo;

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            docCN.Dispose();
            gestorDoc.CargarDocumentos(false);

            if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];
            documento.FilaDocumento.NombreCategoriaDoc = imagenRepresentanteDoc;
            mEntityContext.SaveChanges();
        }

        /// <summary>
        /// Removes the resource main image
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// </param>
        /// <example>POST resource/remove-main-image</example>
        [HttpPost, Route("remove-main-image")]
        public void RemoveMainImage(SetMainImageParams parameters)
        {
            string imagenRepresentanteDoc = string.Empty;
            mNombreCortoComunidad = parameters.community_short_name;

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            docCN.Dispose();
            gestorDoc.CargarDocumentos(false);

            if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];
            documento.FilaDocumento.NombreCategoriaDoc = imagenRepresentanteDoc;
            mEntityContext.SaveChanges();
        }

        /// <summary>
        /// Sets the publisher of a resource using an email if user participates in the community.
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// publisher_email = new publisher email string
        /// </param>
        /// <example>POST resource/set-publisher</example>
        [HttpPost, Route("set-publisher-of-resource")]
        public void SetPublisherOfResource(SetPublisherParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.publisher_email))
            {
                throw new GnossException("The parameter 'publisher_email' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;

            PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
            Guid personaID = persCN.ObtenerPersonaPorEmail(parameters.publisher_email);
            Guid? userID = persCN.ObtenerUsuarioIDDePersonaID(personaID);
            persCN.Dispose();
            Identidad identidad = null;

            if (!userID.HasValue)
            {
                throw new GnossException("The publisher_email " + parameters.publisher_email + " does not exist.", HttpStatusCode.BadRequest);
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            identidad = CargarIdentidad(gestorDoc, FilaProy, userID.Value, true);

            if (identidad == null)
            {
                throw new GnossException("The publisher_email " + parameters.publisher_email + " is not member of the community " + parameters.community_short_name + ".", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            docCN.Dispose();
            gestorDoc.CargarDocumentos(false);

            if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];
            //Ponemos la IdentidadPublicacionID en la tabla DocumentoWebVinBaseRecursos (cuando el tipo de publicacion es Publicado)
            if (documento.FilaDocumentoWebVinBR.TipoPublicacion == (short)TipoPublicacion.Publicado)
            {
                //Ponemos el creadorID en la tabla documento
                documento.CreadorID = identidad.Clave;
                documento.GestorDocumental.CalcularDatosDesnormalizadosDeDocumentoWebVinBaseRecursos(documento.FilaDocumentoWebVinBR, identidad);
            }

            //Agregamos como editor al nuevo perfil
            if (!documento.ListaPerfilesEditores.ContainsKey(identidad.PerfilUsuario.Clave))
            {
                documento.GestorDocumental.AgregarEditorARecurso(documento.Clave, identidad.PerfilUsuario.Clave);
            }
            else if (!documento.ListaPerfilesEditores[identidad.PerfilUsuario.Clave].FilaEditor.Editor)
            {
                documento.ListaPerfilesEditores[identidad.PerfilUsuario.Clave].FilaEditor.Editor = true;
            }

            //guardar documento
            List<Guid> listaProyectosActualNumRec = new List<Guid>();
            foreach (Guid baseRecurso in documento.BaseRecursos)
            {
                Guid proyectoID = gestorDoc.ObtenerProyectoID(baseRecurso);
                listaProyectosActualNumRec.Add(proyectoID);
            }

            Guardar(listaProyectosActualNumRec, gestorDoc, documento);

            //pasarlo por el Base
            ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(documento.Clave);
            controDoc.NotificarAgregarRecursosEnComunidad(listaDocs, FilaProy.ProyectoID, PrioridadBase.ApiRecursos, mAvailableServices);

            //actualizar cola live
            ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, identidad.Clave, AccionLive.Editado, (int)TipoLive.Recurso, "base", PrioridadLive.Baja, mAvailableServices);
        }

        /// <summary>
        /// Sets the publisher of a resource using an email if user participates in the community.
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id_list = resource identifier Guid
        /// publisher_email = new publisher email string
        /// keep_editors = indicates if keep old resource editors or not
        /// </param>
        /// <example>POST resource/set-publisher</example>
        [HttpPost, Route("set-publisher-of-resources-list")]
        public void SetPublisherOfResourceList(SetPublisherManyResourcesParams parameters)
        {

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);

            mNombreCortoComunidad = parameters.community_short_name;

            if (proyCN.EsUsuarioAdministradorProyecto(UsuarioOAuth, FilaProy.ProyectoID))
            {
                throw new GnossException("The OAuth user does not manage the community " + parameters.community_short_name + ".", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(parameters.publisher_email))
            {
                throw new GnossException("The parameter 'publisher_email' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
            Guid personaID = persCN.ObtenerPersonaPorEmail(parameters.publisher_email);
            Guid? userID = persCN.ObtenerUsuarioIDDePersonaID(personaID);
            persCN.Dispose();

            if (!userID.HasValue)
            {
                throw new GnossException("The publisher_email " + parameters.publisher_email + " does not exist.", HttpStatusCode.BadRequest);
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, userID.Value, true);

            if (identidad == null)
            {
                throw new GnossException("The publisher_email " + parameters.publisher_email + " is not member of the community " + parameters.community_short_name + ".", HttpStatusCode.BadRequest);
            }

            if (parameters.resource_id_list == null || parameters.resource_id_list.Count == 0)
            {
                throw new GnossException("The parameter 'resource_id_list' must have some resource_id.", HttpStatusCode.BadRequest);
            }

            Guid perfilID = identidad.PerfilID;
            Dictionary<int, List<Guid>> BloquesDocumentos = new Dictionary<int, List<Guid>>();
            int numBloque = 0;
            int numMaxBloque = 100;
            foreach (Guid documentoID in parameters.resource_id_list)
            {
                if (!BloquesDocumentos.ContainsKey(numBloque))
                {
                    BloquesDocumentos.Add(numBloque, new List<Guid>());
                }
                BloquesDocumentos[numBloque].Add(documentoID);
                if (BloquesDocumentos[numBloque].Count == numMaxBloque)
                {
                    numBloque++;
                }
            }

            foreach (int numBloqueActual in BloquesDocumentos.Keys)
            {
                gestorDoc = new GestorDocumental(docCN.ObtenerDocumentosPorID(BloquesDocumentos[numBloqueActual]), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
                Dictionary<Guid, short> listadoDocsConTipo = new Dictionary<Guid, short>();
                Dictionary<Guid, int> listadoDocsConTipoParaLive = new Dictionary<Guid, int>();

                foreach (Guid idDoc in gestorDoc.ListaDocumentos.Keys)
                {
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[idDoc];

                    if (documento.FilaDocumento.ProyectoID.HasValue && documento.FilaDocumento.ProyectoID.Value == FilaProy.ProyectoID)
                    {
                        //Ponemos el creadorID en la tabla documento
                        documento.CreadorID = identidad.Clave;

                        //Ponemos la IdentidadPublicacionID en la tabla DocumentoWebVinBaseRecursos (cuando el tipo de publicacion es Publicado)
                        List<AD.EntityModel.Models.Documentacion.DocumentoWebVinBaseRecursos> filasDocWebVin = documento.FilaDocumento.DocumentoWebVinBaseRecursos.ToList();
                        foreach (AD.EntityModel.Models.Documentacion.DocumentoWebVinBaseRecursos fila in filasDocWebVin)
                        {
                            if (fila.TipoPublicacion == (short)TipoPublicacion.Publicado)
                            {
                                documento.GestorDocumental.CalcularDatosDesnormalizadosDeDocumentoWebVinBaseRecursos(fila, identidad);
                            }
                        }

                        if (!parameters.keep_editors)
                        {
                            //Limpiamos los editores del documento
                            documento.ListaPerfilesEditores.Clear();
                            documento.ListaGruposEditores.Clear();
                            documento.GestorDocumental.LimpiarEditores(documento.Clave);
                        }

                        //Agregamos como editor al perfil
                        if (!documento.ListaPerfilesEditores.ContainsKey(perfilID))
                        {
                            documento.GestorDocumental.AgregarEditorARecurso(documento.Clave, perfilID);
                        }
                        else if (!documento.ListaPerfilesEditores[perfilID].FilaEditor.Editor)
                        {
                            documento.ListaPerfilesEditores[perfilID].FilaEditor.Editor = true;
                        }

                        if (!listadoDocsConTipo.ContainsKey(documento.Clave))
                        {
                            listadoDocsConTipo.Add(documento.Clave, (short)documento.TipoDocumentacion);
                            int tipo;
                            switch (documento.TipoDocumentacion)
                            {
                                case TiposDocumentacion.Debate:
                                    tipo = (int)TipoLive.Debate;
                                    break;
                                case TiposDocumentacion.Pregunta:
                                    tipo = (int)TipoLive.Pregunta;
                                    break;
                                default:
                                    tipo = (int)TipoLive.Recurso;
                                    break;
                            }

                            listadoDocsConTipoParaLive.Add(documento.Clave, tipo);
                        }
                    }
                }

                docCN.ActualizarDocumentacion();

                ControladorDocumentacion.AgregarModificacionRecursosModeloBase(listadoDocsConTipo, FilaProy.ProyectoID, PrioridadBase.ApiRecursosBorrarCache, mAvailableServices);
                ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, listadoDocsConTipoParaLive, AccionLive.Editado, PrioridadLive.Baja, mAvailableServices);

                foreach (Guid idDoc in gestorDoc.ListaDocumentos.Keys)
                {
                    ControladorDocumentacion.BorrarCacheControlFichaRecursos(idDoc);
                }
            }

            proyCN.Dispose();
            identidadCN.Dispose();
            personaCN.Dispose();
            docCN.Dispose();
        }

        /// <summary>
        /// Adds a comment in a resource. It can be a response of another parent comment.
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier Guid
        /// user_short_name = publisher user short name string
        /// html_description = Html content of the comment wrapped in a Html paragraph and special characters encoded in ANSI. Example: <p>Descripci&amp;oacute;n del comentario</p> string
        /// parent_comment_id  = optional parent comment identifier Guid. The current comment is its answer
        /// comment_date = publish date of the comment DateTime
        /// publish_home = indicates whether the home must be updated bool</param>
        /// <example>POST resource/comment</example>
        /// <returns>Comment identifier Guid</returns>
        [HttpPost, Route("comment")]
        public Guid Comment(CommentParams parameters)
        {
            //Comprobaciones
            if (string.IsNullOrEmpty(parameters.html_description))
            {
                throw new GnossException("The parameter 'html_ description' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            if (parameters.comment_date == null)
            {
                throw new GnossException("The parameter 'comment_date' cannot be null or empty.", HttpStatusCode.BadRequest);
            }

            mNombreCortoComunidad = parameters.community_short_name;
            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

            if (identidad == null)
            {
                throw new GnossException("The OAuth user is not member of the community " + parameters.community_short_name + ".", HttpStatusCode.BadRequest);
            }

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid baseRecursosProyectoID = proyCN.ObtenerBaseRecursosProyectoPorProyectoID(FilaProy.ProyectoID);
            proyCN.Dispose();

            //Comprobar si el documento existe
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            gestorDoc.CargarDocumentos(false);

            if (!gestorDoc.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist in the community " + parameters.community_short_name, HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[parameters.resource_id];

            if (!documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
            {
                throw new GnossException("The OAuth user has not editing resource permission.", HttpStatusCode.BadRequest);
            }

            if (gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos.Count(doc => doc.DocumentoID.Equals(parameters.resource_id) && doc.BaseRecursosID.Equals(baseRecursosProyectoID)) == 0)
            {
                throw new Exception("El documento con ID '" + parameters.resource_id + "' no se encuentra en la comunidad '" + parameters.community_short_name + "'");
            }
            UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
            if (parameters.UserId.Equals(Guid.Empty))
            {
                parameters.user_short_name = usuarioCN.ObtenerNombreCortoUsuarioPorID(parameters.UserId);
            }
            Guid comentarioID = AgregarNuevoComentario(FilaProy.ProyectoID, parameters.community_short_name, parameters.user_short_name, parameters.comment_date, parameters.html_description, parameters.parent_comment_id);

            //Guardamos la relación entre un comentario y un documento.
            //Aumentar el número de comentarios que tiene el recurso: 
            DocumentoWebVinBaseRecursos docWebBR = gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos.FirstOrDefault(doc => doc.DocumentoID.Equals(parameters.resource_id) && doc.BaseRecursosID.Equals(baseRecursosProyectoID));
            docWebBR.NumeroComentarios = docWebBR.NumeroComentarios + 1;

            DocumentoComentario docComentario = new DocumentoComentario();
            docComentario.ComentarioID = comentarioID;
            docComentario.DocumentoID = parameters.resource_id;
            docComentario.ProyectoID = FilaProy.ProyectoID;

            gestorDoc.DataWrapperDocumentacion.ListaDocumentoComentario.Add(docComentario);
            mEntityContext.DocumentoComentario.Add(docComentario);

            docCN.ActualizarDocumentacion();
            docCN.Dispose();

            //Agregamos el comentario al Base.
            ControladorDocumentacion.AgregarComentarioModeloBaseSimple(comentarioID, FilaProy.ProyectoID, 1, PrioridadBase.Alta, mAvailableServices);

            //Limpiar la caché del recurso para que se vean los comentarios nuevos para los usuarios desconectados.
            DocumentacionCL docCL = new DocumentacionCL("recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
            docCL.InvalidarCacheQueContengaCadena(parameters.resource_id.ToString());
            docCL.Dispose();

            if (parameters.publish_home)
            {
                int tipo;
                switch (documento.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, comentarioID, AccionLive.ComentarioAgregado, tipo, "base", PrioridadLive.Alta, mAvailableServices);
            }

            return comentarioID;
        }

        /// <summary>
        /// Creates a basic ontology resource
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier guid
        /// title = resource title string
        /// description = resource description string
        /// tags = resource tags list string
        /// categories = resource categories guid list
        /// resource_type = resource type short
        ///     Hipervinculo = 0,
        ///     ReferenciaADoc = 1,
        ///     Video = 2,
        ///     Archivo digital = 3,
        ///     Semantico = 5,
        ///     Imagen = 6,
        ///     Ontologia = 7,
        ///     Nota = 8,
        ///     Wiki = 9
        /// resource_url = depending on the type of file uploaded, it will be the URL of the link, the name of the digital file, or the URL of the ontology to which the resource belongs. string
        /// resource_file = (OPTIONAL) Bytes array of the resource that is published
        /// resource_attached_files = (OPTIONAL) resource attached files List SemanticAttachedResource
        /// SemanticAttachedResource:
        ///     file_rdf_properties = (OPCIONAL) Valores de las propiedades rdf que son de tipo archivo
        ///     file_property_type = (OPCIONAL) Tipos de archivos de las propiedades de tipo archivo
        ///     rdf_attached_files = (OPCIONAL) Archivos adjuntos que se suben
        ///     delete_file = indicates resource attached file deleting
        /// creator_is_author = indicates whether the creator is the author
        /// authors = resource authors
        /// auto_tags_title_text = text to label tags in title mode
        /// auto_tags_description_text = text to label tags in description mode
        /// create_screenshot = indicates if the screenshot must be created or not bool
        /// url_screenshot = url where is the image capture or from where it is generated string
        /// predicate_screenshot = rdf property where the screenshot is saved
        /// screenshot_sizes = screenshot sizes available List
        /// priority = action priority int
        /// visibility = resource visibility short:
        ///     open = 0 All users can view the resource
        ///     editors = 1 Only editors can view the resource
        ///     communitymembers = 2 Only community members can view the resource
        ///     specific = 3 Specific users can view the resource
        /// readers_list = resource readers List:
        /// editors_list = resource editors List:
        ///     user_short_name = user short name string
        ///     group_short_name = community group short name
        ///     organization_short_name = organization short name
        /// creation_date = resource creation date nullable datetime
        /// publisher_email = resource publisher email string
        /// publish_home = indicates whether the home must be updated
        /// charge_id = charge identifier string
        /// main_image = main image string
        /// end_of_charge = bool that marks the end of the charge
        /// </param>
        /// <returns>resource identifier guid</returns>
        /// <example>POST resource/create-basic-ontology-resource</example>
        [HttpPost, Route("create-basic-ontology-resource")]
        public string CreateBasicOntologyResource(LoadResourceParams parameters)
        {
            parameters.priority = (int)PrioridadBase.ApiRecursos;
            if (parameters.end_of_load)
            {
                parameters.priority = (int)PrioridadBase.ApiRecursosBorrarCache;
            }

            return SubirRecursoInt(parameters);
        }

        /// <summary>
        /// Creates a complex ontology resource
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier guid
        /// title = resource title string
        /// description = resource description string
        /// tags = resource tags list string
        /// categories = resource categories guid list
        /// resource_type = resource type short
        ///     Hipervinculo = 0,
        ///     ReferenciaADoc = 1,
        ///     Video = 2,
        ///     Archivo digital = 3,
        ///     Semantico = 5,
        ///     Imagen = 6,
        ///     Ontologia = 7,
        ///     Nota = 8,
        ///     Wiki = 9
        /// resource_url = depending on the type of file uploaded, it will be the URL of the link, the name of the digital file, or the URL of the ontology to which the resource belongs. string
        /// resource_file = (OPTIONAL) Bytes array of the resource that is published
        /// resource_attached_files = (OPTIONAL) resource attached files List
        /// SemanticAttachedResource:
        ///     file_rdf_properties = (OPCIONAL) Valores de las propiedades rdf que son de tipo archivo
        ///     file_property_type = (OPCIONAL) Tipos de archivos de las propiedades de tipo archivo
        ///     rdf_attached_files = (OPCIONAL) Archivos adjuntos que se suben
        ///     delete_file = indicates resource attached file deleting
        /// creator_is_author = indicates whether the creator is the author
        /// authors = resource authors
        /// auto_tags_title_text = text to label tags in title mode
        /// auto_tags_description_text = text to label tags in description mode
        /// create_screenshot = indicates if the screenshot must be created or not bool
        /// url_screenshot = url where is the image capture or from where it is generated string
        /// predicate_screenshot = rdf property where the screenshot is saved
        /// screenshot_sizes = screenshot sizes available List
        /// priority = action priority int
        /// visibility = resource visibility short:
        ///     open = 0 All users can view the resource
        ///     editors = 1 Only editors can view the resource
        ///     communitymembers = 2 Only community members can view the resource
        ///     specific = 3 Specific users can view the resource
        /// readers_list = resource readers List:
        /// editors_list = resource editors List:
        ///     user_short_name = user short name string
        ///     group_short_name = community group short name
        ///     organization_short_name = organization short name
        /// creation_date = resource creation date nullable datetime
        /// publisher_email = resource publisher email string
        /// publish_home = indicates whether the home must be updated
        /// charge_id = charge identifier string
        /// main_image = main image string
        /// end_of_charge = bool that marks the end of the charge
        /// </param>
        /// <returns>resource identifier guid</returns>
        /// <example>POST resource/create-complex-ontology-resource</example>
        [HttpPost, Route("create-complex-ontology-resource")]
        public string CreateComplexOntologyResource(LoadResourceParams parameters)
        {
            try
            {
                //mLoggingService.GuardarLogError("Entra en  CreateComplexOntologyResource");

                parameters.priority = (int)PrioridadBase.ApiRecursos;
                if (parameters.end_of_load)
                {
                    parameters.priority = (int)PrioridadBase.ApiRecursosBorrarCache;
                }

                return SubirRecursoInt(parameters);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                throw;
            }
        }


        /// <summary>
        /// Loads the images of a not yet loaded resource.
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier guid
        /// resource_attached_files = resource attached files. List of SemanticAttachedResource
        /// SemanticAttachedResource:
        ///     file_rdf_properties = image name
        ///     file_property_type = type of file
        ///     rdf_attached_files = image to load byte[]
        /// main_image = main image string
        /// </param>
        [HttpPost, Route("upload-images")]
        public bool UploadImages(UploadImagesParams parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            bool esAdmin = proyCN.EsUsuarioAdministradorProyecto(UsuarioOAuth, FilaProy.ProyectoID) || EsAdministradorProyectoMyGnoss(UsuarioOAuth);
            proyCN.Dispose();

            if (!esAdmin)
            {
                throw new GnossException($"The OAuth user does not manage the community {parameters.community_short_name}.", HttpStatusCode.BadRequest);
            }

            SubirArchivosDelRDF(parameters.resource_id, parameters.resource_attached_files, null, parameters.main_image);

            ControladorDocumentacion.InsertarEnColaProcesarFicherosRecursosModificadosOEliminados(parameters.resource_id, TipoEventoProcesarFicherosRecursos.Modificado, mAvailableServices);

            return true;
        }

        /// <summary>
        /// Creates a new massive data load
        /// </summary>
        /// <param name="parameters">Parameters of the load</param>
        /// <returns>True if the load is added to the sql table</returns>
        [HttpPost, Route("create-massive-load")]
        public bool CreateMassiveDataLoad(MassiveDataLoadResource parameters)
        {
            bool load = false;
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            //UsuarioOAuth
            parameters.project_id = proyCN.ObtenerProyectoIDPorNombreCorto(parameters.community_name);
            parameters.state = 0;
            parameters.date_create = DateTime.Now;

            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
            List<Guid> listaUsuario = new List<Guid>();

            listaUsuario.Add(UsuarioOAuth);
            Dictionary<Guid, Guid> dic = personaCN.ObtenerPersonasIDDeUsuariosID(listaUsuario);
            DataWrapperIdentidad identidades = identCN.ObtenerIdentidadDePersonaEnProyecto(parameters.project_id, dic[UsuarioOAuth]);
            if (identidades?.ListaIdentidad != null && identidades?.ListaIdentidad.Count > 0)
            {
                parameters.identity_id = identidades.ListaIdentidad.FirstOrDefault().IdentidadID;
                if (proyCN.EsIdentidadAdministradorProyecto(parameters.identity_id, parameters.project_id, TipoRolUsuario.Administrador))
                {
                    load = proyCN.CrearNuevaCargaMasiva(parameters.load_id, parameters.state, parameters.date_create, parameters.project_id, parameters.identity_id, parameters.ontology, parameters.name, ProyectoAD.MetaOrganizacion);
                }
                else
                {
                    throw new GnossException("Invalid Oauth. Insufficient permissions.", HttpStatusCode.Forbidden);
                }
            }
            else
            {
                throw new GnossException("The user doesn't participate in the community", HttpStatusCode.BadRequest);
            }

            return load;
        }

        /// <summary>
        /// Creates a new massive data load package
        /// </summary>
        /// <param name="parameters">Parameters of the package</param>
        /// <returns>True if the package is load to the sql table</returns>
        [HttpPost, Route("create-massive-load-package")]
        public bool CreateMassiveDataLoadPackage(MassiveDataLoadPackageResource parameters)
        {
            if (parameters == null)
            {
                parameters = (MassiveDataLoadPackageResource)ObtenerVariablesDePeticion(typeof(MassiveDataLoadPackageResource).FullName);
            }

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);

            parameters.state = (short)EstadoPaquete.Pendiente;
            parameters.error = "";
            parameters.date_creation = DateTime.Now;
            parameters.date_processing = null;
            parameters.comprimido = false;

            List<Guid> listaUsuario = new List<Guid>();

            Carga carga = mEntityContext.Carga.Where(item => item.CargaID.Equals(parameters.load_id)).FirstOrDefault();

            if (carga != null && carga.Estado == 0)
            {
                if (proyCN.EsIdentidadAdministradorProyecto(carga.IdentidadID.Value, carga.ProyectoID.Value, TipoRolUsuario.Administrador))
                {
                    DatosRabbitCarga datos = new DatosRabbitCarga();
                    datos.CargaID = parameters.load_id;
                    datos.PaqueteID = parameters.package_id;
                    datos.UrlTriplesOntologia = parameters.ontology_rute;
                    datos.UrlTriplesBusqueda = parameters.search_rute;
                    datos.UrlDatosAcido = parameters.sql_rute;
                    datos.BytesDatosAcido = parameters.sql_bytes;
                    datos.BytesTriplesBusqueda = parameters.search_bytes;
                    datos.BytesTriplesOntologia = parameters.ontology_bytes;

                    if (parameters.isLast)
                    {
                        carga.Estado = 1;
                        mEntityContext.SaveChanges();
                    }

					if (mAvailableServices.CheckIfServiceIsAvailable(mAvailableServices.GetBackServiceCode(BackgroundService.MassiveDataLoader), ServiceType.Background))
                    {
						using (RabbitMQClient rabbitMq = new RabbitMQClient(RabbitMQClient.BD_SERVICIOS_WIN, "ColaDescargaMasiva", mLoggingService, mConfigService, mLoggerFactory.CreateLogger<RabbitMQClient>(), mLoggerFactory))
						{
							rabbitMq.AgregarElementoACola(JsonConvert.SerializeObject(datos));
						}
					}

                    return true;
                }
                else
                {
                    throw new GnossException("Invalid Oauth. Insufficient permissions.", HttpStatusCode.Forbidden);
                }
            }
            else
            {
                throw new GnossException("The load doesn't exit or is closed.", HttpStatusCode.BadRequest);
            }
        }

        [NonAction]
        public object ObtenerVariablesDePeticion(string pNombreClase)
        {
            HttpContext.Request.Body.Position = 0;
            string parametrosPeticion = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            object objetoPeticion = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(pNombreClase);

            string[] listaParametros = parametrosPeticion.Split(",\"".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            List<string> listaVariables = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(pNombreClase).GetType().GetProperties().Select(item => item.Name).ToList();
            int contador = 0;
            foreach (string nombreVariable in listaVariables)
            {
                if (parametrosPeticion.Contains($"{nombreVariable}\":"))
                {
                    Type tipoVariable = objetoPeticion.GetType().GetProperty(nombreVariable).PropertyType;
                    string caracteresInicioVariable = "\":";
                    string caracteresFinalVariable = "\",\"";

                    int inicioValorVariable = parametrosPeticion.IndexOf(caracteresInicioVariable);

                    if (!parametrosPeticion[inicioValorVariable + 2].Equals('\"'))
                    {
                        caracteresFinalVariable = ",\"";
                    }
                    if (contador + 1 == listaVariables.Count)
                    {
                        caracteresFinalVariable = "}";
                    }

                    int finValorVariable = parametrosPeticion.IndexOf(caracteresFinalVariable) - inicioValorVariable;
                    string valor = FormatearValor(parametrosPeticion.Substring(inicioValorVariable, finValorVariable));
                    objetoPeticion.GetType().GetProperty(nombreVariable).SetValue(objetoPeticion, TransformarValor(valor, tipoVariable), null);

                    if (contador + 1 != listaVariables.Count)
                    {
                        int inicioSubstring = inicioValorVariable + finValorVariable + 2;
                        parametrosPeticion = parametrosPeticion.Substring(inicioSubstring, parametrosPeticion.Count() - inicioSubstring);
                    }
                }

                contador++;
            }

            return objetoPeticion;
        }



        private string FormatearValor(string pValor)
        {
            if (pValor.StartsWith("\":\""))
            {
                return pValor.Replace("\":\"", "");
            }
            else
            {
                return pValor.Replace("\":", "");
            }
        }

        private object TransformarValor(string pValor, Type pTipoVariable)
        {
            if (!string.IsNullOrEmpty(pValor))
            {
                if (pTipoVariable.Equals(typeof(Guid)))
                {
                    return new Guid(pValor);
                }
                else if (pTipoVariable.Equals(typeof(char[])))
                {
                    return pValor.ToCharArray();
                }
                else if (pTipoVariable.Equals(typeof(byte[])))
                {
                    pValor = pValor.Replace("[", "");
                    pValor = pValor.Replace("]", "");
                    string[] bytes = pValor.Split(',');
                    byte[] byteArray = new byte[bytes.Length];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (bytes[i].Contains("-"))
                        {
                            int valorNegativo = int.Parse(bytes[i]);
                            bytes[i] = (256 + valorNegativo).ToString();
                        }
                        byteArray[i] = Convert.ToByte(bytes[i]);
                    }

                    return byteArray;
                }
                else if (pTipoVariable.Equals(typeof(DateTime)))
                {
                    return Convert.ToDateTime(pValor);
                }
                else if (pTipoVariable.Equals(typeof(int)))
                {
                    return int.Parse(pValor);
                }
                else if (pTipoVariable.Equals(typeof(bool)))
                {
                    return bool.Parse(pValor);
                }
                else
                {
                    return pValor;
                }
            }

            return pValor;
        }

        /// <summary>
        /// Return load state
        /// </summary>
        /// <param name="pLoadId">Load id</param>
        /// <returns>Load state</returns>
        [HttpPost, Route("load-state")]
        public EstadoCargaModel LoadState([FromBody] Guid pLoadId)
        {
            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            EstadoCargaModel estadoCarga = new EstadoCargaModel();

            List<CargaPaquete> listaPaquetes = proyectoCN.ObtenerPaquetesPorIDCarga(pLoadId);
            estadoCarga.NumPaquetesCorrectos = listaPaquetes.Count(item => item.Estado.Equals((short)EstadoPaquete.Correcto));
            estadoCarga.NumPaquetesErroneos = listaPaquetes.Count(item => item.Estado.Equals((short)EstadoPaquete.Erroneo));
            estadoCarga.NumPaquetesPendientes = listaPaquetes.Count(item => item.Estado.Equals((short)EstadoPaquete.Pendiente));

            estadoCarga.EstadoCarga = EstadoCarga.Pendiente;

            if (estadoCarga.NumPaquetesErroneos > 0 && estadoCarga.NumPaquetesPendientes == 0)
            {
                estadoCarga.EstadoCarga = EstadoCarga.FinalizadaConErrores;
            }
            else if (estadoCarga.NumPaquetesPendientes > 0)
            {
                if (estadoCarga.NumPaquetesCorrectos > 0)
                {
                    estadoCarga.EstadoCarga = EstadoCarga.EnProceso;
                    estadoCarga.Cerrado = false;
                }
            }
            else if (estadoCarga.NumPaquetesCorrectos > 0 && estadoCarga.NumPaquetesErroneos == 0)
            {
                estadoCarga.EstadoCarga = EstadoCarga.Finalizada;
            }

            if (proyectoCN.ObtenerDatosCargaPorID(pLoadId).Estado == 1)
            {
                estadoCarga.Cerrado = true;
            }
            else
            {
                estadoCarga.Cerrado = false;
            }

            return estadoCarga;
        }

        /// <summary>
        /// Test a massive data load
        /// </summary>
        /// <param name="resource">Massive data load test resource</param>
        [HttpPost, Route("test-massive-load")]
        public bool TestMassiveDataLoad(MassiveDataLoadTestResource resource)
        {
            try
            {
                //download nq file
                WebClient client = new WebClient();
                byte[] downloadedData = client.DownloadData(resource.url);

                //summarie of downloaded nq file
                byte[] downloadedFileHash = new MD5CryptoServiceProvider().ComputeHash(downloadedData);

                //if the summaries are different, something is wrong
                if (!resource.fileHash.SequenceEqual(downloadedFileHash))
                {
                    Console.Error.Write("The connection to the server could not be established or nq files are not supported.");
                    throw new WebException("The connection to the server could not be established or nq files are not supported.");
                }
                return true;
            }
            catch (Exception)
            {
                throw new WebException($"The connection to the server could not be established or nq files are not supported. {resource.url}");
            }
        }

        /// <summary>
        /// Close a massive data load
        /// </summary>
        /// <param name="resource">Massive data load identifier</param>
        /// <returns>True if the load is closed</returns>
        [HttpPost, Route("close-massive-load")]
        public bool CloseMassiveDataLoad(CloseMassiveDataLoadResource resource)
        {
            Carga carga = mEntityContext.Carga.Where(x => x.CargaID.Equals(resource.DataLoadIdentifier)).FirstOrDefault();
            if (carga != null)
            {
                carga.Estado = 1;
                mEntityContext.SaveChanges();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Modify a categories resource
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="pResouceID">Resource identifier</param>
        /// <param name="pCategoriesIDs">Categories to modify</param>
        /// <returns>True if modify correct</returns>
        [HttpPost, Route("chage-categories-resource")]
        public bool ModifyCategoriasRecursoInt(ModifyResourceCategories parameters)
        {
            try
            {
                return ModificarCategoriasRecursoInt(parameters);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="pResourceID">Recurso</param>
        /// <param name="pCategoriasIDs">Identificadores de las categorías del tesauro de la comunidad en las que se indexa el recurso</param>
        /// <returns>TRUE si es correcto</returns>
        private bool ModificarCategoriasRecursoInt(ModifyResourceCategories parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;
            if (FilaProy != null)
            {
                ComprobacionCambiosCachesLocales(FilaProy.ProyectoID);
            }
            bool usarReplicacion = false;

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DocumentoWeb documentoEdicion = null;

            string nombreOntologia = string.Empty;
            Elementos.Documentacion.Documento documentoAntiguo = null;

            bool documentoBloqueado = ComprobarDocumentoEnEdicion(parameters.resource_id, identidad.Clave);
            try
            {

                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                List<Guid> listaDocs = new List<Guid>();
                listaDocs.Add(parameters.resource_id);
                docCN.ObtenerVersionDocumentosPorIDs(gestorDoc.DataWrapperDocumentacion, listaDocs, true);
                gestorDoc.CargarDocumentos(false);

                documentoAntiguo = gestorDoc.ListaDocumentos[parameters.resource_id];

                bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);
                Dictionary<string, object> diccionarioParametros = new Dictionary<string, object>();
                if (documentoAntiguo.TipoDocumentacion != TiposDocumentacion.Semantico)
                {
                    List<object> listaParametros = new List<object>();
                    listaParametros.Add(parameters.categories);
                    listaParametros.Add(new List<Guid>(gestorDoc.GestorTesauro.ListaCategoriasTesauro.Keys));
                    diccionarioParametros.Add("categorias", listaParametros);
                }
                documentoEdicion = new DocumentoWeb(documentoAntiguo.FilaDocumento, gestorDoc);
                ValidarDatosRecurso(diccionarioParametros);

                string urlDoc = "";
                docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

                if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                {
                    gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(documentoEdicion.ElementoVinculadoID));

                    gestorDoc.CargarDocumentos(false);

                    urlDoc = UrlIntragnoss + gestorDoc.ListaDocumentos[documentoEdicion.ElementoVinculadoID].Enlace;
                }

                try
                {

                    #region Categorias del tesauro

                    if ((parameters.categories != null && parameters.categories.Count > 0) || documentoEdicion != null)
                    {
                        ReplaceCategories(parameters.categories, gestorDoc, documentoEdicion, identidad.Clave);
                    }

                    #endregion

                    if (AgregarColaBase && (documentoEdicion.FilaDocumento.Tipo != (short)TiposDocumentacion.Semantico || !usarReplicacion))
                    {
                        ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                        controDoc.mActualizarTodosProyectosCompartido = true;
                        controDoc.NotificarModificarTagsRecurso(documentoEdicion.Clave, mAvailableServices);
                    }
                }
                catch (Exception ex)
                {
                    if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                    {
                        mEntityContext.TerminarTransaccionesPendientes(false);
                        //hacer el rollback de virtuoso con el rdf viejo entidadesPrincAntiguas
                        mLoggingService.GuardarLogError(ex, $"ModificarCategoriasRecursoInt: Error al guardar modificaciones en BD Ácida. Se han revertido los cambios en Virtuoso y BD RDF del recurso {parameters.resource_id}",mlogger);
                    }
                    throw;
                }
            }
            finally
            {
                if (documentoBloqueado)
                {
                    docCN.FinalizarEdicionRecurso(parameters.resource_id);
                }
            }


            #region Actualizamos la cache
            try
            {
                //Borrar la caché de la ficha del recurso
                DocumentacionCL documentacionCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                documentacionCL.BorrarControlFichaRecursos(parameters.resource_id);
                documentacionCL.Dispose();
            }
            catch (Exception) { }
            #endregion
            docCN.Dispose();

            return true;
        }

        /// <summary>
        /// Modifies a complex ontology resource
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier guid
        /// title = resource title string
        /// description = resource description string
        /// tags = resource tags list string
        /// categories = resource categories guid list
        /// resource_type = resource type short
        ///     Hipervinculo = 0,
        ///     ReferenciaADoc = 1,
        ///     Video = 2,
        ///     Archivo digital = 3,
        ///     Semantico = 5,
        ///     Imagen = 6,
        ///     Ontologia = 7,
        ///     Nota = 8,
        ///     Wiki = 9
        /// resource_url = depending on the type of file uploaded, it will be the URL of the link, the name of the digital file, or the URL of the ontology to which the resource belongs. string
        /// resource_file = (OPTIONAL) Bytes array of the resource that is published
        /// resource_attached_files = (OPTIONAL) resource attached files List
        /// SemanticAttachedResource:
        ///     file_rdf_properties = (OPCIONAL) Valores de las propiedades rdf que son de tipo archivo
        ///     file_property_type = (OPCIONAL) Tipos de archivos de las propiedades de tipo archivo
        ///     rdf_attached_files = (OPCIONAL) Archivos adjuntos que se suben
        ///     delete_file = indicates resource attached file deleting
        /// creator_is_author = indicates whether the creator is the author
        /// authors = resource authors
        /// auto_tags_title_text = text to label tags in title mode
        /// auto_tags_description_text = text to label tags in description mode
        /// create_screenshot = indicates if the screenshot must be created or not bool
        /// url_screenshot = url where is the image capture or from where it is generated string
        /// predicate_screenshot = rdf property where the screenshot is saved
        /// screenshot_sizes = screenshot sizes available List
        /// priority = action priority int
        /// visibility = resource visibility short:
        ///     open = 0 All users can view the resource
        ///     editors = 1 Only editors can view the resource
        ///     communitymembers = 2 Only community members can view the resource
        ///     specific = 3 Specific users can view the resource
        /// readers_list = resource readers List:
        /// editors_list = resource editors List:
        ///     user_short_name = user short name string
        ///     group_short_name = community group short name
        ///     organization_short_name = organization short name
        /// creation_date = resource creation date nullable datetime
        /// publisher_email = resource publisher email string
        /// publish_home = indicates whether the home must be updated
        /// charge_id = charge identifier string
        /// main_image = main image string
        /// end_of_charge = bool that marks the end of the charge
        /// </param>
        /// <returns>resource identifier guid</returns>
        /// <example>POST resource/modify-complex-ontology-resource</example>
        [HttpPost, Route("modify-complex-ontology-resource")]
        public bool ModifyComplexOntologyResource(LoadResourceParams parameters)
        {
            try
            {
                parameters.priority = (int)PrioridadBase.ApiRecursos;
                if (parameters.end_of_load)
                {
                    parameters.priority = (int)PrioridadBase.ApiRecursosBorrarCache;
                }

                return ModificarRecursoInt(parameters);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                throw;
            }
        }

        /// <summary>
        /// Modifies a basic ontology resource
        /// </summary>
        /// <param name="parameters">
        /// community_short_name = community short name string
        /// resource_id = resource identifier guid
        /// title = resource title string
        /// description = resource description string
        /// tags = resource tags list string
        /// categories = resource categories guid list
        /// resource_type = resource type short
        ///     Hipervinculo = 0,
        ///     ReferenciaADoc = 1,
        ///     Video = 2,
        ///     Archivo digital = 3,
        ///     Semantico = 5,
        ///     Imagen = 6,
        ///     Ontologia = 7,
        ///     Nota = 8,
        ///     Wiki = 9
        /// resource_url = depending on the type of file uploaded, it will be the URL of the link, the name of the digital file, or the URL of the ontology to which the resource belongs. string
        /// resource_file = (OPTIONAL) Bytes array of the resource that is published
        /// resource_attached_files = (OPTIONAL) resource attached files List
        /// SemanticAttachedResource:
        ///     file_rdf_properties = (OPCIONAL) Valores de las propiedades rdf que son de tipo archivo
        ///     file_property_type = (OPCIONAL) Tipos de archivos de las propiedades de tipo archivo
        ///     rdf_attached_files = (OPCIONAL) Archivos adjuntos que se suben
        ///     delete_file = indicates resource attached file deleting
        /// creator_is_author = indicates whether the creator is the author
        /// authors = resource authors
        /// auto_tags_title_text = text to label tags in title mode
        /// auto_tags_description_text = text to label tags in description mode
        /// create_screenshot = indicates if the screenshot must be created or not bool
        /// url_screenshot = url where is the image capture or from where it is generated string
        /// predicate_screenshot = rdf property where the screenshot is saved
        /// screenshot_sizes = screenshot sizes available List
        /// priority = action priority int
        /// visibility = resource visibility short:
        ///     open = 0 All users can view the resource
        ///     editors = 1 Only editors can view the resource
        ///     communitymembers = 2 Only community members can view the resource
        ///     specific = 3 Specific users can view the resource
        /// readers_list = resource readers List:
        /// editors_list = resource editors List:
        ///     user_short_name = user short name string
        ///     group_short_name = community group short name
        ///     organization_short_name = organization short name
        /// creation_date = resource creation date nullable datetime
        /// publisher_email = resource publisher email string
        /// publish_home = indicates whether the home must be updated
        /// charge_id = charge identifier string
        /// main_image = main image string
        /// end_of_charge = bool that marks the end of the charge
        /// </param>
        /// <returns>resource identifier guid</returns>
        /// <example>POST resource/modify-basic-ontology-resource</example>
        [HttpPost, Route("modify-basic-ontology-resource")]
        public bool ModifyBasicOntologyResource(LoadResourceParams parameters)
        {
            parameters.priority = (int)PrioridadBase.ApiRecursos;
            if (parameters.end_of_load)
            {
                parameters.priority = (int)PrioridadBase.ApiRecursosBorrarCache;
            }

            return ModificarRecursoInt(parameters);
        }

        /// <summary>
        /// Method to add / modify / delete triples of complex ontology resource
        /// * Modify: Pass the old object and the new object
        /// * Delete: Pass only the old object
        /// * Add: Pass only the new object
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier guid
        /// resource_triples = 
        ///     predicate = predicate to modify with pipe splitting for the diferent levels
        ///     old_object = old object to replace
        ///     new_object = new object to replace
        ///     gnoss_property = enumeration that indicates the gnoss property to edit, if it is necessary
        /// resource_attached_files = (OPTIONAL) resource attached files list of SemanticAttachedResource.
        /// SemanticAttachedResource:
        ///     file_rdf_properties = (OPCIONAL) Valores de las propiedades rdf que son de tipo archivo
        ///     file_property_type = (OPCIONAL) Tipos de archivos de las propiedades de tipo archivo
        ///     rdf_attached_files = (OPCIONAL) Archivos adjuntos que se suben
        ///     delete_file = indicates resource attached file deleting
        /// main_image = main image string
        /// publish_home = indicates whether the home must be updated
        /// charge_id = charge identifier string
        /// </param>
        [HttpPost, Route("modify-triple-list")]
        public void ModifyTripleList(ModifyResourceTripleListParams parameters)
        {

            mNombreCortoComunidad = parameters.community_short_name;
            ModificarListaDeTripletasPorRecursoInt(parameters);
        }


        /// <summary>
        /// Method to modify the resource's subtype
        /// </summary>
        /// <param name="parameters">
        /// resource_id = resource identifier guid
        /// ontology_name = The ontology name of the resource to modify
        /// subtype = The subtype of the resource to modify
        /// user_id = User that try to modify the resource
        /// </param>
        [HttpPost, Route("modify-subtype")]
        public void ModifySubtype(ModifyResourceSubtype parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;
            ModificarSubtipoRecursoInt(parameters);
        }

        /// <summary>
        /// Modfies a property of a resource
        /// </summary>
        /// <param name="parameters"></param>
        [HttpPost, Route("modify-property")]
        public void ModifyProperty(ModifyResourceProperty parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;
            ModificarPropiedadRecursoInt(parameters);
        }

        /// <summary>
        /// Get meta keywords of ontology
        /// </summary>
        /// <param name="parameters"></param>
        [HttpPost, Route("get-metakeywords")]
        public Dictionary<Guid, List<MetaKeyword>> GetMetakeywords(GetMetakeywordsModel parameters)
        {
            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            AD.EntityModel.Models.Documentacion.Documento documento = documentacionCN.ObtenerDocumentoPorEnlace(parameters.ontology_name);
            Dictionary<Guid, List<MetaKeyword>> dicMetaKeywords = new Dictionary<Guid, List<MetaKeyword>>();
            if (documento != null)
            {
                CallFileService servicioArch = new CallFileService(mConfigService, mLoggingService);
                byte[] byteArray = servicioArch.ObtenerXmlOntologiaBytes(documento.DocumentoID);

                if (byteArray != null)
                {
                    UtilidadesFormulariosSemanticos.ObtenerMetaEtiquetasXMLOntologia(byteArray, dicMetaKeywords, documento.DocumentoID);
                    return dicMetaKeywords;
                }
            }
            else
            {
                throw new GnossException($"The document with id {parameters.resource_id} isn't exist", HttpStatusCode.InternalServerError);
            }

            return null;
        }

        /// <summary>
        /// Eliminar la cache de los recursos
        /// </summary>
        /// <param name="parameters"></param>
        [HttpPost, Route("delete-cache-resources")]
        public bool DeleteCacheResources(Guid project_id)
        {
            try
            {
                DocumentacionCL docCLRec = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                docCLRec.FlushDB();
                docCLRec.Dispose();

                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(project_id, null);
                facetadoCL.Dispose();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// Modfies a list of triples of any resource
        /// </summary>
        /// <param name="parameters"></param>
        [HttpPost, Route("masive-triple-modify")]
        public void MassiveTripleModify(MassiveTripleModifyParameters parameters)
        {
            try
            {
                StringBuilder sbDeleteOntology = new StringBuilder();
                StringBuilder sbDeleteCommunity = new StringBuilder();
                StringBuilder sbInsertOntology = new StringBuilder();
                StringBuilder sbInsertCommunity = new StringBuilder();

                string type = parameters.ontology.Replace(".owl", "");
                if (type.Contains("/"))
                {
                    type = type.Substring(type.LastIndexOf("/") + 1);
                }

                mNombreCortoComunidad = parameters.community_short_name;

                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL    >(), mLoggerFactory);
                Guid proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(parameters.community_short_name);

                //Obtengo el Tipo de cada propiedad
                FacetaCL facetaCL = new FacetaCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetaCL>(), mLoggerFactory);
                DataWrapperFacetas dataWrapperFacetas = facetaCL.ObtenerTodasFacetasDeProyecto(new List<string> { type }, ProyectoAD.MetaOrganizacion, proyectoID, false);
                Dictionary<string, List<string>> informacionOntologias = facetaCL.ObtenerPrefijosOntologiasDeProyecto(proyectoID);
                string auxRdfType;

                FacetaCN facetaCN = new FacetaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetaCN>(), mLoggerFactory);
                facetaCN.CargarFacetaConfigProyRanfoFecha(ProyectoAD.MetaOrganizacion, proyectoID, dataWrapperFacetas);

                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                Dictionary<string, Guid> ontologiasProyecto = proyCN.ObtenerOntologiasConIDPorNombreCortoProy(parameters.community_short_name);
                Dictionary<string, Guid> mainResourIDResourceID = new Dictionary<string, Guid>();

                parameters.triples.ForEach(massiveTriple =>
                {
                    Guid resourceId = Guid.Empty;
                    if (massiveTriple.main_resource_id.LastIndexOf('_') > 0)
                    {
                        string recursoId = massiveTriple.main_resource_id.Substring(0, massiveTriple.main_resource_id.LastIndexOf('_'));
                        if (recursoId.LastIndexOf('_') > 0)
                        {
                            recursoId = recursoId.Substring(recursoId.LastIndexOf('_') + 1);
                        }
                        Guid.TryParse(recursoId, out resourceId);
                    }
                    if (resourceId.Equals(Guid.Empty))
                    {
                        throw new GnossException($"Parameter main_resource_id is not well formed. Must be something like http://gnoss.com/items/Product_0015bb97-81fd-5ee7-a70d-4474f4c723e9_9b8d4c13-b443-4e3c-9a93-252d9e12df8a: {massiveTriple.main_resource_id}", HttpStatusCode.BadRequest);
                    }

                    if (!mainResourIDResourceID.ContainsKey(massiveTriple.main_resource_id))
                    {
                        mainResourIDResourceID.Add(massiveTriple.main_resource_id, resourceId);
                    }
                }
                );

                //Obtengo los proyectos en los que está compartido cada recurso
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                Dictionary<Guid, List<Guid>> listaProyectosPorDocumento = docCN.ObtenerProyectosEstanCompartidosDocsPorID(mainResourIDResourceID.Values.Distinct().ToList());
                parameters.ontology = parameters.ontology.ToLower();

                if (ontologiasProyecto.ContainsKey(parameters.ontology))
                {
                    Guid ontologiaID = ontologiasProyecto[parameters.ontology];
                    Ontologia ontologia = ObtenerOntologia(ontologiaID);

                    foreach (MassiveTriple triple in parameters.triples)
                    {
                        Guid resourceId = mainResourIDResourceID[triple.main_resource_id];

                        if (!string.IsNullOrEmpty(triple.old_value))
                        {
                            AddTripleToStringBuilder(sbDeleteOntology, triple.subject, triple.predicate, triple.old_value, triple.language);

                            if (listaProyectosPorDocumento.ContainsKey(resourceId))
                            {
                                List<TripleWrapper> listaAuxDelete = new List<TripleWrapper> { new TripleWrapper { Subject = triple.subject, Predicate = triple.predicate, Object = triple.old_value, ObjectLanguage = triple.language } };
                                foreach (Guid proyID in listaProyectosPorDocumento[resourceId])
                                {
                                    // enviar sujeto largo como parámetro a este método para que se marque como que no es la entidad padre. 
                                    sbDeleteCommunity.AppendLine(UtilidadesVirtuoso.GenerarTripletasTipoRecursoSemantico(null, null, UrlIntragnoss, parameters.ontology, resourceId, proyID, listaAuxDelete, ontologia, out auxRdfType, dataWrapperFacetas, informacionOntologias, false, triple.main_resource_id));
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(triple.new_value))
                        {
                            AddTripleToStringBuilder(sbInsertOntology, triple.subject, triple.predicate, triple.new_value, triple.language);

                            if (triple.is_new_auxiliary_entity)
                            {
                                AddTripleToStringBuilder(sbInsertOntology, $"{UrlIntragnoss}{resourceId.ToString().ToLower()}", "http://gnoss/hasEntidad", triple.new_value);
                            }

                            if (listaProyectosPorDocumento.ContainsKey(resourceId))
                            {
                                List<TripleWrapper> listaAuxInsert = new List<TripleWrapper> { new TripleWrapper { Subject = triple.subject, Predicate = triple.predicate, Object = triple.new_value, ObjectLanguage = triple.language } };
                                foreach (Guid proyID in listaProyectosPorDocumento[resourceId])
                                {
                                    sbInsertCommunity.AppendLine(UtilidadesVirtuoso.GenerarTripletasTipoRecursoSemantico(null, null, UrlIntragnoss, parameters.ontology, resourceId, proyID, listaAuxInsert, ontologia, out auxRdfType, dataWrapperFacetas, informacionOntologias, false, triple.main_resource_id));
                                }
                            }
                        }
                    }

                    if (sbDeleteOntology.Length > 0)
                    {
                        sbDeleteOntology.Insert(0, $"DELETE DATA FROM <{UrlIntragnoss}{parameters.ontology}> {{");
                        sbDeleteOntology.AppendLine("}");

                        sbDeleteCommunity.Insert(0, $"DELETE DATA FROM <{UrlIntragnoss}{proyectoID.ToString().ToLower()}> {{");
                        sbDeleteCommunity.AppendLine("}");
                    }
                    if (sbInsertOntology.Length > 0)
                    {
                        sbInsertOntology.Insert(0, $"INSERT DATA INTO <{UrlIntragnoss}{parameters.ontology}> {{");
                        sbInsertOntology.AppendLine("}");

                        sbInsertCommunity.Insert(0, $"INSERT DATA INTO <{UrlIntragnoss}{proyectoID.ToString().ToLower()}> {{");
                        sbInsertCommunity.AppendLine("}");
                    }

                    FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
                    try
                    {
                        mEntityContext.NoConfirmarTransacciones = true;
                        facetadoAD.UsarClienteTradicional = true;
                        facetadoAD.IniciarTransaccion();

                        string queryOntology = $"SPARQL {sbDeleteOntology} {sbInsertOntology}";
                        facetadoAD.ActualizarVirtuoso(queryOntology, parameters.ontology);

                        string queryCommunity = $"SPARQL {sbDeleteCommunity} {sbInsertCommunity}";
                        facetadoAD.ActualizarVirtuoso(queryCommunity, proyectoID.ToString().ToLower());

						RdfCN rdfCN = new RdfCN("rdf", mEntityContext, mEntityContextBASE, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<RdfCN>(), mLoggerFactory);
                        rdfCN.EliminarDocumentosDeRDF(listaProyectosPorDocumento.Keys.ToList());

                        mEntityContext.TerminarTransaccionesPendientes(true);

                        DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                        docCL.InvalidarFichasRecursoMVC(listaProyectosPorDocumento.Keys.ToList(), proyectoID);
                    }
                    catch (Exception ex)
                    {
                        mEntityContext.TerminarTransaccionesPendientes(false);
                        mLoggingService.GuardarLogError(ex, mlogger);
                        throw new GnossException("There was an error trying to modify triples. Please, try again later. ", HttpStatusCode.InternalServerError);
                    }
                    finally
                    {
                        mEntityContext.NoConfirmarTransacciones = false;
                        mServicesUtilVirtuosoAndReplication.UsarClienteTradicional = false;
                    }
                }
                else
                {
                    mLoggingService.GuardarLogError($"The ontology {parameters.ontology} isn't defined in proyect ontologies. (Be carefull with lower and upper case)", mlogger);
                }
            }
            catch (GnossException)
            {
                throw;
            }
            catch (Exception ex)
            {
                mEntityContext.TerminarTransaccionesPendientes(false);
                mLoggingService.GuardarLogError(ex, mlogger);
                throw new GnossException("There was an error trying to modify triples. Please, try again later. ", HttpStatusCode.InternalServerError);
            }
        }

        private void AddTripleToStringBuilder(StringBuilder pStringBuilder, string pSujeto, string pPredicado, string pObjeto, string pLanguage = null)
        {
            string sujeto = pSujeto.Trim().TrimStart('<').TrimEnd('>');
            string predicado = pPredicado.TrimStart('<').TrimEnd('>');
            string objeto = pObjeto.Trim();

            if (!string.IsNullOrEmpty(pLanguage))
            {
                pLanguage = $"@{pLanguage.TrimStart('@')}";
            }

            if ((objeto.StartsWith("http://") || objeto.StartsWith("https://")) && Uri.IsWellFormedUriString(objeto, UriKind.Absolute))
            {
                // Es una URI
                objeto = $"<{objeto}>";
            }
            else if (!objeto.StartsWith("\"") || !objeto.EndsWith("\""))
            {
                // El objeto no está envuelto en dobles comillas, se las añado
                objeto = $"\"{objeto.Replace("\"", "\\\"")}\"{pLanguage}";
            }
            else
            {
                // El objeto está envuelto en dobles comillas, 
                // por si acaso se las quito, reemplazo el resto de dobles comillas y se las vuelvo a poner
                objeto = $"\"{objeto.Trim('\"').Replace("\"", "\\\"")}\"{pLanguage}";
            }

            pStringBuilder.AppendLine($"<{sujeto}> <{predicado}> {objeto}. ");
        }


        /// <summary>
        /// Method to add / modify / delete triples of multiple complex ontology resources
        /// * Modify: Pass the old object and the new object
        /// * Delete: Pass only the old object
        /// * Add: Pass only the new object
        /// </summary>
        /// <param name="parameters">List of <see cref="ModifyResourceTripleListParams">ModifyResourceTripleListParams</see></param>
        [HttpPost, Route("modify-multiple-resources-triple-list")]
        public void ModifyMultipleResourcesTripleList(List<ModifyResourceTripleListParams> parameters)
        {
            foreach (ModifyResourceTripleListParams resource in parameters)
            {
                mNombreCortoComunidad = resource.community_short_name;
                ModificarListaDeTripletasPorRecursoInt(resource);
            }
        }

        [HttpPost, Route("get-automatic-labeling")]
        public string GetAutomaticLabelingTags(TagsFromServiceModel parameters)
        {
            try
            {
                string urlEtiquetado = mConfigService.ObtenerUrlServicioEtiquetas();
                Dictionary<string, string> parametrosPeticion = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(urlEtiquetado))
                {
                    if (!urlEtiquetado.EndsWith('/'))
                    {
                        urlEtiquetado += "/";
                    }

                    urlEtiquetado += "EtiquetadoAutomatico/SeleccionarEtiquetasDesdeServicio";

                    ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    Guid projectID = proyectoCN.ObtenerProyectoIDPorNombreCorto(parameters.community_short_name);


                    parametrosPeticion.Add("titulo", parameters.title);
                    parametrosPeticion.Add("descripcion", parameters.description);
                    parametrosPeticion.Add("ProyectoID", projectID.ToString());

                }
                else
                {
                    mLoggingService.GuardarLogError($"El servicios de etiquetas no está configurado correctamente.", mlogger);
                    throw new GnossException("Labeler service is not configured.", HttpStatusCode.BadRequest);
                }

                return UtilWeb.HacerPeticionPost(urlEtiquetado, parametrosPeticion);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError($"Error al obtener las etiquetas. ERROR: {ex.Message}", mlogger);
                throw new GnossException($"Error in labeler service: {ex.Message}", HttpStatusCode.BadRequest);
            }
        }


        #endregion

        #region Nuevos Métodos Api V3

        /// <summary>
        /// Get a list of resources which have been modified from a specific community whose content have been modified or updated from the date provided
        /// </summary>
        /// <param name="search_date">String of date with ISO 8601 format from which the search will filter to get the results</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="community_id">Community identifier</param>        
        /// <returns>List of the identifiers of modified resources</returns>
        /// <example>GET resource/get-modified-resources?community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-modified-resources")]
        public List<Guid> GetModifiedResourcesFromDate(string search_date, string community_short_name = null, Guid? community_id = null)
        {
            List<Guid> listaIDs = null;
            DateTime fechaBusqueda = DateTime.MinValue;
            bool esFecha = DateTime.TryParse(search_date, out fechaBusqueda);

            if (string.IsNullOrEmpty(community_short_name) && community_id.HasValue)
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                community_short_name = proyCL.ObtenerNombreCortoProyecto(community_id.Value);
            }

            if (!string.IsNullOrEmpty(community_short_name) && esFecha && !fechaBusqueda.Equals(DateTime.MinValue) && !fechaBusqueda.Equals(DateTime.MaxValue))
            {
                if (!ComprobarFechaISO8601(search_date))
                {
                    throw new GnossException("The parameter search_date has not the ISO8601 format", HttpStatusCode.BadRequest);
                }

                mNombreCortoComunidad = community_short_name;
                //buscar novedades en los recursos: ediciones, comentarios, votos...
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                listaIDs = docCN.ObtenerDocumentosActivosEnFecha(FilaProy.ProyectoID, fechaBusqueda);
                docCN.Dispose();
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }

            return listaIDs;
        }

        /// <summary>
        /// Gets the resource novelties in the community from the search date
        /// </summary>
        /// <param name="resources_id">Resources identifier</param>
        /// <example>POST resource/get-increased-reading-by-resources</example>
        [HttpPost, Route("get-increased-reading-by-resources")]
        public Dictionary<Guid, AumentedReading> GetIncreasedReading(List<Guid> resources_id)
        {
            Dictionary<Guid, AumentedReading> vuelta = new Dictionary<Guid, AumentedReading>();
            foreach (Guid resourceId in resources_id)
            {
                AD.EntityModel.Models.Documentacion.DocumentoLecturaAumentada lecturaAumentadaDocumento = mEntityContext.DocumentoLecturaAumentada.FirstOrDefault(item => item.DocumentoID.Equals(resourceId));
                if (lecturaAumentadaDocumento != null && lecturaAumentadaDocumento.Validada)
                {
                    AumentedReading lecAumentada = new AumentedReading();
                    lecAumentada.title = lecturaAumentadaDocumento.TituloAumentado;
                    lecAumentada.description = lecturaAumentadaDocumento.DescripcionAumentada;
                    vuelta.Add(resourceId, lecAumentada);
                }
            }
            return vuelta;
        }


        /// <summary>
        /// Get a specific resource by its own identifier and community short name where it belongs.
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="community_short_name">Community short name</param>
        /// <example>GET resource/get-resource-novelties?resource_id={resource_id}&community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-resource")]
        public Resource GetResource(Guid resource_id, string community_short_name)
        {
            Resource resource = new Resource();

            if (!resource_id.Equals(Guid.Empty) && !string.IsNullOrEmpty(community_short_name))
            {
                mNombreCortoComunidad = community_short_name;

                GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                gestorDoc.CargarDocumentos();
                Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);

                resource.resource_id = resource_id;
                resource.title = documento.Titulo;
                resource.description = documento.Descripcion;
                resource.community_short_name = community_short_name;
                resource.authors = documento.Autor;
                resource.creation_date = ConvertirFechaAISO8601(documento.Fecha);
                resource.editors_list = ObtenerListaAnonimaEditoresRecurso(resource_id);
                resource.main_image = documento.FilaDocumento.NombreCategoriaDoc;
                resource.readers_list = ObtenerListaAnonimaLectoresRecurso(resource_id);
                resource.resource_type = (short)documento.TipoDocumentacion;
                resource.resource_url = mControladorBase.UrlsSemanticas.GetURLBaseRecursosFicha(UrlIntragnoss, UtilIdiomas, community_short_name, null, documento, false);
                resource.visibility = documento.FilaDocumento.Visibilidad;
                resource.resource_attached_files = new List<AttachedResource>();

                if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                {
                    LectorXmlConfig lectorXML = new LectorXmlConfig(documento.ElementoVinculadoID, documento.FilaDocumento.ProyectoID.Value, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mVirtuosoAD, mLoggerFactory.CreateLogger<LectorXmlConfig>(), mLoggerFactory);
                    //var configuracion = lectorXML.ObtenerConfiguracionXml();
                    Dictionary<string, List<EstiloPlantilla>> listaEstilos;

                    string rdf = ObtenerRdfRecurso(resource_id);

                    List<ElementoOntologia> instanciasPrincipales = null;

                    if (!documento.GestorDocumental.ListaDocumentos.ContainsKey(documento.ElementoVinculadoID))
                    {
                        gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(documento.ElementoVinculadoID));
                        gestorDoc.CargarDocumentos(false);
                    }

                    //Agrego namespaces y urls:
                    string nombreOntologia = gestorDoc.ListaDocumentos[documento.ElementoVinculadoID].FilaDocumento.Enlace;
                    resource.ontology = nombreOntologia.ToLower();

                    GestionOWL.URLIntragnoss = UrlIntragnoss;

                    GestionOWL gestorOWL = new GestionOWL();
                    string urlOntologia = UrlIntragnoss + "/Ontologia/" + nombreOntologia + "#";
                    gestorOWL.UrlOntologia = urlOntologia;

                    gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;

                    byte[] arrayOntologia = ControladorDocumentacion.ObtenerOntologia(documento.ElementoVinculadoID, out listaEstilos, documento.FilaDocumento.ProyectoID.Value, null, null);

                    //Leo la ontología:
                    Ontologia ontologia = new Ontologia(arrayOntologia, true);
                    ontologia.LeerOntologia();
                    ontologia.EstilosPlantilla = listaEstilos;
                    ontologia.IdiomaUsuario = UtilIdiomas.LanguageCode;
                    ontologia.OntologiaID = documento.ElementoVinculadoID;

                    instanciasPrincipales = gestorOWL.LeerFicheroRDF(ontologia, rdf, true);

                    if (!string.IsNullOrEmpty(((EstiloPlantillaConfigGen)listaEstilos[$"[{LectorXmlConfig.NodoConfigGen}]"][0]).PropiedadImagenRepre.Key))
                    {
                        resource.predicate_screenshot = ((EstiloPlantillaConfigGen)listaEstilos[$"[{LectorXmlConfig.NodoConfigGen}]"][0]).PropiedadImagenRepre.Key;

                        if (listaEstilos.ContainsKey(resource.predicate_screenshot))
                        {
                            EstiloPlantillaEspecifProp estiloScreenshot = (EstiloPlantillaEspecifProp)listaEstilos[resource.predicate_screenshot].FirstOrDefault(estilo => ((EstiloPlantillaEspecifProp)estilo).TieneValor_TipoCampo && ((EstiloPlantillaEspecifProp)estilo).TipoCampo.Equals(TipoCampoOntologia.Imagen));
                            if (estiloScreenshot != null && estiloScreenshot.ImagenMini != null && estiloScreenshot.ImagenMini.Tamanios != null && estiloScreenshot.ImagenMini.Tamanios.Count > 0)
                            {
                                resource.screenshot_sizes = new List<int>(estiloScreenshot.ImagenMini.Tamanios.Keys);
                            }
                        }
                    }

                    List<TipoCampoOntologia> listaTiposConAdjunto = new List<TipoCampoOntologia>() { TipoCampoOntologia.ArchivoLink, TipoCampoOntologia.Archivo, TipoCampoOntologia.Imagen };

                    var configuracionesConAdjunto = listaEstilos.Where(config => config.Value.Count > 0 && config.Value.Exists(estilo => estilo is EstiloPlantillaEspecifProp && ((EstiloPlantillaEspecifProp)estilo).TieneValor_TipoCampo && listaTiposConAdjunto.Contains(((EstiloPlantillaEspecifProp)estilo).TipoCampo)));

                    foreach (var propiedadAdjunto in configuracionesConAdjunto)
                    {
                        foreach (EstiloPlantillaEspecifProp estilo in propiedadAdjunto.Value)
                        {
                            if (listaTiposConAdjunto.Contains(estilo.TipoCampo))
                            {
                                ObtenerAdjuntosDeEntidades(instanciasPrincipales, estilo, resource, propiedadAdjunto.Key, documento);
                            }
                        }
                    }

                    //resource.screenshot_sizes --> ir al XML
                }
                else if (documento.TipoDocumentacion.Equals(TiposDocumentacion.FicheroServidor) || documento.TipoDocumentacion.Equals(TiposDocumentacion.Imagen))
                {
                    byte[] byteArray;
                    resource.link = documento.Enlace;
                    string ext = documento.Enlace.Substring(documento.Enlace.LastIndexOf('.'));
                    if (documento.TipoDocumentacion.Equals(TiposDocumentacion.FicheroServidor))
                    {

                        GestionDocumental gestorDocumental = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                        gestorDocumental.Url = UrlServicioWebDocumentacion;
                        byteArray = gestorDocumental.ObtenerDocumento("BaseRecursos", FilaProy.OrganizacionID, FilaProy.ProyectoID, resource_id, ext);
                        if (byteArray != null)
                        {
                            resource.resource_attached_files.Add(new AttachedResource() { file_rdf_property = "", file_property_type = (short)AttachedResourceFilePropertyTypes.file, rdf_attached_file = byteArray, });
                        }
                    }
                    else
                    {
                        ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                        servicioImagenes.Url = UrlServicioInterno;
                        byteArray = servicioImagenes.ObtenerImagen($"{UtilArchivos.ContentImagenesDocumentos}/{UtilArchivos.DirectorioDocumento(resource_id)}/{resource_id}", ext);
                        if (byteArray != null)
                        {
                            resource.resource_attached_files.Add(new AttachedResource() { file_rdf_property = "", file_property_type = (short)AttachedResourceFilePropertyTypes.image, rdf_attached_file = byteArray, });
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(documento.Enlace))
                {
                    resource.link = documento.Enlace;
                }

                //categorías
                if (documento.Categorias.Count > 0)
                {
                    resource.categories = new List<Guid>();
                    foreach (Guid catID in documento.Categorias.Keys)
                    {
                        resource.categories.Add(catID);
                    }
                }

                if (!string.IsNullOrEmpty(documento.Tags))
                {
                    string[] tags = documento.Tags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tags.Length > 0)
                    {
                        resource.tags = new List<string>();
                        foreach (string tag in tags)
                        {
                            resource.tags.Add(tag);
                        }
                    }
                }

                //LecturaAumentada
                AD.EntityModel.Models.Documentacion.DocumentoLecturaAumentada lecturaAumentadaDocumento = mEntityContext.DocumentoLecturaAumentada.FirstOrDefault(item => item.DocumentoID.Equals(documento.Clave));
                if (lecturaAumentadaDocumento != null && lecturaAumentadaDocumento.Validada)
                {
                    AumentedReading lecAumentada = new AumentedReading();
                    lecAumentada.title = lecturaAumentadaDocumento.TituloAumentado;
                    lecAumentada.description = lecturaAumentadaDocumento.DescripcionAumentada;
                    resource.lecturaAumentada = lecAumentada;
                }
            }

            return resource;
        }

        private void ObtenerAdjuntosDeEntidades(List<ElementoOntologia> entidades, EstiloPlantillaEspecifProp estilo, Resource resourceParams, string pNombrePropiedad, Elementos.Documentacion.Documento pDocumento = null)
        {
            foreach (ElementoOntologia elemento in entidades)
            {
                if (elemento.TipoEntidad.Equals(estilo.NombreEntidad))
                {
                    var propiedadValor = elemento.Propiedades.Where(prop => prop.NombreFormatoUri.Equals(pNombrePropiedad) && (prop.ValoresUnificados.Count > 0 || prop.ListaValoresIdioma.Count > 0));

                    foreach (Propiedad propiedad in propiedadValor)
                    {
                        Dictionary<string, Dictionary<string, ElementoOntologia>> listaValores = null;

                        if (propiedad.ValoresUnificados.Count > 0)
                        {
                            listaValores = new Dictionary<string, Dictionary<string, ElementoOntologia>>();
                            listaValores.Add("", propiedad.ValoresUnificados);
                        }
                        else if (propiedad.ListaValoresIdioma.Count > 0)
                        {
                            listaValores = propiedad.ListaValoresIdioma;
                        }

                        Dictionary<string, string> valoresEliminar = new Dictionary<string, string>();
                        foreach (string idioma in listaValores.Keys)
                        {
                            foreach (string valor in listaValores[idioma].Keys)
                            {
                                string url = $"{BaseUrlContent}/{valor}";
                                string valorPropiedad = valor;
                                if (!string.IsNullOrEmpty(idioma))
                                {
                                    //url = $"{BaseUrlContent}/{idioma}/{valor}";
                                    valorPropiedad = $"{valor}@{idioma}";
                                }
                                if (estilo.TipoCampo.Equals(TipoCampoOntologia.Imagen) || estilo.TipoCampo.Equals(TipoCampoOntologia.ArchivoLink))
                                {

                                    try
                                    {
                                        byte[] bytes = DescargarAdjunto(url);

                                        short tipo = (short)AttachedResourceFilePropertyTypes.image;
                                        if (estilo.TipoCampo.Equals(TipoCampoOntologia.ArchivoLink))
                                        {
                                            tipo = (short)AttachedResourceFilePropertyTypes.linkFile;
                                        }

                                        resourceParams.resource_attached_files.Add(new AttachedResource() { file_rdf_property = valorPropiedad, file_property_type = (short)tipo, rdf_attached_file = bytes, });

                                        if (estilo.ImagenMini != null && estilo.ImagenMini.Tamanios != null)
                                        {
                                            foreach (int tamanio in estilo.ImagenMini.Tamanios.Keys)
                                            {
                                                string valorRecorte = null;
                                                if (tamanio > 0)
                                                {
                                                    valorRecorte = valor.Insert(valor.LastIndexOf('.'), $"_{tamanio}");
                                                }
                                                else
                                                {
                                                    valorRecorte = valor.Insert(valor.LastIndexOf('.'), $"_{estilo.ImagenMini.Tamanios[tamanio]}");
                                                }

                                                string urlRecorte = $"{BaseUrlContent}/{valorRecorte}";

                                                try
                                                {
                                                    bytes = DescargarAdjunto(urlRecorte);

                                                    resourceParams.resource_attached_files.Add(new AttachedResource() { file_rdf_property = valorRecorte, file_property_type = (short)tipo, rdf_attached_file = bytes });
                                                }
                                                catch (Exception ex)
                                                {
                                                    mLoggingService.GuardarLogError(ex, $"Error al descargar el recorte de una imagen: {urlRecorte}",mlogger);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        mLoggingService.GuardarLogError(ex, $"Error al descargar una imagen:",mlogger);
                                    }
                                }
                                else if (estilo.TipoCampo.Equals(TipoCampoOntologia.Archivo))
                                {
                                    byte[] byteArray;
                                    GestionDocumental gestorDocumental = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                                    gestorDocumental.Url = UrlServicioWebDocumentacion;
                                    string directorio = UtilArchivos.ContentDocumentosSemAntiguo + "\\" + pDocumento.ElementoVinculadoID.ToString().Substring(0, 3) + "\\" + pDocumento.ElementoVinculadoID.ToString();
                                    if (!string.IsNullOrEmpty(idioma))
                                    {
                                        directorio += "\\" + idioma;
                                    }
                                    string ext = valor.Substring(valor.LastIndexOf('.'));
                                    byteArray = gestorDocumental.ObtenerDocumentoDeDirectorio(directorio, valor.Substring(0, valor.LastIndexOf('.')), ext);

                                    if (byteArray == null)//Miramos si el documento está en el nuevo directorio:
                                    {
                                        directorio = UtilArchivos.ContentDocumentosSem + "\\" + UtilArchivos.DirectorioDocumento(pDocumento.Clave);
                                        if (!string.IsNullOrEmpty(idioma))
                                        {
                                            directorio += "\\" + idioma;
                                        }

                                        byteArray = gestorDocumental.ObtenerDocumentoDeDirectorio(directorio, valor.Substring(0, valor.LastIndexOf('.')), ext);
                                    }
                                    if (byteArray != null)
                                    {
                                        resourceParams.resource_attached_files.Add(new AttachedResource() { file_rdf_property = valorPropiedad, file_property_type = (short)AttachedResourceFilePropertyTypes.file, rdf_attached_file = byteArray, });
                                    }
                                }
                            }
                        }
                    }
                }

                ObtenerAdjuntosDeEntidades(elemento.EntidadesRelacionadas, estilo, resourceParams, pNombrePropiedad, pDocumento);
            }
        }

        private byte[] DescargarAdjunto(string pUrl)
        {
            WebResponse response = UtilWeb.HacerPeticionGetDevolviendoWebResponse(pUrl);

            BinaryReader binaryReader = new BinaryReader(response.GetResponseStream());
            byte[] bytes = binaryReader.ReadBytes((int)response.ContentLength);
            binaryReader.Close();

            return bytes;
        }

        /// <summary>
        /// Get the novelties of a specific resource by its own identifier and either the community short name or identifier.
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="search_date">String of date with ISO 8601 format from which the search will filter to get the results</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="community_id">Community identifier</param>        
        /// <example>GET resource/get-resource-novelties?resource_id={resource_id}&community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-resource-novelties")]
        public ResourceNoveltiesModel GetResourceNoveltiesFromDate(Guid resource_id, string search_date, string community_short_name = null, Guid? community_id = null)
        {
            ResourceNoveltiesModel novedadesDocumento = null;
            DateTime fechaBusqueda = DateTime.MinValue;
            bool esFecha = DateTime.TryParse(search_date, out fechaBusqueda);

            if (string.IsNullOrEmpty(community_short_name) && community_id.HasValue)
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                community_short_name = proyCL.ObtenerNombreCortoProyecto(community_id.Value);
            }

            if (!resource_id.Equals(Guid.Empty) && !string.IsNullOrEmpty(community_short_name) && esFecha && !fechaBusqueda.Equals(DateTime.MinValue) && !fechaBusqueda.Equals(DateTime.MaxValue))
            {
                mNombreCortoComunidad = community_short_name;

                GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);

                VotoCN votoCN = new VotoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<VotoCN>(), mLoggerFactory);
                gestorDoc.GestorVotos = new GestionVotosDocumento(votoCN.ObtenerVotosDocumentoPorID(resource_id), gestorDoc, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<GestionVotosDocumento>(), mLoggerFactory);

                gestorDoc.CargarDocumentos(false);

                if (gestorDoc.ListaDocumentos.ContainsKey(resource_id))
                {
                    novedadesDocumento = new ResourceNoveltiesModel();
                    Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[resource_id];

                    List<AD.EntityModel.Models.Documentacion.HistorialDocumento> filasHistorialDoc = documento.GestorDocumental.DataWrapperDocumentacion.ListaHistorialDocumento.Where(filaHist => filaHist.DocumentoID.Equals(resource_id) && filaHist.Fecha >= fechaBusqueda).ToList();

                    //si no hay actividad devolvemos el documento vacío
                    if (filasHistorialDoc == null || filasHistorialDoc.Count == 0)
                    {
                        return novedadesDocumento;
                    }

                    novedadesDocumento.authors = documento.Autor;

                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);

                    //categorías
                    if (documento.Categorias.Count > 0)
                    {
                        novedadesDocumento.categories = new List<Guid>();
                        foreach (Guid catID in documento.Categorias.Keys)
                        {
                            novedadesDocumento.categories.Add(catID);
                        }
                    }

                    //comentarios
                    if (documento.Comentarios != null && documento.Comentarios.Count > 0)
                    {
                        if (documento.Comentarios.Count > 0)
                        {
                            novedadesDocumento.comments = new List<Models.CommentModel>();
                            foreach (Comentario comentarioDoc in documento.Comentarios)
                            {
                                AD.EntityModel.Models.Documentacion.DocumentoComentario filaDocumentoComentario = gestorDoc.DataWrapperDocumentacion.ListaDocumentoComentario.Where(item => item.ComentarioID.Equals(comentarioDoc.Clave) && item.DocumentoID.Equals(resource_id)).FirstOrDefault();

                                if (filaDocumentoComentario != null)
                                {
                                    string nomComunidad = proyCN.ObtenerNombreCortoProyecto(filaDocumentoComentario.ProyectoID.Value);
                                    Models.CommentModel comment = new Models.CommentModel();
                                    if (comentarioDoc.Fecha != null)
                                    {
                                        comment.comment_date = ConvertirFechaAISO8601(comentarioDoc.Fecha);
                                    }
                                    comment.community_short_name = nomComunidad;
                                    comment.html_description = comentarioDoc.FilaComentario.Descripcion;
                                    comment.resource_id = resource_id;
                                    comment.parent_comment_id = comentarioDoc.FilaComentario.ComentarioSuperiorID.Value;

                                    List<Guid> listaIdentidades = new List<Guid>() { comentarioDoc.FilaComentario.IdentidadID };
                                    Dictionary<Guid, Guid> dicIdentidadIDUsuarioID = identCN.ObtenerListaUsuarioIDConIdentidadesID(listaIdentidades);

                                    if (dicIdentidadIDUsuarioID != null && dicIdentidadIDUsuarioID.Count > 0 && dicIdentidadIDUsuarioID.ContainsKey(comentarioDoc.FilaComentario.IdentidadID))
                                    {
                                        comment.user_id = dicIdentidadIDUsuarioID[comentarioDoc.FilaComentario.IdentidadID];
                                    }

                                    if (comment.comment_date >= fechaBusqueda)
                                    {
                                        novedadesDocumento.comments.Add(comment);
                                    }
                                }
                            }
                        }
                    }

                    string nomComunidadPublicado = proyCN.ObtenerNombreCortoProyecto(documento.ProyectoID);
                    novedadesDocumento.community_short_name = nomComunidadPublicado;

                    if (documento.Fecha != null)
                    {
                        novedadesDocumento.creation_date = ConvertirFechaAISO8601(documento.Fecha);
                    }

                    novedadesDocumento.description = documento.Descripcion;
                    novedadesDocumento.downloads = documento.FilaDocumento.NumeroTotalDescargas;
                    novedadesDocumento.editors_list = ObtenerListaAnonimaEditoresRecurso(resource_id);

                    if (documento.FilaDocumentoWebVinBRExtra.FechaUltimaVisita.HasValue)
                    {
                        novedadesDocumento.last_view_date = ConvertirFechaAISO8601(documento.FilaDocumentoWebVinBRExtra.FechaUltimaVisita.Value);
                    }

                    novedadesDocumento.main_image = documento.FilaDocumento.NombreCategoriaDoc;

                    foreach (DocumentoWebVinBaseRecursos filaDocWebVinBR in documento.GestorDocumental.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos)
                    {
                        //espacio personal  //"BaseRecursosID='" + filaDocWebVinBR.BaseRecursosID + "'"
                        List<BaseRecursosUsuario> filaBRUsu = documento.GestorDocumental.DataWrapperDocumentacion.ListaBaseRecursosUsuario.Where(baseRec => baseRec.BaseRecursosID.Equals(filaDocWebVinBR.BaseRecursosID)).ToList();

                        if (filaBRUsu != null && filaBRUsu.Count > 0)
                        {
                            if (novedadesDocumento.personal_spaces == null)
                            {
                                novedadesDocumento.personal_spaces = new List<Models.PersonalSpaceModel>();
                            }

                            Models.PersonalSpaceModel espacioPersonal = new Models.PersonalSpaceModel();
                            espacioPersonal.resource_id = resource_id;
                            espacioPersonal.user_id = filaBRUsu[0].UsuarioID;
                            if (filaDocWebVinBR.FechaPublicacion != null)
                            {
                                espacioPersonal.saved_date = ConvertirFechaAISO8601(filaDocWebVinBR.FechaPublicacion.Value);
                            }

                            if (espacioPersonal.saved_date >= fechaBusqueda)
                            {
                                novedadesDocumento.personal_spaces.Add(espacioPersonal);
                            }
                        }

                        //recurso compartido   
                        List<BaseRecursosProyecto> filaBRUproy = documento.GestorDocumental.DataWrapperDocumentacion.ListaBaseRecursosProyecto.Where(baseRecProy => baseRecProy.BaseRecursosID.Equals(filaDocWebVinBR.BaseRecursosID)).ToList();

                        if (filaBRUproy != null && filaBRUproy.Count > 0 && !filaDocWebVinBR.TipoPublicacion.Equals((short)TipoPublicacion.Publicado))
                        {
                            if (novedadesDocumento.shared_on == null)
                            {
                                novedadesDocumento.shared_on = new List<ShareModel>();
                            }

                            ShareModel compartido = new ShareModel();
                            compartido.resource_id = resource_id;
                            if (filaDocWebVinBR.IdentidadPublicacionID.HasValue)
                            {
                                compartido.user_id = identCN.ObtenerUsuarioIDConIdentidadID(filaDocWebVinBR.IdentidadPublicacionID.Value);
                            }

                            if (filaDocWebVinBR.FechaPublicacion.HasValue)
                            {
                                compartido.share_date = ConvertirFechaAISO8601(filaDocWebVinBR.FechaPublicacion.Value);
                            }
                            compartido.origin_community_short_name = nomComunidadPublicado;
                            compartido.destiny_community_short_name = proyCN.ObtenerNombreCortoProyecto(filaBRUproy[0].ProyectoID);

                            if (compartido.share_date >= fechaBusqueda)
                            {
                                novedadesDocumento.shared_on.Add(compartido);
                            }
                        }

                    }

                    novedadesDocumento.plays = documento.FilaDocumento.NumeroTotalDescargas;
                    novedadesDocumento.readers_list = ObtenerListaAnonimaLectoresRecurso(resource_id);
                    novedadesDocumento.resource_type = (short)documento.TipoDocumentacion;
                    novedadesDocumento.resource_url = mControladorBase.UrlsSemanticas.GetURLBaseRecursosFicha(UrlIntragnoss, UtilIdiomas, nomComunidadPublicado, null, documento, false);

                    novedadesDocumento.resource_type_names = new List<ResourceTypeName>();
                    novedadesDocumento.resource_type_names.Add(new ResourceTypeName() { resource_type_name = documento.TipoDocumentacion.ToFrindlyString("es"), resource_type_name_language = "es" });
                    novedadesDocumento.resource_type_names.Add(new ResourceTypeName() { resource_type_name = documento.TipoDocumentacion.ToFrindlyString("en"), resource_type_name_language = "en" });

                    if (documento.TipoDocumentacion.Equals(TiposDocumentacion.Semantico))
                    {
                        string enlace = docCN.ObtenerEnlaceDocumentoVinculadoADocumento(documento.Clave);

                        //proyCN.ontolog
                        FacetaCN facCN = new FacetaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetaCN>(), mLoggerFactory);
                        Guid proyectID = Guid.Empty;
                        if (documento.FilaDocumento.ProyectoID.HasValue)
                        {
                            proyectID = documento.FilaDocumento.ProyectoID.Value;
                        }
                        AD.EntityModel.Models.Faceta.OntologiaProyecto filaOntologia = facCN.ObtenerOntologiaProyectoPorEnlace(proyectID, enlace.Replace(".owl", ""));
                        if (filaOntologia != null)
                        {
                            novedadesDocumento.item_type_names = new List<ItemTypeName>();
                            Dictionary<string, string> itemNames = UtilCadenas.ObtenerTextoPorIdiomas(filaOntologia.NombreOnt);
                            foreach (string idioma in itemNames.Keys)
                            {
                                novedadesDocumento.item_type_names.Add(new ItemTypeName() { item_type_name = itemNames[idioma], item_type_name_language = idioma });
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(documento.Tags))
                    {
                        string[] tags = documento.Tags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tags.Length > 0)
                        {
                            novedadesDocumento.tags = new List<string>();
                            foreach (string tag in tags)
                            {
                                novedadesDocumento.tags.Add(tag);
                            }
                        }
                    }

                    novedadesDocumento.title = documento.Titulo;
                    DateTime fechaUltimaEdicion = DateTime.MinValue;

                    foreach (AD.EntityModel.Models.Documentacion.HistorialDocumento filaHistorialDoc in documento.GestorDocumental.DataWrapperDocumentacion.ListaHistorialDocumento)
                    {
                        if (filaHistorialDoc.Fecha >= fechaBusqueda)
                        {
                            switch ((AccionHistorialDocumento)filaHistorialDoc.Accion)
                            {
                                case AccionHistorialDocumento.Eliminar:
                                    novedadesDocumento.delete_date_by_user = new ResourceDeleteDateByUser();
                                    novedadesDocumento.delete_date_by_user.resource_id = resource_id;
                                    novedadesDocumento.delete_date_by_user.user_id = identCN.ObtenerUsuarioIDConIdentidadID(filaHistorialDoc.IdentidadID);
                                    if (filaHistorialDoc.Fecha != null)
                                    {
                                        novedadesDocumento.delete_date_by_user.delete_date = filaHistorialDoc.Fecha;
                                    }
                                    break;
                                case AccionHistorialDocumento.GuardarDocumento:
                                    if (novedadesDocumento.edition_date_by_user == null)
                                    {
                                        novedadesDocumento.edition_date_by_user = new List<ResourceEditionDateByUser>();
                                    }
                                    ResourceEditionDateByUser edicionDoc = new ResourceEditionDateByUser();
                                    edicionDoc.resource_id = resource_id;
                                    edicionDoc.user_id = identCN.ObtenerUsuarioIDConIdentidadID(filaHistorialDoc.IdentidadID);
                                    if (filaHistorialDoc.Fecha != null)
                                    {
                                        edicionDoc.edition_date = ConvertirFechaAISO8601(filaHistorialDoc.Fecha);
                                        if (filaHistorialDoc.Fecha > fechaUltimaEdicion)
                                        {
                                            fechaUltimaEdicion = filaHistorialDoc.Fecha;
                                        }
                                    }
                                    novedadesDocumento.edition_date_by_user.Add(edicionDoc);
                                    break;
                                case AccionHistorialDocumento.CrearVersion:
                                    if (novedadesDocumento.version_date_by_user == null)
                                    {
                                        novedadesDocumento.version_date_by_user = new List<ResourceVersionDateByUser>();
                                    }

                                    ResourceVersionDateByUser historial = new ResourceVersionDateByUser();
                                    historial.resource_id = resource_id;
                                    historial.user_id = identCN.ObtenerUsuarioIDConIdentidadID(filaHistorialDoc.IdentidadID);
                                    if (filaHistorialDoc.Fecha != null)
                                    {
                                        historial.version_date = ConvertirFechaAISO8601(filaHistorialDoc.Fecha);
                                    }

                                    DocumentacionCN docuCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                                    Dictionary<Guid, int> dicVersiones = docuCN.ObtenerVersionesDocumentoIDPorID(documento.Clave);
                                    docuCN.Dispose();
                                    //si el número de versión es > 0 buscar la versión anterior
                                    if (documento.Version > 0 && dicVersiones != null && dicVersiones.Count > 0)
                                    {
                                        List<KeyValuePair<Guid, int>> listaVersiones = dicVersiones.Where(v => v.Value.Equals(documento.Version - 1)).ToList();
                                        if (listaVersiones.Count > 0)
                                        {
                                            historial.previous_version_resource_id = listaVersiones[0].Key;
                                        }
                                    }
                                    novedadesDocumento.version_date_by_user.Add(historial);
                                    break;
                            }
                        }

                        if (fechaUltimaEdicion != DateTime.MinValue && fechaUltimaEdicion != DateTime.MaxValue)
                        {
                            novedadesDocumento.last_edition_date = ConvertirFechaAISO8601(fechaUltimaEdicion);
                        }
                    }

                    novedadesDocumento.views = documento.FilaDocumento.NumeroTotalConsultas;
                    novedadesDocumento.visibility = documento.FilaDocumento.Visibilidad;

                    foreach (Guid docVotoID in documento.ListaVotos.Keys)
                    {
                        if (documento.ListaVotos[docVotoID].FechaVotacion.Value >= fechaBusqueda)
                        {
                            if (novedadesDocumento.votes == null)
                            {
                                novedadesDocumento.votes = new List<VoteModel>();
                            }

                            VoteModel voto = new VoteModel();
                            voto.resource_id = resource_id;
                            voto.user_id = identCN.ObtenerUsuarioIDConIdentidadID(documento.ListaVotos[docVotoID].IdentidadID);
                            if (documento.ListaVotos[docVotoID].FechaVotacion.HasValue)
                            {
                                voto.vote_date = ConvertirFechaAISO8601(documento.ListaVotos[docVotoID].FechaVotacion.Value);
                            }

                            novedadesDocumento.votes.Add(voto);
                        }
                    }

                    if (documento.FilaDocumento.Enlace != null && (documento.TipoDocumentacion.Equals(TiposDocumentacion.Hipervinculo) || documento.TipoDocumentacion.Equals(TiposDocumentacion.Video)))
                    {
                        novedadesDocumento.link = documento.Enlace;
                    }

                    identCN.Dispose();
                    proyCN.Dispose();
                }
                else
                {
                    throw new GnossException("The resource " + resource_id + " does not exist.", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }

            return novedadesDocumento;
        }

        /// <summary>
        /// Get a list of resources which have been published by a concrete user identifier.
        /// </summary>
        /// <param name="user_id">User identifier</param>
        /// <returns>List of the identifiers of modified resources</returns>
        /// <example>GET resource/get-modified-resources?community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-documents-published-by-user")]
        public Dictionary<string, List<Guid>> GetDocumentsPublishedByUser(Guid user_id,string login)
        {
            if (!user_id.Equals(Guid.Empty)) 
            {
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                return docCN.ObtenerRecursosSubidosPorUsuario(user_id);
            }
            else
            {
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);
                return docCN.ObtenerRecursosSubidosPorUsuario(usuarioCN.ObtenerFilaUsuarioPorLoginOEmail(login).UsuarioID);
            }
        }

        /// <summary>
        /// Gets path styles
        /// </summary>
        /// <param name="id_proyecto">id project identifier</param>
        /// <returns>string of path projecct</returns>
        /// <example>GET resource/get-path-styles?id_proyecto={id_project}</example>
        [HttpGet, Route("get-path-styles")]
        public string GetPathStyles(Guid? id_proyecto)
        {
            ParametroCN paramCN = new ParametroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ParametroCN>(), mLoggerFactory);
            return paramCN.ObtenerPathEstilos(id_proyecto.Value);
        }

        /// <summary>
        /// Method for vote document
        /// </summary>
        /// 
        [HttpPost, Route("vote-document")]
        public void VoteDocument(VotedParameters votedParameters)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            VotoCN votCN = new VotoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<VotoCN>(), mLoggerFactory);
            Guid? votoID = votCN.ObtenerVotosPorUsuario(votedParameters.resource_id, votedParameters.user_id, votedParameters.project_id);
            float valorVoto = votedParameters.vote_value;

            if (valorVoto == 0)
            {
                valorVoto = 1;
            }

            if (!votoID.HasValue || votoID.Equals(Guid.Empty))
            {
                Guid identidadVotadaID = docCN.ObtenerCreadorDocumentoID(votedParameters.resource_id);
                /*Guid VotoID = Guid.NewGuid();
                votCN.insertarVoto(votedParameters.user_id, votedParameters.resource_id, valorVoto, identidadVotadaID, VotoID);
                votCN.insertarVotoDocumento(votedParameters.resource_id, votedParameters.project_id, VotoID);*/
                votCN.insertarVotoDocumento(votedParameters.resource_id, votedParameters.project_id, votedParameters.user_id, valorVoto, identidadVotadaID);

            }
            else
            {
                votCN.ActualizarVoto(votoID.Value, votedParameters.user_id, valorVoto);
            }

        }

        /// <summary>
        /// Check whether a given resource is locked or not.
        /// </summary>
        /// <param name="resource_id">Resource identifier</param>
        [HttpGet, Route("check-document-is-locked")]
        public bool CheckDocumentIsLocked(Guid resource_id)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            return docCN.ObtenerFechaRecursoEnEdicion(resource_id).HasValue;
        }

        /// <summary>
        /// Lock a specific resource from a concrete community setting the seconds the locking will last.
        /// </summary>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="lock_seconds_duration">Seconds that the resource will be locked</param>
        /// <param name="timeout_seconds">Timeout. The default timeout is 60 seconds.</param>"
        [HttpPost, Route("lock-document")]
        public string LockDocument(string community_short_name, Guid resource_id, int lock_seconds_duration, int timeout_seconds)
        {
            mNombreCortoComunidad = community_short_name;

            if (lock_seconds_duration < 1)
            {
                // El tiempo por defecto de bloqueo es 60 segundos
                lock_seconds_duration = 60;
            }

            if (timeout_seconds < 1)
            {
                // El tiempo por defecto de espera es 60 segundos
                timeout_seconds = 60;
            }
            else if (timeout_seconds > 600)
            {
                // El tiempo máximo de espera es de 10 minutos
                timeout_seconds = 600;
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

            docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(resource_id);
            gestorDoc.CargarDocumentos(false);

            Elementos.Documentacion.Documento documentoAntiguo = gestorDoc.ListaDocumentos[resource_id];

            bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

            if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
            {
                throw new GnossException("Invalid Oauth. Insufficient permissions.", HttpStatusCode.Unauthorized);
            }
            else
            {
                try
                {
                    if (docCN.ComprobarDocumentoEnEdicion(resource_id, identidad.Clave, lock_seconds_duration, timeout_seconds) != null)
                    {
                        throw new GnossException($"The resource {resource_id} has been blocked by other updates for more than 60 seconds. Try again later ", HttpStatusCode.Conflict);
                    }
                    else
                    {
                        DateTime? fecha = docCN.ObtenerFechaRecursoEnEdicion(resource_id);
                        if (fecha.HasValue)
                        {
                            return fecha.Value.Ticks.ToString();
                        }
                        else
                        {
                            throw new GnossException($"An error occurred. The resource {resource_id} can't be locked. Try again later ", HttpStatusCode.InternalServerError);
                        }
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, $"Error al bloquear el recurso {resource_id}",mlogger);
                    throw new GnossException($"The resource {resource_id} has been blocked by other updates for more than 60 seconds. Try again later ", HttpStatusCode.Conflict);
                }
            }
        }

        /// <summary>
        /// Unlock a specific resource from a concrete community.
        /// </summary>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="resource_id">Resource identifier</param>
        /// <param name="token">Token</param>
        [HttpPost, Route("unlock-document")]
        public void UnlockDocument(string community_short_name, Guid resource_id, string token)
        {
            mNombreCortoComunidad = community_short_name;
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

            docCN.ObtenerDocumentoPorIDCargarTotal(resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(resource_id);
            gestorDoc.CargarDocumentos(false);

            Elementos.Documentacion.Documento documentoAntiguo = gestorDoc.ListaDocumentos[resource_id];

            bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

            if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
            {
                throw new GnossException("Invalid Oauth. Insufficient permissions.", HttpStatusCode.Unauthorized);
            }
            else
            {
                try
                {
                    DateTime? fecha = docCN.ObtenerFechaRecursoEnEdicion(resource_id);
                    long ticks;
                    if (fecha.HasValue && long.TryParse(token, out ticks) && ticks.Equals(fecha.Value.Ticks))
                    {
                        docCN.FinalizarEdicionRecurso(resource_id);
                    }
                    else
                    {
                        throw new GnossException($"Invalid token. The resource {resource_id} can't be unlocked with token {token}.", HttpStatusCode.Unauthorized);
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, $"Error al desbloquear el recurso {resource_id}",mlogger);
                    throw new GnossException($"An error occurred. The resource {resource_id} can't be unlocked. Try again later ", HttpStatusCode.InternalServerError);
                }
            }
        }

        /// <summary>
        /// Get an attached file from a semantic resource
        /// </summary>
        /// <param name="resource_id">Identifier of the resource</param>
        /// <param name="file_name">Name of the file attached with extension</param>
        /// <param name="community_short_name">Short name of the community where the resource are</param>
        /// <param name="language">Only if the property is multilanguage. The language which we want the file. es, en, de, ca, eu, fr, gl, it, pt</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet, Route("get-attached-file-semantic-resource")]
        public byte[] GetAttachedFileFromSemanticResource(Guid resource_id, string file_name, string community_short_name, string language)
        {
            mNombreCortoComunidad = community_short_name;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(community_short_name);

            if (proyectoID.Equals(Guid.Empty))
            {
                throw new Exception($"Project short name {community_short_name} doesn't exist.");
            }

            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            if (!documentacionCN.ExisteDocumentoEnProyecto(proyectoID, resource_id))
            {
                throw new Exception($"The document {resource_id} doesn't exist in the proyect {community_short_name}");
            }

            string nombreFichero = file_name.Substring(0, file_name.LastIndexOf('.'));
            string extension = $"{file_name.Substring(file_name.LastIndexOf('.'))}";

            GestionDocumental gestorDocumental = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
            gestorDocumental.Url = UrlServicioWebDocumentacion;

            string directorio = Path.Combine(UtilArchivos.ContentDocumentosSem, resource_id.ToString().Substring(0, 2), resource_id.ToString().Substring(0, 4), resource_id.ToString());
            if (!string.IsNullOrEmpty(language))
            {
                directorio = Path.Combine(directorio, language);
            }

            byte[] byteArray = gestorDocumental.ObtenerDocumentoDeDirectorio(directorio, nombreFichero, extension);

            return byteArray;
        }

        #endregion

        #region Métodos privados

        private bool ComprobarDocumentoEnEdicion(Guid pDocumentoID, Guid pIdentidadID)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            bool documentoBloqueado = false;

            if (mHttpContextAccessor.HttpContext.Request.Headers != null && !string.IsNullOrEmpty(mHttpContextAccessor.HttpContext.Request.Headers["X-Correlation-ID"]))
            {
                string token = mHttpContextAccessor.HttpContext.Request.Headers["X-Correlation-ID"];
                DateTime? fechaDocumentoEnEdicion = docCN.ObtenerFechaRecursoEnEdicion(pDocumentoID);
                long ticks;
                if (fechaDocumentoEnEdicion.HasValue && long.TryParse(token, out ticks) && ticks.Equals(fechaDocumentoEnEdicion.Value.Ticks))
                {
                    return false;
                }
                else if (!fechaDocumentoEnEdicion.HasValue || DateTime.UtcNow > fechaDocumentoEnEdicion.Value)
                {
                    if (fechaDocumentoEnEdicion.HasValue)
                    {
                        docCN.FinalizarEdicionRecurso(pDocumentoID);
                    }
                }
                else
                {
                    throw new GnossException($"The resource {pDocumentoID} has been blocked by other updates for more than 60 seconds. Try again later ", HttpStatusCode.Conflict);
                }
            }

            if (!documentoBloqueado)
            {
                DocumentoEnEdicion filaEdicion = docCN.ComprobarDocumentoEnEdicion(pDocumentoID, pIdentidadID);
                                
                if (filaEdicion != null)
                {
                    throw new GnossException($"The resource {pDocumentoID} has been blocked by other updates for more than 60 seconds. Try again later ", HttpStatusCode.Conflict);
                }
                documentoBloqueado = true;
            }
            return documentoBloqueado;
        }

        private List<ReaderEditor> ObtenerListaAnonimaLectoresRecurso(Guid pDocumentoID)
        {
            List<ReaderEditor> documentosIDLectores = new List<ReaderEditor>();

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            List<Guid> listaDocs = new List<Guid>() { pDocumentoID };
            DataWrapperDocumentacion docDW = docCN.ObtenerLectoresYGruposLectoresDocumentos(listaDocs);
            docCN.Dispose();

            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);

            foreach (NombrePerfil filaNombrePerfil in docDW.ListaNombrePerfil)
            {
                Guid? usuarioID = usuCN.ObtenerUsuarioIDPorNombreCorto(filaNombrePerfil.NombrePerfilAtributo);

                ReaderEditor editor = documentosIDLectores.Find(doc => doc.user_id.Equals(pDocumentoID));
                if (editor == null && usuarioID.HasValue)
                {
                    editor = new ReaderEditor();
                    editor.user_id = usuarioID.Value;
                    documentosIDLectores.Add(editor);
                }
            }

            foreach (string nombreGrupoAtributo in docDW.ListaNombreGrupo.Select(item => item.NombreGrupoAtributo))
            {
                ReaderEditor grupoLector = documentosIDLectores.Find(doc => !string.IsNullOrEmpty(doc.group_short_name) && doc.group_short_name.Equals(nombreGrupoAtributo) && string.IsNullOrEmpty(doc.organization_short_name));

                if (grupoLector == null)
                {
                    ReaderEditor readerGr = new ReaderEditor();
                    readerGr.group_short_name = nombreGrupoAtributo;
                    documentosIDLectores.Add(readerGr);
                }
            }

            foreach (NombreGrupoOrg filaNombreGrupoOrg in docDW.ListaNombreGrupoOrg)
            {
                string nombreOrg = filaNombreGrupoOrg.NombreOrganizacion;
                string nomGrLector = filaNombreGrupoOrg.NombreGrupo;

                ReaderEditor grupoOrgEditor = documentosIDLectores.Find(doc => !string.IsNullOrEmpty(doc.group_short_name) && nomGrLector.Equals(doc.group_short_name) && nombreOrg.Equals(doc.organization_short_name));

                if (grupoOrgEditor == null)
                {
                    grupoOrgEditor = new ReaderEditor();
                    grupoOrgEditor.group_short_name = nomGrLector;
                    grupoOrgEditor.organization_short_name = nombreOrg;
                    documentosIDLectores.Add(grupoOrgEditor);
                }
            }

            return documentosIDLectores;
        }

        private List<ReaderEditor> ObtenerListaAnonimaEditoresRecurso(Guid pDocumentoID)
        {
            List<ReaderEditor> documentosIDEditores = new List<ReaderEditor>();
            List<Guid> listaDocID = new List<Guid>() { pDocumentoID };
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion docDW = docCN.ObtenerEditoresYGruposEditoresDocumentos(listaDocID);
            docCN.Dispose();
            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<UsuarioCN>(), mLoggerFactory);

            foreach (AD.EntityModel.Models.Documentacion.NombrePerfil filaNombrePerfil in docDW.ListaNombrePerfil)
            {
                Guid? usuarioID = usuCN.ObtenerUsuarioIDPorNombreCorto(filaNombrePerfil.NombrePerfilAtributo);

                ReaderEditor editor = documentosIDEditores.Find(doc => doc.user_id.Equals(pDocumentoID));
                if (editor == null && usuarioID.HasValue)
                {
                    editor = new ReaderEditor();
                    editor.user_id = usuarioID.Value;
                    documentosIDEditores.Add(editor);
                }
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupo filaNombreGrupo in docDW.ListaNombreGrupo)
            {
                string nomGrEditor = filaNombreGrupo.NombreGrupoAtributo;

                ReaderEditor grupoEditor = documentosIDEditores.Find(doc => nomGrEditor.Equals(doc.group_short_name) && string.IsNullOrEmpty(doc.organization_short_name));
                if (grupoEditor == null)
                {
                    grupoEditor = new ReaderEditor();
                    grupoEditor.group_short_name = nomGrEditor;
                    documentosIDEditores.Add(grupoEditor);
                }
            }

            foreach (AD.EntityModel.Models.Documentacion.NombreGrupoOrg filaNombreGrupoOrg in docDW.ListaNombreGrupoOrg)
            {
                string nombreOrg = filaNombreGrupoOrg.NombreOrganizacion;
                string nomGrEditor = filaNombreGrupoOrg.NombreGrupo;
                Guid documentoID = filaNombreGrupoOrg.DocumentoID;

                ReaderEditor grupoOrgEditor = documentosIDEditores.Find(doc => nomGrEditor.Equals(doc.group_short_name) && nombreOrg.Equals(doc.organization_short_name));

                if (grupoOrgEditor == null)
                {
                    grupoOrgEditor = new ReaderEditor();
                    grupoOrgEditor.group_short_name = nomGrEditor;
                    grupoOrgEditor.organization_short_name = nombreOrg;
                    documentosIDEditores.Add(grupoOrgEditor);
                }
            }

            return documentosIDEditores;
        }

        private void EstablecerPrivacidadRecurso(Elementos.Documentacion.Documento pDocumento, short pVisibilidad, bool pModificarSoloComunidadActual)
        {
            //en el momento que FilaDocumentoWebVinBR.PrivadoEditores = true, no se mira FilaDocumento.Visibilidad
            pDocumento.FilaDocumento.Visibilidad = (short)VisibilidadDocumento.Todos;
            bool privadoEditores = false;

            switch (pVisibilidad)
            {
                case (short)Models.ResourceVisibility.open:
                    privadoEditores = false;
                    break;
                case (short)Models.ResourceVisibility.editors:
                    privadoEditores = true;
                    break;
                case (short)Models.ResourceVisibility.communitymembers:
                    pDocumento.FilaDocumento.Visibilidad = (short)VisibilidadDocumento.PrivadoMiembrosComunidad;
                    break;
                case (short)Models.ResourceVisibility.specific:
                    privadoEditores = true;
                    break;
            }

            if (pModificarSoloComunidadActual && pDocumento.FilaDocumentoWebVinBR != null)
            {
                pDocumento.FilaDocumentoWebVinBR.PrivadoEditores = privadoEditores;
            }
            else
            {
                foreach (AD.EntityModel.Models.Documentacion.DocumentoWebVinBaseRecursos filaDocWebVinBaseRecursos in pDocumento.GestorDocumental.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos.Where(doc => doc.DocumentoID.Equals(pDocumento.Clave)).ToList())
                {
                    filaDocWebVinBaseRecursos.PrivadoEditores = privadoEditores;
                }
            }
        }

        private void ConfigurarLectores(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaLectores, short pVisibilidad)
        {
            if (pVisibilidad.Equals((short)Models.ResourceVisibility.open) || pVisibilidad.Equals((short)Models.ResourceVisibility.editors) || pVisibilidad.Equals((short)Models.ResourceVisibility.communitymembers) || (pVisibilidad.Equals((short)Models.ResourceVisibility.specific) && pListaLectores.Count > 0))
            {
                //se limpian los lectores
                pDocumento.GestorDocumental.LimpiarSoloLectores(pDocumento.Clave);
                pDocumento.GestorDocumental.LimpiarSoloGruposLectores(pDocumento.Clave);
                pDocumento.GestorDocumental.ListaDocumentos[pDocumento.Clave].ListaGruposEditores = null;

                if (pListaLectores != null && pListaLectores.Count > 0)
                {
                    string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaLectores);

                    foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            if (id.StartsWith("g_"))
                            {
                                Guid grupoID = new Guid(id.Replace("g_", ""));
                                pDocumento.GestorDocumental.AgregarGrupoLectorARecurso(pDocumento.Clave, grupoID);
                            }
                            else
                            {
                                Guid perfilID = new Guid(id);
                                if (!pDocumento.ListaPerfilesEditores.ContainsKey(perfilID))
                                {
                                    pDocumento.GestorDocumental.AgregarLectorARecurso(pDocumento.Clave, perfilID);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            mLoggingService.GuardarLogError(ex, mlogger);
                        }
                    }
                }
            }
        }

        private void ConfigurarLectoresAdd(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaLectores)
        {

            string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaLectores);

            foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (id.StartsWith("g_"))
                    {

                        Guid grupoID = new Guid(id.Replace("g_", ""));
                        pDocumento.GestorDocumental.AgregarGrupoLectorARecurso(pDocumento.Clave, grupoID);
                    }
                    else
                    {
                        Guid perfilID = new Guid(id);
                        if (!pDocumento.ListaPerfilesEditores.ContainsKey(perfilID))
                        {
                            pDocumento.GestorDocumental.AgregarLectorARecurso(pDocumento.Clave, perfilID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, mlogger);
                }
            }
        }

        private void ConfigurarLectoresRemove(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaLectores)
        {
            string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaLectores);

            foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (id.StartsWith("g_"))
                    {
                        Guid grupoID = new Guid(id.Replace("g_", ""));
                        pDocumento.GestorDocumental.EliminarGrupoEditorARecurso(pDocumento.Clave, grupoID);
                    }
                    else
                    {
                        Guid perfilID = new Guid(id);
                        if (!pDocumento.ListaPerfilesEditores.ContainsKey(perfilID))
                        {
                            pDocumento.GestorDocumental.EliminarLectorEditorARecurso(pDocumento.Clave, perfilID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, mlogger);
                }
            }
        }


        private void ConfigurarEditores(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaEditores)
        {
            if (pDocumento != null && pDocumento.GestorDocumental != null)
            {
                //se limpian los editores
                pDocumento.GestorDocumental.LimpiarSoloEditores(pDocumento.Clave);
                pDocumento.GestorDocumental.LimpiarSoloGruposEditores(pDocumento.Clave);
                pDocumento.GestorDocumental.ListaDocumentos[pDocumento.Clave].ListaGruposEditores = null;
                string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaEditores);

                foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        if (id.StartsWith("g_"))
                        {
                            Guid grupoID = new Guid(id.Replace("g_", ""));
                            pDocumento.GestorDocumental.AgregarGrupoEditorARecurso(pDocumento.Clave, grupoID);
                        }
                        else
                        {
                            Guid perfilID = new Guid(id);
                            pDocumento.GestorDocumental.AgregarEditorARecurso(pDocumento.Clave, perfilID);
                        }
                    }
                    catch (Exception ex)
                    {
                        mLoggingService.GuardarLogError(ex, mlogger);
                    }
                }
            }
        }

        private void ConfigurarEditoresAdd(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaEditores)
        {
            if (pDocumento != null && pDocumento.GestorDocumental != null)
            {
                string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaEditores);

                foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        if (id.StartsWith("g_"))
                        {
                            Guid grupoID = new Guid(id.Replace("g_", ""));
                            pDocumento.GestorDocumental.AgregarGrupoEditorARecurso(pDocumento.Clave, grupoID);
                        }
                        else
                        {
                            Guid perfilID = new Guid(id);
                            if (!pDocumento.ListaPerfilesEditores.ContainsKey(perfilID))
                            {
                                pDocumento.GestorDocumental.AgregarEditorARecurso(pDocumento.Clave, perfilID);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        mLoggingService.GuardarLogError(ex, mlogger);
                    }
                }
            }
        }

        private void ConfigurarEditoresRemove(Elementos.Documentacion.Documento pDocumento, List<ReaderEditor> pListaEditores)
        {
            if (pDocumento != null && pDocumento.GestorDocumental != null)
            {
                //se limpian los editores
                string perfiles = ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(pListaEditores);

                foreach (string id in perfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        if (id.StartsWith("g_"))
                        {
                            Guid grupoID = new Guid(id.Replace("g_", ""));
                            //Eliminar
                            pDocumento.GestorDocumental.EliminarGrupoEditorARecurso(pDocumento.Clave, grupoID);
                        }
                        else
                        {
                            Guid perfilID = new Guid(id);
                            if (!pDocumento.ListaPerfilesEditores.ContainsKey(perfilID))
                            {
                                pDocumento.GestorDocumental.EliminarLectorEditorARecurso(pDocumento.Clave, perfilID);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        mLoggingService.GuardarLogError(ex, mlogger);
                    }
                }
            }
        }

        private bool ComprobarTienePermisoEdicionRecurso(Guid pResourceID)
        {
			DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
			DataWrapperDocumentacion dwDoc = docCN.ObtenerDocumentoPorID(pResourceID);
			GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
			Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, false);
			docCN.ObtenerDocumentoPorIDCargarTotal(pResourceID, gestorDoc.DataWrapperDocumentacion, true, true, null);
			gestorDoc.CargarDocumentos(false);
			Elementos.Documentacion.Documento documento = gestorDoc.ListaDocumentos[pResourceID];	
            
            docCN.Dispose();

            return documento.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false);
		}

        /// <summary>
        /// Comprueba si la identidad puede editar el recurso
        /// </summary>
        /// <returns>True si puede editarlo. False en caso contrario</returns>
        private bool ComprobarPermisosEdicion(Elementos.Documentacion.Documento pDocumento, Identidad pIdentidad, Proyecto pProyecto)
        {
            if (pDocumento.TipoDocumentacion.Equals(TiposDocumentacion.Wiki) && pDocumento.FilaDocumento.Protegido)
            {
                return false;
            }

            if (!pDocumento.FilaDocumento.UltimaVersion)
            {
                return false;
            }

            if (!ControladorDocumentacion.EsEditorPerfilDeDocumento(pIdentidad.PerfilID, pDocumento, true, Guid.Empty) && !pDocumento.TienePermisosEdicionIdentidad(pIdentidad, pIdentidad.IdentidadOrganizacion, pProyecto, Guid.Empty, false))
            {
                return false;
            }

            //No se puede editar un recuso encuesta
            if (pDocumento.TipoDocumentacion == TiposDocumentacion.Encuesta && !pDocumento.EsBorrador && pDocumento.FilaDocumento.DocumentoRespuestaVoto.Count > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Guarda el documento en la base de datos.
        /// </summary>
        /// <param name="pDocumentacionDS">DataSet de documentación</param>
        /// <param name="pListaProyectosActualizarNumRec">Lista con los proyectos a los que hay que acutulizarles el Num de Recursos</param>
        private void Guardar(List<Guid> pListaProyectosActualizarNumRec, GestorDocumental pGestorDocumental, Elementos.Documentacion.Documento pDocumento)
        {
            DataWrapperTesauro tesauroGuardarDW = null;

            if (pGestorDocumental.GestorTesauro != null)
            {
                tesauroGuardarDW = pGestorDocumental.GestorTesauro.TesauroDW;
            }

            mEntityContext.SaveChanges();

        }

        /// <summary>
        /// Método que realizará las comprobaciones necesarias para validar las listas pasadas como parámetros
        /// </summary>
        /// <param name="pJsonTripletas">Resource.Triples model object</param>
        /// <returns>True si la lista de triples contiene triples y ningún triple tiene ningún elemento nulo o vacío</returns>
        private bool ValidarListaTriples(Triples pJsonTripletas)
        {
            if (pJsonTripletas.triples_list == null || pJsonTripletas.triples_list.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (Triple triple in pJsonTripletas.triples_list)
                {
                    if (string.IsNullOrEmpty(triple.subject) || string.IsNullOrEmpty(triple.predicate) || string.IsNullOrEmpty(triple.object_t))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Obtiene un string con los IDs de los grupos separados por ,.
        /// </summary>
        /// <param name="pListaLectoresEditores">Nombre corto de los grupos</param>
        /// <returns>String con los IDs de los grupos separados por &amp;</returns>
        private string ObtenerPerfilesIDGruposIDLectoresEditoresPorNombreCorto(List<ReaderEditor> pListaLectoresEditores)
        {
            Dictionary<Guid, List<string>> nombreCortoGrupos = new Dictionary<Guid, List<string>>();

            foreach (ReaderEditor lectorEditor in pListaLectoresEditores)
            {
                Guid organizacionID = Guid.Empty;
                string nombreUsuario = lectorEditor.user_short_name;
                string nombreGrupo = lectorEditor.group_short_name;
                string nombreOrganizacion = lectorEditor.organization_short_name;

                if (!string.IsNullOrEmpty(nombreOrganizacion))
                {
                    OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<OrganizacionCN>(), mLoggerFactory);
                    organizacionID = orgCN.ObtenerOrganizacionesIDPorNombre(nombreOrganizacion);
                    orgCN.Dispose();
                    if (organizacionID == null || organizacionID.Equals(Guid.Empty))
                    {
                        throw new GnossException($"The organization short name {nombreOrganizacion} is not exist in the community", HttpStatusCode.BadRequest);
                    }
                }

                if (!nombreCortoGrupos.ContainsKey(organizacionID))
                {
                    nombreCortoGrupos.Add(organizacionID, new List<string>());
                }

                if (!string.IsNullOrEmpty(nombreUsuario))
                {
                    nombreCortoGrupos[organizacionID].Add(nombreUsuario);
                }
                else if (!string.IsNullOrEmpty(nombreGrupo))
                {
                    nombreCortoGrupos[organizacionID].Add(nombreGrupo);
                }
            }

            string grupos = "";
            IdentidadCN idenCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);

            foreach (Guid organizacionID in nombreCortoGrupos.Keys)
            {
                if (organizacionID.Equals(Guid.Empty))
                {
                    //lista de los GrupoID
                    List<Guid> gruposIDs = idenCN.ObtenerGruposIDPorNombreCortoYProyecto(nombreCortoGrupos[Guid.Empty], FilaProy.ProyectoID);

                    foreach (Guid grupoID in gruposIDs)
                    {
                        grupos += "g_" + grupoID + ",";
                    }

                    //lista de perfilesID personales, no grupo
                    List<Guid> perfilesIDs = idenCN.ObtenerPerfilIDPorNombreCortoYProyecto(nombreCortoGrupos[Guid.Empty], FilaProy.ProyectoID);
                    foreach (Guid perfilID in perfilesIDs)
                    {
                        grupos += perfilID + ",";
                    }
                }
                else
                {
                    List<Guid> gruposIDs = idenCN.ObtenerGruposIDPorNombreCortoYOrganizacion(nombreCortoGrupos[organizacionID], organizacionID);

                    foreach (Guid grupoID in gruposIDs)
                    {
                        grupos += "g_" + grupoID + ",";
                    }
                }

            }

            return grupos;
        }

        /// <summary>
        /// Método que transforma la petición para agregar al servicio módulo base en un string con los parámetros que necesita el servicio de replicación para enviar la solicitud.
        /// </summary>
        /// <param name="pDocumentoID">DocumentoID que se ha creado/editado</param>
        /// <param name="pTipoDoc">Tipo de documento que se ha creado.</param>
        /// <param name="pProyectoID">Proyecto donde se ha creado el documento.</param>
        /// <param name="pPrioridadBase">Prioridad para procesarlo por el servicio modulo base.</param>
        /// <param name="pAccion">Acción a realizar sobre el recurso, 0 agregar, 1 eliminar</param>
        /// <returns>Cadena de parámetros necesarios para que el servicio de replicación inserte en el módulo base.</returns>
        private string ObtenerInfoExtraBaseDocumento(Guid pDocumentoID, short pTipoDoc, Guid pProyectoID, short pPrioridadBase, int pAccion)
        {
            int id = -1;

            if (FilaProy != null && FilaProy.TablaBaseProyectoID != -1)
            {
                id = FilaProy.TablaBaseProyectoID;
            }
            else
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                id = proyCL.ObtenerTablaBaseProyectoIDProyectoPorID(pProyectoID);
                proyCL.Dispose();
            }

            string tempString = "";
            //TablaBaseProyectoID
            tempString += id + "|";
            //Tag
            tempString += Constantes.ID_TAG_DOCUMENTO + pDocumentoID.ToString() + Constantes.ID_TAG_DOCUMENTO + "," + Constantes.TIPO_DOC + pTipoDoc + Constantes.TIPO_DOC + "|";
            //Tipo de acción (0 agregado) (1 eliminado)
            tempString += pAccion + "|";
            //Prioridad de procesado por el servicio base.
            tempString += pPrioridadBase + "|";

            //pOtrosArgumentos;

            return tempString;
        }

        private string ObtenerTipoDocumento(string pUrlDoc, short pTipoDoc)
        {
            string rdfType = "";
            if (pTipoDoc == (short)TiposDocumentacion.Semantico && pUrlDoc.Contains("/"))
            {
                rdfType = pUrlDoc.Substring(pUrlDoc.LastIndexOf("/") + 1);
            }
            else
            {
                switch (pTipoDoc)
                {
                    case (short)TiposDocumentacion.Audio:
                        rdfType = "Audio";
                        break;
                    case (short)TiposDocumentacion.Debate:
                        rdfType = "Debate";
                        break;
                    case (short)TiposDocumentacion.Encuesta:
                        rdfType = "Encuesta";
                        break;
                    case (short)TiposDocumentacion.Pregunta:
                        rdfType = "Pregunta";
                        break;
                    case (short)TiposDocumentacion.Video:
                        rdfType = "Video";
                        break;
                    case (short)TiposDocumentacion.Imagen:
                        rdfType = "Imagen";
                        break;
                    case (short)TiposDocumentacion.Nota:
                        rdfType = "Nota";
                        break;
                    case (short)TiposDocumentacion.FicheroServidor:
                        rdfType = "FicheroServidor";
                        break;
                    case (short)TiposDocumentacion.Hipervinculo:
                        rdfType = "Hipervinculo";
                        break;
                    default:
                        rdfType = "Recurso";
                        break;
                }
            }

            return rdfType;
        }

        private void ComprobarUsuarioEnProyectoAdmiteTipoRecurso(Guid pIdentidadID, Guid pIdentidadEnMyGnossID, Guid pProyectoID, Guid pUsuarioID, TiposDocumentacion pTipoDoc, Guid pElementoVinculadoID, bool pEstaCompartiendoRecurso, List<Guid> pListaPermisosOntologias = null, List<TiposDocumentacion> pTipoDocPermitidos = null)
        {
            Guid proyectoOntologiasID = pProyectoID;
            bool identidadDeOtroProyecto = false;

            if (ParametroProyecto.ContainsKey(ParametroAD.ProyectoIDPatronOntologias))
            {
                proyectoOntologiasID = new Guid(ParametroProyecto[ParametroAD.ProyectoIDPatronOntologias]);
                identidadDeOtroProyecto = true;
            }
            else if (Proyecto.FilaProyecto.ProyectoSuperiorID.HasValue)
            {
                proyectoOntologiasID = Proyecto.FilaProyecto.ProyectoSuperiorID.Value;
                identidadDeOtroProyecto = true;
            }

            if (pTipoDoc == TiposDocumentacion.Semantico && !pEstaCompartiendoRecurso)
            {
                if (pListaPermisosOntologias == null)
                {
                    ProyectoCN proyCN = new ProyectoCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                    TipoRolUsuario tipoRolUsuario = proyCN.ObtenerRolUsuarioEnProyecto(pProyectoID, pUsuarioID);
                    pListaPermisosOntologias = proyCN.ObtenerOntologiasPermitidasIdentidadEnProyecto(pIdentidadID, pIdentidadEnMyGnossID, proyectoOntologiasID, tipoRolUsuario, identidadDeOtroProyecto);
                    proyCN.Dispose();
                    proyCL.Dispose();
                }

                if (!pListaPermisosOntologias.Contains(pElementoVinculadoID))
                {
                    throw new GnossException("The user has no permission to upload resources of this ontology.", HttpStatusCode.BadRequest);
                }
            }
            else
            {

                if (pTipoDocPermitidos == null)
                {
                    ProyectoCN proyCN = new ProyectoCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    pTipoDocPermitidos = proyCN.ObtenerTiposDocumentosPermitidosUsuarioEnProyectoPorUsuID(proyectoOntologiasID, pUsuarioID);
                    proyCN.Dispose();
                }


                if (!pTipoDocPermitidos.Contains(pTipoDoc))
                {
                    throw new GnossException("The user has no permission to upload resources of ANY ontology.", HttpStatusCode.BadRequest);
                }
            }
        }

        private Guid AgregarNuevoComentario(Guid pProyectoID, string pNombreCortoProyecto, string pNombreCortoUsuario, DateTime pFechaComentario, string pDescripcionHTML, Guid pComentarioPadreID)
        {
            Guid comentarioID = Guid.NewGuid();

            //Guardamos el nuevo comentario
            DataWrapperComentario comentarioDW = new DataWrapperComentario();
            AD.EntityModel.Models.Comentario.Comentario comentario = new AD.EntityModel.Models.Comentario.Comentario();
            comentario.ComentarioID = comentarioID;

            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
            Guid identidadID = identCN.ObtenerIdentidadIDPorNombreCorto(pNombreCortoUsuario, pProyectoID);
            identCN.Dispose();

            if (identidadID.Equals(Guid.Empty))
            {
                throw new Exception("No se ha encontrado el usuario con nombre corto '" + pNombreCortoUsuario + "' en el proyecto '" + pNombreCortoProyecto + "'");
            }

            comentario.IdentidadID = identidadID;
            comentario.Fecha = pFechaComentario;
            comentario.Descripcion = pDescripcionHTML;
            comentario.Eliminado = false;

            //Controlamos que el comentario padre sea distinto de un guid.empty.
            if (pComentarioPadreID != Guid.Empty)
            {
                comentario.ComentarioSuperiorID = pComentarioPadreID;
            }

            comentarioDW.ListaComentario.Add(comentario);
            mEntityContext.Comentario.Add(comentario);

            ComentarioCN comCN = new ComentarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ComentarioCN>(), mLoggerFactory);
            comCN.ActualizarComentarioEntity();
            comCN.Dispose();

            return comentarioID;
        }

        /// <summary>
        /// Sube un recurso a una comunidad de gnoss
        /// </summary>
        /// <param name="pNombreCortoComunidad">Nombre corto de la comunidad en la que se publicará el recurso</param>
        /// <param name="pDocumentoID">Identificador del documento</param>
        /// <param name="pTitulo">Título del recurso</param>
        /// <param name="pDescripcion">Descripción del recurso</param>
        /// <param name="pTags">Array de strings con los tags del recurso</param>
        /// <param name="pCategoriaIDs">Identificadores de las categorías del tesauro de la comunidad en las que se indexa el recurso</param>
        /// <param name="pTipoDoc">Tipo del documento. Elegir uno de la lista, según corresponda: 
        /// Hipervinculo = 0,
        /// ReferenciaADoc = 1,
        /// Video = 2,
        /// Archivo digital = 3,
        /// Semantico = 5,
        /// Imagen = 6,
        /// Ontologia = 7,
        /// Nota = 8,
        /// Wiki = 9,
        /// </param>
        /// <param name="pUrlDoc">Dependiendo del tipo de archivo subido, será la URL del enlace, el nombre del archivo digital, ó la URL de la ontologia que se a la que pertenece el recurso</param>
        /// <param name="pArchivoDoc">(OPCIONAL) Array de bytes del recurso que se publica</param>
        /// <param name="pPropiedadesRdfArchivo">(OPCIONAL) Valores de las propiedades rdf que son de tipo archivo</param>
        /// <param name="pTipoPropiedadArchivo">(OPCIONAL) Tipos de archivos de las propiedades de tipo archivo</param>
        /// <param name="pArchivosAdjuntosRdf">(OPCIONAL) Archivos adjuntos que se suben</param>
        /// <param name="pCreadorAutorRec">Indica si el creador es autor</param>
        /// <param name="pAutores">Autores</param>
        /// <param name="pTextoTagsAutomaticosTitulo">Texto para etiquetar tags modo título</param>
        /// <param name="pTextoTagsAutomaticosDescripcion">Texto para etiquetar tags modo descripción</param>
        /// <param name="pUrlOauth">URL con la autenticación oauth firmada</param>
        /// <param name="pCrearCaptura">Indica si se debe crear la captura o no</param>
        /// <param name="pUrlCaptura">Url de la que se va a obtener la captura en los recursos semánticos.</param>
        /// <param name="pPredCaptura">Predicado donde se va a insertar la imagen, si hay más de un nivel separarlo por tuberias "|" y cuidado con los predicados de gnossonto porque no se pone el namespace delante.</param>
        /// <param name="pPrioridad">Prioridad de la cargar, si es 11 o más no borra caché</param>
        /// <param name="pGruposEditores">Grupos de editores</param>
        /// <param name="pGruposLectores">Grupos de lectores</param>
        /// <param name="pFechaCreacion">Fecha de creación que deberá tener el recurso</param>
        /// <param name="pImgPrincipal">Cadena con la imagen principal</param>
        /// <param name="pUsarColareplicacion">Indica si inserta en la cola de replicación</param>
        /// <returns>Guid el documento</returns>
        [NonAction]
        public string SubirRecursoInt(LoadResourceParams parameters, StringBuilder pSbSql = null, StringBuilder pSbDocumento = null, StringBuilder pSbDocumentoWebVinBaseRecursos = null, StringBuilder pSbDocumentoWebVinBaseRecursosExtra = null, StringBuilder pSbColaDocumento = null, StringBuilder pSbDocumentoRolGrupoIdentidades = null, StringBuilder pSbDocumentoRolIdentidad = null, StringBuilder pSbDocumentoWebAgCatTesauro = null, StringBuilder pSbVirtuoso = null, Identidad pIdentidad = null)
        {
            bool compartir;
            mNombreCortoComunidad = parameters.community_short_name;

            // Si el título llega con saltos de línea se los quito. 
            parameters.title = parameters.title.Replace("\r\n", " ").Replace("\n", " ");

            bool usarReplicacion = false;

            if (FilaProy != null && pIdentidad == null)
            {
                ComprobacionCambiosCachesLocales(FilaProy.ProyectoID);
            }

            if (parameters.permitir_compartido == null)
            {
                compartir = (FilaProy.TipoAcceso == (short)TipoAcceso.Publico || FilaProy.TipoAcceso == (short)TipoAcceso.Restringido);
            }
            else
            {
                compartir = (bool)parameters.permitir_compartido;
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            bool existeRecurso = documentacionCN.ContieneRecurso(parameters.resource_id, FilaProy.OrganizacionID, FilaProy.ProyectoID);
            documentacionCN.Dispose();

            if (existeRecurso)
            {
                throw new GnossException("A resource already exists in the community with the identifier: " + parameters.resource_id, HttpStatusCode.BadRequest);
            }

            GuardarLogTiempos("Tras cargar gestorDoc y el proyecto");
            Identidad identidad = pIdentidad;
            bool esAdminProyecto = false;

            //validación de datos
            Dictionary<string, object> diccionarioParametros = new Dictionary<string, object>();
            diccionarioParametros.Add("titulo", parameters.title);
            diccionarioParametros.Add("etiquetas", parameters.tags);

            if (parameters.resource_type != (short)TiposDocumentacion.Semantico)
            {
                List<object> listaParametros = new List<object>();
                listaParametros.Add(parameters.categories);
                listaParametros.Add(new List<Guid>(gestorDoc.GestorTesauro.ListaCategoriasTesauro.Keys));
                diccionarioParametros.Add("categorias", listaParametros);
            }

            ValidarDatosRecurso(diccionarioParametros);
            //Aligerar Identidad
            if (identidad == null)
            {
                if (!string.IsNullOrEmpty(parameters.publisher_email))
                {
                    ProyectoCN proyCN = new ProyectoCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
                    esAdminProyecto = proyCN.EsUsuarioAdministradorProyectoMYGnoss(UsuarioOAuth);
                    proyCN.Dispose();
                }
                else
                {
                    identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
                }


                if (esAdminProyecto)
                {
                    PersonaCN persCN = new PersonaCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<PersonaCN>(), mLoggerFactory);
                    Guid personaID = persCN.ObtenerPersonaPorEmail(parameters.publisher_email);
                    Guid? userID = persCN.ObtenerUsuarioIDDePersonaID(personaID);
                    if (userID.HasValue)
                    {
                        identidad = CargarIdentidad(gestorDoc, FilaProy, userID.Value, true);
                    }
                    persCN.Dispose();
                }
            }
            else
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            }

            GuardarLogTiempos("Tras cargar gestor identidad");
            Guid elementoVinculadoID = Guid.Empty;
            string rutaFichero = null;

            if (parameters.resource_type == (short)TiposDocumentacion.Semantico)
            {
                //Si el recurso es semántico al final confiramremos las transacciones
                mEntityContext.NoConfirmarTransacciones = true;

                rutaFichero = parameters.title + ".rdf";

                DocumentacionCN docCN = new DocumentacionCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);



                elementoVinculadoID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, parameters.resource_url);
                if (elementoVinculadoID.Equals(Guid.Empty))
                {
                    throw new Exception("La ontología '" + parameters.resource_url + "' no pertenece al proyecto.");
                }



                gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(elementoVinculadoID));
                docCN.Dispose();
                gestorDoc.CargarDocumentos(false);

                GuardarLogTiempos("Tras obtener ontología");
            }

            //Comprobar si la comunidad permite el tipo de recurso:
            ComprobarUsuarioEnProyectoAdmiteTipoRecurso(identidad.Clave, identidad.IdentidadMyGNOSS.Clave, FilaProy.ProyectoID, identidad.Usuario.Clave, (TiposDocumentacion)parameters.resource_type, elementoVinculadoID, false);

            GuardarLogTiempos("Tras comprobar si usuario puede subir este rec en esta comunidad");

            #region Tags automaticos servicio

            List<string> listaTags = null;

            if (parameters.tags != null)
            {
                listaTags = new List<string>();
                foreach (string tag in parameters.tags)
                {
                    listaTags.Add(tag.ToLower());
                }
            }
            else
            {
                listaTags = new List<string>();
            }

            if (!string.IsNullOrEmpty(parameters.auto_tags_title_text) || !string.IsNullOrEmpty(parameters.auto_tags_description_text))
            {
                List<string> tagsAuto = ObtenerEtiquetasAutomaticas(parameters.auto_tags_title_text, parameters.auto_tags_description_text, FilaProy.ProyectoID);

                foreach (string tagAuto in tagsAuto)
                {
                    if (!listaTags.Contains(tagAuto))
                    {
                        listaTags.Add(tagAuto.ToLower());
                    }
                }

                GuardarLogTiempos("Tras Llamar al servicio automatico de tags");
            }

            #endregion

            #region Fechacreacion

            GuardarLogTiempos("Antes fecha creación");

            DateTime? fechaCreacion = null;

            if (parameters.creation_date.HasValue)
            {
                try
                {
                    fechaCreacion = parameters.creation_date.Value;
                }
                catch (Exception)
                {
                    throw new GnossException("The parameter 'publish_date' does not have the correct format 'dd/MM/yyyy'.", HttpStatusCode.BadRequest);
                }
            }

            #endregion

            string descripcion = string.Empty;
            if (!string.IsNullOrEmpty(parameters.description))
            {
                descripcion = UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(parameters.description.Trim().Replace("\r\n", ""), "<br>"), "</br>"), "</ br>"), "<p>&nbsp;</p>");
            }

            string tags = UtilCadenas.CadenaFormatoTexto(listaTags);

            GuardarLogTiempos("Antes de tipos docs");

            if (parameters.resource_type == (short)TiposDocumentacion.Nota)
            {
                rutaFichero = "Nota";
            }
            else if (parameters.resource_type == (short)TiposDocumentacion.FicheroServidor || parameters.resource_type == (short)TiposDocumentacion.Imagen)
            {
                rutaFichero = parameters.resource_url;

                if (string.IsNullOrEmpty(rutaFichero))
                {
                    throw new Exception("El parametro resource_url no puede ser vacio, o nulo cuando el tipo de Documentación es FicheroServidor(3) o Imagen(6)");
                }
                else if (rutaFichero.Contains("/"))
                {
                    rutaFichero = rutaFichero.Substring(rutaFichero.LastIndexOf("/") + 1);
                }
            }
            else if (parameters.resource_type == (short)TiposDocumentacion.Newsletter)
            {
                rutaFichero = "Newsletter";
            }
            else if (parameters.resource_type == (short)TiposDocumentacion.Video)
            {
                bool esVideoIncrustado = ComprobarEsVideoIncrustado(parameters.resource_url);

                if (esVideoIncrustado)
                {
                    rutaFichero = parameters.resource_url;
                }
                else
                {
                    //Añadimos el documento a la definición y a los dataset que corresponda
                    int indicepunto = parameters.resource_url.LastIndexOf(".");
                    string documentoSinExtension;

                    if (indicepunto != -1)
                    {
                        documentoSinExtension = parameters.resource_url.Substring(0, indicepunto);
                    }
                    else
                    {
                        documentoSinExtension = parameters.resource_url;
                    }

                    rutaFichero = documentoSinExtension + ".flv";
                }
            }
            else
            {
                rutaFichero = parameters.resource_url;
            }

            GuardarLogTiempos("Antes autores");

            string autores;

            if (parameters.creator_is_author)
            {
                if (identidad.Tipo.Equals(TiposIdentidad.ProfesionalCorporativo))
                {
                    //Si participa en modo corporativo, establezco solo el nombre de la organización
                    autores = identidad.NombreOrganizacion;
                }
                else
                {
                    //Si no, pongo su nombre, o el suyo junto al de la organización (según como participe en esta comunidad)
                    autores = identidad.Nombre();
                }

                if (!string.IsNullOrEmpty(parameters.authors))
                {
                    autores = autores + ", " + parameters.authors;
                }
            }
            else
            {
                autores = parameters.authors;
            }

            Guid documentoID = Guid.NewGuid();

            if (!parameters.resource_id.Equals(Guid.Empty))
            {
                documentoID = parameters.resource_id;
                mTieneDocumentoID = true;
            }

            Elementos.Documentacion.Documento doc = gestorDoc.AgregarDocumento(documentoID, rutaFichero, parameters.title.Trim(), descripcion, tags, (TiposDocumentacion)parameters.resource_type, TipoEntidadVinculadaDocumento.Web, true, elementoVinculadoID, compartir, false, parameters.creator_is_author, autores, false, FilaProy.OrganizacionID, identidad.Clave);

            GuardarLogTiempos("Tras Agregar doc al gestor documental");

            //Agrego la comunidad a la que pertenece el documento:
            doc.FilaDocumento.ProyectoID = FilaProy.ProyectoID;

            GuardarLogTiempos("Tras Agregar doc al gestor documental");

            //Agrego la comunidad a la que pertenece el documento:
            doc.FilaDocumento.ProyectoID = FilaProy.ProyectoID;

            if (fechaCreacion.HasValue)
            {
                doc.FilaDocumento.FechaCreacion = fechaCreacion.Value;
            }

            doc.FilaDocumento.FechaModificacion = doc.FilaDocumento.FechaCreacion;
            bool agregarCategoriasTesauro = true;
            List<TripleWrapper> listaTriplesSemanticos = new List<TripleWrapper>();
            string nombreOntologia = "";
            Ontologia ontologia = null;

            if (parameters.resource_type == (short)TiposDocumentacion.Video || parameters.resource_type == (short)TiposDocumentacion.FicheroServidor || parameters.resource_type == (short)TiposDocumentacion.Imagen)
            {
                string nomFichero = AgregarArchivo(parameters.resource_file, parameters.resource_type, rutaFichero, doc.Clave, FilaProy, usarReplicacion);
                if (!string.IsNullOrEmpty(nomFichero))
                {
                    doc.FilaDocumento.FechaCreacion = fechaCreacion.Value;
                }
            }
            else if (parameters.resource_type == (short)TiposDocumentacion.Semantico)
            {
                ontologia = ObtenerOntologia(doc.ElementoVinculadoID);
                nombreOntologia = doc.GestorDocumental.ListaDocumentos[doc.ElementoVinculadoID].FilaDocumento.Enlace;
                int[] tamañosCaptura = null;
                if (parameters.screenshot_sizes != null)
                {
                    tamañosCaptura = parameters.screenshot_sizes.ToArray();
                }

                string imgRepreDoc = AgregarArchivoSemantico(parameters.resource_file, elementoVinculadoID, ontologia, doc.Clave, gestorDoc, parameters.resource_attached_files, false, parameters.priority, parameters.categories, parameters.main_image, usarReplicacion, out listaTriplesSemanticos, parameters.create_screenshot, parameters.url_screenshot, parameters.predicate_screenshot, tamañosCaptura, null, pSbVirtuoso);

                if (imgRepreDoc != null)
                {
                    doc.FilaDocumento.NombreCategoriaDoc = imgRepreDoc;
                }

                GuardarLogTiempos("Tras guardar en virtuso formulario sem");

                if (!string.IsNullOrEmpty(parameters.canonical_url))
                {

                    gestorDoc.DataWrapperDocumentacion.ListaDocumentoUrlCanonica.Add(new DocumentoUrlCanonica { DocumentoID = doc.Clave, UrlCanonica = parameters.canonical_url });
                }
                agregarCategoriasTesauro = !ontologia.ConfiguracionPlantilla.CategorizacionTesauroGnossNoObligatoria;

                GuardarLogTiempos("Tras comprobar si categorias son obligatorias");
            }

            //A partir de aquí, si algo va mal tengo que revertir los cambios hechos en la parte semántica
            try
            {
                List<CategoriaTesauro> listaCategorias = new List<CategoriaTesauro>();

                if (parameters.categories != null && parameters.categories.Count > 0)
                {
                    foreach (Guid clave in parameters.categories)
                    {
                        CategoriaTesauro categoria = gestorDoc.GestorTesauro.ListaCategoriasTesauro[clave];
                        listaCategorias.Add(categoria);
                    }

                    GuardarLogTiempos("Tras agregar categorías");
                }

                gestorDoc.AgregarDocumento(listaCategorias, doc, identidad.Clave, false, Guid.Empty);

                //al agregar el documento, está machacando en la ListaDocumentos el doc por un documentoweb
                doc = gestorDoc.ListaDocumentos[doc.Clave];

                //GuardarEditoresGrupos(doc, pGruposEditores, pGruposLectores);
                EstablecerPrivacidadRecurso(doc, parameters.visibility, false);
                ConfigurarLectores(doc, parameters.readers_list, parameters.visibility);

                //Ajusto la fecha de publicación:
                foreach (DocumentoWebVinBaseRecursos row in gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos)
                {
                    row.FechaPublicacion = doc.FilaDocumento.FechaCreacion;
                }
                if (parameters.editors_list != null && parameters.editors_list.Count > 0)
                {
                    ConfigurarEditores(doc, parameters.editors_list);
                }

                //si se comprueba que la lista del documento no lo contenga, se está cargando la lista y al agregar el perfil no se refresca
                if (!doc.ListaPerfilesEditores.ContainsKey(identidad.PerfilID))
                {
                    doc.GestorDocumental.AgregarEditorARecurso(doc.Clave, identidad.PerfilID);
                }

                GuardarLogTiempos("Tras agregar editores");

                //Ajusto la fecha de publicación:
                foreach (DocumentoWebVinBaseRecursos row in gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos)
                {
                    row.FechaPublicacion = doc.FilaDocumento.FechaCreacion;
                }

                GuardarLogTiempos("Tras modificar fecha publicacion");

                if (!mIndexarRecursos)
                {
                    foreach (DocumentoWebVinBaseRecursos row in gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos)
                    {
                        row.IndexarRecurso = false;
                    }
                }

                GuardarLogTiempos("Tras gestionar indexado");


                GuardarLogTiempos("Antes guardar en BD ");

                List<Guid> listaProyActualizar = new List<Guid>();
                listaProyActualizar.Add(FilaProy.ProyectoID);

                //LecturaAumntada
                if (parameters.aumented_reading != null)
                {
                    DocumentoLecturaAumentada docLecturaAumentada = new DocumentoLecturaAumentada();
                    docLecturaAumentada.DescripcionAumentada = parameters.aumented_reading.description;
                    docLecturaAumentada.TituloAumentado = parameters.aumented_reading.title;
                    docLecturaAumentada.Validada = true;
                    docLecturaAumentada.DocumentoID = parameters.resource_id;
                    mEntityContext.DocumentoLecturaAumentada.Add(docLecturaAumentada);
                }

                if (pSbSql != null)
                {
                    GenerarScriptSql(gestorDoc.DataWrapperDocumentacion, pSbDocumento, pSbDocumentoWebVinBaseRecursos, pSbDocumentoWebVinBaseRecursosExtra, pSbColaDocumento, pSbDocumentoRolGrupoIdentidades, pSbDocumentoRolIdentidad, pSbDocumentoWebAgCatTesauro);
                }
                else
                {
                    Guardar(listaProyActualizar, gestorDoc, doc);
                }

                if (parameters.resource_type == (short)TiposDocumentacion.Semantico)
                {
                    mEntityContext.TerminarTransaccionesPendientes(true);
                }

                GuardarLogTiempos("Tras Guardar en BD");

                if (parameters.create_screenshot)
                {
                    GuardarLogTiempos("pCrearCaptura == true");

                    if (doc.TipoDocumentacion == TiposDocumentacion.Hipervinculo || doc.TipoDocumentacion == TiposDocumentacion.Imagen || doc.TipoDocumentacion == TiposDocumentacion.Nota || doc.TipoDocumentacion == TiposDocumentacion.Video || doc.EsPresentacionIncrustada || doc.EsVideoIncrustado)
                    {
                        if (doc.TipoDocumentacion == TiposDocumentacion.Hipervinculo && parameters.resource_file != null)
                        {
                            System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(new MemoryStream(parameters.resource_file));

                            MemoryStream ms = new MemoryStream();
                            imagen.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] buffer = ms.ToArray();

                            ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                            servicioImagenes.Url = UrlServicioImagenes;
                            servicioImagenes.AgregarImagenADirectorio(buffer, "../imagenesEnlaces/" + UtilArchivos.DirectorioDocumento(doc.Clave), doc.Clave.ToString(), ".jpg");

                            List<Guid> documentosID = new List<Guid>();
                            foreach (AD.EntityModel.Models.Documentacion.Documento dr in gestorDoc.DataWrapperDocumentacion.ListaDocumento)
                            {
                                documentosID.Add(dr.DocumentoID);
                            }

                            ControladorDocumentacion.CapturarImagenesWeb(documentosID);

                            GuardarLogTiempos("Tras Caputra web");
                        }
                        else if (doc.TipoDocumentacion == TiposDocumentacion.Nota || doc.EsVideoIncrustado || doc.EsPresentacionIncrustada || doc.TipoDocumentacion == TiposDocumentacion.Hipervinculo)
                        {
                            ControladorDocumentacion.CapturarImagenWeb(doc.Clave, true, PrioridadColaDocumento.Alta, mAvailableServices);
                        }
                        else if (doc.TipoDocumentacion == TiposDocumentacion.Imagen)
                        {
                            ControladorDocumentacion.CapturarImagenWeb(doc.Clave, false, PrioridadColaDocumento.Alta, mAvailableServices);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (parameters.resource_type == (short)TiposDocumentacion.Semantico)
                {
                    // Ha fallado algo durante el guardado en el modelo ACID, 

                    mEntityContext.TerminarTransaccionesPendientes(false);

                    mLoggingService.GuardarLogError(ex, $"SubirRecursoInt: Error al guardar modificaciones en BD Ácida. Se han revertido los cambios en Virtuoso y BD RDF del recurso {doc.Clave}",mlogger);
                }
                throw;
            }

            //ControladorDocumentacion.EstablecePrivacidadRecursoEnMetaBuscadorDesdeServicio(doc, identidad, true, "acid");
            //GuardarLogTiempos("Tras establecer privacidad recurso");

            if (AgregarColaBase && (doc.FilaDocumento.Tipo != (short)TiposDocumentacion.Semantico || !usarReplicacion))
            {
                GuardarLogTiempos("Antes de GuardarRecursoEnGrafoBusqueda");

                #region Guardado en el Grafo de búsqueda

                //ControladorDocumentacion.GuardarRecursoEnGrafoBusqueda(doc, Proyecto, listaTriplesSemanticos, ontologia, gestorDoc.GestorTesauro, UrlIntragnoss, parameters.create_version, null, PrioridadBase.ApiRecursos);
                DataWrapperProyecto dataWrapperProyecto = new DataWrapperProyecto();
                dataWrapperProyecto.ListaProyecto.Add(FilaProy);
                Proyecto proyecto = new GestionProyecto(dataWrapperProyecto, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestionProyecto>(), mLoggerFactory).ListaProyectos[FilaProy.ProyectoID];
                ControladorDocumentacion.GuardarRecursoEnGrafoBusqueda(doc, proyecto, listaTriplesSemanticos, ontologia, gestorDoc.GestorTesauro, UrlIntragnoss, parameters.create_version, null, PrioridadBase.ApiRecursos, mAvailableServices);

                #endregion

                GuardarLogTiempos("Tras GuardarRecursoEnGrafoBusqueda");
            }

            try
            {
                int tipo;
                switch (doc.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                if (pSbVirtuoso == null && (AgregarColaLive || parameters.publish_home))
                {
                    ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, doc.Clave, AccionLive.Agregado, tipo, "base", PrioridadLive.Baja, mAvailableServices);
                    ControladorDocumentacion.ActualizarGnossLive(FilaProy.ProyectoID, identidad.Clave, AccionLive.RecursoAgregado, (int)TipoLive.Miembro, "base", PrioridadLive.Baja, mAvailableServices);
                    GuardarLogTiempos("Tras Actualizar Live");
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, "Error al insertar en la cola del Live", mlogger);
                GuardarLogTiempos("Tras Actualizar Live con FALLO");
            }

            try
            {
                if (pSbVirtuoso == null && ((parameters.readers_list != null && parameters.readers_list.Count > 0) || (parameters.editors_list != null && parameters.editors_list.Count > 0)))
                {
                    DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                    docCL.Dispose();
                }
            }
            catch (Exception)
            { /*error invalidando caché*/ }

            #region ColaSitemaps

            BaseComunidadCN baseComunidadColaSiteMapsCN = new BaseComunidadCN("base", mEntityContext, mLoggingService, mEntityContextBASE, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<BaseComunidadCN>(), mLoggerFactory);

            //si existe el sitemap de la comunidad añado el recurso
            if (pSbVirtuoso == null && FilaParametroGeneral.TieneSitemapComunidad && doc.FilaDocumentoWebVinBR.FechaPublicacion.HasValue)
            {
                baseComunidadColaSiteMapsCN.InsertarFilaEnColaColaSitemaps(doc.FilaDocumento.DocumentoID, TiposEventoSitemap.RecursoNuevo, 0, doc.FilaDocumentoWebVinBR.FechaPublicacion.Value, 1, FilaProy.NombreCorto);
                baseComunidadColaSiteMapsCN.Dispose();
            }

            GuardarLogTiempos("Tras llamar a InsertarFilaEnColaColaSitemaps");

            #endregion

            GuardarLogTiempos("Fin documento " + doc.Clave);
            EscribirLogTiempos(doc.Clave);
            if (mIDEntidadPrincipal != null)
            {
                return mIDEntidadPrincipal;
            }
            else
            {
                return doc.Clave.ToString();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pCrearVersion">Booleano, indica si se quiere editar el recurso o crear una nueva versión del mismo</param>
        /// <param name="pNombreCortoComunidad">Nombre corto de la comunidad en la que está el recurso</param>
        /// <param name="pDocumentoID">Identificador del recurso que se quiere modificar</param>
        /// <param name="pTitulo">Título del recurso</param>
        /// <param name="pDescripcion">Descripción del recurso</param>
        /// <param name="pTags">Array de strings con los tags del recurso</param>
        /// <param name="pCategoriaIDs">Identificadores de las categorías del tesauro de la comunidad en las que se indexa el recurso</param>
        /// <param name="pUrlDoc">Dependiendo del tipo de archivo subido, será la URL del enlace, el nombre del archivo digital, ó la URL de la ontologia que se a la que pertenece el recurso</param>
        /// <param name="pArchivoDoc">(OPCIONAL) Array de bytes del recurso que se publica</param>
        /// <param name="pPropiedadesRdfArchivo">(OPCIONAL) Valores de las propiedades rdf que son de tipo archivo</param>
        /// <param name="pTipoPropiedadArchivo">(OPCIONAL) Tipos de archivos de las propiedades de tipo archivo</param>
        /// <param name="pArchivosAdjuntosRdf">(OPCIONAL) Archivos adjuntos que se suben</param>
        /// <param name="pUrlOauth">URL con la autenticación oauth firmada</param>
        /// <param name="pCreadorAutorRec">Indica si el creador es autor</param>
        /// <param name="pAutores">Autores</param>
        /// <param name="pTextoTagsAutomaticosTitulo">Texto para etiquetar tags modo título</param>
        /// <param name="pTextoTagsAutomaticosDescripcion">Texto para etiquetar tags modo descripción</param>
        /// <param name="pImgPrincipal">Cadena con la imagen principal</param>
        /// <returns>TRUE si es correcto</returns>
        private bool ModificarRecursoInt(LoadResourceParams parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            // Si el título llega con saltos de línea se los quito. 
            parameters.title = parameters.title.Replace("\r\n", " ").Replace("\n", " ");

            bool usarReplicacion = false;

            if (FilaProy != null)
            {
                ComprobacionCambiosCachesLocales(FilaProy.ProyectoID);
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            DocumentacionCN docCN = new DocumentacionCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DocumentoWeb documentoEdicion = null;
            List<ElementoOntologia> entidadesPrincAntiguas = null;
            string rdfTexto = null;
            bool agregarCategoriasTesauro = true;
            List<TripleWrapper> listaTriplesSemanticos = new List<TripleWrapper>();
            Ontologia ontologia = null;
            string nombreOntologia = string.Empty;
            Elementos.Documentacion.Documento documentoAntiguo = null;

            bool documentoBloqueado = ComprobarDocumentoEnEdicion(parameters.resource_id, identidad.Clave);
            try
            {

                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                List<Guid> listaDocs = new List<Guid>();
                listaDocs.Add(parameters.resource_id);
                docCN.ObtenerVersionDocumentosPorIDs(gestorDoc.DataWrapperDocumentacion, listaDocs, true);
                gestorDoc.CargarDocumentos(false);

                documentoAntiguo = gestorDoc.ListaDocumentos[parameters.resource_id];

                bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

                if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
                }

                //validación de datos
                Dictionary<string, object> diccionarioParametros = new Dictionary<string, object>();
                diccionarioParametros.Add("titulo", parameters.title);
                diccionarioParametros.Add("etiquetas", parameters.tags);

                if (documentoAntiguo.TipoDocumentacion != TiposDocumentacion.Semantico)
                {
                    List<object> listaParametros = new List<object>();
                    listaParametros.Add(parameters.categories);
                    listaParametros.Add(new List<Guid>(gestorDoc.GestorTesauro.ListaCategoriasTesauro.Keys));
                    diccionarioParametros.Add("categorias", listaParametros);
                }

                ValidarDatosRecurso(diccionarioParametros);

                if (!parameters.create_version)
                {
                    documentoEdicion = new DocumentoWeb(documentoAntiguo.FilaDocumento, gestorDoc);
                }
                else
                {
                    if (!documentoAntiguo.FilaDocumento.UltimaVersion)
                    {
                        throw new Exception("No es la última versión del documento");
                    }

                    documentoEdicion = new DocumentoWeb(CreateDocumentVersion(documentoAntiguo, identidad).FilaDocumento, gestorDoc);
                }

                documentoEdicion.Titulo = parameters.title;

                #region Autores

                string autores = "";
                if (parameters.creator_is_author)
                {
                    documentoEdicion.FilaDocumento.CreadorEsAutor = true;

                    if (identidad.Tipo.Equals(TiposIdentidad.ProfesionalCorporativo))
                    {
                        //Si participa en modo corporativo, establezco solo el nombre de la organización
                        autores = identidad.NombreOrganizacion;
                    }
                    else
                    {
                        //Si no, pongo su nombre, o el suyo junto al de la organización (según como participe en esta comunidad)
                        autores = identidad.Nombre();
                    }

                    if (!string.IsNullOrEmpty(parameters.authors))
                    {
                        autores = autores + ", " + parameters.authors;
                    }
                }
                else
                {
                    autores = parameters.authors;
                    documentoEdicion.FilaDocumento.CreadorEsAutor = false;
                    documentoEdicion.FilaDocumento.Autor = autores;
                }

                if (!string.IsNullOrEmpty(autores))
                {
                    documentoEdicion.FilaDocumento.Autor = autores;
                }

                #endregion

                string descripcion = string.Empty;

                if (!string.IsNullOrEmpty(parameters.description))
                {
                    descripcion = UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(parameters.description.Trim().Replace("\r\n", ""), "<br>"), "</br>"), "</ br>"), "<p>&nbsp;</p>");
                }

                documentoEdicion.Descripcion = descripcion;

                #region Tags automaticos servicio

                List<string> listaTags = new List<string>();

                if (parameters.tags != null)
                {
                    foreach (string tag in parameters.tags)
                    {
                        listaTags.Add(tag.ToLower());
                    }
                }

                ReplaceTags(listaTags, documentoEdicion, parameters.auto_tags_title_text, parameters.auto_tags_description_text);

                #endregion

                string urlDoc = "";
                docCN = new DocumentacionCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

                if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                {
                    gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(documentoEdicion.ElementoVinculadoID));

                    gestorDoc.CargarDocumentos(false);

                    urlDoc = UrlIntragnoss + gestorDoc.ListaDocumentos[documentoEdicion.ElementoVinculadoID].Enlace;
                }

                EditarUrlCanonicaDeDocumento(documentoEdicion, parameters.canonical_url);

                string rutaFichero = null;

                if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                {
                    rutaFichero = parameters.title + ".rdf";

                    gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(documentoEdicion.ElementoVinculadoID));
                    gestorDoc.CargarDocumentos(false);
                }
                else if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.FicheroServidor || documentoEdicion.TipoDocumentacion == TiposDocumentacion.Imagen)
                {
                    rutaFichero = parameters.resource_url;

                    if (rutaFichero.Contains("/"))
                    {
                        rutaFichero = rutaFichero.Substring(rutaFichero.LastIndexOf("/") + 1);
                    }
                }
                else if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Video && !documentoEdicion.EsVideoIncrustado)
                {
                    //Añadimos el documento a la definición y a los dataset que corresponda
                    int indicepunto = parameters.resource_url.LastIndexOf(".");
                    string documentoSinExtension;

                    if (indicepunto != -1)
                    {
                        documentoSinExtension = parameters.resource_url.Substring(0, indicepunto);
                    }
                    else
                    {
                        documentoSinExtension = parameters.resource_url;
                    }

                    rutaFichero = documentoSinExtension + ".flv";
                }
                else if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Hipervinculo || documentoEdicion.TipoDocumentacion == TiposDocumentacion.ReferenciaADoc || documentoEdicion.EsVideoIncrustado)
                {
                    rutaFichero = parameters.resource_url;
                }

                if (rutaFichero != null)
                {
                    documentoEdicion.Enlace = rutaFichero;
                }

                try
                {
                    if ((documentoEdicion.TipoDocumentacion == TiposDocumentacion.Video || documentoEdicion.TipoDocumentacion == TiposDocumentacion.FicheroServidor || documentoEdicion.TipoDocumentacion == TiposDocumentacion.Imagen) && !documentoEdicion.EsVideoIncrustado)
                    {
                        AgregarArchivo(parameters.resource_file, (short)documentoEdicion.TipoDocumentacion, parameters.resource_url, documentoEdicion.Clave, FilaProy, usarReplicacion);
                    }
                    else if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                    {
                        ontologia = ObtenerOntologia(documentoEdicion.ElementoVinculadoID);

                        #region Obtengo RDF antiguo

                        nombreOntologia = gestorDoc.ListaDocumentos[ontologia.OntologiaID].FilaDocumento.Enlace;
                        GestionOWL gestorOWL = new GestionOWL();
                        gestorOWL.UrlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";
                        gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
                        GestionOWL.FicheroConfiguracionBD = "acid";
                        GestionOWL.URLIntragnoss = UrlIntragnoss;
                        try
                        {
                            RdfDS rdfAuxDS = ControladorDocumentacion.ObtenerRDFDeBDRDF(parameters.resource_id, FilaProy.ProyectoID);

                            if (rdfAuxDS.RdfDocumento.Count > 0)
                            {
                                rdfTexto = rdfAuxDS.RdfDocumento[0].RdfSem;
                            }
                            if (rdfAuxDS != null)
                            {
                                rdfAuxDS.Dispose();
                                rdfAuxDS = null;
                            }

                        }
                        catch { }

                        if (string.IsNullOrEmpty(rdfTexto))
                        {
                            MemoryStream buffer = new MemoryStream(ObtenerRDFDeVirtuosoControlCheckpoint(parameters.resource_id, nombreOntologia, gestorOWL.UrlOntologia, gestorOWL.NamespaceOntologia, ontologia, false));
                            StreamReader reader2 = new StreamReader(buffer);
                            rdfTexto = reader2.ReadToEnd();
                            reader2.Close();
                            reader2.Dispose();
                        }

                        entidadesPrincAntiguas = gestorOWL.LeerFicheroRDF(ontologia, rdfTexto, true);

                        #endregion

                        mEntityContext.NoConfirmarTransacciones = true;

                        string imgRepreDoc = AgregarArchivoSemantico(parameters.resource_file, documentoEdicion.ElementoVinculadoID, ontologia, documentoEdicion.Clave, gestorDoc, parameters.resource_attached_files, !parameters.create_version, parameters.priority, parameters.categories, parameters.main_image, usarReplicacion, out listaTriplesSemanticos, false, "", "", null, entidadesPrincAntiguas);

                        if (imgRepreDoc != null)
                        {
                            documentoEdicion.FilaDocumento.NombreCategoriaDoc = imgRepreDoc;
                        }

                        agregarCategoriasTesauro = !ontologia.ConfiguracionPlantilla.CategorizacionTesauroGnossNoObligatoria;

                        GuardarLogTiempos("Tras comprobar si categorias son obligatorias");
                    }

                    List<Guid> listaProyectosActualNumRec = new List<Guid>();
                    foreach (Guid baseRecurso in documentoEdicion.BaseRecursos)
                    {
                        Guid proyectoID = gestorDoc.ObtenerProyectoID(baseRecurso);
                        listaProyectosActualNumRec.Add(proyectoID);
                    }

                    #region Categorias del tesauro

                    if (parameters.categories != null && parameters.categories.Count > 0)
                    {
                        ReplaceCategories(new List<Guid>(parameters.categories), gestorDoc, documentoEdicion, identidad.Clave);
                    }

                    #endregion

                    documentoEdicion.FilaDocumento.FechaModificacion = DateTime.Now;

                    EstablecerPrivacidadRecurso(documentoEdicion, parameters.visibility, false);
                    ConfigurarLectores(documentoEdicion, parameters.readers_list, parameters.visibility);

                    if (parameters.editors_list != null && parameters.editors_list.Count > 0)
                    {
                        ConfigurarEditores(documentoEdicion, parameters.editors_list);
                    }

                    if (identidad.Clave.Equals(documentoEdicion.CreadorID) && !documentoEdicion.ListaPerfilesEditores.ContainsKey(identidad.PerfilID))
                    {
                        gestorDoc.AgregarEditorARecurso(documentoEdicion.Clave, identidad.PerfilID);
                    }
                    else
                    {
                        //cargar perfil del creador del documento
                        IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
                        Guid? perfilIdentidadCreador = identidadCN.ObtenerPerfilIDDeIdentidadID(documentoEdicion.CreadorID);
                        identidadCN.Dispose();
                        if (!documentoEdicion.ListaPerfilesEditores.ContainsKey(perfilIdentidadCreador.Value))
                        {
                            gestorDoc.AgregarEditorARecurso(documentoEdicion.Clave, perfilIdentidadCreador.Value);
                        }
                    }

                    Guardar(listaProyectosActualNumRec, gestorDoc, documentoEdicion);

                    if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                    {
                        mEntityContext.TerminarTransaccionesPendientes(true);
                    }
                }
                catch (Exception ex)
                {
                    if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Semantico)
                    {
                        mEntityContext.TerminarTransaccionesPendientes(false);
                        //hacer el rollback de virtuoso con el rdf viejo entidadesPrincAntiguas
                        if (parameters.create_version)
                        {
                            //Solo hay que eliminarlo de virtuoso si estamos versionando.
                            ControladorDocumentacion.BorrarRDFDeVirtuoso(documentoEdicion.Clave.ToString(), nombreOntologia, UrlIntragnoss, null, FilaProy.ProyectoID, usarReplicacion);
                            ControladorDocumentacion.BorrarRDFDeBDRDF(documentoEdicion.Clave);
                            //throw;
                        }
                        else //Borro de BD RDF para que vaya a virtuoso a por el la Web:
                        {
                            try
                            {
                                if (entidadesPrincAntiguas != null)
                                {
                                    //Guardar en Virtuoso lo que hay en la BD RDF (que es lo que había antes en Virtuoso, es decir, el rdf viejo)
                                    ControladorDocumentacion.GuardarRDFEnVirtuoso(entidadesPrincAntiguas, nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, parameters.resource_id.ToString(), false, "", false, usarReplicacion, (short)parameters.priority);
                                    ControladorDocumentacion.GuardarRDFEnBDRDF(rdfTexto, documentoEdicion.Clave, FilaProy.ProyectoID);
                                    //throw;
                                }
                            }
                            catch (Exception exc)
                            {
                                mLoggingService.GuardarLogError(exc, $"Error al revertir los cambios de virtuoso del recurso {parameters.resource_id}", mlogger);
                                //throw;
                            }
                        }

                        mLoggingService.GuardarLogError(ex, $"ModificarRecursoInt: Error al guardar modificaciones en BD Ácida. Se han revertido los cambios en Virtuoso y BD RDF del recurso {parameters.resource_id}", mlogger);
                    }
                    throw;
                }
            }
            finally
            {
                if (documentoBloqueado)
                {
                    docCN.FinalizarEdicionRecurso(parameters.resource_id);
                }
            }

            //se eliminan las imagenes viejas del recurso
            List<string> listaIDsImagenes = ExtractImageIds(parameters.resource_attached_files);

            if (listaIDsImagenes.Count > 0)
            {
                EliminarArchivosDelRDFExceptoLista(parameters.resource_id, listaIDsImagenes);
            }

            #region Actualizamos la cache

            try
            {
                //Borrar la caché de la ficha del recurso
                DocumentacionCL documentacionCL = new DocumentacionCL("acid", "recursos", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                documentacionCL.BorrarControlFichaRecursos(parameters.resource_id);
                documentacionCL.Dispose();

                if (parameters.readers_list != null || parameters.editors_list != null)
                {
                    DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                    docCL.InvalidarPerfilesConRecursosPrivados(FilaProy.ProyectoID);
                    docCL.Dispose();
                }
            }
            catch (Exception) { }

            #endregion

            #region Actualizar Grafo de Búsqueda y cola GnossLIVE

            if (AgregarColaBase && (documentoEdicion.FilaDocumento.Tipo != (short)TiposDocumentacion.Semantico || !usarReplicacion))
            {
                ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                controDoc.mActualizarTodosProyectosCompartido = true;
                controDoc.NotificarModificarTagsRecurso(documentoEdicion, Proyecto, listaTriplesSemanticos, ontologia, gestorDoc.GestorTesauro, parameters.create_version, documentoAntiguo, PrioridadBase.ApiRecursos, mAvailableServices);
            }

            foreach (Guid baseRecurso in documentoEdicion.BaseRecursos)
            {
                Guid proyecto = gestorDoc.ObtenerProyectoID(baseRecurso);

                int tipo;
                switch (documentoEdicion.TipoDocumentacion)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                if (AgregarColaLive || parameters.publish_home)
                {
                    if (parameters.create_version)
                    {
                        ControladorDocumentacion.ActualizarGnossLive(proyecto, documentoAntiguo.Clave, AccionLive.Eliminado, tipo, "base", PrioridadLive.Baja, mAvailableServices);
                        ControladorDocumentacion.ActualizarGnossLive(proyecto, documentoEdicion.Clave, AccionLive.Agregado, tipo, "base", PrioridadLive.Baja, mAvailableServices);
                    }
                    else
                    {
                        ControladorDocumentacion.ActualizarGnossLive(proyecto, documentoEdicion.Clave, AccionLive.Editado, tipo, false, "base", PrioridadLive.Baja, Constantes.PRIVACIDAD_CAMBIADA, mAvailableServices);
                    }
                }
            }

            #endregion

            //ControladorDocumentacion.EstablecePrivacidadRecursoEnMetaBuscadorDesdeServicio(documentoEdicion, identidad, true, "acid");

            if (documentoEdicion.TipoDocumentacion == TiposDocumentacion.Hipervinculo || documentoEdicion.TipoDocumentacion == TiposDocumentacion.Imagen || documentoEdicion.TipoDocumentacion == TiposDocumentacion.Nota || documentoEdicion.TipoDocumentacion == TiposDocumentacion.Video || documentoEdicion.EsPresentacionIncrustada || documentoEdicion.EsVideoIncrustado)
            {
                ControladorDocumentacion.CapturarImagenWeb(documentoEdicion.Clave, false, PrioridadColaDocumento.Baja, mAvailableServices);
            }

            ControladorDocumentacion.InsertarEnColaProcesarFicherosRecursosModificadosOEliminados(documentoEdicion.Clave, TipoEventoProcesarFicherosRecursos.Modificado, mAvailableServices);

            docCN.Dispose();

            return true;
        }

        private void EditarUrlCanonicaDeDocumento(Elementos.Documentacion.Documento pDocumento, string pUrlCanonica)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            string urlCanonica = docCN.ObtenerDocumentoUrlCanonica(pDocumento.Clave);
            AD.EntityModel.Models.Documentacion.DocumentoUrlCanonica filaCanonica = null;

            if (!string.IsNullOrEmpty(urlCanonica))
            {
                // El documento tenía configurada una url canonica
                filaCanonica = new AD.EntityModel.Models.Documentacion.DocumentoUrlCanonica { DocumentoID = pDocumento.Clave, UrlCanonica = urlCanonica };
                pDocumento.GestorDocumental.DataWrapperDocumentacion.ListaDocumentoUrlCanonica.Add(filaCanonica);
            }

            if (!string.IsNullOrEmpty(pUrlCanonica))
            {
                if (filaCanonica == null)
                {
                    pDocumento.GestorDocumental.DataWrapperDocumentacion.ListaDocumentoUrlCanonica.Add(new AD.EntityModel.Models.Documentacion.DocumentoUrlCanonica { DocumentoID = pDocumento.Clave, UrlCanonica = pUrlCanonica });
                }
                else if (!filaCanonica.UrlCanonica.Equals(pUrlCanonica))
                {
                    filaCanonica.UrlCanonica = pUrlCanonica;
                }
            }
            else if (filaCanonica != null)
            {
                mEntityContext.DocumentoUrlCanonica.Remove(filaCanonica);
                pDocumento.GestorDocumental.DataWrapperDocumentacion.ListaDocumentoUrlCanonica.Remove(filaCanonica);
            }
        }

        private void AgregarIdentidadesEditorasRecurso(Elementos.Documentacion.Documento documentoAntiguo, GestorDocumental gestorDoc, Identidad identidad)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            List<Guid> listaIdentidadesURLSem = new List<Guid>();

            foreach (AD.EntityModel.Models.Documentacion.DocumentoWebVinBaseRecursos filaDocVinBR in gestorDoc.DataWrapperDocumentacion.ListaDocumentoWebVinBaseRecursos)
            {
                if (filaDocVinBR.IdentidadPublicacionID.HasValue && !listaIdentidadesURLSem.Contains(filaDocVinBR.IdentidadPublicacionID.Value))
                {
                    listaIdentidadesURLSem.Add(filaDocVinBR.IdentidadPublicacionID.Value);
                }
            }

            if (!listaIdentidadesURLSem.Contains(identidad.Clave))
            {
                listaIdentidadesURLSem.Add(identidad.Clave);
            }

            gestorDoc.DataWrapperDocumentacion.Merge(docCN.ObtenerEditoresDocumento(documentoAntiguo.Clave));

            List<Guid> listaPerfilesURlSem = new List<Guid>();

            //Cargo las identidades de los editores
            foreach (EditorRecurso editor in documentoAntiguo.ListaPerfilesEditores.Values)
            {
                if (!listaPerfilesURlSem.Contains(editor.FilaEditor.PerfilID))
                {
                    listaPerfilesURlSem.Add(editor.FilaEditor.PerfilID);
                }
            }
            if (listaIdentidadesURLSem.Count > 0 || listaPerfilesURlSem.Count > 0)
            {
                IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
                DataWrapperIdentidad dataWrapperIdentidad = new DataWrapperIdentidad();

                if (listaIdentidadesURLSem.Count > 0)
                {
                    dataWrapperIdentidad.Merge(identidadCN.ObtenerIdentidadesPorID(listaIdentidadesURLSem, false));
                }

                if (listaPerfilesURlSem.Count > 0)
                {
                    dataWrapperIdentidad.Merge(identidadCN.ObtenerIdentidadesDePerfiles(listaPerfilesURlSem));
                }

                GestionIdentidades gestorIdent = new GestionIdentidades(dataWrapperIdentidad, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                identidadCN.Dispose();

                if (gestorDoc.GestorIdentidades == null)
                {
                    gestorDoc.GestorIdentidades = gestorIdent;
                }
                else
                {
                    gestorDoc.GestorIdentidades.DataWrapperIdentidad.Merge(gestorIdent.DataWrapperIdentidad);
                    gestorDoc.GestorIdentidades.RecargarHijos();
                }
            }

            //Cargo los grupos de los editores
            List<Guid> listaGrupos = new List<Guid>();
            foreach (Elementos.Documentacion.GrupoEditorRecurso grupoEditor in documentoAntiguo.ListaGruposEditores.Values)
            {
                if (!listaGrupos.Contains(grupoEditor.Clave))
                {
                    listaGrupos.Add(grupoEditor.Clave);
                }
            }
            if (listaGrupos.Count > 0)
            {
                IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<IdentidadCN>(), mLoggerFactory);
                DataWrapperIdentidad identDW = identidadCN.ObtenerGruposPorIDGrupo(listaGrupos, false);

                GestionIdentidades gestorIdent = new GestionIdentidades(identDW, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                identidadCN.Dispose();

                if (gestorDoc.GestorIdentidades == null)
                {
                    gestorDoc.GestorIdentidades = gestorIdent;
                }
                else
                {
                    gestorDoc.GestorIdentidades.DataWrapperIdentidad.Merge(gestorIdent.DataWrapperIdentidad);
                    gestorDoc.GestorIdentidades.RecargarHijos();
                }
            }
        }

        private void ValidarDatosRecurso(Dictionary<string, object> pDicParametros)
        {
            foreach (string parametro in pDicParametros.Keys)
            {
                switch (parametro.ToLower())
                {
                    case "etiquetas":

                        List<string> etiquetas = (List<string>)pDicParametros[parametro];

                        if (etiquetas != null && etiquetas.Count > 0)
                        {
                            foreach (string etiqueta in etiquetas)
                            {
                                if (string.IsNullOrEmpty(etiqueta))
                                {
                                    throw new GnossException("The tags list contains, at least, a null tag.", HttpStatusCode.BadRequest);
                                }
                            }
                        }

                        break;

                    case "categorias":

                        List<object> lista = (List<object>)pDicParametros[parametro];
                        List<Guid> categoriasRecurso = (List<Guid>)lista[0];
                        List<Guid> categoriasTesauro = (List<Guid>)lista[1];

                        if (categoriasRecurso == null || categoriasRecurso.Count == 0)
                        {
                            throw new GnossException("The resource is not categorized.", HttpStatusCode.BadRequest);
                        }
                        else if (categoriasTesauro == null || categoriasTesauro.Count == 0)
                        {
                            throw new GnossException("No loaded thesaurus categories", HttpStatusCode.BadRequest);
                        }
                        else
                        {
                            foreach (Guid catID in categoriasRecurso)
                            {
                                if (catID.Equals(Guid.Empty))
                                {
                                    throw new GnossException("The categories list has some null or empty category.", HttpStatusCode.BadRequest);
                                }
                                else
                                {
                                    if (!categoriasTesauro.Contains(catID))
                                    {
                                        throw new GnossException("The category: " + catID + " of the resource does not belong to the community thesaurus", HttpStatusCode.BadRequest);
                                    }
                                }
                            }
                        }

                        break;

                    case "categoriassem":

                        List<object> listaParam = (List<object>)pDicParametros[parametro];
                        List<Guid> categoriasSem = (List<Guid>)listaParam[0];
                        bool obligatorias = (bool)listaParam[1];
                        List<Guid> listaCatTesauro = (List<Guid>)listaParam[2];

                        if (categoriasSem == null || categoriasSem.Count == 0)
                        {
                            if (obligatorias)
                            {
                                throw new GnossException("The resource is not categorized and the community has mandatory categorization.", HttpStatusCode.BadRequest);
                            }
                        }
                        else if (listaCatTesauro == null || listaCatTesauro.Count == 0)
                        {
                            throw new GnossException("No loaded thesaurus categories", HttpStatusCode.BadRequest);
                        }
                        else
                        {
                            foreach (Guid catID in categoriasSem)
                            {
                                if (catID.Equals(Guid.Empty))
                                {
                                    throw new GnossException("The categories list has some null or empty category.", HttpStatusCode.BadRequest);
                                }
                                else
                                {
                                    if (!listaCatTesauro.Contains(catID))
                                    {
                                        throw new GnossException("The category: " + catID + " of the resource does not belong to the community thesaurus", HttpStatusCode.BadRequest);
                                    }
                                }
                            }
                        }

                        break;

                    case "rdf":

                        List<object> listaRDF = (List<object>)pDicParametros[parametro];
                        byte[] bytesRDF = (byte[])listaRDF[0];
                        List<ElementoOntologia> instanciasPrincipales = (List<ElementoOntologia>)listaRDF[1];

                        if (instanciasPrincipales == null || instanciasPrincipales.Count == 0 || bytesRDF.Length == 0)
                        {
                            throw new GnossException("The resource does not have RDF or it is empty.", HttpStatusCode.BadRequest);
                        }
                        else if (instanciasPrincipales.Count > 1)
                        {
                            string mensaje = "The RDF has more than one main entity: ";
                            string coma = string.Empty;

                            foreach (ElementoOntologia eo in instanciasPrincipales)
                            {
                                mensaje += coma + eo.ID;
                                coma = ", ";
                            }

                            throw new GnossException(mensaje, HttpStatusCode.BadRequest);
                        }                        

                        break;
                }
            }
        }               

        private void ComprobacionCambiosCachesLocales(Guid pProyectoID)
        {
            UtilServicios.ComprobacionCambiosCachesLocales(pProyectoID);
        }

        /// <summary>
        /// Obtiene las etiquetas automáticas de un recurso a partir del título y descripción.
        /// </summary>
        /// <param name="pTitulo">Título</param>
        /// <param name="pDescripcion">Descripción</param>
        /// <param name="pProyectoID">ID de proyecto</param>
        /// <returns>Lista con las etiquetas</returns>
        private List<string> ObtenerEtiquetasAutomaticas(string pTitulo, string pDescripcion, Guid pProyectoID)
        {
            try
            {
                //TODO Javier Migrar a peticion rest
                /*EtiquetadoAutomatico servicioEtiquetas = new EtiquetadoAutomatico();

                string urlEtiquetado = null; //TODO Javier Conexion.ObtenerUrlServicioEtiquetadoAutomatico(pProyectoID, null);

                if (Uri.IsWellFormedUriString(urlEtiquetado, UriKind.Absolute))
                {
                    servicioEtiquetas.Url = urlEtiquetado + "/EtiquetadoAutomatico.asmx";

                    string etiquetas = servicioEtiquetas.SeleccionarEtiquetasDesdeServicio(pTitulo, pDescripcion, pProyectoID.ToString());
                    string[] etiquetasA = (etiquetas).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    return new List<string>(etiquetasA);
                }
                */
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, " Error en etiquetado automático", mlogger);
            }
            return new List<string>();
        }


        private bool ComprobarEsVideoIncrustado(string Enlace)
        {
            if (!string.IsNullOrEmpty(Enlace))
            {
                if ((Enlace.StartsWith("http://www.youtube.com") || Enlace.StartsWith("http://youtube.com") || Enlace.StartsWith("www.youtube.com")) && Enlace.Contains("/watch?") || Enlace.StartsWith("https://www.youtube.com") || Enlace.StartsWith("youtu.be/") || Enlace.StartsWith("http://youtu.be/"))
                {
                    string v = "";
                    if (Enlace.StartsWith("youtu.be/") || Enlace.StartsWith("http://youtu.be/"))
                    {
                        v = Enlace.Replace("http://youtu.be/", "").Replace("youtu.be/", "");

                        if (v.Contains("/"))
                        {
                            v = v.Substring(0, v.IndexOf("/"));
                        }
                    }
                    else
                    {
                        v = System.Web.HttpUtility.ParseQueryString(new Uri(Enlace).Query).Get("v");
                    }

                    return (!string.IsNullOrEmpty(v));
                }
                else if (Enlace.StartsWith("http://www.vimeo.com") || Enlace.StartsWith("http://vimeo.com") || Enlace.StartsWith("www.vimeo.com") || Enlace.StartsWith("https://www.vimeo.com"))
                {
                    string v = (new Uri(Enlace)).AbsolutePath;
                    int idVideo;
                    int inicio = v.LastIndexOf("/");
                    return (int.TryParse(v.Substring(inicio + 1, v.Length - inicio - 1), out idVideo));
                }
                else if (Enlace.StartsWith("http://www.ted.com/talks/") || Enlace.StartsWith("www.ted.com/talks/") || Enlace.StartsWith("ted.com/talks/") || Enlace.StartsWith("http://tedxtalks.ted.com/video/") || Enlace.StartsWith("tedxtalks.ted.com/video/"))
                {
                    return true;
                }
            }

            return false;
        }

        private string AgregarArchivo(byte[] pFichero, short pTipoDoc, string pNombreFichero, Guid pDocumentoID, Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS.Proyecto pFilaProyecto, bool pUsarColareplicacion)
        {
            string nomFicheroCargado = "";
            short accion = -1;

            if (pFichero != null)
            {
                TiposDocumentacion tipoDocumentoGnoss = (TiposDocumentacion)pTipoDoc;

                byte[] buffer1 = pFichero;

                int resultado = 0;

                FileInfo archivoInfo = new FileInfo(pNombreFichero);
                string extensionArchivo = System.IO.Path.GetExtension(archivoInfo.Name).ToLower();

                if (tipoDocumentoGnoss == TiposDocumentacion.Video)
                {
                    accion = 0;

                    //Subimos el fichero al servidor
                    ServicioVideos servicioVideos = new ServicioVideos(mConfigService, mLoggingService, mLoggerFactory.CreateLogger<ServicioVideos>(), mLoggerFactory);
                    resultado = servicioVideos.AgregarVideo(buffer1, extensionArchivo, pDocumentoID);
                }
                else if (tipoDocumentoGnoss == TiposDocumentacion.Imagen)
                {
                    accion = 0;
                    extensionArchivo = pNombreFichero.Substring(pNombreFichero.LastIndexOf("."));

                    ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                    servicioImagenes.Url = UrlServicioImagenes;
                    bool correcto = servicioImagenes.AgregarImagenADirectorio(buffer1, UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.DirectorioDocumento(pDocumentoID), pDocumentoID.ToString(), extensionArchivo);

                    if (correcto)
                    {
                        GestionDocumental gd = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                        gd.Url = UrlServicioWebDocumentacion;
                        //gd.Timeout = 600000;
                        mLoggingService.AgregarEntrada("Adjunto archivo al gestor documental");
                        string idAuxGestorDocumental = gd.AdjuntarDocumento(buffer1, TipoEntidadVinculadaDocumentoTexto.BASE_RECURSOS, pFilaProyecto.OrganizacionID, pFilaProyecto.ProyectoID, pDocumentoID, extensionArchivo);
                        if (!idAuxGestorDocumental.Equals("Error"))
                        {
                            resultado = 1;
                        }

                        mLoggingService.AgregarEntrada("Adjuntado");
                    }
                }
                else
                {
                    accion = 0;

                    if (TieneGoogleDriveConfigurado)
                    {
                        try
                        {
                            string especialID = pNombreFichero;
                            //TODO Javier migrar (redes sociales)
                            //OAuthGoogleDrive gd = new OAuthGoogleDrive();
                            string googleID = null; //gd.SubirDocumento(especialID, extensionArchivo, buffer1);

                            //Nombre del fichero con ID de google.
                            nomFicheroCargado = especialID.Substring(0, especialID.LastIndexOf('.')) + ID_GOOGLE + googleID + extensionArchivo;

                            //Se ha subido correctamente a googledrive.
                            resultado = 1;
                        }
                        catch (Exception ex)
                        {
                            resultado = 0;
                        }
                    }
                    else
                    {
                        //Subimos el fichero al servidor
                        GestionDocumental gd = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                        gd.Url = UrlServicioWebDocumentacion;

                        TipoEntidadVinculadaDocumento tipoEntidaVinDoc = TipoEntidadVinculadaDocumento.Web;
                        string tipoEntidadTexto = ControladorDocumentacion.ObtenerTipoEntidadAdjuntarDocumento(tipoEntidaVinDoc);
                        string idAuxGestorDocumental = gd.AdjuntarDocumento(buffer1, tipoEntidadTexto, pFilaProyecto.OrganizacionID, pFilaProyecto.ProyectoID, pDocumentoID, extensionArchivo);
                        if (!idAuxGestorDocumental.Equals("Error"))
                        {
                            resultado = 1;
                        }
                    }
                }

                if (resultado != 1)
                {
                    throw new GnossException("File uploading failed. GoogleDrive=" + TieneGoogleDriveConfigurado, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new Exception("There is no file to upload. GoogleDrive=" + TieneGoogleDriveConfigurado);
            }
            return nomFicheroCargado;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pFichero"></param>
        /// <param name="pOntologiaID"></param>
        /// <param name="pDocumentoID"></param>
        /// <param name="pGestorDocumental"></param>
        /// <param name="pPropiedadesRdfArchivo"></param>
        /// <param name="pTipoPropiedadArchivo"></param>
        /// <param name="pArchivosAdjuntosRdf"></param>
        /// <param name="pEliminarRdfViejo"></param>
        /// <param name="pPrioridad"></param>
        /// <param name="pCategoriaIDs"></param>
        /// <param name="pImgPrincipal">Cadena con la imagen principal</param>
        /// <returns></returns>
        private string AgregarArchivoSemantico(byte[] pFichero, Guid pOntologiaID, Ontologia pOntologia, Guid pDocumentoID, GestorDocumental pGestorDocumental, List<AttachedResource> pArchivosAdjuntos, bool pEliminarRdfViejo, int pPrioridad, List<Guid> pCategoriaIDs, string pImgPrincipal, bool pUsarColareplicacion, out List<TripleWrapper> pListaTriplesSemanticos, bool pCrearCaptura, string pUrlCaptura, string pPredCaptura, int[] pSize, List<ElementoOntologia> pEntidadesPrincAntiguas, StringBuilder pSbVirtuoso = null)
        {
            string imagenRepresentanteDoc = null;
            string nombreOntologia = pGestorDocumental.ListaDocumentos[pOntologiaID].FilaDocumento.Enlace;
            GestionOWL gestorOWL = new GestionOWL();
            gestorOWL.UrlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";
            gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
            GestionOWL.FicheroConfiguracionBD = "acid";
            GestionOWL.URLIntragnoss = UrlIntragnoss;

            if (pOntologia == null)
            {
                //Obtengo la ontología y su archivo de configuración:
                pOntologia = ObtenerOntologia(pOntologiaID);
            }

            if (pOntologia != null)
            {
                try
                {
                    StreamReader reader = new StreamReader(new MemoryStream(pFichero));
                    string lineaRDF = reader.ReadToEnd();
                    reader.Close();
                    reader.Dispose();
                    reader = null;

                    mInstanciasPrincipales = gestorOWL.LeerFicheroRDF(pOntologia, lineaRDF, true, true);
                    GuardarLogTiempos(Environment.NewLine + "Leido RDF");
                }
                catch (Exception ex)
                {
                    throw new Exception("El RDF del recurso no es correcto. " + ex.Message);
                }
                //Comprobar los Idiomas.
                List<string> idiomasDisponible = new List<string>();
                foreach (ElementoOntologia elementoOntologia in mInstanciasPrincipales)
                {
                    ModificarInstanciasIdioma(elementoOntologia, idiomasDisponible);
                }
                EstablecerIdiomasRecurso(pOntologia, mInstanciasPrincipales, idiomasDisponible);

                //validar categorias(si no es semantico validar las categorias en el metodo anterior en la pila) y rdf
                //lista que almacena las categorias y si en la comunidad no es obligatorio categorizar sobre el tesauro de GNOSS
                List<object> listaParametrosCategorias = new List<object>();
                listaParametrosCategorias.Add(pCategoriaIDs);
                if (pOntologia.ConfiguracionPlantilla == null)
                {
                    listaParametrosCategorias.Add(false);
                }
                else
                {
                    listaParametrosCategorias.Add(!pOntologia.ConfiguracionPlantilla.CategorizacionTesauroGnossNoObligatoria);
                }
                listaParametrosCategorias.Add(new List<Guid>(pGestorDocumental.GestorTesauro.ListaCategoriasTesauro.Keys));
                Dictionary<string, object> dicParametros = new Dictionary<string, object>();
                dicParametros.Add("categoriassem", listaParametrosCategorias);
                //lista que almacena el archivo rdf y las instancias principalestras leer el rdf de la ontologia
                List<object> listaParametrosRDF = new List<object>();
                listaParametrosRDF.Add(pFichero);
                listaParametrosRDF.Add(mInstanciasPrincipales);
                dicParametros.Add("rdf", listaParametrosRDF);

                ValidarDatosRecurso(dicParametros);

                if (!mTieneDocumentoID)
                {
                    CambiarIDsElementoOngologia(mInstanciasPrincipales, pDocumentoID);
                }

                if (pEliminarRdfViejo)
                {
                    List<string> entidadesYaAgregadas = new List<string>();
                    Dictionary<string, string> cambioIDs = new Dictionary<string, string>();

                    #region Obtengo RDF antiguo

                    if (pEntidadesPrincAntiguas == null || pEntidadesPrincAntiguas.Count == 0)
                    {
                        RdfDS rdfAuxDS = ControladorDocumentacion.ObtenerRDFDeBDRDF(pDocumentoID, FilaProy.ProyectoID);
                        string rdfTexto = null;

                        if (rdfAuxDS.RdfDocumento.Count > 0)
                        {
                            rdfTexto = rdfAuxDS.RdfDocumento[0].RdfSem;
                        }

                        if (string.IsNullOrEmpty(rdfTexto))
                        {
                            MemoryStream buffer = new MemoryStream(ObtenerRDFDeVirtuosoControlCheckpoint(pDocumentoID, pGestorDocumental.ListaDocumentos[pOntologiaID].Enlace, gestorOWL.UrlOntologia, gestorOWL.NamespaceOntologia, pOntologia, false));
                            StreamReader reader2 = new StreamReader(buffer);
                            rdfTexto = reader2.ReadToEnd();
                            reader2.Close();
                            reader2.Dispose();
                        }

                        rdfAuxDS.Dispose();
                        rdfAuxDS = null;

                        pEntidadesPrincAntiguas = gestorOWL.LeerFicheroRDF(pOntologia, rdfTexto, true);
                    }

                    #endregion

                    RecuperarIDsElementoOngologia(mInstanciasPrincipales, null, entidadesYaAgregadas, cambioIDs, pEntidadesPrincAntiguas);

                    CambiarSegundosIDsElementoOngologia(mInstanciasPrincipales, cambioIDs);
                }

                mIDEntidadPrincipal = UrlIntragnoss + "items/" + mInstanciasPrincipales[0].ID;

                //Subo archivos del RDF:
                imagenRepresentanteDoc = SubirArchivosDelRDF(pDocumentoID, pArchivosAdjuntos, mInstanciasPrincipales, pImgPrincipal);

                if (pCrearCaptura)
                {
                    //Debemos modificar el documento RDf y insertar un nivel nuevo junto con su imagen.
                    string rutaRec = UtilArchivos.ContentImagenes + "/" + UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.ContentImagenesSemanticas + "/" + UtilArchivos.DirectorioDocumento(pDocumentoID) + "/" + Guid.NewGuid() + ".jpg";

                    #region Insertamos fila en la cola

                    // Preparamos la fila que hay que añadir a ColaDocumento
                    string enteros = "";
                    if (pSize != null)
                    {
                        foreach (int entero in pSize)
                        {
                            enteros += ", " + entero;
                        }

                        if (enteros.StartsWith(", "))
                        {
                            enteros = enteros.Substring(2);
                        }
                    }

                    string infoExtra = pUrlCaptura + "|" + rutaRec + "|" + enteros;
                    DataWrapperDocumentacion documentoColaDW = new DataWrapperDocumentacion();
                    AD.EntityModel.Models.Documentacion.ColaDocumento colaDocumentoRow = new AD.EntityModel.Models.Documentacion.ColaDocumento();
                    colaDocumentoRow.DocumentoID = pDocumentoID;
                    colaDocumentoRow.AccionRealizada = Convert.ToInt16(0); //Agregar
                    colaDocumentoRow.Estado = Convert.ToInt16(0); // Espera
                    colaDocumentoRow.FechaEncolado = DateTime.Now;
                    colaDocumentoRow.Prioridad = (short)pPrioridad;
                    colaDocumentoRow.InfoExtra = infoExtra;
                    documentoColaDW.ListaColaDocumento.Add(colaDocumentoRow);
                    mEntityContext.ColaDocumento.Add(colaDocumentoRow);

                    DocumentacionCN docCN = new DocumentacionCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                    docCN.ActualizarDocumentacion();
                    docCN.Dispose();

                    GuardarLogTiempos("Tras agregar fila a coladocumentoRow");

                    #endregion

                    mEntidadesABorrar = new List<string>();
                    mURIEntidadesABorrar = new Dictionary<string, string>();
                    //El valor a modificar se encuentra en una propiedad hija
                    //Método recursivo
                    foreach (ElementoOntologia eo in mInstanciasPrincipales)
                    {
                        ModificarInstanciaPorNivel(pOntologia, eo.Propiedades, pDocumentoID, pPredCaptura, "", rutaRec, true, new GnossStringBuilder(), new GnossStringBuilder(), new GnossStringBuilder(), new GnossStringBuilder());
                    }

                    GuardarLogTiempos("Tras modificar por nivel");
                }

                //Guardo el RDF:
                //string nombreTemporal = Path.GetRandomFileName() + ".rdf";
                //string rutaGuardar = Path.GetTempPath() + nombreTemporal;

                Stream stream = gestorOWL.PasarOWL(null, pOntologia, mInstanciasPrincipales, null, null);
                stream.Position = 0; //al escribir el stream se queda en la última posición
                string ficheroRDF = new StreamReader(stream).ReadToEnd();

                RdfDS rdfDS = null;

                //Solo API Modificación.
                if (pEliminarRdfViejo)
                {
                    //El RDF ya existia por lo que hay que remplazarlo:
                    //Borrado de triples a través de la replicación.
                    //10/03/2017 ahora se hace un MODIFY delete insert en GuardarRDFEnVirtuoso
                    //ControladorDocumentacion.BorrarRDFDeVirtuoso(pDocumentoID.ToString(), nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, pUsarColareplicacion);

                    //Obtención RDF modelo ACID.
                    rdfDS = ControladorDocumentacion.ObtenerRDFDeBDRDF(pDocumentoID, FilaProy.ProyectoID);

                    if (rdfDS.RdfDocumento.Count == 0)
                    {
                        //Si no hay un RDF, le damos un valor nulo para que inserte al final el nuevo.
                        rdfDS.Dispose();
                        rdfDS = null;
                    }
                }

                #region SemWeb

                string infoExtra_Replicacion = "";
                //ControladorDocumentacion.AgregarModificacionRecursoModeloBase(docWeb.Clave, docWeb.FilaDocumento.Tipo, proyectoID, PrioridadBase.Alta);
                infoExtra_Replicacion += "" + ObtenerInfoExtraBaseDocumentoAgregar(pDocumentoID, (short)TiposDocumentacion.Semantico, FilaProy.ProyectoID, (short)pPrioridad);
                infoExtra_Replicacion += "|;|%|;|";

                //Insercción tripletas virtuoso a través del servicio de replicación.
                pListaTriplesSemanticos = ControladorDocumentacion.GuardarRDFEnVirtuoso(mInstanciasPrincipales, nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, pDocumentoID.ToString(), false, infoExtra_Replicacion, false, pUsarColareplicacion, (short)pPrioridad);


                //        string triple1 = "<http://gnoss/asdas> <http://ign.gnoss.com/ign.owl#name> \"test\"@es .";
                //        string triple2 = "<http://gnoss/asdas> <http://ign.gnoss.com/ign.owl#name> \"test\" .";
                //        string triple3 = "<http://gnoss/asdas> <http://ign.gnoss.com/ign.owl#id> <http://gnoss/asdas> .";

                //if (pSbVirtuoso != null)
                {
                    try
                    {
                        //Guardado de los triples en el modelo ACID.
                        ControladorDocumentacion.GuardarRDFEnBDRDF(ficheroRDF, pDocumentoID, FilaProy.ProyectoID, rdfDS);
                    }
                    catch (Exception)
                    {
                        if (!pEliminarRdfViejo) //Solo hay que eliminarlo de virtuoso si no estamos editando.
                        {
                            ControladorDocumentacion.BorrarRDFDeVirtuoso(pDocumentoID.ToString(), nombreOntologia, UrlIntragnoss, null, FilaProy.ProyectoID, pUsarColareplicacion);
                            throw;
                        }
                        else //Borro de BD RDF para que vaya a virtuoso a por el la Web:
                        {
                            try
                            {
                                if (pEntidadesPrincAntiguas != null)
                                {
                                    //Guardar en Virtuoso lo que hay en la BD RDF (que es lo que había antes en Virtuoso, es decir, el rdf viejo)
                                    ControladorDocumentacion.GuardarRDFEnVirtuoso(pEntidadesPrincAntiguas, nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, pDocumentoID.ToString(), false, "", false, pUsarColareplicacion, (short)pPrioridad);
                                    throw;
                                }
                            }
                            catch (Exception ex)
                            {
                                mLoggingService.GuardarLogError(ex, $"Error al revertir los cambios de virtuoso del recurso {pDocumentoID}", mlogger);
                                throw;
                            }
                        }
                    }
                }

                //Borramos el fichero temporal:
                //FileInfo fichRDF = new FileInfo(rutaGuardar);
                //fichRDF.Delete();
                stream = null;
                ficheroRDF = null;

                #endregion

                #region Imagen por defecto de la ontología
                //La ontología tiene icono
                if (imagenRepresentanteDoc == null && pGestorDocumental.ListaDocumentos[pOntologiaID].FilaDocumento.NombreCategoriaDoc != null)
                {
                    imagenRepresentanteDoc = pGestorDocumental.ListaDocumentos[pOntologiaID].FilaDocumento.NombreCategoriaDoc;
                }

                #endregion

                return imagenRepresentanteDoc;
            }
            else
            {
                throw new GnossException("It could not obtain the ontology.", HttpStatusCode.BadRequest);
            }
        }

        private void EstablecerIdiomasRecurso(Ontologia pOntologia, List<ElementoOntologia> mInstanciasPrincipales, List<string> idiomasDisponible)
        {
            if (!string.IsNullOrEmpty(PropiedadIdiomaBusquedaComunidad(pOntologia)))
            {
                Propiedad propIdio = EstiloPlantilla.ObtenerPropiedadACualquierNivelPorNombre(PropiedadIdiomaBusquedaComunidad(pOntologia), null, mInstanciasPrincipales);

                if (propIdio != null)
                {
                    propIdio.LimpiarValor();

                    if (idiomasDisponible.Count > 0)
                    {
                        foreach (string idioma in idiomasDisponible)
                        {
                            propIdio.DarValor(idioma, null);
                        }
                    }
                    else if (!string.IsNullOrEmpty(FilaParametroGeneral.IdiomaDefecto))
                    {
                        propIdio.DarValor(FilaParametroGeneral.IdiomaDefecto, null);
                    }
                    else
                    {
                        propIdio.DarValor(pOntologia.IdiomaUsuario, null);
                    }
                }
            }
        }
        [NonAction]
        private void ModificarInstanciasIdioma(ElementoOntologia pEntidadOntologia, List<string> pIdiomasDisponibles)
        {
            foreach (Propiedad prop in pEntidadOntologia.Propiedades)
            {
                if (prop.Tipo.Equals(TipoPropiedad.DatatypeProperty))
                {
                    foreach (string idioma in prop.ListaValoresIdioma.Keys)
                    {
                        if (prop.ListaValoresIdioma[idioma].Count > 0)
                        {
                            if (!pIdiomasDisponibles.Contains(idioma))
                            {
                                pIdiomasDisponibles.Add(idioma);
                            }
                        }
                    }
                }
                else
                {
                    foreach (string keysListaValores in prop.ListaValores.Keys)
                    {
                        if (prop.ListaValores[keysListaValores] != null && !prop.TieneSelectorEntidad)
                        {
                            ModificarInstanciasIdioma(prop.ListaValores[keysListaValores], pIdiomasDisponibles);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Url de la propiedad que debe contener al menos un idioma para realizar la busqueda por el mismo.
        /// </summary>
        [NonAction]
        public string PropiedadIdiomaBusquedaComunidad(Ontologia pOntologia)
        {
            string propiedadIdioma = "";
            if (string.IsNullOrEmpty(mPropiedadIdiomaBusquedaComunidad))
            {
                if (ParametroProyecto.ContainsKey(ParametroAD.PropiedadContenidoMultiIdioma))
                {
                    propiedadIdioma = ParametroProyecto[Es.Riam.Gnoss.AD.Parametro.ParametroAD.PropiedadContenidoMultiIdioma];
                    if (propiedadIdioma.StartsWith("dce:"))
                    {
                        propiedadIdioma = propiedadIdioma.Replace("dce:", "dc:");
                    }

                    if (propiedadIdioma.Contains(":") && pOntologia.NamespacesDefinidosInv.ContainsKey(propiedadIdioma.Split(':')[0]))
                    {
                        propiedadIdioma = pOntologia.NamespacesDefinidosInv[propiedadIdioma.Split(':')[0]] + propiedadIdioma.Substring(propiedadIdioma.IndexOf(":") + 1);
                    }
                    mPropiedadIdiomaBusquedaComunidad = propiedadIdioma;
                }
            }
            return mPropiedadIdiomaBusquedaComunidad;
        }

        /// <summary>
        /// Set or fomat a string value type into a date type one.
        /// </summary>
        /// <param name="pFecha">Value of the date to be formatted</param>
        [NonAction]
        public string FormateDate(DateTime? pFecha)
        {
            string fecha = "NULL";
            if (pFecha.HasValue)
            {
                fecha = $"'{pFecha.Value.ToString("yyyy/MM/dd HH:mm:ss.fff")}'";
            }
            return fecha;
        }

        /// <summary>
        /// Set, format or normalized a guid value provided into a valid one.
        /// </summary>
        /// <param name="pGuid">Value of the Guid to be formatted</param>
        [NonAction]
        public string FormatGuid(Guid? pGuid)
        {
            string guid = "NULL";
            if (pGuid.HasValue)
            {
                guid = $"'{pGuid.Value}'";
            }
            return guid;
        }

        /// <summary>
        /// Set, format or normalized a string value provided into a valid one.
        /// </summary>
        /// <param name="cadena">String value to be formatted</param>
        [NonAction]
        public string FormatString(string cadena)
        {
            if (cadena == null)
            {
                cadena = "NULL";
            }
            else
            {
                cadena = $"'{cadena}'";
            }
            return cadena;
        }

        /// <summary>
        /// Generate sql inserts for the documents given in the DataWrapper
        /// </summary>
        /// <param name="pDataWrappperDocumentacion">DataWrapper with the documents to generate inserts</param>
        /// <param name="pSbDocumento">StringBuilder that contains the insert to the table 'Documento'</param>
        /// <param name="pSbDocumentoWebVinBaseRecursos">StringBuilder that contains the insert to the table 'DocumentoWebVinBaseRecursos'</param>
        /// <param name="pSbDocumentoWebVinBaseRecursosExtra">StringBuilder that contains the insert to the table 'DocumentoWebVinBaseRecursosExtra'</param>
        /// <param name="pSbColaDocumento">StringBuilder that contains the insert to the table 'ColaDocumento'</param>
        /// <param name="pSbDocumentoRolGrupoIdentidades">StringBuilder that contains the insert to the table 'DocumentoRolGrupoIdentidad'</param>
        /// <param name="pSbDocumentoRolIdentidad">StringBuilder that contains the insert to the table 'DocumentoRolIdentidad'</param>
        /// <param name="pSbDocumentoWebAgCatTesauro">StringBuilder that contains the insert to the table 'DocumentoWebAgCatTesauro'</param>
        [NonAction]
        public void GenerarScriptSql(DataWrapperDocumentacion pDataWrappperDocumentacion, StringBuilder pSbDocumento, StringBuilder pSbDocumentoWebVinBaseRecursos, StringBuilder pSbDocumentoWebVinBaseRecursosExtra, StringBuilder pSbColaDocumento, StringBuilder pSbDocumentoRolGrupoIdentidades, StringBuilder pSbDocumentoRolIdentidad, StringBuilder pSbDocumentoWebAgCatTesauro)
        {
            List<AD.EntityModel.Models.Documentacion.Documento> listaDocumento = pDataWrappperDocumentacion.ListaDocumento.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumento.Length == 0 && listaDocumento.Count > 0)
            {
                pSbDocumento.AppendLine($"INSERT INTO Documento (DocumentoID, OrganizacionID, CompartirPermitido, ElementoVinculadoID, Titulo, Descripcion, Tipo, Enlace, FechaCreacion, CreadorID, TipoEntidad, NombreCategoriaDoc, NombreElementoVinculado, ProyectoID, Publico, Borrador, FichaBibliograficaID, CreadorEsAutor, Valoracion, Autor, FechaModificacion, IdentidadProteccionID, FechaProteccion, UltimaVersion, Eliminado, Protegido, NumeroComentariosPublicos, NumeroTotalVotos, NumeroTotalConsultas, NumeroTotalDescargas, VersionFotoDocumento, Rank, Rank_Tiempo, Licencia, Tags, Visibilidad) VALUES");
            }
            foreach (AD.EntityModel.Models.Documentacion.Documento item in listaDocumento)
            {
                pSbDocumento.AppendLine($" ('{item.DocumentoID.ToString()}', {item.OrganizacionID.ToString()}, {item.CompartirPermitido}, {FormatGuid(item.ElementoVinculadoID)}, {FormatString(item.Titulo)}, {FormatString(item.Descripcion)}, {item.Tipo}, {FormatString(item.Enlace)}, {FormateDate(item.FechaCreacion)},{FormatGuid(item.CreadorID)}, {item.TipoEntidad}, '{item.NombreCategoriaDoc}', '{item.NombreElementoVinculado}', {FormatGuid(item.ProyectoID)}, {item.Publico}, {item.Borrador}, {FormatGuid(item.FichaBibliograficaID)}, {item.CreadorEsAutor}, {item.Valoracion}, {FormatString(item.Autor)}, {FormateDate(item.FechaModificacion)},{FormatGuid(item.IdentidadProteccionID)}, {FormateDate(item.FechaProteccion)}, {item.UltimaVersion}, {item.Eliminado}, {item.Protegido}, {item.NumeroComentariosPublicos}, {item.NumeroTotalVotos}, {item.NumeroTotalConsultas}, {item.NumeroTotalDescargas}, {item.VersionFotoDocumento}, {item.Rank}, {item.Rank_Tiempo}, '{item.Licencia}', '{item.Tags}', {item.Visibilidad}),");
            }

            List<DocumentoWebVinBaseRecursos> listaDocumentoWebVinBaseRecursos = pDataWrappperDocumentacion.ListaDocumentoWebVinBaseRecursos.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumentoWebVinBaseRecursos.Length == 0 && listaDocumentoWebVinBaseRecursos.Count > 0)
            {
                pSbDocumentoWebVinBaseRecursos.AppendLine($"INSERT INTO DocumentoWebVinBaseRecursos (DocumentoID, BaseRecursosID, IdentidadPublicacionID, FechaPublicacion, TipoPublicacion, Compartido, LinkAComunidadOrigen, Eliminado, NumeroComentarios, NumeroVotos, PublicadorOrgID, PermiteComentarios, NivelCertificacionID, Rank,Rank_Tiempo, IndexarRecurso, PrivadoEditores, FechaCertificacion) VALUES");
            }
            foreach (DocumentoWebVinBaseRecursos item in listaDocumentoWebVinBaseRecursos)
            {
                bool compartido = item.TipoPublicacion == 0 ? false : true;
                pSbDocumentoWebVinBaseRecursos.AppendLine($" ('{item.DocumentoID.ToString()}', '{item.BaseRecursosID.ToString()}', {FormatGuid(item.IdentidadPublicacionID)}, {FormateDate(item.FechaPublicacion)}, {item.TipoPublicacion}, {compartido}, {item.LinkAComunidadOrigen}, {item.Eliminado}, {item.NumeroComentarios}, {item.NumeroVotos}, {FormatGuid(item.PublicadorOrgID)}, {item.PermiteComentarios}, {FormatGuid(item.NivelCertificacionID)}, {item.Rank}, {item.Rank_Tiempo}, {item.IndexarRecurso}, {item.PrivadoEditores}, {FormateDate(item.FechaCertificacion)}),");
            }

            List<DocumentoWebVinBaseRecursosExtra> listaDocumentoWebVinBaseRecursosExtra = pDataWrappperDocumentacion.ListaDocumentoWebVinBaseRecursosExtra.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumentoWebVinBaseRecursosExtra.Length == 0 && listaDocumentoWebVinBaseRecursosExtra.Count > 0)
            {
                pSbDocumentoWebVinBaseRecursosExtra.AppendLine("INSERT INTO DocumentoWebVinBaseRecursosExtra (DocumentoID, BaseRecursosID, NumeroDescargas, NumeroConsultas, FechaUltimaVisita) VALUES");
            }
            foreach (DocumentoWebVinBaseRecursosExtra item in listaDocumentoWebVinBaseRecursosExtra)
            {
                pSbDocumentoWebVinBaseRecursosExtra.AppendLine($" ('{item.DocumentoID.ToString()}', '{item.BaseRecursosID.ToString()}', {item.NumeroDescargas}, {item.NumeroConsultas}, {FormateDate(item.FechaUltimaVisita)}),");
            }

            List<ColaDocumento> listaColaDocumento = pDataWrappperDocumentacion.ListaColaDocumento.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbColaDocumento.Length == 0 && listaColaDocumento.Count > 0)
            {
                pSbColaDocumento.AppendLine("INSERT INTO ColaDocumento (DocumentoID, AccionRealizada, Estado, FechaEncolado, FechaProcesado,Prioridad,InfoExtra,EstadoCargaID) VALUES");
            }
            foreach (ColaDocumento item in listaColaDocumento)
            {
                pSbColaDocumento.AppendLine($"({FormatGuid(item.DocumentoID)}, {item.AccionRealizada}, {item.Estado}, {FormateDate(item.FechaEncolado)},{FormateDate(item.FechaProcesado)}, {item.Prioridad}, {FormatString(item.InfoExtra)}, {item.EstadoCargaID}),");
            }

            List<DocumentoRolGrupoIdentidades> listaDocumentoRolGrupoIdentidades = pDataWrappperDocumentacion.ListaDocumentoRolGrupoIdentidades.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumentoRolGrupoIdentidades.Length == 0 && listaDocumentoRolGrupoIdentidades.Count > 0)
            {
                pSbDocumentoRolGrupoIdentidades.AppendLine("INSERT INTO DocumentoRolGrupoIdentidades (DocumentoID, GrupoID, Editor) VALUES ");
            }
            foreach (DocumentoRolGrupoIdentidades item in listaDocumentoRolGrupoIdentidades)
            {
                pSbDocumentoRolGrupoIdentidades.AppendLine($"({FormatGuid(item.DocumentoID)}, {FormatGuid(item.GrupoID)}, {item.Editor}),");
            }

            List<DocumentoRolIdentidad> listaDocumentoRolIdentidad = pDataWrappperDocumentacion.ListaDocumentoRolIdentidad.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumentoRolIdentidad.Length == 0 && listaDocumentoRolIdentidad.Count > 0)
            {
                pSbDocumentoRolIdentidad.AppendLine("INSERT INTO DocumentoRolIdentidad (DocumentoID, PerfilID, Editor) VALUES ");
            }
            foreach (DocumentoRolIdentidad item in listaDocumentoRolIdentidad)
            {
                pSbDocumentoRolIdentidad.AppendLine($"({FormatGuid(item.DocumentoID)}, {FormatGuid(item.PerfilID)}, {item.Editor}),");
            }

            List<DocumentoWebAgCatTesauro> listaDocumentoWebAgCatTesauro = pDataWrappperDocumentacion.ListaDocumentoWebAgCatTesauro.Where(doc => mEntityContext.Entry(doc).State.Equals(EntityState.Added)).ToList();
            if (pSbDocumentoWebAgCatTesauro.Length == 0 && listaDocumentoWebAgCatTesauro.Count > 0)
            {
                pSbDocumentoRolIdentidad.AppendLine("INSERT INTO DocumentoWebAgCatTesauro (Fecha, TesauroID, CategoriaTesauroID, BaseRecursosID, DocumentoID) VALUES ");
            }
            foreach (DocumentoWebAgCatTesauro item in listaDocumentoWebAgCatTesauro)
            {
                pSbDocumentoRolIdentidad.AppendLine($"({FormateDate(item.Fecha)}, {FormatGuid(item.TesauroID)}, {FormatGuid(item.CategoriaTesauroID)}, {FormatGuid(item.BaseRecursosID)}, {FormatGuid(item.DocumentoID)}),");
            }
        }

        [NonAction]
        private void CambiarIDsElementoOngologia(List<ElementoOntologia> pListaEntidades, Guid pNuevoID, List<string> pListaEntidadesProcesadas = null)
        {
            if(pListaEntidadesProcesadas == null)
            {
                pListaEntidadesProcesadas = new List<string>();
            }

            foreach (ElementoOntologia elemento in pListaEntidades)
            {
                if (!pListaEntidadesProcesadas.Contains(elemento.ID))
                {
                    pListaEntidadesProcesadas.Add(elemento.ID);
                    if (elemento.ID != null)
                    {
                        string antiguoID = elemento.ID.Substring(0, elemento.ID.LastIndexOf("_"));
                        antiguoID = antiguoID.Substring(antiguoID.LastIndexOf("_") + 1);
                        elemento.ID = elemento.ID.Replace(antiguoID, pNuevoID.ToString());

                        string antiguoDirectorioImg = UtilArchivos.DirectorioDocumento(new Guid(antiguoID));
                        string nuevoDirectorioImg = UtilArchivos.DirectorioDocumento(pNuevoID);

                        foreach (Propiedad propiedad in elemento.Propiedades)
                        {
                            if (propiedad.UnicoValor.Key != null)
                            {
                                propiedad.UnicoValor = new KeyValuePair<string, ElementoOntologia>(propiedad.UnicoValor.Key.Replace(antiguoDirectorioImg, nuevoDirectorioImg).Replace(antiguoID, pNuevoID.ToString()), propiedad.UnicoValor.Value);
                            }
                            else
                            {
                                Dictionary<string, ElementoOntologia> listaValores = new Dictionary<string, ElementoOntologia>(propiedad.ListaValores);

                                propiedad.ListaValores.Clear();

                                foreach (string valor in listaValores.Keys)
                                {
                                    propiedad.ListaValores.Add(valor.Replace(antiguoDirectorioImg, nuevoDirectorioImg).Replace(antiguoID, pNuevoID.ToString()), listaValores[valor]);
                                }
                            }
                        }

                        if (elemento.EntidadesRelacionadas.Count > 0)
                        {
                            CambiarIDsElementoOngologia(elemento.EntidadesRelacionadas, pNuevoID, pListaEntidadesProcesadas);
                        }
                    }
                }
            }
        }

        [NonAction]
        private void CambiarSegundosIDsElementoOngologia(List<ElementoOntologia> pListaEntidades, Dictionary<string, string> pAntiguosNuevosIDs, List<string> pListaEntidadesProcesadas = null)
        {
            if(pListaEntidadesProcesadas == null)
            {
                pListaEntidadesProcesadas = new List<string>();
            }

            foreach (ElementoOntologia elemento in pListaEntidades)
            {
                if (!pListaEntidadesProcesadas.Contains(elemento.ID))
                {
                    pListaEntidadesProcesadas.Add(elemento.ID);
                    foreach (Propiedad propiedad in elemento.Propiedades)
                    {
                        if (propiedad.Tipo == TipoPropiedad.ObjectProperty)
                        {
                            if (propiedad.UnicoValor.Key != null)
                            {
                                if (pAntiguosNuevosIDs.Keys.Any())
                                {
                                    propiedad.UnicoValor = new KeyValuePair<string, ElementoOntologia>(propiedad.UnicoValor.Key.Replace(pAntiguosNuevosIDs.Keys.FirstOrDefault(), pAntiguosNuevosIDs[pAntiguosNuevosIDs.Keys.FirstOrDefault()]), propiedad.UnicoValor.Value);
                                }
                            }
                            else
                            {
                                var propiedadesExistentes = pAntiguosNuevosIDs.Keys.Where(idAntiguo => propiedad.ListaValores.ContainsKey(idAntiguo));

                                if (propiedadesExistentes.Any())
                                {
                                    Dictionary<string, ElementoOntologia> listaValores = new Dictionary<string, ElementoOntologia>(propiedad.ListaValores);

                                    propiedad.ListaValores.Clear();

                                    foreach (string antiguoID in propiedadesExistentes)
                                    {
                                        string keyElementoNuevo = listaValores.First(item => item.Key.Contains(pAntiguosNuevosIDs[antiguoID])).Key;
                                        string key = keyElementoNuevo.Replace(antiguoID, pAntiguosNuevosIDs[antiguoID]);
                                        propiedad.ListaValores.Add(key, listaValores[keyElementoNuevo]);
                                        listaValores.Remove(keyElementoNuevo);
                                    }

                                    foreach (string key in listaValores.Keys)
                                    {
                                        propiedad.ListaValores.Add(key, listaValores[key]);
                                    }
                                }
                            }
                        }
                    }

                    if (elemento.EntidadesRelacionadas.Count > 0)
                    {
                        CambiarSegundosIDsElementoOngologia(elemento.EntidadesRelacionadas, pAntiguosNuevosIDs, pListaEntidadesProcesadas);
                    }
                }
            }
        }

        [NonAction]
        private void RecuperarIDsElementoOngologia(List<ElementoOntologia> pListaEntidades, Propiedad pPropiedadPadre, List<string> pEntidadesYaAgregadas, Dictionary<string, string> pCambioIDs, List<ElementoOntologia> pEntidadesPrincAntiguas)
        {
            foreach (ElementoOntologia elemento in pListaEntidades)
            {
                if (elemento != null && elemento.ID != null)
                {
                    string antiguoID = elemento.ID.Substring(elemento.ID.LastIndexOf("_") + 1);
                    elemento.ID = ObtenerIDEntidadAntigua(elemento, pPropiedadPadre, pEntidadesYaAgregadas, pEntidadesPrincAntiguas);
                    string nuevoID = elemento.ID.Substring(elemento.ID.LastIndexOf("_") + 1);

                    if (antiguoID != nuevoID)
                    {
                        pCambioIDs.Add(antiguoID, nuevoID);
                    }

                    foreach (Propiedad propiedad in elemento.Propiedades)
                    {
                        if (propiedad.Tipo == TipoPropiedad.ObjectProperty && !propiedad.TieneSelectorEntidad)
                        {
                            RecuperarIDsElementoOngologia(new List<ElementoOntologia>(propiedad.ValoresUnificados.Values), propiedad, pEntidadesYaAgregadas, pCambioIDs, pEntidadesPrincAntiguas);
                        }

                    }
                }
            }
        }

        [NonAction]
        private string ObtenerIDEntidadAntigua(ElementoOntologia pEntidad, Propiedad pPropiedadPadre, List<string> pEntidadesYaAgregadas, List<ElementoOntologia> pEntidadesPrincAntiguas)
        {
            string entidadID = null;
            foreach (ElementoOntologia entidadPrin in pEntidadesPrincAntiguas)
            {
                if (pPropiedadPadre == null)
                {
                    if (entidadPrin.TipoEntidad == pEntidad.TipoEntidad)
                    {
                        entidadID = entidadPrin.ID;
                        break;
                    }
                }
                else
                {
                    Propiedad propAux = EstiloPlantilla.ObtenerPropiedadACualquierNivelPorNombre(pPropiedadPadre.Nombre, pPropiedadPadre.ElementoOntologia.TipoEntidad, entidadPrin);

                    if (propAux != null && propAux.Rango == pPropiedadPadre.Rango)
                    {
                        if (pPropiedadPadre.FunctionalProperty && propAux.UnicoValor.Key != null && pPropiedadPadre.UnicoValor.Key == null)
                        {
                            if (propAux.UnicoValor.Value.TipoEntidad == pEntidad.TipoEntidad)
                            {
                                entidadID = propAux.UnicoValor.Key;
                            }
                        }
                        else if (!pPropiedadPadre.FunctionalProperty)
                        {
                            if (propAux.ListaValores.ContainsKey(pEntidad.ID))
                            {
                                entidadID = pEntidad.ID;
                            }
                            else
                            {
                                var listaValoresDisponibles = propAux.ListaValores.Where(item => !pPropiedadPadre.ListaValores.ContainsKey(item.Key) && !pEntidadesYaAgregadas.Contains(entidadID));

                                if (listaValoresDisponibles.Count() > 0 && Uri.IsWellFormedUriString(listaValoresDisponibles.First().Key, UriKind.RelativeOrAbsolute))
                                {
                                    entidadID = listaValoresDisponibles.First().Key;
                                }
                                else
                                {
                                    entidadID = pEntidad.ID;
                                }
                            }
                        }

                        break;
                    }
                }
            }

            if (entidadID != null && !pEntidadesYaAgregadas.Contains(entidadID) && !entidadID.Contains("http://"))
            {
                pEntidadesYaAgregadas.Add(entidadID);
            }
            else
            {
                entidadID = pEntidad.ID;
            }

            return entidadID;
        }

        /// <summary>
        /// Upload the resource attached files.
        /// </summary>
        /// <param name="pDocumentoID">Identificador del documento</param>
        /// <param name="pOntologiaID">Identificador de la ontología</param>
        /// <param name="pPropiedadesRdfArchivo">Propiedades que se van a agregar</param>
        /// <param name="pTipoPropiedadArchivo">Tipos de las propiedades de los archivos</param>
        /// <param name="pArchivosAdjuntosRdf">Archivos adjuntos</param>
        /// <param name="pInstanciasPrincipales">ElementoOntologia</param>
        /// <param name="pImgPrincipal">Cadena con la imagen principal</param>
        /// <returns></returns>
        [NonAction]
        private string SubirArchivosDelRDF(Guid pDocumentoID, List<AttachedResource> pListaArchivosAdjuntos, List<ElementoOntologia> pInstanciasPrincipales, string pImgPrincipal)
        {
            string imagenRepresentanteDoc = null;

            if (pListaArchivosAdjuntos != null && pListaArchivosAdjuntos.Count > 0)
            {
                GestionDocumental gd = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
                gd.Url = UrlServicioWebDocumentacion;

                ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                servicioImagenes.Url = UrlServicioImagenes;

                ServicioVideos servDocLink = new ServicioVideos(mConfigService, mLoggingService, mLoggerFactory.CreateLogger<ServicioVideos>(), mLoggerFactory);

                foreach (AttachedResource adjunto in pListaArchivosAdjuntos)
                {
                    if (adjunto.file_property_type == (short)AttachedResourceFilePropertyTypes.file)
                    {
                        string nombreArchivo = adjunto.file_rdf_property.Substring(0, adjunto.file_rdf_property.LastIndexOf("."));
                        string extension = adjunto.file_rdf_property.Substring(adjunto.file_rdf_property.LastIndexOf("."));

                        if (TieneGoogleDriveConfigurado)
                        {
                            if (extension.Contains("@"))
                            {
                                extension = extension.Substring(0, extension.IndexOf("@"));
                            }

                            try
                            {
                                string especialID = nombreArchivo + extension;
                                //TODO Javier migrar redes sociales
                                /*OAuthGoogleDrive gdrive = new OAuthGoogleDrive();

                                if (adjunto.delete_file)
                                {
                                    //id de google sin extension
                                    string googleID = nombreArchivo;
                                    string docID = googleID.Substring(googleID.LastIndexOf('#') + 1);
                                    ;
                                    gdrive.EliminarDocumento(docID);
                                }
                                else
                                {
                                    string googleID = gdrive.SubirDocumento(especialID, extension, adjunto.rdf_attached_file);
                                    //Nombre del fichero con ID de google.
                                    string newFileName = especialID.Substring(0, especialID.LastIndexOf('.')) + ID_GOOGLE + googleID + extension;
                                    ModificarNombreFicheroPorNombreFicheroGoogleDrive(mOntologia, pDocumentoID, newFileName, nombreArchivo + extension, pInstanciasPrincipales);
                                }
                                */
                            }
                            catch (Exception ex)
                            {
                                mLoggingService.GuardarLogError(ex, mlogger);
                                throw new GnossException("Incorrect attached files. GoogleDrive=" + TieneGoogleDriveConfigurado, HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            string directorio = UtilArchivos.ContentDocumentosSem + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(pDocumentoID);

                            if (extension.Contains("@"))
                            {
                                directorio = directorio + "\\" + extension.Substring(extension.IndexOf("@") + 1);
                                extension = extension.Substring(0, extension.IndexOf("@"));
                            }

                            if (adjunto.delete_file)
                            {
                                gd.BorrarDocumentoDeDirectorio(directorio, nombreArchivo, extension);
                            }
                            else
                            {
                                string idAuxGestorDocumental = gd.AdjuntarDocumentoADirectorio(adjunto.rdf_attached_file, directorio, nombreArchivo, extension);

                                if (idAuxGestorDocumental != null && idAuxGestorDocumental.Equals("Error"))
                                {
                                    throw new GnossException("Incorrect attached files.", HttpStatusCode.BadRequest);
                                }
                            }
                        }
                    }
                    else if (adjunto.file_property_type == (short)AttachedResourceFilePropertyTypes.image)
                    {
                        if (adjunto.delete_file)
                        {
                            string especialID = adjunto.file_rdf_property;
                            especialID = especialID.Substring(0, especialID.LastIndexOf("."));
                            especialID = especialID.Substring(especialID.LastIndexOf('/') + 1);
                            string directorioViejo = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticasAntiguo + "\\" + pDocumentoID.ToString();
                            string[] imagenes = servicioImagenes.ObtenerIDsImagenesPorNombreImagen(directorioViejo, especialID);

                            if (imagenes != null)
                            {
                                foreach (string imagen in imagenes)
                                {
                                    if (!servicioImagenes.BorrarImagenDeDirectorio(UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticasAntiguo + "\\" + pDocumentoID.ToString() + "\\" + imagen))
                                    {
                                        throw new GnossException("Error deleting image.", HttpStatusCode.BadRequest);
                                    }
                                }
                            }

                            string directorioNuevo = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticas + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(pDocumentoID);
                            imagenes = servicioImagenes.ObtenerIDsImagenesPorNombreImagen(directorioNuevo, especialID);

                            if (imagenes != null)
                            {
                                foreach (string imagen in imagenes)
                                {
                                    if (!servicioImagenes.BorrarImagenDeDirectorio(adjunto.file_rdf_property))
                                    {
                                        throw new GnossException("Error deleting image.", HttpStatusCode.BadRequest);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(new MemoryStream(adjunto.rdf_attached_file), true);
                            //MemoryStream ms = new MemoryStream();
                            //imagen.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            //byte[] buffer1 = ms.ToArray();

                            string especialID = adjunto.file_rdf_property;
                            string extensionArchivo = especialID.Substring(especialID.LastIndexOf("."));
                            especialID = especialID.Substring(0, especialID.LastIndexOf("."));

                            if (especialID.Contains("[IMGPrincipal]"))
                            {
                                especialID = especialID.Replace("[IMGPrincipal]", "");

                                string tamañosImgs = especialID.Substring(1, especialID.IndexOf("]") - 1);

                                if (tamañosImgs[tamañosImgs.Length - 1] != ',')
                                {
                                    tamañosImgs += ",";
                                }

                                especialID = especialID.Substring(especialID.IndexOf("]") + 1);

                                if (especialID.Contains("/"))
                                {
                                    especialID = especialID.Substring(especialID.IndexOf("/") + 1);
                                }

                                imagenRepresentanteDoc = tamañosImgs + UtilArchivos.ContentImagenes + "/" + UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.ContentImagenesSemanticas + "/" + UtilArchivos.DirectorioDocumento(pDocumentoID) + "/" + especialID + extensionArchivo;
                            }

                            if (!servicioImagenes.AgregarImagenADirectorio(adjunto.rdf_attached_file, UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticas + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(pDocumentoID), especialID, extensionArchivo))
                            {
                                throw new GnossException("Incorrect attached images.", HttpStatusCode.BadRequest);
                            }
                        }
                    }
                    else if (adjunto.file_property_type == (short)AttachedResourceFilePropertyTypes.linkFile)
                    {
                        string nombreArchivo = adjunto.file_rdf_property.Substring(0, adjunto.file_rdf_property.LastIndexOf("."));
                        string extension = adjunto.file_rdf_property.Substring(adjunto.file_rdf_property.LastIndexOf("."));
                        string idioma = null;
                        string idiomaSimple = null;
                        if (extension.Contains("@"))
                        {
                            idiomaSimple = extension.Substring(extension.IndexOf("@") + 1);
                            idioma = idiomaSimple + "/";
                            extension = extension.Substring(0, extension.IndexOf("@"));
                        }

                        string nombreDefArc = nombreArchivo;

                        if (adjunto.delete_file)
                        {
                            //TODO Javier descomentar
                            //servDocLink.BorrarDocumentoDeDirectorio(pDocumentoID, idioma + nombreArchivo, extension);
                        }
                        else
                        {
                            if (nombreDefArc.Contains("/"))
                            {
                                nombreDefArc = nombreDefArc.Substring(nombreDefArc.LastIndexOf("/") + 1);

                                foreach (string trozo in nombreArchivo.Split('/'))
                                {
                                    //El ID del archivo ha cambiado al reemplzar los IDs y hay que actualizarlo:
                                    Guid antiguoID = Guid.Empty;
                                    if (Guid.TryParse(trozo, out antiguoID))
                                    {
                                        nombreArchivo = nombreArchivo.Replace(antiguoID.ToString(), pDocumentoID.ToString());
                                    }
                                }
                            }

                            nombreDefArc = UtilCadenas.EliminarCaracteresUrlSem(nombreDefArc);

                            string nombreParaRdf = string.Empty;

                            if (!string.IsNullOrEmpty(idioma))
                            {
                                nombreParaRdf = Path.Combine(UtilArchivos.ContentDocLinks, UtilArchivos.DirectorioDocumento(pDocumentoID), idioma, nombreDefArc);
                            }
                            else
                            {
                                nombreParaRdf = Path.Combine(UtilArchivos.ContentDocLinks, UtilArchivos.DirectorioDocumento(pDocumentoID), nombreDefArc);
                            }

                            if (!ReemplazarValorPropiedadEntidad(nombreArchivo + extension, nombreParaRdf + extension, pInstanciasPrincipales, idiomaSimple))
                            {
                                throw new GnossException("The link attachments do not correspond to those indicated in the RDF. Perhaps the property that is being given a value is not set as 'ArchivoLink'.", HttpStatusCode.BadRequest);
                            }

                            if (!servDocLink.AgregarDocumento(adjunto.rdf_attached_file, pDocumentoID, idioma + nombreDefArc, extension))
                            {
                                throw new GnossException("Incorrect attached link files.", HttpStatusCode.BadRequest);
                            }
                        }
                    }
                }
                //TODO Javier descomentar servDocLink.Dispose();
            }

            if (!string.IsNullOrEmpty(pImgPrincipal))
            {
                try
                {
                    string extensionImgPrincipal = pImgPrincipal.Substring(pImgPrincipal.LastIndexOf("."));
                    pImgPrincipal = pImgPrincipal.Substring(0, pImgPrincipal.LastIndexOf("."));

                    if (pImgPrincipal.Contains("[IMGPrincipal]"))
                    {
                        pImgPrincipal = pImgPrincipal.Replace("[IMGPrincipal]", "");
                    }

                    string tamañosImgs = pImgPrincipal.Substring(1, pImgPrincipal.IndexOf("]") - 1);

                    if (tamañosImgs[tamañosImgs.Length - 1] != ',')
                    {
                        tamañosImgs += ",";
                    }

                    pImgPrincipal = pImgPrincipal.Substring(pImgPrincipal.IndexOf("]") + 1);

                    if (pImgPrincipal.Contains("/"))
                    {
                        pImgPrincipal = pImgPrincipal.Substring(pImgPrincipal.IndexOf("/") + 1);
                    }

                    imagenRepresentanteDoc = tamañosImgs + UtilArchivos.ContentImagenes + "/" + UtilArchivos.ContentImagenesDocumentos + "/" + UtilArchivos.ContentImagenesSemanticas + "/" + UtilArchivos.DirectorioDocumento(pDocumentoID) + "/" + pImgPrincipal + extensionImgPrincipal;
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, mlogger);
                    throw new GnossException("The image has incorrect format. Ejemplo: [IMGPrincipal][240,]a27b1e80-9755-7ad5-2731-e2ac6be9b463.jpg", HttpStatusCode.BadRequest);
                }
            }

            return imagenRepresentanteDoc;
        }

        /// <summary>
        /// Modificamos las instancias por nivel
        /// </summary>
        /// <param name="pOntologia"></param>
        /// <param name="pPropertyList"></param>
        /// <param name="pDocID"></param>
        /// <param name="pPredicado"></param>
        /// <param name="pObjetoViejo"></param>
        /// <param name="pObjetoNuevo"></param>
        /// <param name="pAccion"></param>
        [NonAction]
        private void ModificarInstanciaPorNivel(Ontologia pOntologia, List<Propiedad> pPropertyList, Guid pDocID, string pPredicado, string pObjetoViejo, string pObjetoNuevo, bool pEsEntidadPrincipal, GnossStringBuilder pSbTriplesInsertar, GnossStringBuilder pSbTriplesEliminar, GnossStringBuilder pSbTriplesInsertarBusqueda, GnossStringBuilder pSbTriplesEliminarBusqueda)
        {
            string predicadoComparar = "";
            bool predicadoEncontrado = false;
            string sujetoEntidadPrincipal = $"http://gnoss/{pDocID.ToString().ToUpper()}";

            //Recorremos el predicado buscando las propiedades específicas
            //Al llegar al final se realizan las acciones deseadas sobre la lista de valores o valor único.
            if (pPredicado.Contains("|"))
            {
                //Buscar propiedad y volver a lanzar
                char[] delimiter = { '|' };
                string[] pred = pPredicado.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                //Cuando la URL completa contiene la URL de la ontología, abreviar el predicado y quitarle la URL de la ontologia.
                predicadoComparar = ObtenerPredicadoCorrectoSegunOntologia(pred[0], pOntologia.GestorOWL.UrlOntologia);

                foreach (Propiedad prop in pPropertyList)
                {
                    if (prop.Nombre == predicadoComparar)
                    {
                        predicadoEncontrado = true;

                        //Diferencias cuando hay que agnadir un campo nuevo y cuando hay que tratar un campo.
                        //Agregar nuevo nivel: El objeto nuevo no tiene '|' y no hay ojeto viejo
                        if (!string.IsNullOrEmpty(pObjetoNuevo) && !pObjetoNuevo.Contains("|") && string.IsNullOrEmpty(pObjetoViejo))
                        {
                            //Añadimos el nuevo valor:
                            //Incluir objeto tipo instanciaEntidad y añadir propiedad
                            ElementoOntologia instanciaEntidad = pOntologia.GetEntidadTipo(prop.Rango, true);
                            instanciaEntidad.ID = instanciaEntidad.TipoEntidad + "_" + pDocID.ToString() + "_" + Guid.NewGuid();

                            if (instanciaEntidad.ID.Contains("#"))
                            {
                                //Si el id contiene # le quitamos de # para atras todo dejando la parte delantera
                                instanciaEntidad.ID = instanciaEntidad.ID.Substring(instanciaEntidad.ID.LastIndexOf("#") + 1);
                            }
                            else if (instanciaEntidad.ID.Contains("/"))
                            {
                                //Si el id contiene / le quitamos de / para atrás todo dejando la parte delantera.
                                instanciaEntidad.ID = instanciaEntidad.ID.Substring(instanciaEntidad.ID.LastIndexOf("/") + 1);
                            }

                            //Agregamos el campo nuevo
                            if (prop.FunctionalProperty)
                            {
                                //Al ser funcional debemos borrar la entidad anterior de entidades relacionadas.
                                if (prop.UnicoValor.Value != null)
                                {
                                    prop.ElementoOntologia.EntidadesRelacionadas.Remove(prop.UnicoValor.Value);
                                    pSbTriplesEliminar.AppendLine($"<{prop.ElementoOntologia.Uri}> <{prop.NombreFormatoUri}> <{prop.UnicoValor.Value}> . ");
                                    pSbTriplesEliminar.AppendLine($"<{UrlIntragnoss}{pDocID.ToString().ToLower()}> <http://gnoss/hasEntidad> <{prop.UnicoValor.Value}> . ");
                                    pSbTriplesEliminar.AppendLine($"<{prop.UnicoValor.Value}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{instanciaEntidad.TipoEntidad}> .");
                                    pSbTriplesEliminar.AppendLine($"<{prop.UnicoValor.Value}> <http://www.w3.org/2000/01/rdf-schema#label> \"{instanciaEntidad.TipoEntidad}\" .");

                                    pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, prop, prop.UnicoValor.Value.ToString()));
                                }

                                pSbTriplesInsertar.AppendLine($"<{prop.ElementoOntologia.Uri}> <{prop.NombreFormatoUri}> <{instanciaEntidad.Uri}> . ");
                                pSbTriplesInsertar.AppendLine($"<{UrlIntragnoss}{pDocID.ToString().ToLower()}> <http://gnoss/hasEntidad> <{instanciaEntidad.Uri}> . ");
                                pSbTriplesInsertar.AppendLine($"<{instanciaEntidad.Uri}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{instanciaEntidad.TipoEntidad}> .");
                                pSbTriplesInsertar.AppendLine($"<{instanciaEntidad.Uri}> <http://www.w3.org/2000/01/rdf-schema#label> \"{instanciaEntidad.TipoEntidad}\" .");
                                prop.UnicoValor = new KeyValuePair<string, ElementoOntologia>(instanciaEntidad.ID, instanciaEntidad);

                                pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, prop, instanciaEntidad.Uri));
                            }
                            else
                            {
                                if (!prop.ListaValores.ContainsKey(instanciaEntidad.ID))
                                {
                                    pSbTriplesInsertar.AppendLine($"<{prop.ElementoOntologia.Uri}> <{prop.NombreFormatoUri}> <{instanciaEntidad.Uri}> . ");
                                    pSbTriplesInsertar.AppendLine($"<{UrlIntragnoss}{pDocID.ToString().ToLower()}> <http://gnoss/hasEntidad> <{instanciaEntidad.Uri}> . ");
                                    pSbTriplesInsertar.AppendLine($"<{instanciaEntidad.Uri}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{instanciaEntidad.TipoEntidad}> .");
                                    pSbTriplesInsertar.AppendLine($"<{instanciaEntidad.Uri}> <http://www.w3.org/2000/01/rdf-schema#label> \"{instanciaEntidad.TipoEntidad}\" .");
                                    prop.ListaValores.Add(instanciaEntidad.ID, instanciaEntidad);

                                    pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, prop, instanciaEntidad.Uri));
                                }
                            }

                            //Se agrega el nuevo nivel.
                            prop.ElementoOntologia.EntidadesRelacionadas.Add(instanciaEntidad);

                            Propiedad p = instanciaEntidad.ObtenerPropiedad(pred[1]);
                            ModificarPropiedad(pDocID, p, pred[1], "", pObjetoNuevo, pEsEntidadPrincipal, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                        }
                        else
                        {
                            string predIDFromPObject = "";
                            string objetoNuevoProcesado = "";
                            string objetoViejoProcesado = "";
                            if (!string.IsNullOrEmpty(pObjetoNuevo) && pObjetoNuevo.Contains("|") && !string.IsNullOrEmpty(pObjetoViejo) && pObjetoViejo.Contains("|"))
                            {
                                //Modificar objeto: el objeto nuevo y viejo tienen el mismo id'|'
                                if (pObjetoNuevo.Split('|')[0].Equals(pObjetoViejo.Split('|')[0]))
                                {
                                    predIDFromPObject = pObjetoNuevo.Split('|')[0];
                                    predIDFromPObject = predIDFromPObject.Substring(predIDFromPObject.LastIndexOf("/") + 1);
                                    objetoNuevoProcesado = pObjetoNuevo.Substring(pObjetoNuevo.IndexOf("|") + 1);
                                    objetoViejoProcesado = pObjetoViejo.Substring(pObjetoViejo.IndexOf("|") + 1);
                                }
                                else
                                {
                                    throw new Exception("El objeto nuevo y viejo no tienen el mismo ID del predicado.");
                                }
                            }
                            else if (!string.IsNullOrEmpty(pObjetoNuevo) && pObjetoNuevo.Contains("|"))
                            {
                                //Agregar nuevo objeto: el objeto nuevo tiene '|'
                                predIDFromPObject = pObjetoNuevo.Split('|')[0];
                                predIDFromPObject = predIDFromPObject.Substring(predIDFromPObject.LastIndexOf("/") + 1);
                                objetoNuevoProcesado = pObjetoNuevo.Substring(pObjetoNuevo.IndexOf("|") + 1);
                                objetoViejoProcesado = pObjetoViejo;

                                if (!prop.ValoresUnificados.ContainsKey(predIDFromPObject))
                                {
                                    ElementoOntologia entidadtipo = pOntologia.GetEntidadTipo(prop.Rango, true);
                                    entidadtipo.ID = predIDFromPObject;

                                    //Agregamos el campo nuevo
                                    if (prop.FunctionalProperty)
                                    {
                                        if (prop.UnicoValor.Key == null)
                                        {
                                            //Unico valor
                                            prop.UnicoValor = new KeyValuePair<string, ElementoOntologia>(predIDFromPObject, entidadtipo);
                                            pSbTriplesInsertar.AppendLine($"<{prop.ElementoOntologia.Uri}> <{prop.NombreFormatoUri}> <{entidadtipo.Uri}> . ");
                                            pSbTriplesInsertar.AppendLine($"<{UrlIntragnoss}{pDocID.ToString().ToLower()}> <http://gnoss/hasEntidad> <{entidadtipo.Uri}> . ");
                                            pSbTriplesInsertar.AppendLine($"<{entidadtipo.Uri}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{entidadtipo.TipoEntidad}> .");
                                            pSbTriplesInsertar.AppendLine($"<{entidadtipo.Uri}> <http://www.w3.org/2000/01/rdf-schema#label> \"{entidadtipo.TipoEntidad}\" .");

                                            pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, prop, entidadtipo.Uri));
                                        }
                                        else
                                        {
                                            throw new Exception("No se puede agregar '" + pObjetoNuevo + "' a una propiedad funcional, ya que actualmente tiene el valor '" + prop.UnicoValor.Value + "'.");
                                        }
                                    }
                                    else
                                    {
                                        if (!prop.ListaValores.ContainsKey(predIDFromPObject))
                                        {
                                            prop.ListaValores.Add(predIDFromPObject, entidadtipo);
                                            pSbTriplesInsertar.AppendLine($"<{prop.ElementoOntologia.Uri}> <{prop.NombreFormatoUri}> <{entidadtipo.Uri}> . ");
                                            pSbTriplesInsertar.AppendLine($"<{UrlIntragnoss}{pDocID.ToString().ToLower()}> <http://gnoss/hasEntidad> <{entidadtipo.Uri}> . ");
                                            pSbTriplesInsertar.AppendLine($"<{entidadtipo.Uri}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{entidadtipo.TipoEntidad}> .");
                                            pSbTriplesInsertar.AppendLine($"<{entidadtipo.Uri}> <http://www.w3.org/2000/01/rdf-schema#label> \"{entidadtipo.TipoEntidad}\" .");

                                            pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, prop, entidadtipo.Uri));
                                        }
                                    }

                                    prop.ElementoOntologia.EntidadesRelacionadas.Add(entidadtipo);
                                }
                            }
                            else if (!string.IsNullOrEmpty(pObjetoViejo) && pObjetoViejo.Contains("|"))
                            {
                                //Eliminar objeto: el objeto viejo tiene '|'
                                predIDFromPObject = pObjetoViejo.Split('|')[0];
                                predIDFromPObject = predIDFromPObject.Substring(predIDFromPObject.LastIndexOf("/") + 1);
                                objetoViejoProcesado = pObjetoViejo.Substring(pObjetoViejo.IndexOf("|") + 1);
                                objetoNuevoProcesado = pObjetoNuevo;
                            }

                            //Es necesario especifcar el id del segundo nivel (el objeto que enlaza)
                            if (!string.IsNullOrEmpty(predIDFromPObject))
                            {
                                if (prop.FunctionalProperty)
                                {
                                    //Solo modificamos si el id es el mismo que el de la propiedad.
                                    if (predIDFromPObject.Equals(prop.UnicoValor.Key))
                                    {
                                        ModificarInstanciaPorNivel(pOntologia, prop.UnicoValor.Value.Propiedades, pDocID, pPredicado.Substring(pPredicado.IndexOf("|") + 1), objetoViejoProcesado, objetoNuevoProcesado, false, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                                    }
                                }
                                else
                                {
                                    foreach (string propID in prop.ListaValores.Keys)
                                    {
                                        //Solo modificamos si el id es el mismo que el de la propiedad.
                                        if (predIDFromPObject.Equals(propID))
                                        {
                                            ModificarInstanciaPorNivel(pOntologia, prop.ListaValores[predIDFromPObject].Propiedades, pDocID, pPredicado.Substring(pPredicado.IndexOf("|") + 1), objetoViejoProcesado, objetoNuevoProcesado, false, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("No se especifica el ID del segundo nivel que se quiere modificar.");
                            }
                        }
                    }
                }
            }
            else
            {
                predicadoComparar = ObtenerPredicadoCorrectoSegunOntologia(pPredicado, pOntologia.GestorOWL.UrlOntologia);

                //Tratamiento del último nivel
                foreach (Propiedad prop in pPropertyList)
                {
                    if (prop.Nombre == predicadoComparar)
                    {
                        predicadoEncontrado = true;

                        if (!string.IsNullOrEmpty(pObjetoNuevo) && pObjetoNuevo.Contains("|"))
                        {
                            string[] delimiter = { "|" };
                            string[] objetos = pObjetoNuevo.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                            foreach (Propiedad p in pPropertyList)
                            {
                                if (p.UnicoValor.Key == objetos[0] || p.ListaValores.ContainsKey(objetos[0]))
                                {
                                    ModificarPropiedad(pDocID, prop, predicadoComparar, pObjetoViejo, objetos[1], pEsEntidadPrincipal, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(pObjetoViejo) && pObjetoViejo.Contains("|"))
                        {
                            string[] delimiter = { "|" };
                            string[] objetos = pObjetoViejo.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                            foreach (Propiedad p in pPropertyList)
                            {
                                if (p.UnicoValor.Key == objetos[0] || p.ListaValores.ContainsKey(objetos[0]))
                                {
                                    ModificarPropiedad(pDocID, prop, predicadoComparar, objetos[1], pObjetoNuevo, pEsEntidadPrincipal, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                                }
                            }
                        }
                        else
                        {
                            ModificarPropiedad(pDocID, prop, predicadoComparar, pObjetoViejo, pObjetoNuevo, pEsEntidadPrincipal, pSbTriplesInsertar, pSbTriplesEliminar, pSbTriplesInsertarBusqueda, pSbTriplesEliminarBusqueda);
                        }
                    }
                }
            }

            if (!predicadoEncontrado)
            {
                throw new GnossException($"El predicado '{pPredicado}' que está intentnado editar no se encuentra en la ontología.", HttpStatusCode.BadRequest);
            }
        }

        [NonAction]
        private void ModificarPropiedad(Guid pDocID, Propiedad pProp, string pPredicado, string pObjetoViejo, string pObjetoNuevo, bool pEsEntidadPrincipal, GnossStringBuilder pSbTriplesInsertar, GnossStringBuilder pSbTriplesEliminar, GnossStringBuilder pSbTriplesInsertarBusqueda, GnossStringBuilder pSbTriplesEliminarBusqueda)
        {
            if (pProp.EspecifPropiedad.TipoCampo.Equals(TipoCampoOntologia.ArchivoLink))
            {
                pObjetoNuevo = ModificarValorObjetoArchivoLink(pObjetoNuevo);
            }

            if (string.IsNullOrEmpty(pObjetoNuevo))
            {
                //Eliminamos el valor único de la propiedad
                if (pProp.FunctionalProperty)
                {
                    throw new GnossException($"No se puede eliminar '{pObjetoNuevo}' a una propiedad funcional, siempre tiene que tener un valor.", HttpStatusCode.BadRequest);
                }
                else
                {
                    string objetoViejoOriginal = pObjetoViejo;
                    if (pProp.Tipo == TipoPropiedad.ObjectProperty)
                    {
                        if (pObjetoViejo.Contains("items/"))
                        {
                            pObjetoViejo = pObjetoViejo.Substring(pObjetoViejo.IndexOf("items/") + 6);
                        }

                        if (!string.IsNullOrEmpty(pProp.Rango))
                        {
                            mEntidadesABorrar.Add(pObjetoViejo);
                            mURIEntidadesABorrar.Add(pObjetoViejo, objetoViejoOriginal);
                        }

                        if (pProp.ListaValores.ContainsKey(pObjetoViejo) && pProp.ListaValores[pObjetoViejo] != null)
                        {
                            //Borro Entidad Vieja
                            pProp.ElementoOntologia.EntidadesRelacionadas.Remove(pProp.ListaValores[pObjetoViejo]);
                        }
                    }

                    if (UtilCadenas.EsMultiIdioma(pObjetoViejo))
                    {
                        string idioma = pObjetoViejo.Substring(pObjetoViejo.LastIndexOf("@") + 1);
                        pObjetoViejo = pObjetoViejo.Substring(0, pObjetoViejo.LastIndexOf("@"));

                        if (pProp.ListaValoresIdioma.ContainsKey(idioma))
                        {
                            if (pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoViejo))
                            {
                                pProp.ListaValoresIdioma[idioma].Remove(pObjetoViejo);
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, idioma, true);
                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo, idioma));
                            }
                            //Parte de código para las migraciones de la versión 4 a la 5.
                            else if (mConfigService.ObtenerProcesarStringGrafo() && pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoViejo.Replace("''", "\"")))
                            {
                                pProp.ListaValoresIdioma[idioma].Remove(pObjetoViejo.Replace("''", "\""));
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, idioma, true);
                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo, idioma));
                            }
                            else
                            {
                                throw new GnossException($"No se ha encontrado '{pObjetoViejo}' en el idioma '{idioma}' en la lista de valores de la propiedad '{pProp.Nombre}'", HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException($"No se ha encontrado el idioma '{idioma}' en la lista de valores de la propiedad '{pProp.Nombre}'", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        if (pProp.ListaValores.ContainsKey(pObjetoViejo))
                        {
                            EliminarEntidadesAuxiliaresRelacionadas(pProp.ListaValores[pObjetoViejo]);
                            pProp.ListaValores.Remove(pObjetoViejo);
                            if (pProp.Tipo.Equals(TipoPropiedad.ObjectProperty))
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, objetoViejoOriginal, pTripleDeEliminacion: true, pObjetoEsUri: true);
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", objetoViejoOriginal, pTripleDeEliminacion: true, pObjetoEsUri: true);

                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, objetoViejoOriginal));
                            }
                            else
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true);

                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo));
                            }
                        }
                        else if (pProp.ListaValores.ContainsKey($"{UrlIntragnoss}items/{pObjetoViejo}"))
                        {
                            pProp.ListaValores.Remove(UrlIntragnoss + "items/" + pObjetoViejo);

                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, $"{UrlIntragnoss}items/{pObjetoViejo}", pTripleDeEliminacion: true, pObjetoEsUri: true);
                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", $"{UrlIntragnoss}items/{pObjetoViejo}", pTripleDeEliminacion: true, pObjetoEsUri: true);

                            pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, $"{UrlIntragnoss}items/{pObjetoViejo}"));
                        }
                        else if (mConfigService.ObtenerProcesarStringGrafo() && pProp.ListaValores.ContainsKey(pObjetoViejo.Replace("''", "\"")))
                        {
                            EliminarEntidadesAuxiliaresRelacionadas(pProp.ListaValores[pObjetoViejo.Replace("''", "\"")]);
                            pProp.ListaValores.Remove(pObjetoViejo.Replace("''", "\""));
                            if (pProp.Tipo.Equals(TipoPropiedad.ObjectProperty))
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, objetoViejoOriginal, pTripleDeEliminacion: true, pObjetoEsUri: true);
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", objetoViejoOriginal, pTripleDeEliminacion: true, pObjetoEsUri: true);

                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, objetoViejoOriginal));
                            }
                            else
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true);

                                pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo));
                            }
                        }
                        else
                        {
                            throw new Exception("No se ha encontrado '" + pObjetoViejo + "' en la lista de valores de la propiedad '" + pProp.Nombre + "'");
                        }
                    }

                }
            }
            else if (string.IsNullOrEmpty(pObjetoViejo))
            {
                if (UtilCadenas.EsMultiIdioma(pObjetoNuevo))
                {
                    string idioma = pObjetoNuevo.Substring(pObjetoNuevo.LastIndexOf("@") + 1);
                    pObjetoNuevo = pObjetoNuevo.Substring(0, pObjetoNuevo.LastIndexOf("@"));

                    if (pProp.FunctionalProperty && pProp.ListaValoresIdioma.ContainsKey(idioma) && pProp.ListaValoresIdioma[idioma].Count > 0)
                    {
                        throw new Exception("No se puede agregar '" + pObjetoNuevo + "' a una propiedad funcional, ya que actualmente tiene el valor '" + pProp.UnicoValor.Value + "'.");
                    }

                    if (!pProp.ListaValoresIdioma.ContainsKey(idioma))
                    {
                        pProp.ListaValoresIdioma.Add(idioma, new Dictionary<string, ElementoOntologia>());
                    }

                    if (!pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoNuevo))
                    {
                        pProp.ListaValoresIdioma[idioma].Add(pObjetoNuevo, null);
                    }
                    AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesInsertar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoNuevo, idioma);

                    pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoNuevo, idioma));
                }
                else
                {
                    //Agregamos el campo nuevo
                    if (pProp.FunctionalProperty)
                    {
                        if (pProp.UnicoValor.Key == null)
                        {
                            //Unico valor
                            pProp.UnicoValor = new KeyValuePair<string, ElementoOntologia>(pObjetoNuevo, null);

                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesInsertar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoNuevo, pObjetoEsUri: pProp.Tipo.Equals(TipoPropiedad.ObjectProperty));

                            pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoNuevo));
                        }
                        else
                        {
                            throw new Exception("No se puede agregar '" + pObjetoNuevo + "' a una propiedad funcional, ya que actualmente tiene el valor '" + pProp.UnicoValor.Value + "'.");
                        }
                    }
                    else
                    {
                        if (!pProp.ListaValores.ContainsKey(pObjetoNuevo))
                        {
                            pProp.ListaValores.Add(pObjetoNuevo, null);

                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesInsertar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoNuevo, pObjetoEsUri: pProp.Tipo.Equals(TipoPropiedad.ObjectProperty));

                            pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoNuevo));
                        }
                    }
                }
            }
            else
            {
                //1º: Eliminamos el valor viejo de la propiedad:
                if (UtilCadenas.EsMultiIdioma(pObjetoViejo))
                {
                    string idioma = pObjetoViejo.Substring(pObjetoViejo.LastIndexOf("@") + 1);
                    pObjetoViejo = pObjetoViejo.Substring(0, pObjetoViejo.LastIndexOf("@"));

                    if (pProp.ListaValoresIdioma.ContainsKey(idioma) && pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoViejo))
                    {
                        pProp.ListaValoresIdioma[idioma].Remove(pObjetoViejo);

                        if (pProp.ListaValoresIdioma[idioma].Count == 0)
                        {
                            pProp.ListaValoresIdioma.Remove(idioma);
                        }
                        AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, idioma, pTripleDeEliminacion: true);

                        pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo, idioma));
                    }
                    else if (mConfigService.ObtenerProcesarStringGrafo() && pProp.ListaValoresIdioma.ContainsKey(idioma) && pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoViejo.Replace("''", "\"")))
                    {
                        pProp.ListaValoresIdioma[idioma].Remove(pObjetoViejo.Replace("''", "\""));

                        if (pProp.ListaValoresIdioma[idioma].Count == 0)
                        {
                            pProp.ListaValoresIdioma.Remove(idioma);
                        }

                        AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, idioma, pTripleDeEliminacion: true);

                        pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo, idioma));
                    }
                    else
                    {
                        throw new Exception("MultiIdioma: No se ha encontrado '" + pObjetoViejo + "' en la lista de valores de la propiedad '" + pProp.Nombre + "'");
                    }
                }
                else
                {
                    if (pProp.FunctionalProperty)
                    {
                        if (pProp.UnicoValor.Key == null && pProp.ListaValoresIdioma.Count() > 0)
                        {
                            throw new Exception("No se ha enviado el idioma para '" + pObjetoViejo + "' y es una propiedad funcional");
                        }
                        if (pProp.Tipo == TipoPropiedad.ObjectProperty)
                        {
                            if (!string.IsNullOrEmpty(pProp.Rango))
                            {
                                mEntidadesABorrar.Add(pProp.UnicoValor.Key);
                                mURIEntidadesABorrar.Add(pProp.UnicoValor.Key, pProp.UnicoValor.Key);
                            }

                            if (pProp.UnicoValor.Value != null)
                            {
                                //Borro Entidad Vieja
                                if (!pProp.ElementoOntologia.EntidadesRelacionadas.Remove(pProp.UnicoValor.Value))
                                {
                                    //Si no se ha borrado de la lista se lanza una excepción.
                                    throw new Exception("Funcional: No se ha encontrado '" + pObjetoViejo + "' en la propiedad '" + pProp.Nombre + "'");
                                }
                            }
                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pProp.UnicoValor.Key, pTripleDeEliminacion: true, pObjetoEsUri: true);
                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", pProp.UnicoValor.Key, pTripleDeEliminacion: true, pObjetoEsUri: true);
                        }
                        else
                        {
                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pProp.UnicoValor.Key, pTripleDeEliminacion: true);
                        }

                        pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pProp.UnicoValor.Key));

                        //Unico valor
                        pProp.UnicoValor = new KeyValuePair<string, ElementoOntologia>(null, null);
                    }
                    else
                    {
                        //ListaValores
                        if (pProp.ListaValores.ContainsKey(pObjetoViejo))
                        {
                            if (pProp.Tipo == TipoPropiedad.ObjectProperty)
                            {
                                if (!string.IsNullOrEmpty(pProp.Rango))
                                {
                                    mEntidadesABorrar.Add(pObjetoViejo);
                                    mURIEntidadesABorrar.Add(pObjetoViejo, pObjetoViejo);
                                }

                                if (pProp.ListaValores[pObjetoViejo] != null)
                                {
                                    //Borro Entidad Vieja
                                    pProp.ElementoOntologia.EntidadesRelacionadas.Remove(pProp.ListaValores[pObjetoViejo]);
                                }
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true, pObjetoEsUri: true);
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", pObjetoViejo, pTripleDeEliminacion: true, pObjetoEsUri: true);
                            }
                            else
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true);
                            }

                            pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo));

                            pProp.ListaValores.Remove(pObjetoViejo);
                        }
                        else if (mConfigService.ObtenerProcesarStringGrafo() && pProp.ListaValores.ContainsKey(pObjetoViejo.Replace("''", "\"")))
                        {
                            if (pProp.Tipo == TipoPropiedad.ObjectProperty)
                            {
                                if (!string.IsNullOrEmpty(pProp.Rango))
                                {
                                    mEntidadesABorrar.Add(pObjetoViejo);
                                    mURIEntidadesABorrar.Add(pObjetoViejo, pObjetoViejo);
                                }

                                if (pProp.ListaValores[pObjetoViejo.Replace("''", "\"")] != null)
                                {
                                    //Borro Entidad Vieja
                                    pProp.ElementoOntologia.EntidadesRelacionadas.Remove(pProp.ListaValores[pObjetoViejo.Replace("''", "\"")]);
                                }
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true, pObjetoEsUri: true);
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", pObjetoViejo, pTripleDeEliminacion: true, pObjetoEsUri: true);
                            }
                            else
                            {
                                AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoViejo, pTripleDeEliminacion: true);
                            }

                            pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoViejo));

                            pProp.ListaValores.Remove(pObjetoViejo);
                        }
                        else if (pProp.ListaValores.ContainsKey(UrlIntragnoss + "items/" + pObjetoViejo))
                        {
                            //es entidad externa basta con borrarlo de la lista de valores
                            pProp.ListaValores.Remove(UrlIntragnoss + "items/" + pObjetoViejo);

                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, $"{UrlIntragnoss}items/{pObjetoViejo}", pTripleDeEliminacion: true, pObjetoEsUri: true);
                            AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesEliminar, $"{UrlIntragnoss}{pDocID.ToString().ToLower()}", "http://gnoss/hasEntidad", $"{UrlIntragnoss}items/{pObjetoViejo}", pTripleDeEliminacion: true, pObjetoEsUri: true);

                            pSbTriplesEliminarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, $"{UrlIntragnoss + "items/" + pObjetoViejo}"));
                        }
                        else
                        {
                            throw new Exception("No se ha encontrado '" + pObjetoViejo + "' en la lista de valores de la propiedad '" + pProp.Nombre + "'");
                        }
                    }
                }

                //2º: Agrego el nuevo valor a la propiedad:
                if (UtilCadenas.EsMultiIdioma(pObjetoNuevo))
                {
                    string idioma = pObjetoNuevo.Substring(pObjetoNuevo.LastIndexOf("@") + 1);
                    pObjetoNuevo = pObjetoNuevo.Substring(0, pObjetoNuevo.LastIndexOf("@"));

                    if (!pProp.ListaValoresIdioma.ContainsKey(idioma))
                    {
                        pProp.ListaValoresIdioma.Add(idioma, new Dictionary<string, ElementoOntologia>());
                    }

                    if (!pProp.ListaValoresIdioma[idioma].ContainsKey(pObjetoNuevo))
                    {
                        pProp.ListaValoresIdioma[idioma].Add(pObjetoNuevo, null);

                        AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesInsertar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoNuevo, idioma);

                        pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoNuevo, idioma));
                    }
                }
                else
                {
                    if (pProp.FunctionalProperty)
                    {
                        //Unico valor
                        pProp.UnicoValor = new KeyValuePair<string, ElementoOntologia>(pObjetoNuevo, null);
                    }
                    else
                    {
                        if (!pProp.ListaValores.ContainsKey(pObjetoNuevo))
                        {
                            pProp.ListaValores.Add(pObjetoNuevo, null);
                        }
                    }

                    AniadirTripleGrafoOntologiaAStringBuilder(pSbTriplesInsertar, pProp.ElementoOntologia.Uri, pProp.NombreFormatoUri, pObjetoNuevo, pObjetoEsUri: pProp.Tipo.Equals(TipoPropiedad.ObjectProperty));

                    pSbTriplesInsertarBusqueda.AppendLine(GenerarTripleBusqueda(pDocID, pEsEntidadPrincipal, pProp, pObjetoNuevo));
                }
            }
        }

        private void AniadirTripleGrafoOntologiaAStringBuilder(GnossStringBuilder pSb, string pSujeto, string pPredicado, string pObjeto, string pIdioma = null, bool pTripleDeEliminacion = false, bool pObjetoEsUri = false)
        {
            if (pObjetoEsUri)
            {
                pSb.AppendLine($"<{pSujeto}> <{pPredicado}> <{pObjeto}> . ");
            }
            else
            {
                //Uri uri = null;
                //Uri.TryCreate(pObjeto, UriKind.Absolute, out uri);

                //if (uri != null && pObjeto.Contains("http://") && !pObjeto.Contains("|") && !pObjeto.Contains(" ") && !pObjeto.Contains(","))
                //{
                //    pSb.AppendLine($"<{pSujeto}> <{pPredicado}> <{pObjeto}> . ");
                //}
                //else
                {
                    pSb.AppendLine($"<{pSujeto}> <{pPredicado}> {GenerarObjetoStringParaTriple(pObjeto, pIdioma)} . ");

                    if (pTripleDeEliminacion && pObjeto.StartsWith("http") && Uri.IsWellFormedUriString(pObjeto, UriKind.Absolute))
                    {
                        // Es posible que en el grafo esté guardado como una URI, añado también un triple de eliminación para la URI
                        pSb.AppendLine($"<{pSujeto}> <{pPredicado}> <{pObjeto}> . ");
                    }
                }
            }
        }

        private string GenerarObjetoStringParaTriple(string pObjeto, string pIdioma = null)
        {
            if (!string.IsNullOrEmpty(pIdioma) && (!pIdioma.StartsWith("@")))
            {
                pIdioma = $"@{pIdioma}";
            }

            return $"\"\"\"{pObjeto.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"\"\"{pIdioma}";
        }

        private string GenerarTripleBusqueda(Guid pDocID, bool pEsEntidadPrincipal, Propiedad pPropiedad, string pValor, string pIdioma = null)
        {
            string sujetoBusqueda = $"http://gnoss/{pDocID.ToString().ToUpper()}";
            if (!pEsEntidadPrincipal)
            {
                sujetoBusqueda = pPropiedad.ElementoOntologia.Uri.ToLowerSearchGraph();
            }

            string idioma = null;
            if (!string.IsNullOrEmpty(pIdioma))
            {
                idioma = $"@{pIdioma}";
            }

            string valor = pValor;
            int auxInt;
            long auxLong;
            float auxFloat;
            if (pPropiedad.Tipo.Equals(TipoPropiedad.ObjectProperty))
            {
                var listaFacetasExternasCoinciden = ListaFacetasExternas.Where(item => pValor.StartsWith(item));
                bool esObjetoExterno = false;
                foreach (string entidadID in listaFacetasExternasCoinciden)
                {
                    string uriEntidad = entidadID;
                    if (!uriEntidad.EndsWith("_"))
                    {
                        uriEntidad += "_";
                    }
                    Regex regex = new Regex(@"(?im)" + uriEntidad + @"[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}_[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}");

                    if (regex.IsMatch(pValor))
                    {
                        esObjetoExterno = true;
                        break;
                    }
                }

                if (esObjetoExterno)
                {
                    string idCorto = pValor.Substring(0, pValor.LastIndexOf('_'));
                    idCorto = idCorto.Substring(idCorto.LastIndexOf('_') + 1);

                    valor = $"<http://gnoss/{idCorto.ToUpper()}>";
                }
                else if (EsIDGnoss(pValor))
                {
                    valor = $"<{pValor}>";
                }
                else
                {
                    valor = $"<{pValor.ToLowerSearchGraph()}>";
                }
            }
            else if ((pPropiedad.RangoEsEntero || pPropiedad.RangoEsNumerico || pPropiedad.RangoEsFecha) && long.TryParse(valor, out auxLong))
            {
                if (int.TryParse(valor, out auxInt))
                {
                    valor = $"\"{pValor}\"^^<http://www.w3.org/2001/XMLSchema#int>";
                }
                else
                {
                    valor = $"\"{pValor}\"^^<http://www.w3.org/2001/XMLSchema#long>";
                }

            }
            else if (pPropiedad.RangoEsFloat && float.TryParse(valor, out auxFloat))
            {
                valor = $"\"{pValor.Replace(",", ".")}\"^^<http://www.w3.org/2001/XMLSchema#decimal>";
            }
            else if (FacetadoAD.TIPOS_GEOMETRIA.Any(item => valor.ToLower().StartsWith($"{item}(")))
            {
                valor = "\"" + pValor + "\"^^<http://www.openlinksw.com/schemas/virtrdf#Geometry>";
            }
            else
            {


                if (!ListaFacetasTextoInvariable.Any(item => item.Equals(pPropiedad.NombreConNamespace)))
                {
                    valor = valor.ToLowerSearchGraph();
                }
                valor = GenerarObjetoStringParaTriple(valor, idioma);
            }

            return $"<{sujetoBusqueda}> <{pPropiedad.NombreFormatoUri}> {valor} . ";
        }

        private bool EsIDGnoss(string pObjeto)
        {
            Guid aux = Guid.Empty;

            return pObjeto.StartsWith("http://gnoss/") && Guid.TryParse(pObjeto.Substring("http://gnoss/".Length), out aux);
        }

        private DataWrapperFacetas ObtenerFacetasExternasDeProyecto()
        {
            if (!mFacetaDSPorProyectoID.ContainsKey(FilaProy.ProyectoID))
            {
                FacetaCN facetaCN = new FacetaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetaCN>(), mLoggerFactory);
                DataWrapperFacetas facetaDW = facetaCN.ObtenerFacetaObjetoConocimientoProyecto(FilaProy.OrganizacionID, FilaProy.ProyectoID, true);

                mFacetaDSPorProyectoID.TryAdd(FilaProy.ProyectoID, facetaDW);
            }
            return mFacetaDSPorProyectoID[FilaProy.ProyectoID];
        }

        /// <summary>
        /// Si el inicio del predicado coincide con la URL de la ontología de GNOSS, hay que devolver solo el nombre de la propiedad, no toda la URL
        /// </summary>
        /// <param name="pPredicado">Predicado a comprobar</param>
        /// <returns>URL o Nombre de la propiedad</returns>
        private string ObtenerPredicadoCorrectoSegunOntologia(string pPredicado, string pUrlOntologia)
        {
            string predicadoFinal = pPredicado;
            if (pPredicado.StartsWith(pUrlOntologia))
            {
                predicadoFinal = pPredicado.Substring(pPredicado.IndexOf(pUrlOntologia));
            }

            return predicadoFinal;
        }

        private void ModificarNombreFicheroPorNombreFicheroGoogleDrive(Ontologia pOntologia, Guid pDocID, string pNewFileName, string pOldFileName, List<ElementoOntologia> pInstanciasPrincipales)
        {
            foreach (ElementoOntologia eo in pInstanciasPrincipales)
            {
                foreach (Propiedad prop in eo.Propiedades)
                {
                    if (prop.Tipo == TipoPropiedad.DatatypeProperty)
                    {
                        if (prop.FunctionalProperty)
                        {
                            //Solo modificamos si el id es el mismo que el de la propiedad.
                            if (pOldFileName.Equals(prop.UnicoValor.Key))
                            {
                                //modificarInstanciaPorNivel(pOntologia, eo.Propiedades, pDocID, prop.Nombre, pOldFileName, pNewFileName);
                                prop.UnicoValor = new KeyValuePair<string, ElementoOntologia>(pNewFileName, null);
                            }
                        }
                        else
                        {
                            if (prop.ListaValores.ContainsKey(pOldFileName))
                            {
                                prop.ListaValores.Remove(pOldFileName);
                                prop.ListaValores.Add(pNewFileName, null);
                            }
                        }
                    }
                }

                ModificarNombreFicheroPorNombreFicheroGoogleDrive(pOntologia, pDocID, pNewFileName, pOldFileName, eo.EntidadesRelacionadas);
            }
        }

        private bool ReemplazarValorPropiedadEntidad(string pAntiguoValor, string pNuevoValor, List<ElementoOntologia> pInstanciasPrincipales, string pIdioma)
        {
            foreach (ElementoOntologia entidad in pInstanciasPrincipales)
            {
                foreach (Propiedad propiedad in entidad.Propiedades)
                {
                    if (propiedad.ElementoOntologia == null)
                    {
                        propiedad.ElementoOntologia = entidad;
                    }

                    if (propiedad.Tipo == TipoPropiedad.DatatypeProperty && propiedad.EspecifPropiedad.TipoCampo == TipoCampoOntologia.ArchivoLink)
                    {
                        if (string.IsNullOrEmpty(pIdioma))
                        {
                            if (propiedad.ValoresUnificados.ContainsKey(pAntiguoValor))
                            {
                                propiedad.LimpiarValor();
                                propiedad.DarValor(pNuevoValor, null);
                                return true;
                            }
                            else if (propiedad.ValoresUnificados.ContainsKey(pNuevoValor))
                            {
                                //Devolvemos true pero no es necesario modificar el rdf porque ya tiene el valor que deseamos
                                return true; 
                            }
                        }
                        else if (propiedad.ListaValoresIdioma.ContainsKey(pIdioma))
                        {
                            if (propiedad.ListaValoresIdioma[pIdioma].ContainsKey(pAntiguoValor))
                            {
                                propiedad.ListaValoresIdioma[pIdioma].Clear();
                                propiedad.ListaValoresIdioma[pIdioma].Add(pNuevoValor, null);
                                return true;
                            }
                            else if (propiedad.ListaValoresIdioma[pIdioma].ContainsKey(pNuevoValor))
                            {
                                //Devolvemos true pero no es necesario modificar el rdf porque ya tiene el valor que deseamos
                                return true;
                            }
                        }
                    }
                }

                if (ReemplazarValorPropiedadEntidad(pAntiguoValor, pNuevoValor, entidad.EntidadesRelacionadas, pIdioma))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new version of the document and puts it as main document of the page.
        /// </summary>
        /// <returns>Created version. Null if any error occurs</returns>
        private Elementos.Documentacion.Documento CreateDocumentVersion(Elementos.Documentacion.Documento pDocument, Identidad pIdentity)
        {
            Elementos.Documentacion.Documento nuevoDoc = pDocument.GestorDocumental.CrearNuevaVersionDocumento(pDocument, true, pIdentity);

            if (nuevoDoc != null)
            {
                if (!pDocument.GestorDocumental.ListaDocumentos.ContainsKey(nuevoDoc.Clave))
                {
                    pDocument.GestorDocumental.ListaDocumentos.Add(nuevoDoc.Clave, nuevoDoc);
                }

                pDocument.FilaDocumento.FechaModificacion = DateTime.Now;
            }
            return nuevoDoc;
        }

        private void ReplaceTags(List<string> pTags, DocumentoWeb pDocumentoEdicion, string pTextoTagsAutomaticosTitulo, string pTextoTagsAutomaticosDescripcion)
        {
            if (!string.IsNullOrEmpty(pTextoTagsAutomaticosTitulo) || !string.IsNullOrEmpty(pTextoTagsAutomaticosDescripcion))
            {
                List<string> tagsAuto = ObtenerEtiquetasAutomaticas(pTextoTagsAutomaticosTitulo, pTextoTagsAutomaticosDescripcion, FilaProy.ProyectoID);

                foreach (string tagAuto in tagsAuto)
                {
                    if (!pTags.Contains(tagAuto))
                    {
                        pTags.Add(tagAuto);
                    }
                }
            }

            pDocumentoEdicion.ListaTagsSoloLectura = pTags;
            pDocumentoEdicion.FilaDocumento.Tags = UtilCadenas.CadenaFormatoTexto(pTags);
        }

        private void ReplaceCategories(List<Guid> pCategoriaIDs, GestorDocumental pGestorDoc, DocumentoWeb pDocumentoEdicion, Guid pIdentidadID)
        {
            if (pGestorDoc != null && pDocumentoEdicion != null)
            {
                List<CategoriaTesauro> listaCategorias = new List<CategoriaTesauro>();

                if (pCategoriaIDs != null)
                {
                    foreach (Guid clave in pCategoriaIDs)
                    {
                        CategoriaTesauro categoria = pGestorDoc.GestorTesauro.ListaCategoriasTesauro[clave];
                        listaCategorias.Add(categoria);
                    }
                }

                List<CategoriaTesauro> listaCategoriasNuevas = new List<CategoriaTesauro>();
                List<CategoriaTesauro> listaCategoriasEliminadas = new List<CategoriaTesauro>();

                foreach (CategoriaTesauro categoria in listaCategorias)
                {
                    if (!pDocumentoEdicion.Categorias.ContainsKey(categoria.Clave))
                    {
                        listaCategoriasNuevas.Add(categoria);
                    }
                }

                foreach (CategoriaTesauro categoria in pDocumentoEdicion.Categorias.Values)
                {
                    if (!listaCategorias.Contains(categoria))
                    {
                        listaCategoriasEliminadas.Add(categoria);
                    }
                }

                pGestorDoc.VincularDocumentoACategorias(listaCategoriasNuevas, pDocumentoEdicion, pIdentidadID, FilaProy.ProyectoID);
                pGestorDoc.DesvincularDocumentoDeCategorias(pDocumentoEdicion, listaCategoriasEliminadas, pIdentidadID, FilaProy.ProyectoID);

                pDocumentoEdicion.RecargarCategoriasTesauro();
            }
        }

        /// <summary>
        /// Extracts a list with the images identifiers
        /// </summary>
        /// <param name="resource_attached_files">resource attached files</param>
        /// <returns>String list with images identifiers</returns>
        private List<string> ExtractImageIds(List<AttachedResource> resource_attached_files)
        {
            List<string> idsImagenes = new List<string>();

            if (resource_attached_files != null)
            {
                foreach (AttachedResource adjunto in resource_attached_files)
                {
                    string nombre = adjunto.file_rdf_property;

                    if (!string.IsNullOrEmpty(nombre) && nombre.Contains("[IMGPrincipal]"))
                    {
                        nombre = nombre.Substring(nombre.LastIndexOf(']') + 1);
                    }

                    if (adjunto.file_property_type.Equals((short)AttachedResourceFilePropertyTypes.image) && !idsImagenes.Contains(nombre))
                    {
                        idsImagenes.Add(nombre);
                    }
                }
            }

            return idsImagenes;
        }

        private void EliminarArchivosDelRDFExceptoLista(Guid pDocumentoID, List<string> pListaIdsMantener)
        {
            if (pListaIdsMantener.Count > 0)
            {
                ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
                servicioImagenes.Url = UrlServicioImagenes;

                string directorio = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticasAntiguo + "\\" + pDocumentoID.ToString();

                //obtiene todas las imágenes
                string[] imagenes = servicioImagenes.ObtenerIDsImagenesPorNombreImagen(directorio, string.Empty);

                if (imagenes != null)
                {
                    foreach (string imagen in imagenes)
                    {
                        if (!pListaIdsMantener.Contains(imagen))
                        {
                            if (!servicioImagenes.BorrarImagenDeDirectorio(UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticasAntiguo + "\\" + pDocumentoID.ToString() + "\\" + imagen))
                            {
                                throw new GnossException("Error deleting image", HttpStatusCode.BadRequest);
                            }
                        }
                    }
                }

                directorio = UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticas + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(pDocumentoID);

                //obtiene todas las imágenes
                imagenes = servicioImagenes.ObtenerIDsImagenesPorNombreImagen(directorio, string.Empty);

                if (imagenes != null)
                {
                    foreach (string imagen in imagenes)
                    {
                        if (!pListaIdsMantener.Contains(imagen))
                        {
                            if (!servicioImagenes.BorrarImagenDeDirectorio(UtilArchivos.ContentImagenesDocumentos + "\\" + UtilArchivos.ContentImagenesSemanticas + "\\" + UtilArchivos.DirectorioDocumentoFileSystem(pDocumentoID) + "\\" + imagen))
                            {
                                throw new GnossException("Error deleting image", HttpStatusCode.BadRequest);
                            }
                        }
                    }
                }
            }
        }

        private byte[] ObtenerRDFDeVirtuosoControlCheckpoint(Guid pDocumentoID, string pNombreOntologia, string pUrlOntologia, string pNamespaceOntologia, Ontologia pOntologia, bool pTraerEntidadesExternas = false)
        {
            byte[] resultado = null;
            int intentos = 0;

            try
            {
                resultado = ControladorDocumentacion.ObtenerRDFDeVirtuoso(pDocumentoID, pNombreOntologia, UrlIntragnoss, pUrlOntologia, pNamespaceOntologia, pOntologia, "acid", pTraerEntidadesExternas);

                while (resultado == null && intentos < 5)
                {
                    // Lo reintento de nuevo, es posible que todavía no se haya replicado
                    intentos++;
                    Thread.Sleep(1000);
                    resultado = ControladorDocumentacion.ObtenerRDFDeVirtuoso(pDocumentoID, pNombreOntologia, UrlIntragnoss, pUrlOntologia, pNamespaceOntologia, pOntologia, "acid", pTraerEntidadesExternas);
                }

                if (resultado == null)
                {
                    throw new Exception($"El RDF del recurso {pDocumentoID} no existe en virtuoso. Ontología {pUrlOntologia}");
                }
            }
            catch (Exception)
            {
                if (intentos == 5)
                {
                    // La excepción la hemos producido nosotros, simplemente la relanzamos
                    throw;
                }
                //Cerramos las conexiones
                ControladorConexiones.CerrarConexiones();

                DateTime horaActual = DateTime.Now;

                //Realizamos una consulta ask a virtuoso para comprobar si está funcionando
                while (!UtilidadesVirtuoso.ServidorOperativo("acid", UrlIntragnoss) && DateTime.Now > horaActual.AddMinutes(2))
                {
                    //Dormimos 30 segundos
                    Thread.Sleep(30 * 1000);
                }

                resultado = ControladorDocumentacion.ObtenerRDFDeVirtuoso(pDocumentoID, pNombreOntologia, UrlIntragnoss, pUrlOntologia, pNamespaceOntologia, pOntologia, "acid", pTraerEntidadesExternas);

                while (resultado == null && intentos < 5)
                {
                    // Lo reintento de nuevo, es posible que todavía no se haya replicado
                    intentos++;
                    Thread.Sleep(1000);
                    resultado = ControladorDocumentacion.ObtenerRDFDeVirtuoso(pDocumentoID, pNombreOntologia, UrlIntragnoss, pUrlOntologia, pNamespaceOntologia, pOntologia, "acid", pTraerEntidadesExternas);
                }

                if (resultado == null)
                {
                    throw new Exception($"El RDF del recurso {pDocumentoID} no existe en virtuoso. Ontología {pUrlOntologia}");
                }
            }

            return resultado;
        }

        private void ModificarSubtipoRecursoInt(ModifyResourceSubtype parameters)
        {
            //Cargar documento y obtener campos
            Configuracion.ObtenerDesdeFicheroConexion = true;
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            GestorDocumental gestorDocumental = new GestorDocumental(docCN.ObtenerDocumentoPorID(parameters.resource_id), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException($"The resource {parameters.resource_id} does not exist in the community {parameters.community_short_name}", HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = null;
            if (parameters.user_id.HasValue && !parameters.user_id.Value.Equals(Guid.Empty))
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, parameters.user_id.Value, true);
            }
            else
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            }
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(parameters.resource_id);
            docCN.ObtenerVersionDocumentosPorIDs(gestorDoc.DataWrapperDocumentacion, listaDocs, true);
            gestorDoc.CargarDocumentos(false);

            Elementos.Documentacion.Documento documentoAntiguo = gestorDoc.ListaDocumentos[parameters.resource_id];
            string antiguoTitulo = documentoAntiguo.Titulo;
            string antiguaDescripcion = documentoAntiguo.Descripcion;
            string antiguaNombreCategoriaDoc = documentoAntiguo.FilaDocumento.NombreCategoriaDoc;
            DateTime antiguaFechaModificacion = documentoAntiguo.FilaDocumento.FechaModificacion.Value;

            #region Carga identidades documento

            AgregarIdentidadesEditorasRecurso(documentoAntiguo, gestorDoc, identidad);

            #endregion

            bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

            if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, identidad.IdentidadOrganizacion, Proyecto, Guid.Empty, false))
            {
                throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
            }

            Guid ontologiaID = documento.ElementoVinculadoID;

            if (!documento.GestorDocumental.ListaDocumentos.ContainsKey(ontologiaID))
            {
                documento.GestorDocumental.DataWrapperDocumentacion.Merge(docCN.ObtenerDocumentoPorID(ontologiaID));
                docCN.Dispose();
                documento.GestorDocumental.CargarDocumentos(false);
            }

            //Agrego namespaces y urls:
            string nombreOntologia = documento.GestorDocumental.ListaDocumentos[ontologiaID].FilaDocumento.Enlace;
            string urlOntologia = $"{BaseURLFormulariosSem}/Ontologia/{nombreOntologia}#";

            GestionOWL gestorOWL = new GestionOWL();
            gestorOWL.UrlOntologia = urlOntologia;
            gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
            GestionOWL.URLIntragnoss = UrlIntragnoss;


            try
            {
                FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
                string grafoOntología = $"<{UrlIntragnoss}{nombreOntologia}>";

                FacetadoDS facetadoDS = facetadoAD.LeerDeVirtuoso($"SELECT ?s FROM {grafoOntología} WHERE {{<{UrlIntragnoss}{parameters.resource_id}> <http://gnoss/hasEntidad> ?s. ?s rdf:type <{parameters.previous_type}>}}", "Uri", nombreOntologia, true);
                string sujeto = null;
                if (facetadoDS.Tables["Uri"].Rows.Count > 0)
                {
                    sujeto = (string)facetadoDS.Tables["Uri"].Rows[0][0];
                }
                else
                {
                    throw new GnossException($"The resource {parameters.resource_id} doesn't has triples in virtuoso", HttpStatusCode.BadRequest);
                }

                //Grafo de ontología
                string eliminarGrafoOntologia = $"DELETE FROM GRAPH {grafoOntología} {{?s ?p ?o}} FROM {grafoOntología} WHERE {{?s ?p ?o filter(?p in(<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>, <http://www.w3.org/2000/01/rdf-schema#label>) AND ?s = <{sujeto}>)}}";
                string insertarGrafoOntologia = $"INSERT DATA INTO {grafoOntología} {{<{sujeto}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <{parameters.subtype}> . {Environment.NewLine} <{sujeto}> <http://www.w3.org/2000/01/rdf-schema#label> \"{parameters.subtype}\" .}}";


                facetadoAD.ActualizarVirtuoso(eliminarGrafoOntologia, nombreOntologia);
                facetadoAD.ActualizarVirtuoso(insertarGrafoOntologia, nombreOntologia);

                //Grafo de búsqueda
                string grafoBusqueda = $"<{UrlIntragnoss}{documento.ProyectoID.ToString().ToLower()}>";
                string sujetoBusqueda = $"<http://gnoss/{parameters.resource_id.ToString().ToUpper()}>";

                string eliminarGrafoBusqueda = $"DELETE FROM GRAPH {grafoBusqueda} {{?s ?p ?o}} FROM {grafoBusqueda} WHERE {{?s ?p ?o filter(?p in (<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>, <http://gnoss/type>) AND ?s = {sujetoBusqueda})}}";
                string insertarGrafoBusqueda = $"INSERT DATA INTO {grafoBusqueda} {{{sujetoBusqueda} <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> \"{nombreOntologia.Replace(".owl", "")}\" . {Environment.NewLine} {sujetoBusqueda} <http://gnoss/type> \"{parameters.subtype}\" .}}";

                facetadoAD.ActualizarVirtuoso(eliminarGrafoBusqueda, documento.ProyectoID.ToString());
                facetadoAD.ActualizarVirtuoso(insertarGrafoBusqueda, documento.ProyectoID.ToString());

                //Elimina el rdf del recurso modificado de base de datos
                ControladorDocumentacion.BorrarRDFDeBDRDF(parameters.resource_id);

                //Reprocesa el recurso por el base
                ControladorDocumentacion.AgregarRecursoModeloBaseSimple(parameters.resource_id, documento.ProyectoID, (short)documento.TipoDocumentacion, null, "", PrioridadBase.Alta, false, -1, (short)TiposElementosEnCola.InsertadoEnGrafoBusquedaDesdeWeb, mAvailableServices);

                //Borra cache del recurso modificado
                try
                {
                    //anular cache del documento
                    ControladorDocumentacion.BorrarCacheControlFichaRecursos(parameters.resource_id);

                    //borrar cache recursos
                    FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                    facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(FilaProy.ProyectoID, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));
                }
                catch (Exception)
                {
                    mLoggingService.GuardarLogError($"Error al eliminar los datos de la cache del recurso {parameters.resource_id}", mlogger);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError($"Error al actualizar virtuoso: {ex.Message}", mlogger);
                throw new GnossException($"Error: {ex.Message}", HttpStatusCode.InternalServerError);
            }

        }

        private List<ElementoOntologia> ModificarListaDeTripletasPorRecursoInt(ModifyResourceTripleListParams parameters)
        {
            bool usarColaReplicacion = false;
            if (FilaProy != null)
            {
                ComprobacionCambiosCachesLocales(FilaProy.ProyectoID);
            }

            PrioridadBase prioridadBase = PrioridadBase.ApiRecursos;
            if (parameters.end_of_load)
            {
                prioridadBase = PrioridadBase.ApiRecursosBorrarCache;
            }

            //Cargar Documento y obtener campos
            Configuracion.ObtenerDesdeFicheroConexion = true;
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            AD.EntityModel.Models.Documentacion.Documento documento = docCN.ObtenerDocumentoPorIdentificador(parameters.resource_id);
            AD.EntityModel.Models.Documentacion.Documento documentoOntologia = docCN.ObtenerDocumentoPorIdentificador(documento.ElementoVinculadoID.Value);

            if (!documento.ProyectoID.Equals(FilaProy.ProyectoID))
            {
                throw new GnossException($"The resource {parameters.resource_id} does not exist in the community {parameters.community_short_name}", HttpStatusCode.BadRequest);
            }

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = null;
            if (parameters.user_id.HasValue && !parameters.user_id.Value.Equals(Guid.Empty))
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, parameters.user_id.Value, true);
            }
            else
            {
                identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            }
            docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
            List<Guid> listaDocs = new List<Guid>();
            listaDocs.Add(parameters.resource_id);
            docCN.ObtenerVersionDocumentosPorIDs(gestorDoc.DataWrapperDocumentacion, listaDocs, true);
            gestorDoc.CargarDocumentos(false);

            Elementos.Documentacion.Documento documentoAntiguo = gestorDoc.ListaDocumentos[parameters.resource_id];

            DateTime antiguaFechaModificacion = documentoAntiguo.FilaDocumento.FechaModificacion.Value;

            #region Carga identidades documento

            AgregarIdentidadesEditorasRecurso(documentoAntiguo, gestorDoc, identidad);

            #endregion

            bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);
            GnossStringBuilder sbTriplesInsertar = new GnossStringBuilder();
            GnossStringBuilder sbTriplesEliminar = new GnossStringBuilder();
            GnossStringBuilder sbTriplesInsertarBusqueda = new GnossStringBuilder();
            GnossStringBuilder sbTriplesEliminarBusqueda = new GnossStringBuilder();
            List<ElementoOntologia> entidadesPrinBorrar = new List<ElementoOntologia>();
            List<string> listaSujetosBorrar = new List<string>();
            List<string> listaSujetosBorrarBusqueda = new List<string>();
            List<ElementoOntologia> instanciasPrincipales = null;

            if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, identidad.IdentidadOrganizacion, Proyecto, Guid.Empty, false))
            {
                throw new GnossException("The OAuth user has no permission on resource editing.", HttpStatusCode.BadRequest);
            }

            bool documentoBloqueado = ComprobarDocumentoEnEdicion(parameters.resource_id, identidad.Clave);
            try
            {
                //Primero obtener RDf del recurso
                Guid ontologiaID = documento.ElementoVinculadoID.Value;

                //Agrego namespaces y urls:
                string nombreOntologia = documentoOntologia.Enlace;
                string urlOntologia = $"{BaseURLFormulariosSem}/Ontologia/{nombreOntologia}#";
                nombreOntologia = nombreOntologia.ToLower();

                GestionOWL gestorOWL = new GestionOWL();
                gestorOWL.UrlOntologia = urlOntologia;
                gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
                GestionOWL.URLIntragnoss = UrlIntragnoss;

                //Obtengo la ontología y su archivo de configuración:
                Ontologia ontologia = ObtenerOntologia(ontologiaID);

                string rdfTexto = null;

                //Obtenemos el RDF de la BD.
                RdfDS rdfDS = ControladorDocumentacion.ObtenerRDFDeBDRDF(parameters.resource_id, documento.ProyectoID.Value);
                if (rdfDS.RdfDocumento.Count > 0)
                {
                    rdfTexto = rdfDS.RdfDocumento[0].RdfSem;
                }

                // RDF desde virtuoso
                if (string.IsNullOrEmpty(rdfTexto))
                {
                    MemoryStream buffer = new MemoryStream(ObtenerRDFDeVirtuosoControlCheckpoint(parameters.resource_id, documentoOntologia.Enlace, urlOntologia, gestorOWL.NamespaceOntologia, ontologia));
                    StreamReader reader = new StreamReader(buffer);
                    rdfTexto = reader.ReadToEnd();
                    reader.Close();
                    reader.Dispose();

                    //Si no hay un RDF en BD, le damos un valor nulo para que inserte al final el nuevo.
                    rdfDS.Dispose();
                    rdfDS = null;
                }

                List<ElementoOntologia> entidadesPrincAntiguas = gestorOWL.LeerFicheroRDF(ontologia, rdfTexto, true);

                if (!rdfTexto.Contains("xmlns:gnossonto="))
                {
                    gestorOWL.NamespaceOntologia = gestorOWL.NamespacesRDFLeyendo.FirstOrDefault(item => item.Value.Equals(gestorOWL.UrlOntologia)).Key;
                }
                instanciasPrincipales = gestorOWL.LeerFicheroRDF(ontologia, rdfTexto, true);

                #region Modificación de los campos

                mEntidadesABorrar = new List<string>();
                mURIEntidadesABorrar = new Dictionary<string, string>();

                //Modificar o añadir la línea
                //Diferentes casos:
                //* Propiedad Datos

                //** Funcional: El valor se encuentra en Único Valor
                //** No Funcional: GHabrá una lista de valores y en caa valor se encuentra los valores a modificar

                //* Propiedad OPbjeto (Tiene niveles hijos)
                //** Funcional: Dentro de único Valor se encuentra el ID del hijo y una propiedad Hija con valores, donde se encuentra el que queremso modificar, dentro de los campos a cambiar igual que en datos.
                //** No Funcional: Dentro de ListaVariables se encuentran diferentes propiedades y dentro de ellas tendremos los campos a cambiar igual que en datos.

                foreach (ModifyResourceTriple triple in parameters.resource_triples)
                {
                    string descripcion = "";
                    switch (triple.gnoss_property)
                    {
                        case GnossResourceProperty.title:
                            if (!string.IsNullOrEmpty(triple.new_object))
                            {
                                //Modificamos el título en el modelo ACID
                                Dictionary<string, string> titulosAntiguos = UtilCadenas.ObtenerTextoPorIdiomas(documentoAntiguo.Titulo);
                                Dictionary<string, string> titulosNuevos = UtilCadenas.ObtenerTextoPorIdiomas(triple.new_object);
                                string tituloNuevoCadena = "";

                                if (titulosNuevos.Count > 0)
                                {
                                    foreach (KeyValuePair<string, string> tituloAntiguo in titulosAntiguos)
                                    {
                                        if (titulosNuevos.ContainsKey(tituloAntiguo.Key))
                                        {
                                            tituloNuevoCadena += titulosNuevos[tituloAntiguo.Key] + "@" + tituloAntiguo.Key + "|||";
                                        }
                                        else
                                        {
                                            tituloNuevoCadena += tituloAntiguo.Value + "@" + tituloAntiguo.Key + "|||";
                                        }
                                    }
                                    foreach (KeyValuePair<string, string> tituloNuevo in titulosNuevos)
                                    {
                                        if (!titulosAntiguos.ContainsKey(tituloNuevo.Key))
                                        {
                                            tituloNuevoCadena += tituloNuevo.Key + "@" + tituloNuevo.Value + "|||";
                                        }
                                    }
                                }
                                else
                                {
                                    tituloNuevoCadena = triple.new_object;
                                }

                                documento.Titulo = tituloNuevoCadena;

                                if (ParametrosAplicacionDS.ParametroAplicacion.Any(item => item.Parametro.Equals(TiposParametrosAplicacion.LecturaAumentada) && item.Valor.Equals("1")))
                                {
                                    DocumentoLecturaAumentada lecturaAumentadaDocumento = mEntityContext.DocumentoLecturaAumentada.FirstOrDefault(item => item.DocumentoID.Equals(documento.DocumentoID));
                                    if (lecturaAumentadaDocumento != null && lecturaAumentadaDocumento.Validada)
                                    {
                                        lecturaAumentadaDocumento.Validada = false;
                                    }
                                }
                            }
                            else
                            {
                                throw new GnossException("You cannot change the title for an empty string.", HttpStatusCode.BadRequest);
                            }
                            break;
                        case GnossResourceProperty.description:
                            //Modificamos la descripción en el modelo ACID
                            descripcion = UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(UtilCadenas.TrimEndCadena(triple.new_object.Trim().Replace("\r\n", ""), "<br>"), "</br>"), "</ br>"), "<p>&nbsp;</p>");

                            documento.Descripcion = descripcion;

                            if (ParametrosAplicacionDS.ParametroAplicacion.Any(item => item.Parametro.Equals(TiposParametrosAplicacion.LecturaAumentada) && item.Valor.Equals("1")))
                            {
                                DocumentoLecturaAumentada lecturaAumentadaDocumento = mEntityContext.DocumentoLecturaAumentada.FirstOrDefault(item => item.DocumentoID.Equals(documento.DocumentoID));
                                if (lecturaAumentadaDocumento != null && lecturaAumentadaDocumento.Validada)
                                {
                                    lecturaAumentadaDocumento.Validada = false;
                                }
                            }

                            break;
                    }

                    ElementoOntologia elementoOntologia = instanciasPrincipales.FirstOrDefault();
                    if (triple.predicate.Contains("|"))
                    {
                        //El valor a modificar se encuentra en una propiedad hija
                        //Método recursivo
                        ModificarInstanciaPorNivel(ontologia, elementoOntologia.Propiedades, parameters.resource_id, triple.predicate, triple.old_object, triple.new_object, true, sbTriplesInsertar, sbTriplesEliminar, sbTriplesInsertarBusqueda, sbTriplesEliminarBusqueda);

                    }
                    else
                    {
                        //El valor a modificar se encuentra en una propiedad del primer nivel
                        if (elementoOntologia != null)
                        {
                            Propiedad propiedad = elementoOntologia.Propiedades.Where(item => item.Nombre.Equals(triple.predicate)).FirstOrDefault();
                            if (propiedad != null)
                            {
                                ModificarPropiedad(parameters.resource_id, propiedad, triple.predicate, triple.old_object, triple.new_object, true, sbTriplesInsertar, sbTriplesEliminar, sbTriplesInsertarBusqueda, sbTriplesEliminarBusqueda);
                            }
                            else
                            {
                                throw new GnossException($"La propiedad {triple.predicate} no existe en la ontología {elementoOntologia.ID}", HttpStatusCode.BadRequest);
                            }
                        }
                    }
                }

                //Borro las entidades obsoletas:
                foreach (string entidadID in mEntidadesABorrar)
                {
                    foreach (ElementoOntologia eo in instanciasPrincipales)
                    {
                        if (eo.ID == entidadID || eo.ID == entidadID.Replace($"{UrlIntragnoss}items/", ""))
                        {
                            entidadesPrinBorrar.Add(eo);
                            break;
                        }
                        else if (mURIEntidadesABorrar.ContainsKey(entidadID))
                        {
                            listaSujetosBorrar.Add($"<{mURIEntidadesABorrar[entidadID]}>");
                            if (!mConfigService.ObtenerProcesarStringGrafo())
                            {
                                listaSujetosBorrarBusqueda.Add($"<{mURIEntidadesABorrar[entidadID]}>");
                            }
                            else
                            {
                                listaSujetosBorrarBusqueda.Add($"<{mURIEntidadesABorrar[entidadID].ToLower()}>");
                            }
                        }
                    }
                }

                foreach (ElementoOntologia eo in entidadesPrinBorrar)
                {
                    instanciasPrincipales.Remove(eo);

                    ElementoOntologia elem = entidadesPrincAntiguas.FirstOrDefault(item => item.ID.Equals(eo.ID));
                    if (elem != null)
                    {
                        listaSujetosBorrar.Add($"<{eo.Uri}>");

                        string idCorto = eo.Uri.Substring(0, eo.Uri.LastIndexOf('_'));
                        idCorto = idCorto.Substring(idCorto.LastIndexOf('_') + 1);

                        listaSujetosBorrarBusqueda.Add($"<http://gnoss/{idCorto.ToUpper()}>");

                    }
                }

                #endregion

                if (parameters.resource_attached_files != null && parameters.resource_attached_files.Count > 0)
                {
                    //obtener ruta imagen servidor
                    string rutaImagen = SubirArchivosDelRDF(parameters.resource_id, parameters.resource_attached_files, instanciasPrincipales, parameters.main_image);

                    if (!string.IsNullOrEmpty(rutaImagen))
                    {
                        //asignar la rutaImagen
                        documento.NombreCategoriaDoc = rutaImagen;
                    }
                }

                //Terminamos las modificaciones y guardamos el documento.
                documento.FechaModificacion = DateTime.Now;
                List<TripleWrapper> listaTriplesSemanticos = null;

                try
                {
                    Stream stream = gestorOWL.PasarOWL(null, ontologia, instanciasPrincipales, null, null);
                    stream.Position = 0; //al escribir el stream se queda en la última posición
                    string ficheroRDF = new StreamReader(stream).ReadToEnd();

                    string infoExtra_Replicacion = "";

                    if (documento.ProyectoID.HasValue)
                    {
                        infoExtra_Replicacion += $"{ObtenerInfoExtraBaseDocumentoAgregar(parameters.resource_id, documento.Tipo, documento.ProyectoID.Value, (short)prioridadBase)}";
                    }

                    // SemWeb
                    try
                    {
                        listaTriplesSemanticos = ControladorDocumentacion.GuardarRDFEnVirtuoso(instanciasPrincipales, nombreOntologia, UrlIntragnoss, "acid", documento.ProyectoID.Value, parameters.resource_id.ToString(), false, infoExtra_Replicacion, documento.Borrador, usarColaReplicacion, (short)prioridadBase, sbTriplesInsertar, sbTriplesEliminar);

                        if (listaSujetosBorrar != null && listaSujetosBorrar.Count > 0)
                        {
                            FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
                            string queryOntology = $"DELETE FROM GRAPH <{UrlIntragnoss}{nombreOntologia}> {{?s ?p ?o}} FROM <{UrlIntragnoss}{nombreOntologia}> WHERE {{?s ?p ?o filter(?s in ({string.Join(",", listaSujetosBorrar)}))}}";
                            facetadoAD.ActualizarVirtuoso(queryOntology, nombreOntologia);
                        }

                        try
                        {
                            ControladorDocumentacion.GuardarRDFEnBDRDF(ficheroRDF, parameters.resource_id, documento.ProyectoID.Value, rdfDS);
                            mEntityContext.SaveChanges();
                            mEntityContext.TerminarTransaccionesPendientes(true);
                        }
                        catch (Exception ex)
                        {
                            mEntityContext.TerminarTransaccionesPendientes(false);
                            //revertir cambios virtuoso con las instanciasPrincipales antiguas
                            mLoggingService.GuardarLogError(ex, $"Error al guardar modificaciones en BD RDF. Se van a revertir los cambios en virtuoso del recurso {parameters.resource_id}", mlogger);
                            ControladorDocumentacion.GuardarRDFEnVirtuoso(entidadesPrincAntiguas, nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, parameters.resource_id.ToString(), false, infoExtra_Replicacion, false, usarColaReplicacion, (short)PrioridadBase.ApiRecursos);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        mLoggingService.GuardarLogError(ex, $"Error al modificar recurso en BD RDF o Virtuoso. Se van a revertir los cambios en el ácido del recurso {parameters.resource_id}", mlogger);
                        throw;
                    }

                    stream = null;
                    ficheroRDF = null;
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, $"Error al modificar recurso. Se van a revertir los cambios en el ácido del recurso {parameters.resource_id}", mlogger);

                    mEntityContext.TerminarTransaccionesPendientes(false);

                    throw;
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
                mEntityContext.TerminarTransaccionesPendientes(false);
                throw;
            }
            finally
            {
                if (documentoBloqueado)
                {
                    docCN.FinalizarEdicionRecurso(parameters.resource_id);
                }
            }

            // Actualizar cola GnossLIVE
            ControladorDocumentacion controDoc = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);

            List<DocumentoWebVinBaseRecursos> basesRecursoDocumento = docCN.ObtenerListaDocumentoWebVinBaseRecursoPorDocumentoID(documento.DocumentoID);

            foreach (DocumentoWebVinBaseRecursos documentoWebVinBaseRecursos in basesRecursoDocumento)
            {
                Guid baseRecursosId = documentoWebVinBaseRecursos.BaseRecursosID;
                Guid proyectoBR = gestorDoc.ObtenerProyectoID(baseRecursosId);

                //si la comunidad es en la que se publicó y coincide con el proyecto pasado como parámetro se inserta directamente, si no, lo hace el BASE
                if ((proyectoBR.Equals(documento.ProyectoID) && Proyecto.Clave.Equals(proyectoBR)) || basesRecursoDocumento.Count == 1)
                {
                    FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
                    if (sbTriplesEliminarBusqueda != null && sbTriplesEliminarBusqueda.GetStringBuilder().Count > 0)
                    {
                        foreach (StringBuilder stringBuilder in sbTriplesEliminarBusqueda.GetStringBuilder())
                        {
                            stringBuilder.Insert(0, $"DELETE DATA FROM <{UrlIntragnoss}{proyectoBR}> {{");
                            stringBuilder.AppendLine("}");
                            facetadoAD.ActualizarVirtuoso(stringBuilder.ToString(), proyectoBR.ToString());
                        }
                    }

                    if (sbTriplesInsertarBusqueda != null && sbTriplesInsertarBusqueda.GetStringBuilder().Count > 0)
                    {
                        foreach (StringBuilder stringBuilder in sbTriplesInsertarBusqueda.GetStringBuilder())
                        {
                            stringBuilder.Insert(0, $"INSERT DATA INTO <{UrlIntragnoss}{proyectoBR}> {{");
                            stringBuilder.AppendLine("}");
                            facetadoAD.ActualizarVirtuoso(stringBuilder.ToString(), proyectoBR.ToString());
                        }
                    }

                    if (listaSujetosBorrarBusqueda != null && listaSujetosBorrarBusqueda.Count > 0)
                    {
                        string queryDelete = $"DELETE FROM GRAPH <{UrlIntragnoss}{proyectoBR}> {{?s ?p ?o}} FROM <{UrlIntragnoss}{proyectoBR}> WHERE {{?s ?p ?o filter(?s in ({string.Join(",", listaSujetosBorrarBusqueda)}))}}";
                        facetadoAD.ActualizarVirtuoso(queryDelete, $"{UrlIntragnoss}{proyectoBR}");
                    }

                    ControladorDocumentacion.AgregarRecursoModeloBaseSimple(parameters.resource_id, proyectoBR, documento.Tipo, null, "", PrioridadBase.Alta, false, -1, (short)TiposElementosEnCola.InsertadoEnGrafoBusquedaDesdeWeb, mAvailableServices);
                }
                else
                {
                    controDoc.mActualizarTodosProyectosCompartido = true;
                    controDoc.NotificarAgregarRecursosEnComunidad(listaDocs, proyectoBR, prioridadBase, mAvailableServices);
                }

                int tipo;
                switch ((TiposDocumentacion)documento.Tipo)
                {
                    case TiposDocumentacion.Debate:
                        tipo = (int)TipoLive.Debate;
                        break;
                    case TiposDocumentacion.Pregunta:
                        tipo = (int)TipoLive.Pregunta;
                        break;
                    default:
                        tipo = (int)TipoLive.Recurso;
                        break;
                }

                if (AgregarColaLive || parameters.publish_home)
                {
                    ControladorDocumentacion.ActualizarGnossLive(proyectoBR, documento.DocumentoID, AccionLive.Editado, tipo, "base", PrioridadLive.Baja, mAvailableServices);
                }                
            }

            try
            {
                //anular cache del documento
                ControladorDocumentacion.BorrarCacheControlFichaRecursos(parameters.resource_id);
                if (mConfigService.ObtenerProcesarStringGrafo())
                {
                    ControladorDocumentacion.BorrarRDFDeBDRDF(parameters.resource_id);
                }

                ControladorDocumentacion.InsertarEnColaProcesarFicherosRecursosModificadosOEliminados(parameters.resource_id, TipoEventoProcesarFicherosRecursos.Modificado, mAvailableServices);

                //borrar cache recursos
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(FilaProy.ProyectoID, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));
            }
            catch (Exception)
            {/* error invalidar cache */}

            return instanciasPrincipales;
        }

        private void ModificarPropiedadRecursoInt(ModifyResourceProperty parameters)
        {
            //Cargar Documento y obtener campos
            Configuracion.ObtenerDesdeFicheroConexion = true;
            DocumentacionCN docCN = new DocumentacionCN("acid", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            DataWrapperDocumentacion docDW = docCN.ObtenerDocumentoPorID(parameters.resource_id);

            //Crear nuevo gestorDocumental
            GestorDocumental gestorDocumental = new GestorDocumental(docDW, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);

            if (!gestorDocumental.ListaDocumentos.ContainsKey(parameters.resource_id))
            {
                throw new GnossException("The resource " + parameters.resource_id + " does not exist.", HttpStatusCode.BadRequest);
            }

            Elementos.Documentacion.Documento documento = gestorDocumental.ListaDocumentos[parameters.resource_id];

            GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
            Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
            //DocumentacionCN docCN = new DocumentacionCN("acid");
            DocumentoWeb documentoEdicion = null;
            bool documentoBloqueado = ComprobarDocumentoEnEdicion(parameters.resource_id, identidad.Clave);

            try
            {
                docCN.ObtenerDocumentoPorIDCargarTotal(parameters.resource_id, gestorDoc.DataWrapperDocumentacion, true, true, null);
                List<Guid> listaDocs = new List<Guid>();
                listaDocs.Add(parameters.resource_id);
                gestorDoc.CargarDocumentos(false);

                Elementos.Documentacion.Documento documentoAntiguo = gestorDoc.ListaDocumentos[parameters.resource_id];

                bool esAdminProyectoMyGnoss = EsAdministradorProyectoMyGnoss(UsuarioOAuth);

                if (!esAdminProyectoMyGnoss && !documentoAntiguo.TienePermisosEdicionIdentidad(identidad, null, Proyecto, Guid.Empty, false))
                {
                    throw new Exception("El usuario no tiene permisos para editar el documento");
                }

                //Obtenemos el documento que vamos a editar:
                documentoEdicion = new DocumentoWeb(documentoAntiguo.FilaDocumento, gestorDoc);

                switch (parameters.property)
                {
                    case "sioc_t:Tag":
                        List<string> etiquetas = UtilCadenas.SepararTexto(parameters.new_object);
                        ReplaceTags(etiquetas, documentoEdicion, null, null);
                        break;

                    case "skos:ConceptID":
                        List<string> categorias = UtilCadenas.SepararTexto(parameters.new_object);
                        List<Guid> listaCatsID = new List<Guid>();

                        foreach (string cat in categorias)
                        {
                            Guid catID = Guid.Empty;
                            if (Guid.TryParse(cat, out catID))
                            {
                                listaCatsID.Add(catID);
                            }
                        }

                        ReplaceCategories(listaCatsID, gestorDoc, documentoEdicion, identidad.Clave);
                        break;
                }

                List<Guid> listaProyectosActualNumRec = new List<Guid>();
                foreach (Guid baseRecurso in documentoEdicion.BaseRecursos)
                {
                    Guid proyectoID = gestorDoc.ObtenerProyectoID(baseRecurso);
                    listaProyectosActualNumRec.Add(proyectoID);
                }

                //Terminamos las modificaciones y guardamos el documento.
                documentoEdicion.FilaDocumento.FechaModificacion = DateTime.Now;
                Guardar(listaProyectosActualNumRec, gestorDoc, documentoEdicion);
            }
            finally
            {
                if (documentoBloqueado)
                {
                    docCN.FinalizarEdicionRecurso(parameters.resource_id);
                }
            }

            //meterlo en el base
            ControladorDocumentacion.AgregarRecursoModeloBaseSimple(parameters.resource_id, documentoEdicion.ProyectoID, (short)documentoEdicion.TipoDocumentacion, PrioridadBase.ApiRecursos, mAvailableServices);

            try
            {
                //anular cache del documento
                ControladorDocumentacion.BorrarCacheControlFichaRecursos(parameters.resource_id);

                //borrar cache recursos
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarResultadosYFacetasDeBusquedaEnProyecto(FilaProy.ProyectoID, FacetadoAD.TipoBusquedaToString(TipoBusqueda.Recursos));
            }
            catch (Exception)
            {/* error invalidar cache */}
        }

        private Ontologia ObtenerOntologia(Guid pOntologiaId)
        {
            Ontologia ontologia = null;
            try
            {
                DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCL>(), mLoggerFactory);
                Guid? xmlID = docCL.ObtenerIDXmlOntologia(pOntologiaId);
                if (!xmlID.HasValue)
                {
                    xmlID = Guid.NewGuid();
                }
                docCL.Dispose();

                if (!OntologiasCargadas.ContainsKey(pOntologiaId) || OntologiasCargadas[pOntologiaId].Key != xmlID)
                {
                    if (OntologiasCargadas.ContainsKey(pOntologiaId))
                    {
                        OntologiasCargadas.Remove(pOntologiaId);
                    }

                    Dictionary<string, List<EstiloPlantilla>> listaEstilos = new Dictionary<string, List<EstiloPlantilla>>();
                    byte[] arrayOnto = ControladorDocumentacion.ObtenerOntologia(pOntologiaId, out listaEstilos, FilaProy.ProyectoID);

                    //Leo la ontología:
                    ontologia = new Ontologia(arrayOnto, true);
                    ontologia.OntologiaID = pOntologiaId;
                    ontologia.LeerOntologia();
                    ontologia.EstilosPlantilla = listaEstilos;
                    try
                    {
                        OntologiasCargadas.Add(pOntologiaId, new KeyValuePair<Guid, Ontologia>(xmlID.Value, ontologia));
                    }
                    catch (Exception) {/*error al añadir la ontologia*/}
                }

                ontologia = OntologiasCargadas[pOntologiaId].Value;
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, mlogger);
            }

            return ontologia;
        }

        private void EliminarEntidadesAuxiliaresRelacionadas(ElementoOntologia pElementoOntologia)
        {
            if (pElementoOntologia != null && pElementoOntologia.Propiedades != null)
            {
                foreach (Propiedad propiedad in pElementoOntologia.Propiedades)
                {
                    if (propiedad.Tipo == TipoPropiedad.ObjectProperty && !string.IsNullOrEmpty(propiedad.Rango))
                    {
                        foreach (string uriEntidad in propiedad.ValoresUnificados.Keys)
                        {
                            if (!mEntidadesABorrar.Contains(uriEntidad))
                            {
                                mEntidadesABorrar.Add(uriEntidad);
                                mURIEntidadesABorrar.Add(uriEntidad, propiedad.ValoresUnificados[uriEntidad].Uri);
                                EliminarEntidadesAuxiliaresRelacionadas(propiedad.ValoresUnificados[uriEntidad]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Se encarga de modificar el objeto de una propiedad de tipo ArchivoLink para
        /// que se cargue igual que se modifica la ruta del archivo para enviarlo al servicio
        /// interno (se hacen varias modificaciones para que se pueda guardar correctamente en el servidor)
        /// </summary>
        /// <param name="pValor">Objeto de un triple de una propiedad de tipo ArchivoLink</param>
        /// <returns>El objeto modificado</returns>
        private static string ModificarValorObjetoArchivoLink(string pValor)
        {
            Regex regexIdArchivo = new Regex("_([A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12})$");
            if (!string.IsNullOrEmpty(pValor))
            {
                string ruta = pValor.Substring(0, pValor.LastIndexOf("/") + 1);
                string archivo = pValor.Substring(pValor.LastIndexOf("/") + 1);

                string nombreArchivo = archivo.Substring(0, archivo.LastIndexOf("."));
                string extension = archivo.Substring(archivo.LastIndexOf("."));

                string idArchivo = string.Empty;
                //comprobamos si el nombre trae el id al final para no recortarlo
                if (regexIdArchivo.IsMatch(nombreArchivo))
                {
                    idArchivo = regexIdArchivo.Match(nombreArchivo).Groups[1].Value;
                    nombreArchivo = nombreArchivo.Replace($"_{idArchivo}", string.Empty);
                }

                nombreArchivo = UtilCadenas.EliminarCaracteresUrlSem(nombreArchivo);

                //Volvemos a poner el id en el nombre
                if (!string.IsNullOrEmpty(idArchivo))
                {
                    nombreArchivo += $"_{idArchivo}";
                }

                return ruta.Replace('/', Path.DirectorySeparatorChar) + nombreArchivo + extension;
            }

            return string.Empty;
        }

        #endregion
    }
}