using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Parametro;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.Elementos.ParametroAplicacion;
using Es.Riam.Gnoss.Elementos.Suscripcion;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.RDF;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.Controles.Documentacion;
using Es.Riam.Gnoss.Web.Controles.ParametroAplicacionGBD;
using Es.Riam.Gnoss.Web.Controles.ServicioImagenesWrapper;
using Es.Riam.Gnoss.Web.MVC.Models;
using Es.Riam.Interfaces.InterfacesOpen;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC
{
    /// <summary>
    /// Descripción breve de ControladorApiRecursos
    /// </summary>
    public class ControladorApiRecursos
    {
        #region Miembros

        /// <summary>
        /// Devuelve la URL del servicio de documentación
        /// </summary>
        string mUrlServicioWebDocumentacion;

        /// <summary>
        /// Devuelve la URL del servicio de documentación en links.
        /// </summary>
        private string mUrlServicioDocumentosLink;

        /// <summary>
        /// Devuelve la URL del servicio de imágenes
        /// </summary>
        private string mUrlServicioImagenes;

        /// <summary>
        /// Dataset de los parámetros de aplicación
        /// </summary>
        private GestorParametroAplicacion mParametrosAplicacionDS;

        private string mUrlServicioInterno = null;

        /// <summary>
        /// Parametros de configuración del proyecto.
        /// </summary>
        private Dictionary<string, string> mParametroProyecto;

        private Guid mProyectoID;

        #endregion

        #region Constructores

        private readonly IHttpContextAccessor mHttpContextAccessor;
        private readonly LoggingService mLoggingService;
        private readonly EntityContext mEntityContext;
        private readonly ConfigService mConfigService;
        private readonly VirtuosoAD mVirtuosoAD;
        private readonly RedisCacheWrapper mRedisCacheWrapper;
        private readonly GnossCache mGnossCache;
        private readonly EntityContextBASE mEntityContextBASE;
        private readonly IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;
        private readonly IAvailableServices mAvailableServices;
        private ILogger mlogger;
        private ILoggerFactory mLoggerFactory;
        public ControladorApiRecursos(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, IAvailableServices availableServices, ILogger<ControladorApiRecursos> logger, ILoggerFactory loggerFactory)
        {
            mHttpContextAccessor = httpContextAccessor;
            mLoggingService = loggingService;
            mEntityContext = entityContext;
            mConfigService = configService;
            mVirtuosoAD = virtuosoAD;
            mRedisCacheWrapper = redisCacheWrapper;
            mGnossCache = gnossCache;
            mEntityContextBASE = entityContextBASE;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mAvailableServices = availableServices;
            mlogger = logger;
            mLoggerFactory = loggerFactory;
        }

        #endregion

        #region Métodos Generales

        #region Públicos

        /// <summary>
        /// Elimina de forma masiva los recursos vinculados a la comunidad
        /// </summary>
        /// <param name="pOntologiaProyecto">Campo OntologiaProyecto de la tabla OntologiaProyecto</param>
        /// <param name="pOrganizacionID">Identificador de la organización</param>
        /// <param name="pProyectoID">Identificador de la comunidad</param>
        /// <param name="pUrlIntragnoss">Url interna de gnoss</param>
        /// <param name="pUrlServicioWebDocumentacion">Url del servicio web de documentación</param>
        public void EliminarMasivoRecursosComunidad(Guid pOrganizacionID, Guid pProyectoID, string pUrlIntragnoss, string pUrlServicioWebDocumentacion)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            FacetaCN facetaCN = new FacetaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetaCN>(), mLoggerFactory);
            List<AD.EntityModel.Models.Faceta.OntologiaProyecto> listaOntologias = facetaCN.ObtenerOntologias(pProyectoID, false);
            Dictionary<Guid, List<Guid>> dicOntologiaDocumentosVinculados = new Dictionary<Guid, List<Guid>>();
            Dictionary<Guid, string> dicOntologiaEnlace = new Dictionary<Guid, string>();

            mProyectoID = pProyectoID;

            foreach (string ontologia in listaOntologias.Select(item => item.OntologiaProyecto1))
            {
                string nombreOntologia = ontologia;
                if (!ontologia.Contains(".owl"))
                {
                    nombreOntologia += ".owl";
                }

                Guid proyOntoID = pProyectoID;
                ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                Dictionary<string, string> parametroProyecto = proyectoCL.ObtenerParametrosProyecto(pProyectoID);
                proyectoCL.Dispose();

                if (parametroProyecto.ContainsKey(ParametroAD.ProyectoIDPatronOntologias))
                {
                    proyOntoID = new Guid(parametroProyecto[ParametroAD.ProyectoIDPatronOntologias]);
                }

                Guid ontologiaID = docCN.ObtenerOntologiaAPartirNombre(proyOntoID, nombreOntologia);

                if (!ontologiaID.Equals(Guid.Empty))
                {
                    dicOntologiaEnlace.Add(ontologiaID, docCN.ObtenerEnlaceDocumentoPorDocumentoID(ontologiaID));
                    dicOntologiaDocumentosVinculados.Add(ontologiaID, ObtenerDocumentosIDVinculadosAOntologiaProyecto(ontologiaID, pProyectoID));
                }
            }

            //lista de ids de todos los documentos vinculados a la comunidad
            List<Guid> idsDocumento = new List<Guid>();
            foreach (Guid ontologiaID in dicOntologiaDocumentosVinculados.Keys)
            {
                idsDocumento.AddRange(dicOntologiaDocumentosVinculados[ontologiaID]);
            }

            //Borrado del grafo de ontología de los elementos de la lista obtenida
            BorradoGrafoTodaOntologiaComunidad(dicOntologiaDocumentosVinculados, dicOntologiaEnlace, pProyectoID, pUrlIntragnoss);

            //Borrar los grafos de búsqueda en los que están compartidos los recursos
            BorrarGrafoBusqueda(idsDocumento, pProyectoID);
            //Borrado de la BD rdf de los proyectos donde estén compartidos esos documentos
            BorradoRDFTodaComunidad(idsDocumento, pProyectoID);

            //Borrado de imágenes
            BorrarImagenRecursos(idsDocumento);

            //Borrado de archivos
            BorrarArchivosDocumentosTodasOntologiasComunidad(dicOntologiaDocumentosVinculados, dicOntologiaEnlace);

            //Borrar los documentos vinculados de la base ácida
            BorrarDocumentos(idsDocumento);

            facetaCN.Dispose();
            docCN.Dispose();
        }

        /// <summary>
        /// Elimina de forma masiva los recursos vinculados a la ontología
        /// </summary>
        /// <param name="pOntologiaProyecto">Campo OntologiaProyecto de la tabla OntologiaProyecto</param>
        /// <param name="pOrganizacionID">Identificador de la organización</param>
        /// <param name="pProyectoID">Identificador de la comunidad</param>
        /// <param name="pUrlIntragnoss">Url interna de gnoss</param>
        /// <param name="pUrlServicioWebDocumentacion">Url del servicio web de documentación</param>
        public string EliminarMasivoRecursosOntologia(string pOntologiaProyecto, Guid pOrganizacionID, Guid pProyectoID, string pUrlIntragnoss, string pUrlServicioWebDocumentacion)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            string mensaje = string.Empty;

            if (!pOntologiaProyecto.Contains(".owl"))
            {
                pOntologiaProyecto += ".owl";
            }

            mProyectoID = pProyectoID;
            Guid proyOntoID = pProyectoID;
            ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
            Dictionary<string,string> parametroProyecto = proyectoCL.ObtenerParametrosProyecto(pProyectoID);
            proyectoCL.Dispose();

            if (parametroProyecto.ContainsKey(ParametroAD.ProyectoIDPatronOntologias))
            {
                proyOntoID = new Guid(parametroProyecto[ParametroAD.ProyectoIDPatronOntologias]);
            }

            Guid ontologiaID = docCN.ObtenerOntologiaAPartirNombre(proyOntoID, pOntologiaProyecto);

            if (!ontologiaID.Equals(Guid.Empty))
            {
                //Obtenemos lista de identificadores cuyo elemento vinculado es el ID del documento de la ontologia
                List<Guid> idsDocumento = ObtenerDocumentosIDVinculadosAOntologiaProyecto(ontologiaID, pProyectoID);

                if (idsDocumento.Count > 0)
                {
                    //Borrado del grafo de ontología de los elementos de la lista obtenida
                    BorradoGrafoOntologia(idsDocumento, ontologiaID, pProyectoID, pUrlIntragnoss);

                    //Borrar los grafos de búsqueda en los que están compartidos los recursos
                    BorrarGrafoBusqueda(idsDocumento, pProyectoID);

                    //Borrado de la BD rdf de los proyectos donde estén compartidos esos documentos
                    BorradoRDF(idsDocumento);

                    //Borrado de imágenes
                    BorrarImagenRecursos(idsDocumento);

                    //Borrado de archivos
                    BorrarArchivosDocumentosOntologia(idsDocumento, ontologiaID);

                    //Borrar los documentos vinculados de la base ácida
                    BorrarDocumentos(idsDocumento);

                    mensaje = $"Ontología {pOntologiaProyecto} borrada";
                }
                else
                {                    
                    mensaje = "No se han encontrado documentos vinculados a la ontologia";
                }
            }

            docCN.Dispose();
            return mensaje;
        }

        /// <summary>
        /// Obtiene la lista de identificadores cuyo elemento vinculado es el ID de la ontologia(documentoID)
        /// </summary>
        /// <param name="pOntologiaProyecto">Corresponde al campo OntologiaProyecto de la tabla OntologiaProyecto</param>
        /// <param name="pProyectoID">Identificador de la comunidad</param>
        /// <returns>Lista de ids de los documentos vinculados a esa ontología</returns>
        public List<Guid> ObtenerDocumentosIDVinculadosAOntologiaProyecto(Guid pOntologiaID, Guid pProyectoID)
        {
            List<Guid> lista = new List<Guid>();
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);

            try
            {
                lista = docCN.ObtenerDocumentosIDVinculadosAOntologiaProyecto(pOntologiaID, pProyectoID);
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error ObtenerDocumentosIDVinculadosAOntologiaProyecto al obtener documentos vinculados a la ontologia: {pOntologiaID} del proyecto: {pProyectoID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }

            docCN.Dispose();
            return lista;
        }

        /// <summary>
        /// Borra del grafo de la ontología los documentos de la lista
        /// </summary>
        /// <param name="pDocumentosID">Lista de documentosID a eliminar</param>
        /// <param name="pOntologiaID">Identificador del documento ontología</param>
        /// <param name="pProyectoID">Identificador del proyecto</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss de la aplicación</param>
        public void BorradoGrafoTodaOntologiaComunidad(Dictionary<Guid, List<Guid>> pDicOntologiaDocumentosVinculados, Dictionary<Guid, string> pDicOntologiaEnlace, Guid pProyectoID, string pUrlIntragnoss)
        {
            foreach (Guid ontologiaID in pDicOntologiaDocumentosVinculados.Keys)
            {
                string enlace = pDicOntologiaEnlace[ontologiaID];

                foreach (Guid documentoID in pDicOntologiaDocumentosVinculados[ontologiaID])
                {
                    try
                    {
                        ControladorDocumentacion.BorrarRDFDeVirtuoso(documentoID.ToString(), enlace, pUrlIntragnoss, "", pProyectoID, null, false);
                    }
                    catch (Exception ex)
                    {
                        GuardarLogError($"Error BorradoGrafoOntologia al borrar el documento: {documentoID} del grafo de la ontologia: {ontologiaID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                    }
                }
            }
        }

        private ControladorDocumentacion mControladorDocumentacion;

        private ControladorDocumentacion ControladorDocumentacion
        {
            get
            {
                if(mControladorDocumentacion == null)
                {
                    mControladorDocumentacion = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ControladorDocumentacion>(), mLoggerFactory);
                }
                return mControladorDocumentacion;
            }
        }

        /// <summary>
        /// Borra del grafo de la ontología los documentos de la lista
        /// </summary>
        /// <param name="pDocumentosID">Lista de documentosID a eliminar</param>
        /// <param name="pOntologiaID">Identificador del documento ontología</param>
        /// <param name="pProyectoID">Identificador del proyecto</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss de la aplicación</param>
        public void BorradoGrafoOntologia(List<Guid> pDocumentosID, Guid pOntologiaID, Guid pProyectoID, string pUrlIntragnoss)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            string enlace = docCN.ObtenerEnlaceDocumentoPorDocumentoID(pOntologiaID);

            foreach (Guid documentoID in pDocumentosID)
            {
                try
                {
                    ControladorDocumentacion.BorrarRDFDeVirtuoso(documentoID.ToString(), enlace, pUrlIntragnoss, "", pProyectoID, null, false);
                }
                catch (Exception ex)
                {
                    GuardarLogError("Error BorradoGrafoOntologia al borrar el documento:" + documentoID + " del grafo de la ontologia: " + pOntologiaID + "\n Traza: " + ex.StackTrace + "\n Mensaje error: " + ex.Message, "BorrarRecursos");                    
                }
            }

            docCN.Dispose();
        }

        /// <summary>
        /// Borra del grafo de la ontología los documentos de la lista
        /// </summary>
        /// <param name="pDocumentosID">Lista de documentosID a eliminar</param>
        /// <param name="pOntologiaID">Identificador del documento ontología</param>
        /// <param name="pProyectoID">Identificador del proyecto</param>
        /// <param name="pUrlIntragnoss">UrlIntragnoss de la aplicación</param>
        public void BorradoGrafoOntologia(List<Guid> pDocumentosID, Guid pProyectoID, string pUrlIntragnoss)
        {
            foreach (Guid documentoID in pDocumentosID)
            {
                try
                {                    
                    ControladorDocumentacion.BorrarRDFDeVirtuosoPorGrafo(documentoID.ToString(), pUrlIntragnoss, "", pProyectoID, null, false);
                }
                catch (Exception ex)
                {
                    GuardarLogError($"Error BorradoGrafoOntologia al borrar el documento: {documentoID} \n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                }
            }
        }

        /// <summary>
        /// Borra en el grafo de busqueda el recurso.
        /// </summary>
        /// <param name="pDocumentosID"></param>
        public void BorradoGrafoBusqueda(List<Guid> pDocumentosID, Guid pProyectoActualID)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            string urlIntragnoss = ParametrosAplicacionDS.ParametroAplicacion.Find(parametroApp => parametroApp.Parametro.Equals("UrlIntragnoss")).Valor;
            FacetadoCN facetadoCN = new FacetadoCN(urlIntragnoss, pProyectoActualID.ToString(), mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);
            
            foreach(Guid documentoID in pDocumentosID)
            {
                facetadoCN.BorrarRecurso(pProyectoActualID.ToString(), documentoID, 0, "", false, true, true);
            }

            docCN.Dispose();
        }

        /// <summary>
        /// Borra los documentos de la BD rdf de todos los proyectos donde estén compartidos
        /// </summary>
        /// <param name="pDocumentosID">Lista de ids de los documentos a borrar</param>
        /// <param name="pProyectoID">Identificador de la comunidad</param>
        public void BorradoRDF(List<Guid> pDocumentosID)
        {
            RdfCN rdfCN = new RdfCN("rdf", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<RdfCN>(), mLoggerFactory);
            foreach (Guid documentoID in pDocumentosID)
            {
                try
                {
                    rdfCN.EliminarDocumentoDeRDF(documentoID);
                }
                catch (Exception ex)
                {
                    GuardarLogError($"Error BorradoRDF al borrar el documento: {documentoID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                }
            }
            rdfCN.Dispose();
        }

        /// <summary>
        /// Borra los documentos de la BD rdf de todos los proyectos donde estén compartidos
        /// </summary>
        /// <param name="pDocumentosID">Lista de ids de los documentos a borrar</param>
        /// <param name="pProyectoID">Identificador del proyecto</param>
        public void BorradoRDFTodaComunidad(List<Guid> pDocumentosID, Guid pProyectoID)
        {
            RdfCN rdfCN = new RdfCN("rdf", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<RdfCN>(), mLoggerFactory);
            Dictionary<string, List<Guid>> documentosOrdenados = new Dictionary<string, List<Guid>>();
            try
            {
                foreach (Guid documentoID in pDocumentosID)
                {
                    //clasificamos los documentos a borrar
                    string clave = documentoID.ToString().Substring(0, 3);

                    if (!documentosOrdenados.ContainsKey(clave))
                    {
                        documentosOrdenados.Add(clave, new List<Guid>());
                    }
                    documentosOrdenados[clave].Add(documentoID);
                }

                foreach (string claveDoc in documentosOrdenados.Keys)
                {
                    rdfCN.EliminarDocumentosDeRDF(claveDoc, documentosOrdenados[claveDoc]);
                }
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error BorradoRDF al borrar los documentos del proyecto: {pProyectoID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }

            rdfCN.Dispose();
        }

        /// <summary>
        /// Borra los grafos de búsqueda en los que están compartidos los recursos
        /// </summary>
        /// <param name="pDocumentosID"></param>
        public void BorrarGrafoBusqueda(List<Guid> pDocumentosID, Guid pProyectoActualID)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            //obtener los ids de los proyectos donde está compartido cada documento
            Dictionary<Guid, List<Guid>> documentosCompartidosEn = docCN.ObtenerProyectosEstanCompartidosDocsPorID(pDocumentosID);
            Dictionary<Guid, TiposDocumentacion> documentosTipo = docCN.ObtenerTiposDocumentosPorDocumentosID(pDocumentosID);
            string urlIntragnoss = ParametrosAplicacionDS.ParametroAplicacion.Find(parametroApp => parametroApp.Parametro.Equals("UrlIntragnoss")).Valor;
            FacetadoCN facetadoCN = new FacetadoCN(urlIntragnoss, pProyectoActualID.ToString(), mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<FacetadoCN>(), mLoggerFactory);

            //Insertar las filas en colatagscomunidades por cada proyecto en el que está compartido el documento
            foreach (Guid documentoID in documentosCompartidosEn.Keys)
            {
                short tipo = (short)documentosTipo[documentoID];
                foreach (Guid proyectoID in documentosCompartidosEn[documentoID])
                {
                    try
                    {
                        if (proyectoID.Equals(pProyectoActualID))
                        {
                            facetadoCN.BorrarRecurso(proyectoID.ToString(), documentoID, 0, "", false, true, true);
                        }
                        ControladorDocumentacion.EliminarRecursoModeloBaseSimple(documentoID, proyectoID, tipo, null, string.Empty, 0, (short)PrioridadBase.ApiRecursos, mAvailableServices);
                    }
                    catch (Exception ex)
                    {
                        GuardarLogError($"Error al borrar autocompletar: {documentoID} del proyecto: {proyectoID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                    }
                }

            }
            docCN.Dispose();
        }

        /// <summary>
        /// Borra las imagenes de los recursos
        /// </summary>
        /// <param name="pDocumentosID">Lista de identificadores de los recursos cuyas imágenes se van a borrar</param>
        public void BorrarImagenRecursos(List<Guid> pDocumentosID)
        {
            ServicioImagenes servicioImagenes = new ServicioImagenes(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<ServicioImagenes>(), mLoggerFactory);
            servicioImagenes.Url = UrlServicioImagenes;
            string docFallidos = string.Empty;

            try
            {
                if (pDocumentosID.Count > 0)
                {
                    foreach (Guid docID in pDocumentosID)
                    {
                        string directorioViejo = Path.Combine(UtilArchivos.ContentImagenesDocumentos, UtilArchivos.ContentImagenesSemanticasAntiguo, docID.ToString());
                        string directorio = Path.Combine(UtilArchivos.ContentImagenesDocumentos, UtilArchivos.ContentImagenesSemanticas, UtilArchivos.DirectorioDocumento(docID));

                        if (!servicioImagenes.BorrarImagenesDeRecurso(directorioViejo) && !servicioImagenes.BorrarImagenesDeRecurso(directorio))
                        {
                            docFallidos += docID + ", ";
                        }
                    }
                    if (docFallidos.Length > 0)
                    {
                        docFallidos = docFallidos.Substring(0, docFallidos.LastIndexOf(","));
                    }
                }
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error BorrarImagenRecursos al borrar imágenes de los recursos de la comunidad: {docFallidos}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }

        }

        /// <summary>
        /// Borra los archivos de los documentos vinculados a una ontología
        /// </summary>
        /// <param name="pOntologiaID">Identificador de la ontologia</param>
        public void BorrarArchivosDocumentosTodasOntologiasComunidad(Dictionary<Guid, List<Guid>> pDicOntologiaDocumentosVinculados, Dictionary<Guid, string> pDicOntologiaEnlace)
        {
            try
            {
                foreach (Guid ontologiaID in pDicOntologiaDocumentosVinculados.Keys)
                {
                    BorrarArchivosDocumentosOntologia(pDicOntologiaDocumentosVinculados[ontologiaID], ontologiaID);
                }
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error BorrarArchivosDocumentosTodasOntologiasComunidad al borrar archivos de todas las ontologías de la comunidad: \n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }
        }

        /// <summary>
        /// Borra los archivos de los documentos vinculados a una ontología
        /// </summary>
        /// <param name="pOntologiaID">Identificador de la ontologia</param>
        public void BorrarArchivosDocumentosOntologia(List<Guid> pDocumentosID, Guid pOntologiaID)
        {
            GestionDocumental gestionDoc = new GestionDocumental(mLoggingService, mConfigService, mLoggerFactory.CreateLogger<GestionDocumental>(), mLoggerFactory);
            gestionDoc.Url = UrlServicioWebDocumentacion;

            try
            {
                if (!pOntologiaID.Equals(Guid.Empty))
                {
                    gestionDoc.BorrarArchivosDeOntologia(pOntologiaID);
                }
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error BorrarArchivosDocumentosOntologia al borrar archivos de la ontología:{pOntologiaID} \n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }

            foreach (Guid docID in pDocumentosID)
            {
                try
                {
                    //borra el directorio del documentoID y todos los archivos contenidos en él
                    string directorio = Path.Combine(UtilArchivos.ContentDocumentosSem, UtilArchivos.DirectorioDocumento(docID));
                    gestionDoc.BorrarDocumentosDeDirectorio(directorio);
                }
                catch (Exception ex)
                {
                    GuardarLogError($"Error BorrarArchivosDocumentosOntologia al borrar el directorio del documento:{docID} \n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                }

                try
                {
                    //borra directorio de los ArchivoLink
                    //TODO Javier migrar a rest
                    //ServicioDocumentosLink servDocLink = new ServicioDocumentosLink();
                    //servDocLink.Url = UrlServicioDocumentosLink;
                    //servDocLink.BorrarDirectorioDeDocumento(docID);
                }
                catch (Exception ex)
                {
                    GuardarLogError($"Error BorrarArchivosDocumentosOntologia al borrar el directorio de archivosLink del documento:{docID}\n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
                }
            }
        }

        /// <summary>
        /// Borra del ácido los documentos
        /// </summary>
        /// <param name="pDocumentosID">Lista de identificadores de los documentos a borrar</param>
        public void BorrarDocumentos(List<Guid> pDocumentosID)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<DocumentacionCN>(), mLoggerFactory);
            try
            {
                docCN.EliminarDocumentos(pDocumentosID);
            }
            catch (Exception ex)
            {
                GuardarLogError($"Error BorrarDocumentosVinculados al borrar documentos vinculados. \n Traza: {ex.StackTrace}\n Mensaje error: {ex.Message}", "BorrarRecursos");
            }
            docCN.Dispose();
        }

        /// <summary>
        /// Guarda el log del error.
        /// </summary>
        public static void GuardarLogError(string pError, string pNombreLog)
        {
            string directorio = System.AppDomain.CurrentDomain.BaseDirectory + "logs";
            Directory.CreateDirectory(directorio);
            string rutaFichero = Path.Combine(directorio, $"log_{pNombreLog}_{DateTime.Now.ToString("yyyy-MM-dd")}.log");

            //Si el fichero supera el tamaño máximo lo elimino
            if (File.Exists(rutaFichero))
            {
                FileInfo fichero = new FileInfo(rutaFichero);
                if (fichero.Length > 1000000)
                {
                    fichero.Delete();
                }
            }

            //Añado el error al fichero
            using (StreamWriter sw = new StreamWriter(rutaFichero, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(Environment.NewLine + "Fecha: " + DateTime.Now + Environment.NewLine + Environment.NewLine);
                // Escribo el error
                sw.Write(pError);
                sw.WriteLine(Environment.NewLine + Environment.NewLine + "___________________________________________________________________________________________" + Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }
        }

        #endregion

        #endregion

        #region Propiedades

        /// <summary>
        /// Devuelve la URL del servicio de documentación
        /// </summary>
        public string UrlServicioWebDocumentacion
        {
            get
            {
                if (mUrlServicioWebDocumentacion == null)
                {
                    List<ParametroAplicacion> filas = ParametrosAplicacionDS.ParametroAplicacion.Where(parametroApp=>parametroApp.Parametro.Equals(TiposParametrosAplicacion.UrlServicioWebDocumentacion)).ToList();

                    if (filas.Count > 0)
                    {
                        ParametroAplicacion fila = filas[0];
                        mUrlServicioWebDocumentacion = fila.Valor;
                    }
                    else
                    {
                        mUrlServicioWebDocumentacion = "";
                    }
                }
                return mUrlServicioWebDocumentacion;
            }
        }

        public string UrlServicioInterno
        {
            get
            {
                if (mUrlServicioInterno == null)
                {
                    if (ParametroProyecto != null && ParametroProyecto.ContainsKey(TiposParametrosAplicacion.UrlIntragnossServicios))
                    {
                        mUrlServicioInterno = ParametroProyecto[TiposParametrosAplicacion.UrlIntragnossServicios];
                    }
                    else
                    {
                        List<ParametroAplicacion> filas = ParametrosAplicacionDS.ParametroAplicacion.Where(parametroApp=>parametroApp.Parametro.Equals(TiposParametrosAplicacion.UrlIntragnossServicios)).ToList();

                        if (filas.Count > 0)
                        {
                            mUrlServicioInterno = filas[0].Valor;
                        }
                        else
                        {
                            mUrlServicioInterno = "";
                        }
                    }
                }
                return mUrlServicioInterno;
            }
        }

        /// <summary>
        /// Devuelve la URL del servicio de documentación
        /// </summary>
        public string UrlServicioImagenes
        {
            get
            {
                if (mUrlServicioImagenes == null)
                {
                    if (!string.IsNullOrEmpty(UrlServicioInterno))
                    {
                        mUrlServicioImagenes = UrlServicioInterno;
                    }
                    else
                    {
                        mUrlServicioImagenes = "";
                    }
                }
                return mUrlServicioImagenes;
            }
        }

        /// <summary>
        /// Devuelve la URL del servicio de documentación en links.
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
        /// Devuelve el ParametrosAplicacionDS
        /// </summary>
        public GestorParametroAplicacion ParametrosAplicacionDS
        {
            get
            {
                if (mParametrosAplicacionDS == null)
                {
                    mParametrosAplicacionDS = new GestorParametroAplicacion();
                    ParametroAplicacionGBD parametroAplicacionGBD = new ParametroAplicacionGBD(mLoggingService, mEntityContext, mConfigService);
                    parametroAplicacionGBD.ObtenerConfiguracionGnoss(mParametrosAplicacionDS);
                }

                return mParametrosAplicacionDS;
            }
        }

        /// <summary>
        /// Parametros de configuración del proyecto.
        /// </summary>
        private Dictionary<string, string> ParametroProyecto
        {
            get
            {
                if (mParametroProyecto == null)
                {
                    ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication, mLoggerFactory.CreateLogger<ProyectoCL>(), mLoggerFactory);
                    mParametroProyecto = proyectoCL.ObtenerParametrosProyecto(mProyectoID);
                    proyectoCL.Dispose();
                }

                return mParametroProyecto;
            }
        }

        #endregion
    }
}
