using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.AD.Facetado.Model;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Tesauro;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Tesauro;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.Controles.Documentacion;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Es.Riam.Semantica.OWL;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Es.Riam.AbstractsOpen;
using System.Text;
using Universal.Common.Extensions;
using System.Data;
using static Es.Riam.Gnoss.Web.MVC.Models.Tesauro.TesauroModels;
using Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS;
using Microsoft.Azure.Amqp.Framing;
using Es.Riam.Interfaces.InterfacesOpen;
using Microsoft.Extensions.Logging;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    /// <summary>
    /// Use it to create / modify / delete nodes in a thesaurus
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ThesaurusController : ControlApiGnossBase
    {
        private ILogger mlogger;
        private ILoggerFactory mLoggerFactory;
        public ThesaurusController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, IAvailableServices availableServices, ILogger<ThesaurusController> logger, ILoggerFactory loggerFactory)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication, availableServices,logger,loggerFactory)
        {
            mlogger = logger;
            mLoggerFactory = loggerFactory;
        }

        #region Public

        /// <summary>
        /// Get a thesaurus ontology form its ontology url
        /// </summary>
        /// <param name="thesaurus_ontology_url">Thesaurus ontology url</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="source">Thesaurus identifier</param>
        /// <returns>RDF of a semantic thesaurus</returns>
        /// <example>GET thesaurus/get-thesaurus?thesaurus_ontology_url=bbvataxonomy.owl&community_short_name=knowledgegraph&source=category</example>        
        [HttpGet, Route("get-thesaurus")]
        public string GetNodoTesauroSemantico(string thesaurus_ontology_url, string community_short_name, string source)
        {
            mNombreCortoComunidad = community_short_name;

            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(community_short_name);
            pry.Dispose();
            if (!proyectoID.Equals(Guid.Empty))
            {
                ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

                thesaurus_ontology_url = AjustarUrlGrafoSegunUrlIntragnoss(thesaurus_ontology_url);

                Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(thesaurus_ontology_url, ProyectoTraerOntosID);

                if (docOnto == null)
                {
                    throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
                }

                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

                Guid ontologiaID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, thesaurus_ontology_url);
                if (ontologiaID == Guid.Empty)
                {
                    throw new GnossException("There is no ontology resource with the specified URL", HttpStatusCode.BadRequest);
                }
                string nombreOntologia = docCN.ObtenerEnlaceDocumentoPorDocumentoID(ontologiaID);
                docCN.Dispose();

                string urlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";

                byte[] arrayOnto = ControladorDocumentacion.ObtenerOntologia(ontologiaID, proyectoID);
                Ontologia ontologia = new Ontologia(arrayOnto, true);
                ontologia.LeerOntologia();
                ontologia.OntologiaID = ontologiaID;

                MemoryStream buffer = new MemoryStream(ControladorDocumentacion.ObtenerRDFTesauroSemanticoDeVirtuoso(thesaurus_ontology_url, source, UrlIntragnoss, ontologia, urlOntologia));

                StreamReader reader = new StreamReader(buffer);
                string rdfTexto = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();

                return rdfTexto;
            }
            else
            {
                throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Move a category of a semantic thesaurus another parent given its full path from the root
        /// </summary>
        /// <param name="parameters">Parameters</param>
        [HttpPost, Route("move-node")]
        public void MoverNodoTesauroSemantico(ParamsMoveNode parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            parameters.thesaurus_ontology_url = AjustarUrlGrafoSegunUrlIntragnoss(parameters.thesaurus_ontology_url);
            parameters.resources_ontology_url = AjustarUrlGrafoSegunUrlIntragnoss(parameters.resources_ontology_url);

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.thesaurus_ontology_url, ProyectoTraerOntosID);

            if (docOnto == null)
            {
                throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            Guid elementoVinculadoID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, parameters.resources_ontology_url);
            GestorDocumental gestorDoc = new GestorDocumental(docCN.ObtenerDocumentoPorID(elementoVinculadoID), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
            docCN.Dispose();

            if (elementoVinculadoID == Guid.Empty || !gestorDoc.ListaDocumentos.ContainsKey(elementoVinculadoID))
            {
                throw new GnossException("There is no ontology resource with the specified URL", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.MoverCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, parameters.resources_ontology_url, UrlIntragnoss, parameters.category_id, parameters.path, docOnto, gestorDoc.ListaDocumentos[elementoVinculadoID], true, ProyectoTraerOntosID, mAvailableServices);
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        /// <summary>
        /// Change the name of a thesaurus category
        /// </summary>
        /// <param name="parameters">Parameters to change the category name</param>
        [HttpPost, Route("change-category-name")]
        public void ChangeCategoryName(ParamsChangeCategoryName parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCN>(), mLoggerFactory);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                AD.EntityModel.Models.Tesauro.CategoriaTesauro categoriaTesauro = dataWrapperTesauro.ListaCategoriaTesauro.Where(item => item.CategoriaTesauroID.Equals(parameters.category_id)).FirstOrDefault();

                if (categoriaTesauro == null)
                {
                    throw new GnossException($"There is no category with this category id: {parameters.category_id}", HttpStatusCode.BadRequest);
                }

                categoriaTesauro.Nombre = parameters.new_category_name;
                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCL>(), mLoggerFactory);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarCacheTesauroFaceta(proyectoID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Create a new thesaurus category
        /// </summary>
        /// <param name="parameters">Parameters to create the new category</param>
        [HttpPost, Route("create-category")]
        public void CreateCategory(ParamsCreateCategory parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCN>(), mLoggerFactory);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                GestionTesauro gestorTesauro = new GestionTesauro(dataWrapperTesauro, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestionTesauro>(), mLoggerFactory);

                if (parameters.parent_category_id.HasValue && !parameters.parent_category_id.Value.Equals(Guid.Empty))
                {
                    if (gestorTesauro.ListaCategoriasTesauro.ContainsKey(parameters.parent_category_id.Value))
                    {
                        var categoriaSuperior = gestorTesauro.ListaCategoriasTesauro[parameters.parent_category_id.Value];
                        gestorTesauro.AgregarSubcategoria(categoriaSuperior, parameters.category_name);
                    }
                }
                else
                {
                    gestorTesauro.AgregarCategoriaPrimerNivel(parameters.category_name);
                }

                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCL>(), mLoggerFactory);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarCacheTesauroFaceta(proyectoID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Delete a thesaurus category
        /// </summary>
        /// <param name="parameters">Parameters to delete the specific category</param>
        [HttpPost, Route("delete-category")]
        public void DeleteCategory(ParamsDeleteCategory parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCN>(), mLoggerFactory);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                GestionTesauro gestorTesauro = new GestionTesauro(dataWrapperTesauro, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestionTesauro>(), mLoggerFactory);

                if (gestorTesauro.ListaCategoriasTesauro.ContainsKey(parameters.category_id))
                {
                    var categoriaEliminar = gestorTesauro.ListaCategoriasTesauro[parameters.category_id];

                    DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
                    List<Guid> listaCategoriasEliminar = categoriaEliminar.ObtenerCategoriasHijosNietos();
                    listaCategoriasEliminar.Add(categoriaEliminar.Clave);
                    DataWrapperDocumentacion dataWrapperDocumentacion = documentacionCN.ObtenerDocWebAgCatTesauroPorCategoriasId(listaCategoriasEliminar);
                    gestorTesauro.GestorDocumental = new GestorDocumental(dataWrapperDocumentacion, mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);

                    gestorTesauro.EliminarCategoriaEHijos(categoriaEliminar);
                    documentacionCN.Actualizar();
                }
                else
                {
                    throw new GnossException($"The category {parameters.category_id} isn't exists in the community {parameters.community_short_name}", HttpStatusCode.BadRequest);
                }

                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<TesauroCL>(), mLoggerFactory);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarCacheTesauroFaceta(proyectoID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Removes a category of a semantic thesaurus moving all resources that were linked to it to another indicating its complete path from the root.
        /// </summary>
        /// <param name="parameters">Parameters</param>
        [HttpPost, Route("delete-node")]
        public void EliminarNodoTesauroSemantico(ParamsDeleteNode parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            parameters.thesaurus_ontology_url = AjustarUrlGrafoSegunUrlIntragnoss(parameters.thesaurus_ontology_url);
            parameters.resources_ontology_url = AjustarUrlGrafoSegunUrlIntragnoss(parameters.resources_ontology_url);

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.thesaurus_ontology_url, ProyectoTraerOntosID);

            if (docOnto == null)
            {
                throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
            }

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            Guid elementoVinculadoID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, parameters.resources_ontology_url);
            GestorDocumental gestorDoc = new GestorDocumental(docCN.ObtenerDocumentoPorID(elementoVinculadoID), mLoggingService, mEntityContext, mLoggerFactory.CreateLogger<GestorDocumental>(), mLoggerFactory);
            docCN.Dispose();

            if (elementoVinculadoID == Guid.Empty || !gestorDoc.ListaDocumentos.ContainsKey(elementoVinculadoID))
            {
                throw new GnossException("There is no ontology resource with the specified URL", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.EliminarCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, parameters.resources_ontology_url, UrlIntragnoss, BaseURLFormulariosSem, parameters.category_id, docOnto, gestorDoc.ListaDocumentos[elementoVinculadoID], true, ProyectoTraerOntosID, mAvailableServices);
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        /// <summary>
        /// Add a category as a parent of another
        /// </summary>
        /// <param name="parameters">Model with the requiered parameter to add new node to a thesaurus</param>
        /// <example>POST thesaurus/set-node-parent</example>
        [HttpPost, Route("set-node-parent")]
        public void AgregarPadreANodoTesauroSemantico(ParamsParentNode parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.thesaurus_ontology_url, ProyectoTraerOntosID);

            if (docOnto == null)
            {
                throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
            }

            string[] arrayTesSem = ControladorDocumentacion.ObtenerDatosFacetaTesSem(parameters.thesaurus_ontology_url);

            string triplesInsertar = "";
            List<TripleWrapper> triplesComInsertar = new List<TripleWrapper>();

            List<string> propiedades = new List<string>() { arrayTesSem[4], arrayTesSem[7] };

            FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);

            //Obtengo categoría:
            FacetadoDS dataSetCategorias = facCN.ObtenerValoresPropiedadesEntidad(parameters.thesaurus_ontology_url, parameters.child_category_id, propiedades);
            if (dataSetCategorias.Tables[0].Rows.Count == 0)
            {
                facCN.Dispose();
                throw new GnossException("There is no category with this URI '" + parameters.child_category_id + "'", HttpStatusCode.BadRequest);
            }

            List<string> padres = FacetadoCN.ObtenerObjetosDataSetSegunPropiedad(dataSetCategorias, parameters.child_category_id, arrayTesSem[7]);
            if (padres.Count == 0)
            {
                facCN.Dispose();
                throw new GnossException("You can not put as a child of another a root category", HttpStatusCode.BadRequest);
            }

            FacetadoDS dataSetCategoriasPadre = facCN.ObtenerValoresPropiedadesEntidad(parameters.thesaurus_ontology_url, parameters.parent_category_id, false);
            if (dataSetCategoriasPadre.Tables[0].Rows.Count == 0)
            {
                facCN.Dispose();
                throw new GnossException("There is no category with this URI '" + parameters.parent_category_id + "'", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.EscribirTripletaEntidad(parameters.child_category_id, arrayTesSem[7], parameters.parent_category_id, ref triplesInsertar, triplesComInsertar, false, null);
            ControladorDocumentacion.EscribirTripletaEntidad(parameters.parent_category_id, arrayTesSem[4], parameters.child_category_id, ref triplesInsertar, triplesComInsertar, false, null);

            //Genero las triples de la comunidad:
            string triplesComInsertarDef = "";
            List<string> listaAux = new List<string>();

            FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoAD>(), mLoggerFactory);
            foreach (TripleWrapper triple in triplesComInsertar)
            {
                triplesComInsertarDef += facetadoAD.GenerarTripletaSinConversionesAbsurdas(PasarObjetoALower(triple.Subject, listaAux), triple.Predicate, PasarObjetoALower(triple.Object, listaAux), null);
            }

            //Guardo tesauro
            string nombreOntologia = docOnto.FilaDocumento.Enlace;

            //Guardo triples grafo tesauro semántico:
            facCN.InsertaTripletas(nombreOntologia, triplesInsertar, (short)PrioridadBase.ApiRecursos, true);

            //Guardo triples en los proyectos en los que está subido y compartido el tesauro semántico:
            foreach (Guid proyectoID in docOnto.ListaProyectos)
            {
                facCN.InsertaTripletas(proyectoID.ToString().ToLower(), triplesComInsertarDef, (short)PrioridadBase.ApiRecursos, true);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarCacheTesauroFaceta(proyectoID);
                facetadoCL.Dispose();
            }

            dataSetCategorias.Dispose();
            dataSetCategoriasPadre.Dispose();
        }

        /// <summary>
        /// Modifies the name of a category of semantic thesaurus
        /// </summary>
        /// <param name="parameters">Model with the parameters for change the node name</param>
        /// <example>POST thesaurus/change-node-name</example>
        [HttpPost, Route("change-node-name")]
        public void CambiarNombreANodoTesauroSemantico(ParamsChangeName parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.thesaurus_ontology_url, ProyectoTraerOntosID);

            if (docOnto == null)
            {
                throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.RenombrarCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, UrlIntragnoss, parameters.category_id, parameters.category_name, docOnto, true, ProyectoTraerOntosID);
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        /// <summary>
        /// Enter a category in a semantic thesaurus.
        /// </summary>
        /// <param name="parameters">Model with the parameters to add a category to a Thesaurus</param>
        /// <example>POST thesaurus/insert-node</example>
        [HttpPost, Route("insert-node")]
        public void InsertarNodoTesauroSemantico(ParamsInsertNode parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            if (!parameters.thesaurus_ontology_url.Contains(UrlIntragnoss))
            {
                if (parameters.thesaurus_ontology_url.Contains("/"))
                {
                    parameters.thesaurus_ontology_url = parameters.thesaurus_ontology_url.Substring(parameters.thesaurus_ontology_url.LastIndexOf("/") + 1);
                }

                parameters.thesaurus_ontology_url = UrlIntragnoss + parameters.thesaurus_ontology_url;
            }

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.thesaurus_ontology_url, ProyectoTraerOntosID);

            if (docOnto == null)
            {
                throw new GnossException("There is no ontology thesaurus with the specified URL", HttpStatusCode.BadRequest);
            }

            string nombreOntologia = docOnto.FilaDocumento.Enlace;
            GestionOWL gestorOWL = new GestionOWL();
            gestorOWL.UrlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";
            gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
            GestionOWL.FicheroConfiguracionBD = "acid";
            GestionOWL.URLIntragnoss = UrlIntragnoss;

            byte[] arrayOnto = ControladorDocumentacion.ObtenerOntologia(docOnto.Clave, FilaProy.ProyectoID);

            if (arrayOnto != null)
            {
                Ontologia ontologia = new Ontologia(arrayOnto, true);
                ontologia.LeerOntologia();

                StreamReader reader = new StreamReader(new MemoryStream(parameters.rdf_category));
                string lineaRDF = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
                reader = null;

                List<ElementoOntologia> instanciasPrincipales = gestorOWL.LeerFicheroRDF(ontologia, lineaRDF, true, true);
                if (instanciasPrincipales.Count != 1)
                {
                    throw new GnossException("The RDF can contain only one element, the category", HttpStatusCode.BadRequest);
                }

                string[] arrayTesSem = ControladorDocumentacion.ObtenerDatosFacetaTesSem(parameters.thesaurus_ontology_url);

                if (instanciasPrincipales[0].ObtenerPropiedad(arrayTesSem[4]).ValoresUnificados.Count > 0)
                {
                    throw new GnossException("You can only add categories without child nodes", HttpStatusCode.BadRequest);
                }

                List<string> padres = new List<string>(instanciasPrincipales[0].ObtenerPropiedad(arrayTesSem[7]).ValoresUnificados.Keys);

                ControladorDocumentacion.CrearCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, UrlIntragnoss, instanciasPrincipales[0], padres, docOnto, true, null, FilaProy.ProyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCL>(), mLoggerFactory);
                facetadoCL.InvalidarCacheTesauroFaceta(FilaProy.ProyectoID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new GnossException("The ontology can not be empty", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Create a Thesaurus with the Collections and Concepts in the parameters
        /// </summary>
        /// <param name="thesaurus">Thesaurus that will be loaded</param>
        [HttpPost, Route("create-thesaurus")]
        public void CreateThesaurus(Thesaurus thesaurus)
        {
            mNombreCortoComunidad = thesaurus.CommunityShortName;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            thesaurus.Ontology = ParsearOntologia(thesaurus.Ontology);

            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            if (!documentacionCN.ExisteOntologiaEnProyecto(proyectoID, thesaurus.Ontology))
            {
                throw new Exception($"La ontología {thesaurus.Ontology} no existe en el proyecto {thesaurus.CommunityShortName} con id {proyectoID}");
            }

            string urlIntragnoss = UrlIntragnoss;
            if (!urlIntragnoss.EndsWith('/'))
            {
                urlIntragnoss += $"{urlIntragnoss}/";
            }

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            facetadoCN.IniciarTransaccion();

            try
            {
                CrearTesauro(thesaurus, urlIntragnoss, proyectoID, facetadoCN);
                AgregarProyectoConfigExtraSem(proyectoID, thesaurus.Source, thesaurus.Collection.ScopeNote, thesaurus.Collection.Subject, thesaurus.Ontology, urlIntragnoss);
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }
            facetadoCN.TerminarTransaccion(true);
        }

        /// <summary>
        /// Modify the indicated Thesaurus. Replace current data with the list of Collection and Concept given.
        /// </summary>
        /// <param name="thesaurus">Thesaurus that will be loaded</param>
        [HttpPost, Route("modify-thesaurus")]
        public void ModifyThesaurus(Thesaurus thesaurus)
        {
            mNombreCortoComunidad = thesaurus.CommunityShortName;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            thesaurus.Ontology = ParsearOntologia(thesaurus.Ontology);

            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            if (!documentacionCN.ExisteOntologiaEnProyecto(proyectoID, thesaurus.Ontology))
            {
                throw new Exception($"La ontología {thesaurus.Ontology} no existe en el proyecto {thesaurus.CommunityShortName} con id {proyectoID}");
            }

            string urlIntragnoss = UrlIntragnoss;
            if (!urlIntragnoss.EndsWith('/'))
            {
                urlIntragnoss += $"{urlIntragnoss}/";
            }

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            facetadoCN.IniciarTransaccion();

            try
            {
                ControladorDocumentacion.EliminarTesauroOntologiaBusqueda(thesaurus.Ontology, thesaurus.Source, proyectoID, facetadoCN);
                if (thesaurus.Collection != null)
                {
                    CrearTesauro(thesaurus, urlIntragnoss, proyectoID, facetadoCN);
                }
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }
            facetadoCN.TerminarTransaccion(true);
        }

        /// <summary>
        /// Add new category with they narrowers at the thesaurus
        /// </summary>
        /// <param name="conceptToAdd">Model with the params to add a concept</param>
        /// <exception cref="Exception"></exception>
        [HttpPost, Route("add-thesaurus-category")]
        public void AddCategory(ConceptToAddModel conceptToAdd)
        {
            mNombreCortoComunidad = conceptToAdd.CommunityShortName;

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            StringBuilder triplesGrafoOntologia = new StringBuilder();
            StringBuilder triplesGrafoBusqueda = new StringBuilder();

            conceptToAdd.Ontology = ParsearOntologia(conceptToAdd.Ontology);

            GenerarTriplesConcept(conceptToAdd.Concept, conceptToAdd.Source, UrlIntragnoss, triplesGrafoOntologia, triplesGrafoBusqueda, conceptToAdd.ParentCategorySubject);

            if (string.IsNullOrEmpty(conceptToAdd.ParentCategorySubject))
            {
                GenerarMemberCollection(conceptToAdd.Concept, conceptToAdd.Source, triplesGrafoOntologia, triplesGrafoBusqueda, facetadoCN);
            }
            else
            {
                GenerarNarrowerPadre(conceptToAdd.ParentCategorySubject, conceptToAdd.Concept, conceptToAdd.Source, triplesGrafoOntologia, triplesGrafoBusqueda);
            }

            facetadoCN.IniciarTransaccion();

            try
            {
                InsertarTriplesConcept(conceptToAdd.Ontology, proyectoID, triplesGrafoOntologia, triplesGrafoBusqueda, facetadoCN);
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }

            facetadoCN.TerminarTransaccion(true);
        }

        /// <summary>
        /// Modify the concept given by parameter and its narrowers if you indicated it
        /// </summary>
        /// <param name="conceptToModify">Model with the concept to modify</param>
        [HttpPost, Route("modify-thesaurus-category")]
        public void ModifyCategory(ConceptToModifyModel conceptToModify)
        {
            mNombreCortoComunidad = conceptToModify.CommunityShortName;

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            conceptToModify.Ontology = ParsearOntologia(conceptToModify.Ontology);

            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            if (!documentacionCN.ExisteOntologiaEnProyecto(proyectoID, conceptToModify.Ontology))
            {
                throw new Exception($"La ontología {conceptToModify.Ontology} no existe en el proyecto {conceptToModify.CommunityShortName} con id {proyectoID}");
            }

            StringBuilder triplesGrafoOntologia = new StringBuilder();
            StringBuilder triplesGrafoBusqueda = new StringBuilder();

            conceptToModify.Ontology = ParsearOntologia(conceptToModify.Ontology);

            if (!conceptToModify.ModifyNarrower)
            {
                conceptToModify.Concept.Narrower = null;
            }

            GenerarTriplesConcept(conceptToModify.Concept, conceptToModify.Source, UrlIntragnoss, triplesGrafoOntologia, triplesGrafoBusqueda, conceptToModify.ParentCategorySubject);

            if (string.IsNullOrEmpty(conceptToModify.ParentCategorySubject))
            {
                GenerarMemberCollection(conceptToModify.Concept, conceptToModify.Source, triplesGrafoOntologia, triplesGrafoBusqueda, facetadoCN);
            }
            else
            {
                GenerarNarrowerPadre(conceptToModify.ParentCategorySubject, conceptToModify.Concept, conceptToModify.Source, triplesGrafoOntologia, triplesGrafoBusqueda);
            }

            facetadoCN.IniciarTransaccion();

            try
            {
                EliminarConcept(conceptToModify.Ontology, conceptToModify.Concept, proyectoID, conceptToModify.Source, facetadoCN, conceptToModify.ModifyNarrower);
                InsertarTriplesConcept(conceptToModify.Ontology, proyectoID, triplesGrafoOntologia, triplesGrafoBusqueda, facetadoCN);
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }

            facetadoCN.TerminarTransaccion(true);
        }

        /// <summary>
        /// Delete the thesaurus indicated by the source given by parameter
        /// </summary>
        /// <param name="thesaurus"></param>
        /// <exception cref="Exception"></exception>
        [HttpPost, Route("delete-thesaurus")]
        public void DeleteThesaurus(ThesaurusToDeleteModel thesaurus)
        {
            mNombreCortoComunidad = thesaurus.CommunityShortName;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            thesaurus.Ontology = ParsearOntologia(thesaurus.Ontology);

            DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            if (!documentacionCN.ExisteOntologiaEnProyecto(proyectoID, thesaurus.Ontology))
            {
                throw new Exception($"La ontología {thesaurus.Ontology} no existe en el proyecto {thesaurus.CommunityShortName} con id {proyectoID}");
            }

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            facetadoCN.IniciarTransaccion();

            try
            {
                ControladorDocumentacion.EliminarTesauroOntologiaBusqueda(thesaurus.Ontology, thesaurus.Source, proyectoID, facetadoCN);
                DataWrapperProyecto dwProyecto = proyectoCN.ObtenerTesaurosSemanticosConfigEdicionDeProyecto(proyectoID);
                ProyectoConfigExtraSem filaConfig = dwProyecto.ListaProyectoConfigExtraSem.FirstOrDefault(config => config.ProyectoID.Equals(proyectoID) && config.UrlOntologia.Equals(thesaurus.Ontology) && config.SourceTesSem.Equals(thesaurus.Source));
                dwProyecto.ListaProyectoConfigExtraSem.Remove(filaConfig);
                mEntityContext.Remove(filaConfig);
                mEntityContext.SaveChanges();
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }
            facetadoCN.TerminarTransaccion(true);
        }

        /// <summary>
        /// Delete the concept indicated and it's childrens
        /// </summary>
        /// <param name="pConceptToDelete">Model with the subject of the concept to delete</param>
        /// <exception cref="Exception"></exception>
        [HttpPost, Route("delete-thesaurus-category")]
        public void DeleteThesaurusCategory(ConceptToDeleteModel pConceptToDelete)
        {
            mNombreCortoComunidad = pConceptToDelete.CommunityShortName;

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCN>(), mLoggerFactory);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(mNombreCortoComunidad);

            pConceptToDelete.Ontology = ParsearOntologia(pConceptToDelete.Ontology);

            facetadoCN.IniciarTransaccion();

            try
            {
                EliminarTriplesConceptGrafos(facetadoCN, proyectoID, pConceptToDelete.ConceptSubject, pConceptToDelete.Ontology, true);
            }
            catch (Exception ex)
            {
                facetadoCN.TerminarTransaccion(false);
                throw new Exception(ex.Message, ex);
            }

            facetadoCN.TerminarTransaccion(true);
        }

        #endregion

        #region Private

        /// <summary>
        /// Genera una propiedad member del Collection del tesauro para el Concept indicado
        /// </summary>
        /// <param name="pConcept">Concept que estamos cargando</param>
        /// <param name="pSource">Source del tesauro</param>
        /// <param name="pTriplesGrafoOntologia">Conjunto de triples para el grafo de ontología</param>
        /// <param name="pTriplesGrafoBusqueda">Conjunto de triples para el frafo de búsqeeda</param>
        /// <param name="pFacetadoCN">FacetadoCN inicializado</param>
        private void GenerarMemberCollection(Concept pConcept, string pSource, StringBuilder pTriplesGrafoOntologia, StringBuilder pTriplesGrafoBusqueda, FacetadoCN pFacetadoCN)
        {
            string sujetoCollection = string.Empty;

            string querySujetoCollection = $"select distinct(?s) where {{ ?s <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.w3.org/2008/05/skos#Collection> . ?s <http://purl.org/dc/elements/1.1/source> \"{pSource}\" . }}";

            FacetadoDS resultadoConsulta = pFacetadoCN.LeerDeVirtuoso(querySujetoCollection, "Collection", string.Empty);

            foreach (DataRow fila in resultadoConsulta.Tables["Collection"].Rows)
            {
                sujetoCollection = fila["s"].ToString();
            }

            if (string.IsNullOrEmpty(sujetoCollection))
            {
                throw new GnossException($"No hay ningún Collection asociado al source \"{pSource}\" y estás cargando un Concept de primer nivel. Revisa el Collection cargado para este source por favor.", HttpStatusCode.BadRequest);
            }

            pTriplesGrafoOntologia.AppendLine($"<{sujetoCollection}> <http://www.w3.org/2008/05/skos#member> <{GenerarSujetoConcept(pConcept, UrlIntragnoss, pSource)}> . ");
            pTriplesGrafoBusqueda.AppendLine($"<{sujetoCollection}> <http://www.w3.org/2008/05/skos#member> <{GenerarSujetoConcept(pConcept, UrlIntragnoss, pSource)}> . ");
        }

        /// <summary>
        /// Genera la relación narrower del concept que estamos cargando con su padre
        /// </summary>
        /// <param name="pSujetoPadre">Sujeto del padre del concept que estamos cargando</param>
        /// <param name="pConcept">Concept que estamos cargando</param>
        /// <param name="pSource">Source del tesauro</param>
        /// <param name="pTriplesGrafoOntologia">Conjunto de triples para el grafo de ontología</param>
        /// <param name="pTriplesGrafoBusqueda">Conjunto de triples para el grafo de búsqueda</param>
        private void GenerarNarrowerPadre(string pSujetoPadre, Concept pConcept, string pSource, StringBuilder pTriplesGrafoOntologia, StringBuilder pTriplesGrafoBusqueda)
        {
            pTriplesGrafoBusqueda.AppendLine($"<{pSujetoPadre}> <http://www.w3.org/2008/05/skos#narrower> <{GenerarSujetoConcept(pConcept, UrlIntragnoss, pSource)}>");
            pTriplesGrafoOntologia.AppendLine($"<{pSujetoPadre}> <http://www.w3.org/2008/05/skos#narrower> <{GenerarSujetoConcept(pConcept, UrlIntragnoss, pSource)}>");
        }

        /// <summary>
        /// Create a thesaurus according the parameters given
        /// </summary>
        /// <param name="pTesauro">Thesaurus with the Collection and the Concept to create</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pFacetadoCN">FacetadoCN initialized</param>
        private void CrearTesauro(Thesaurus pTesauro, string pUrlIntragnoss, Guid pProyectoID, FacetadoCN pFacetadoCN)
        {
            InsertarTriplesCollection(pTesauro, pUrlIntragnoss, pProyectoID, pFacetadoCN);
            InsertarTriplesConcept(pTesauro, pUrlIntragnoss, pProyectoID, pFacetadoCN);
        }

        /// <summary>
        /// Delete current data of the Concept indicated in the param from ontology and search graph
        /// </summary>
        /// <param name="pOntologia">Ontology of the thesaurus to remove</param> 
        /// <param name="pConcept">Concept to delete</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pSource"></param>
        /// <param name="pFacetadoCN">FacetadoCN initialized</param>
        /// <param name="pModificarNarrower">Indicates if the narrower of the concept will be modify</param>
        private void EliminarConcept(string pOntologia, Concept pConcept, Guid pProyectoID, string pSource, FacetadoCN pFacetadoCN, bool pModificarNarrower)
        {
            string sujetoConcept = GenerarSujetoConcept(pConcept, UrlIntragnoss, pSource);

            EliminarTriplesConceptGrafos(pFacetadoCN, pProyectoID, sujetoConcept, pOntologia, pModificarNarrower);
        }

        /// <summary>
        /// Get all the triples of the subjet given from ontology and search graph. Then remove it less broader and narrower if it's indicated
        /// </summary>
        /// <param name="pFacetadoCN">FacetadoCN initialized</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pSujetoConcept">Subject of the concept to delete</param>
        /// <param name="pOntologia">Ontology of the thesaurus to remove</param> 
        /// <param name="pEliminarHijos">Indicates if childrens of the concept will be removed</param>
        private void EliminarTriplesConceptGrafos(FacetadoCN pFacetadoCN, Guid pProyectoID, string pSujetoConcept, string pOntologia, bool pEliminarHijos)
        {
            pFacetadoCN.EliminarConceptEHijos(pOntologia, pSujetoConcept, pEliminarHijos);
            pFacetadoCN.EliminarConceptEHijos(pProyectoID.ToString(), pSujetoConcept, pEliminarHijos);
        }

        /// <summary>
        /// Get all the triples of the graph and subject indicated
        /// </summary>
        /// <param name="pGrafo">graph from will be geted the triples</param>
        /// <param name="pFacetadoCN">FacetadoCN initialiced</param>
        /// <param name="pSujetoConcept">Subject of the concept to get all their triples</param>
        /// <param name="pConjuntoTriples">StringBuilder when the triples will be loaded</param>
        private void ObtenerTriplesConceptBorrarGrafo(string pGrafo, FacetadoCN pFacetadoCN, string pSujetoConcept, StringBuilder pConjuntoTriples)
        {
            List<string> listaTriplesConcept = pFacetadoCN.ObtenerListaTriplesDeSujeto(pGrafo, pSujetoConcept);

            foreach (string tripleConcept in listaTriplesConcept)
            {
                pConjuntoTriples.AppendLine(tripleConcept);
                if (tripleConcept.Contains("<http://www.w3.org/2008/05/skos#narrower>"))
                {
                    string sujetoConceptHijo = tripleConcept.Split(" ")[2];
                    ObtenerTriplesConceptBorrarGrafo(pGrafo, pFacetadoCN, sujetoConceptHijo, pConjuntoTriples);
                }
            }
        }

        /// <summary>
        /// Insert all the triples for the list of the Concepts given in the Thesaurus parameter
        /// </summary>
        /// <param name="pTesauro">Thesaurus to load</param>
        /// <param name="pUrlIntragnoss">Url intragnoss</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pFacetadoCN">FacetadoCN initializated</param>
        /// <exception cref="Exception"></exception>
        private void InsertarTriplesConcept(Thesaurus pTesauro, string pUrlIntragnoss, Guid pProyectoID, FacetadoCN pFacetadoCN)
        {
            StringBuilder triplesConceptGrafoOntologia = new StringBuilder();
            StringBuilder triplesConceptGrafoBusqueda = new StringBuilder();

            foreach (Concept concept in pTesauro.Collection.Member)
            {
                GenerarTriplesConcept(concept, pTesauro.Source, pUrlIntragnoss, triplesConceptGrafoOntologia, triplesConceptGrafoBusqueda);
            }

            try
            {
                pFacetadoCN.InsertaTripletas(pTesauro.Ontology, triplesConceptGrafoOntologia.ToString(), 1);
                pFacetadoCN.InsertaTripletas(pProyectoID.ToString(), triplesConceptGrafoBusqueda.ToString(), 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ha ocurrido un error al escribir los triples de los Concept en virtuoso.\n {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Insert all the triples for the list of the Concepts given in the Thesaurus parameter
        /// </summary>
        /// <param name="pOntology">Ontology of the thesaurus</param>
        /// <param name="pTriplesGrafoOntologia">StringBuilder where the triples of the ontology graph will be storage</param>
        /// <param name="pTriplesGrafoBusqueda">StringBuilder where the triples of the search graph will be storage</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pFacetadoCN">FacetadoCN initializated</param>
        /// <exception cref="Exception"></exception>
        private void InsertarTriplesConcept(string pOntology, Guid pProyectoID, StringBuilder pTriplesGrafoOntologia, StringBuilder pTriplesGrafoBusqueda, FacetadoCN pFacetadoCN)
        {
            try
            {
                pFacetadoCN.InsertaTripletas(pOntology, pTriplesGrafoOntologia.ToString(), 1);
                pFacetadoCN.InsertaTripletas(pProyectoID.ToString(), pTriplesGrafoBusqueda.ToString(), 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ha ocurrido un error al escribir los triples de los Concept en virtuoso.\n {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate the triples of the Concept given by parameter
        /// </summary>
        /// <param name="pConcept">Concept to generate the triples</param>
        /// <param name="pSource">Source of the Thesaurus</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss</param>
        /// <param name="pTriplesGrafoOntologia">StringBuilder where the triples of the ontology graph will be storage</param>
        /// <param name="pTriplesGrafoBusqueda">StringBuilder where the triples of the search graph will be storage</param>
        /// <param name="pSujetoPadre">Subject of the parenth. Nullable</param>
        private void GenerarTriplesConcept(Concept pConcept, string pSource, string pUrlIntragnoss, StringBuilder pTriplesGrafoOntologia, StringBuilder pTriplesGrafoBusqueda, string pSujetoPadre = "")
        {
            string sujeto = GenerarSujetoConcept(pConcept, pUrlIntragnoss, pSource);

            GenerarTriplesConceptGrafoOntologia(pConcept, pTriplesGrafoOntologia, sujeto, pSource);
            GenerarTriplesConceptGrafoBusqueda(pConcept, pTriplesGrafoBusqueda, sujeto, pSource);
            GenerarPrefLabelMultiIdioma(pConcept, pTriplesGrafoOntologia, pTriplesGrafoBusqueda, sujeto);
            GenerarTriplesRelacionesConcept(pConcept, pTriplesGrafoOntologia, pTriplesGrafoBusqueda, sujeto, pUrlIntragnoss, pSource, pSujetoPadre);

            if (pConcept.Narrower != null)
            {
                foreach (Concept pConceptHijo in pConcept.Narrower)
                {
                    GenerarTriplesConcept(pConceptHijo, pSource, pUrlIntragnoss, pTriplesGrafoOntologia, pTriplesGrafoBusqueda, sujeto);
                }
            }
        }

        /// <summary>
        /// Insert all the triples for the list of the Collections given in the Thesaurus parameter
        /// </summary>
        /// <param name="pTesauro">Thesaurus to load</param>
        /// <param name="pUrlIntragnoss">Url intragnoss</param>
        /// <param name="pProyectoID">Identifier of the project</param>
        /// <param name="pFacetadoCN">FacetadoCN initializated</param>
        /// <exception cref="Exception"></exception>
        private void InsertarTriplesCollection(Thesaurus pTesauro, string pUrlIntragnoss, Guid pProyectoID, FacetadoCN pFacetadoCN)
        {
            pFacetadoCN.InsertarTriplesCollection(pTesauro, pUrlIntragnoss, pProyectoID);
        }

        /// <summary>
        /// Add triples to the string builder to generate the Concept in the ontology graph (RdfType, Label, Symbol, Source, Identifier)
        /// </summary>
        /// <param name="pConcept">Concept to be generated</param>
        /// <param name="pStringBuilder">StringBuilder where the triples will be stored</param>
        /// <param name="pSujeto">Subject of the Concept to generate</param>
        /// <param name="pSource">Source of the thesaurus</param>
        private void GenerarTriplesConceptGrafoOntologia(Concept pConcept, StringBuilder pStringBuilder, string pSujeto, string pSource)
        {
            pStringBuilder.AppendLine($"<{pSujeto}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://www.w3.org/2008/05/skos#Concept> . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://www.w3.org/2000/01/rdf-schema#label> \"http://www.w3.org/2008/05/skos#Concept\" . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#symbol> \"{pConcept.Symbol}\" . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://purl.org/dc/elements/1.1/source> \"{pSource}\" . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://purl.org/dc/elements/1.1/identifier> \"{pConcept.Identifier}\" . ");
        }

        /// <summary>
        /// Add triples to the string builder to generate the Concept in the ontology graph (Symbol, Source, Identifier)
        /// </summary>
        /// <param name="pConcept">Concept to be generated</param>
        /// <param name="pStringBuilder">StringBuilder where the triples will be stored</param>
        /// <param name="pSujeto">Subject of the Concept to generate</param>
        private void GenerarTriplesConceptGrafoBusqueda(Concept pConcept, StringBuilder pStringBuilder, string pSujeto, string pSource)
        {
            pStringBuilder.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#symbol> \"{pConcept.Symbol}\"^^<http://www.w3.org/2001/XMLSchema#int> . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://purl.org/dc/elements/1.1/source> \"{pSource}\" . ");
            pStringBuilder.AppendLine($"<{pSujeto}> <http://purl.org/dc/elements/1.1/identifier> \"{pConcept.Identifier}\" . ");
        }

        /// <summary>
        /// Generate the triples of the property PrefLabel with their language
        /// </summary>
        /// <param name="pConcept">Concept that has the property preflabel</param>
        /// <param name="pStringBuilderOntologia">StringBuilder where the triples of the ontology graph will be storage</param>
        /// <param name="pStringBuilderBusqueda">StringBuilder where the triples of the search graph will be storage</param>
        /// <param name="pSujeto">Subject of the Concept</param>
        private void GenerarPrefLabelMultiIdioma(Concept pConcept, StringBuilder pStringBuilderOntologia, StringBuilder pStringBuilderBusqueda, string pSujeto)
        {
            foreach (string clave in pConcept.PrefLabel.Keys)
            {
                pStringBuilderOntologia.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#prefLabel> \"{pConcept.PrefLabel[clave]}\"@{clave} . ");
                pStringBuilderBusqueda.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#prefLabel> \"{pConcept.PrefLabel[clave]}\"@{clave} . ");
            }
        }

        /// <summary>
        /// Generate the triples that define the relation between the Concepts. (Broader, Narrower, Related)
        /// </summary>
        /// <param name="pConcept">Concept that will be generated</param>
        /// <param name="pStringBuilderOntologia">StringBuilder where the triples of the ontology graph will be storage</param>
        /// <param name="pStringBuilderBusqueda">StringBuilder where the triples of the search graph will be storage</param>
        /// <param name="pSujeto">Subject of the concept</param>
        /// <param name="pUrlIntraGnoss">UrlIntragnoss</param>
        /// <param name="pSource">Souce of the thesaurus</param>
        /// <param name="pSujetoPadre">Subject of the parent of the Concept</param>
        private void GenerarTriplesRelacionesConcept(Concept pConcept, StringBuilder pStringBuilderOntologia, StringBuilder pStringBuilderBusqueda, string pSujeto, string pUrlIntraGnoss, string pSource, string pSujetoPadre = "")
        {
            if (pConcept.Narrower != null)
            {
                foreach (Concept narrower in pConcept.Narrower)
                {
                    pStringBuilderOntologia.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#narrower> <{GenerarSujetoConcept(narrower, pUrlIntraGnoss, pSource)}> . ");
                    pStringBuilderBusqueda.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#narrower> <{GenerarSujetoConcept(narrower, pUrlIntraGnoss, pSource)}> . ");
                }
            }
            if (!pSujetoPadre.IsNullOrEmpty())
            {
                pStringBuilderOntologia.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#broader> <{GenerarSujetoString(pSujetoPadre, pUrlIntraGnoss)}> . ");
                pStringBuilderBusqueda.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#broader> <{GenerarSujetoString(pSujetoPadre, pUrlIntraGnoss)}> . ");
            }
            if (pConcept.RelatedTo != null)
            {
                foreach (Concept relatedTo in pConcept.RelatedTo)
                {
                    pStringBuilderOntologia.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#related> <{GenerarSujetoConcept(relatedTo, pUrlIntraGnoss, pSource)}> . ");
                    pStringBuilderBusqueda.AppendLine($"<{pSujeto}> <http://www.w3.org/2008/05/skos#related> <{GenerarSujetoConcept(relatedTo, pUrlIntraGnoss, pSource)}> . ");
                }
            }
        }

        /// <summary>
        /// Add the row that represents the thesaurus in the database
        /// </summary>
        /// <param name="pProyectoID">ProyectID</param>
        /// <param name="pSource">Source of the thesaurus to load</param>
        /// <param name="pScopeNote">Scope note of the Collection</param>
        /// <param name="pSujeto">Subject of the Collection</param>
        /// <param name="pOntologia">Ontology</param>
        /// <param name="pUrlIntragnoss">Url intragnoss</param>
        private void AgregarProyectoConfigExtraSem(Guid pProyectoID, string pSource, Dictionary<string, string> pScopeNote, string pSujeto, string pOntologia, string pUrlIntragnoss)
        {
            ProyectoConfigExtraSem filaConfig = new ProyectoConfigExtraSem();
            filaConfig.ProyectoID = pProyectoID;
            filaConfig.Tipo = 0;
            filaConfig.Editable = true;
            filaConfig.Nombre = pSource.First().ToUpper() + pSource.Substring(1);
            filaConfig.SourceTesSem = pSource;
            if (!string.IsNullOrEmpty(pSujeto))
            {
                if (pSujeto.StartsWith(pUrlIntragnoss))
                {   
                    filaConfig.PrefijoTesSem = pSujeto.Substring(pSujeto.LastIndexOf('/') + 1);
                }
                else
                {
                    filaConfig.PrefijoTesSem = pSujeto;
                }
            }
            else
            {
                filaConfig.PrefijoTesSem = pSource;
            }

            List<string> listaIdiomas = pScopeNote.Keys.ToList();
            string idiomas = string.Empty;
            foreach (string idioma in listaIdiomas)
            {
                idiomas += $"{idioma},";
            }
            idiomas = idiomas.Substring(0, idiomas.Length - 1);

            filaConfig.Idiomas = idiomas;
            filaConfig.UrlOntologia = pOntologia;

            if (mEntityContext.ProyectoConfigExtraSem.FirstOrDefault(proy => proy.ProyectoID.Equals(filaConfig.ProyectoID) && proy.UrlOntologia.Equals(filaConfig.UrlOntologia) && proy.SourceTesSem.Equals(filaConfig.SourceTesSem)) == null)
            {
                mEntityContext.ProyectoConfigExtraSem.Add(filaConfig);
                mEntityContext.SaveChanges();
            }
        }

        /// <summary>
        /// Generate the Subject of the Concept given by parameter
        /// </summary>
        /// <param name="pConcept">Concept to generate the Subject</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss</param>
        /// <param name="pSource">Source of the tesaurus</param>
        /// <returns>The Subject of the Concept</returns>
        private string GenerarSujetoConcept(Concept pConcept, string pUrlIntragnoss, string pSource)
        {
            string sujeto = pConcept.Subject;
            if (string.IsNullOrEmpty(sujeto))
            {
                sujeto = $"{pUrlIntragnoss}items/{pSource}_{pConcept.Identifier}";
            }

            if (!sujeto.StartsWith(pUrlIntragnoss))
            {
                sujeto = $"{pUrlIntragnoss}items/{sujeto}";
            }

            return sujeto;
        }

        /// <summary>
        /// Generate the Subject of the Concept given by parameter
        /// </summary>
        /// <param name="pConceptSubject">Concept to generate the Subject</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss</param>
        /// <returns>The Subject of the Concept</returns>
        private string GenerarSujetoString(string pConceptSubject, string pUrlIntragnoss)
        {
            string sujeto = pConceptSubject;

            if (!sujeto.StartsWith(pUrlIntragnoss))
            {
                sujeto = $"{pUrlIntragnoss}items/{sujeto}";
            }

            return sujeto;
        }

        /// <summary>
        /// Adjust the url of the graph adding the current UrlIntragnoss.
        /// </summary>
        /// <param name="pUrlGrafo">Url of the graph</param>
        /// <returns>URL of the graph adding the current UrlIntragnoss</returns>
        private string AjustarUrlGrafoSegunUrlIntragnoss(string pUrlGrafo)
        {
            if (pUrlGrafo.Contains("/"))
            {
                pUrlGrafo = pUrlGrafo.Substring(pUrlGrafo.LastIndexOf("/") + 1);
            }

            if (pUrlGrafo.LastIndexOf("#") == (pUrlGrafo.Length - 1))
            {
                pUrlGrafo = pUrlGrafo.Substring(0, pUrlGrafo.Length - 1);
            }

            return UrlIntragnoss + pUrlGrafo;
        }

        /// <summary>
        /// Makes sure that the ontology end with .owl if it dont, if it end with with .owl don't do nothing
        /// </summary>
        /// <param name="pOntology">Ontology name</param>
        /// <returns>The ontology name ending with .owl</returns>
        private string ParsearOntologia(string pOntology)
        {
            if (!pOntology.EndsWith(".owl"))
            {
                pOntology += ".owl";
            }

            return pOntology;
        }

        #endregion
    }
}