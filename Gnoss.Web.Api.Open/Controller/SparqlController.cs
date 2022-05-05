using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Servicios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    /// <summary>
    /// Use it to query our sparql endpoint
    /// </summary>
    [ApiController]
    [Route("sparql-endpoint")]
    public class SparqlController : ControlApiGnossBase
    {

        public SparqlController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
                : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication )
        {

        }

        #region Metodos Web

        /// <summary>
        /// Get a DataSet with the result of the query
        /// </summary>
        /// <param name="sparql_query">Sparql parameters</param>
        /// <returns>SPARQL Query Results in JSON</returns>
        [HttpPost, Route("query")]
        public ActionResult QueryVirtuoso(SparqlQuery sparql_query)
        {
            string jsonResponse = string.Empty;

            if (TienePermisoAplicacionEnOntologia(sparql_query.ontology))
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                Guid proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(sparql_query.community_short_name);

                string communityQuery = "";
                string query = JuntarPartesConsultaSparql(sparql_query.ontology, sparql_query.query_select, sparql_query.query_where);

                try
                {
                    jsonResponse = EjecutarConsultaEnVirtuoso(query, sparql_query.ontology, sparql_query.ontology);

                    if (!string.IsNullOrEmpty(communityQuery) && !proyectoID.Equals(Guid.Empty))
                    {
                        jsonResponse = EjecutarConsultaEnVirtuoso(communityQuery, proyectoID.ToString().ToLower(), sparql_query.ontology);
                    }
                }
                catch
                {
                    throw new GnossException("Error in sparql endpoint.\n\rQuery: '" + query + "'", HttpStatusCode.BadRequest);
                }
            }

            return Content(jsonResponse, "application/json");
        }

        #endregion

        #region Metodos privados

        /// <summary>
        /// Verdad si una aplicación tiene permiso para consultar una ontología concreta
        /// </summary>
        /// <param name="pOntologia">Ontología que se intenta consultar</param>
        /// <returns>Verdad si la aplicación tiene permiso para consultar la ontología</returns>
        private bool TienePermisoAplicacionEnOntologia(string pOntologia)
        {
            Guid test = Guid.Empty;
            if (Guid.TryParse(pOntologia, out test))
            {
                //Intenta acceder al grafo de una comunidad, compruebo que es administrador de ella
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                bool tienePermiso = proyCN.EsUsuarioAdministradorProyecto(UsuarioOAuth, test);

                if (!tienePermiso)
                {
                    throw new GnossException("Unauthorized", HttpStatusCode.Unauthorized);
                }
            }
            else
            {
                //Intenta acceder al grafo de una ontología, compruebo que administra alguna comunidad en la que se está usando esta ontología
                DocumentacionCN documentacionCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                bool tienePermiso = documentacionCN.ComprobarUsuarioAdministraOntologia(UsuarioOAuth, pOntologia + ".owl");

                if (!tienePermiso)
                {
                    string mensajeErrorNoAutorizado = "The ontology: " + pOntologia + " is not configured as an object of knowledge in any community managed by this user";
                    throw new GnossException(mensajeErrorNoAutorizado, System.Net.HttpStatusCode.BadRequest);
                }
            }
            
            return true;
        }
        

        /// <summary>
        /// Junta todas las partes de una consulta SPARQL
        /// </summary>
        /// <param name="pFacetadoCN">FacetadoCN que se va a usar para realizar la consulta</param>
        /// <param name="pOntologia">Ontología en la que se va a hacer la consulta</param>
        /// <param name="pSelect">Parte Select de la consulta</param>
        /// <param name="pWhere">Parte Where de la consulta</param>
        /// <returns>Consulta SPARQL formada</returns>
        private string JuntarPartesConsultaSparql(string pOntologia, string pSelect, string pWhere)
        {
            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, pOntologia, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

            string grafo = pOntologia;
            Guid test = Guid.Empty;
            if (Guid.TryParse(grafo, out test))
            {
                //Es el grafo de una comunidad
                facetadoCN.InformacionOntologias = ObtenerPrefijosDeOntologia(pOntologia, true);
            }
            else
            {
                //Es el grafo de una ontología
                facetadoCN.InformacionOntologias = ObtenerPrefijosDeOntologia(pOntologia, false);
                grafo += ".owl";
            }


            string prefijos = facetadoCN.FacetadoAD.NamespacesVirtuosoLectura;

            string from = "\r\n FROM <" + UrlIntragnoss + grafo + ">";

            string query = prefijos + pSelect + from + pWhere;

            facetadoCN.Dispose();

            return query;
        }

        /// <summary>
        /// Lee de virtuosos a partir de una consulta SPARQL. Si hay errores por culpa de un checkpoint, la consulta se reintenta
        /// </summary>
        /// <param name="pDataSet">DataSet a cargar (null si es una consulta de actualización)</param>
        /// <param name="pQuery">Query a ejecutar</param>
        /// <param name="pOntologia">Ontología en la que se va a realizar la consulta</param>
        /// <param name="pNombreTabla">Nombre de la tabla a cargar en el DataSet (null si es una consulta de actualización)</param>
        /// <param name="pEsActualizacion">Verdad si es una consulta de actualización, falso si es una consulta de lectura</param>
        private string EjecutarConsultaEnVirtuoso(string pQuery, string pOntologia, string pNombreTabla)
        {
            FacetadoCN facetadoCN = null;
            string jsonResultado = string.Empty;

            try
            {
                facetadoCN = new FacetadoCN(UrlIntragnoss, pOntologia, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                jsonResultado = facetadoCN.FacetadoAD.LeerDeVirtuosoJSON(pQuery, pNombreTabla, pOntologia);
                
            }
            catch (Exception)
            {
                //Cerramos las conexiones
                ControladorConexiones.CerrarConexiones();

                DateTime horaActual = DateTime.Now;

                //Realizamos una consulta ask a virtuoso para comprobar si está funcionando
                while (!UtilidadesVirtuoso.ServidorOperativo("acid", UrlIntragnoss) && DateTime.Now < horaActual.AddMinutes(2))
                {
                    //Dormimos 30 segundos
                    Thread.Sleep(30 * 1000);
                }

                facetadoCN = new FacetadoCN(UrlIntragnoss, pOntologia, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

                jsonResultado = facetadoCN.FacetadoAD.LeerDeVirtuosoJSON(pQuery, pOntologia, pOntologia);
            }
            finally
            {
                if (facetadoCN != null)
                {
                    facetadoCN.Dispose();
                    facetadoCN = null;
                }
            }

            return jsonResultado;
        }

        /// <summary>
        /// Obtiene la lista de items extra que se obtendrá de la búsqueda y su prefijo (recetas, peliculas, etc)
        /// </summary>
        private Dictionary<string, List<string>> ObtenerPrefijosDeOntologia(string pOntologia, bool pEsGrafoComunidad)
        {
            Dictionary<string, List<string>> informacionOntologias = new Dictionary<string, List<string>>();

            //ObtenerOntologias
            FacetaCL facetaCL = new FacetaCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);

            if (pEsGrafoComunidad)
            {
                //Es el grafo de una comunidad, obtengo los namespaces de todas sus ontologías
                informacionOntologias = facetaCL.ObtenerPrefijosOntologiasDeProyecto(new Guid(pOntologia));
            }
            else
            {
                //Es el grafo de una ontología, obtengo sólo los namespaces para esta ontología
                informacionOntologias = facetaCL.ObtenerPrefijosDeOntologia(pOntologia, Guid.Empty);
            }

            return informacionOntologias;
        }

        #endregion
    }
}