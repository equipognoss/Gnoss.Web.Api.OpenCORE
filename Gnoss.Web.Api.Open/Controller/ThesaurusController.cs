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

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    /// <summary>
    /// Use it to create / modify / delete nodes in a thesaurus
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ThesaurusController : ControlApiGnossBase
    {

        public ThesaurusController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {
        }

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

            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
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

                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

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

                //HttpResponseMessage response = new HttpResponseMessage();
                //response.StatusCode = HttpStatusCode.OK;
                //response.Content = new StreamContent(buffer);
                //response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/rdf");
                //response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                //{
                //    FileName = source + ".rdf"
                //};
                //return response;

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

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            Guid elementoVinculadoID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, parameters.resources_ontology_url);
            GestorDocumental gestorDoc = new GestorDocumental(docCN.ObtenerDocumentoPorID(elementoVinculadoID), mLoggingService, mEntityContext);
            docCN.Dispose();

            if (elementoVinculadoID == Guid.Empty || !gestorDoc.ListaDocumentos.ContainsKey(elementoVinculadoID))
            {
                throw new GnossException("There is no ontology resource with the specified URL", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.MoverCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, parameters.resources_ontology_url, UrlIntragnoss, parameters.category_id, parameters.path, docOnto, gestorDoc.ListaDocumentos[elementoVinculadoID], true, ProyectoTraerOntosID);
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
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
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                AD.EntityModel.Models.Tesauro.CategoriaTesauro categoriaTesauro = dataWrapperTesauro.ListaCategoriaTesauro.Where(item => item.CategoriaTesauroID.Equals(parameters.category_id)).FirstOrDefault();

                if (categoriaTesauro == null)
                {
                    throw new GnossException($"There is no category with this category id: {parameters.category_id}", HttpStatusCode.BadRequest);
                }

                categoriaTesauro.Nombre = parameters.new_category_name;
                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
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
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                GestionTesauro gestorTesauro = new GestionTesauro(dataWrapperTesauro, mLoggingService, mEntityContext);

                if (parameters.parent_catergory_id.HasValue && !parameters.parent_catergory_id.Value.Equals(Guid.Empty))
                {
                    if (gestorTesauro.ListaCategoriasTesauro.ContainsKey(parameters.parent_catergory_id.Value))
                    {
                        var categoriaSuperior = gestorTesauro.ListaCategoriasTesauro[parameters.parent_catergory_id.Value];
                        gestorTesauro.AgregarSubcategoria(categoriaSuperior, parameters.category_name);
                    }
                }
                else
                {
                    gestorTesauro.AgregarCategoriaPrimerNivel(parameters.category_name);
                }

                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
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
            ProyectoCN pry = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Guid proyectoID = pry.ObtenerProyectoIDPorNombre(parameters.community_short_name);

            if (!proyectoID.Equals(Guid.Empty))
            {
                TesauroCN tesauroCN = new TesauroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperTesauro dataWrapperTesauro = tesauroCN.ObtenerTesauroDeProyecto(proyectoID);
                GestionTesauro gestorTesauro = new GestionTesauro(dataWrapperTesauro, mLoggingService, mEntityContext);

                if (gestorTesauro.ListaCategoriasTesauro.ContainsKey(parameters.category_id))
                {
                    var categoriaEliminar = gestorTesauro.ListaCategoriasTesauro[parameters.category_id];

                    DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    List<Guid> listaCategoriasEliminar = categoriaEliminar.ObtenerCategoriasHijosNietos();
                    listaCategoriasEliminar.Add(categoriaEliminar.Clave);
                    DataWrapperDocumentacion dataWrapperDocumentacion = documentacionCN.ObtenerDocWebAgCatTesauroPorCategoriasId(listaCategoriasEliminar);
                    gestorTesauro.GestorDocumental = new GestorDocumental(dataWrapperDocumentacion, mLoggingService, mEntityContext);

                    gestorTesauro.EliminarCategoriaEHijos(categoriaEliminar);
                    documentacionCN.Actualizar();
                }
                else
                {
                    throw new GnossException($"The category {parameters.category_id} isn't exists in the community {parameters.community_short_name}", HttpStatusCode.BadRequest);
                }

                tesauroCN.Actualizar();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                tesauroCL.InvalidarCacheDeTesauroDeProyecto(proyectoID);
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
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

            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            Guid elementoVinculadoID = docCN.ObtenerOntologiaAPartirNombre(ProyectoTraerOntosID, parameters.resources_ontology_url);
            GestorDocumental gestorDoc = new GestorDocumental(docCN.ObtenerDocumentoPorID(elementoVinculadoID), mLoggingService, mEntityContext);
            docCN.Dispose();

            if (elementoVinculadoID == Guid.Empty || !gestorDoc.ListaDocumentos.ContainsKey(elementoVinculadoID))
            {
                throw new GnossException("There is no ontology resource with the specified URL", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.EliminarCategoriaTesauroSemantico(parameters.thesaurus_ontology_url, parameters.resources_ontology_url, UrlIntragnoss, BaseURLFormulariosSem, parameters.category_id, parameters.path, docOnto, gestorDoc.ListaDocumentos[elementoVinculadoID], true, ProyectoTraerOntosID);
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        /// <summary>
        /// Add a category as a parent of another
        /// </summary>
        /// <param name="url_ontology_thesaurus">URL ontology semantic thesaurus</param>
        /// <param name="short_community_name">URL of the community is raised semantic ontology thesaurus</param>
        /// <param name="catergory_parent_key">URI of the parent category</param>
        /// <param name="category_child_key">URI of the category child</param>
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

            List<string> propiedades = new List<string>();
            propiedades.Add(arrayTesSem[4]);//hasHijo
            propiedades.Add(arrayTesSem[7]);//hasPadre

            FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

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

            FacetadoDS dataSetCategoriasPadre = facCN.ObtenerValoresPropiedadesEntidad(parameters.thesaurus_ontology_url, parameters.parent_catergory_id, false);

            if (dataSetCategoriasPadre.Tables[0].Rows.Count == 0)
            {
                facCN.Dispose();
                throw new GnossException("There is no category with this URI '" + parameters.parent_catergory_id + "'", HttpStatusCode.BadRequest);
            }

            ControladorDocumentacion.EscribirTripletaEntidad(parameters.child_category_id, arrayTesSem[7], parameters.parent_catergory_id, ref triplesInsertar, triplesComInsertar, false, null);
            ControladorDocumentacion.EscribirTripletaEntidad(parameters.parent_catergory_id, arrayTesSem[4], parameters.child_category_id, ref triplesInsertar, triplesComInsertar, false, null);

            //Genero las triples de la comunidad:
            string triplesComInsertarDef = "";
            List<string> listaAux = new List<string>();

            FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
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
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                facetadoCL.InvalidarCacheTesauroFaceta(proyectoID);
                facetadoCL.Dispose();
            }

            dataSetCategorias.Dispose();
            dataSetCategoriasPadre.Dispose();
        }

        /// <summary>
        /// Modifies the name of a category of semantic thesaurus
        /// </summary>
        /// <param name="url_ontology_thesaurus">URL ontology semantic thesaurus</param>
        /// <param name="short_community_name">UURL of the community is raised semantic ontology thesaurus</param>
        /// <param name="category_key">URI of the category</param>
        /// <param name="category_name">Name category, supports multi language with format: nombre@es|||name@en|||</param>
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
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        /// <summary>
        /// Enter a category in a semantic thesaurus.
        /// </summary>
        /// <param name="url_ontology_thesaurus">URL ontology semantic thesaurus</param>
        /// <param name="short_community_name">UURL of the community is raised semantic ontology thesaurus</param>
        /// <param name="rdf_category">Rdf insert category</param>
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
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                facetadoCL.InvalidarCacheTesauroFaceta(FilaProy.ProyectoID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new GnossException("The ontology can not be empty", HttpStatusCode.BadRequest);
            }

        }

        /// <summary>
        /// Ajusta la URL del grafo poniendole la UrlIntragnoss actual.
        /// </summary>
        /// <param name="pUrlGrafo">Url del grafo</param>
        /// <returns>URL del grafo poniendole la UrlIntragnoss actual</returns>
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
    }
}