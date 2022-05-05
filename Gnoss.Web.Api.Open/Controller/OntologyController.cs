using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Documentacion;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Elementos.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    /// <summary>
    /// Use it to work with ontologies
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class OntologyController : ControlApiGnossBase
    {

        public OntologyController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {

        }

        /// <summary>
        /// Gets the url of graphs
        /// </summary>
        /// <returns>URL of graphs</returns>
        /// <example>GET ontology/get-graphs-url</example>
        [HttpGet, Route("get-graphs-url")]
        public string ObtenerUrlIntraGnoss()
        {
            return UrlIntragnoss;
        }

        /// <summary>
        /// Save a fraction of an owl file. It's used in large ontologies. The OWL file can be splitted in many files, and each file can contains a number of entities. 
        /// </summary>
        /// <param name="parameters">Parameters of upload-partitioned-ontology</param>
        /// <example>POST ontology/upload-partitioned-ontology</example>
        [HttpPost, Route("upload-partitioned-ontology")]
        public void GuardarOntologiaFraccionada(FileOntology parameters)
        {
            if (!string.IsNullOrEmpty(parameters.community_short_name) && !string.IsNullOrEmpty(parameters.ontology_name) && !string.IsNullOrEmpty(parameters.file_name) && parameters.file != null && parameters.file.Length > 0)
            {
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid proyectoID = proyCN.ObtenerProyectoIDPorNombre(parameters.community_short_name);
                GestionProyecto gestorProy = new GestionProyecto(proyCN.ObtenerProyectoPorID(proyectoID), mLoggingService, mEntityContext);
                Proyecto proyecto = gestorProy.ListaProyectos[proyectoID];
                proyCN.Dispose();

                if (proyecto != null)
                {
                    if (proyecto.EsAdministradorUsuario(UsuarioOAuth))
                    {
                        mNombreCortoComunidad = parameters.community_short_name;

                        //identidad
                        GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
                        Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);
                        
                        DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        Guid ontologiaID = docCN.ObtenerOntologiaAPartirNombre(FilaProy.ProyectoID, parameters.ontology_name);

                        if (!ontologiaID.Equals(Guid.Empty))
                        {
                            string servArchivosUrl = mConfigService.ObtenerUrlServicio("urlArchivos");
                            var item = new
                            {
                                pOntologiaID = ontologiaID,
                                pNombreFraccion = parameters.file_name,
                                pFichero = parameters.file
                            };
                            CallWebMethods.CallPostApi(servArchivosUrl, "GuardarOntologiaFraccionada", item);

                            //borra la caché del xml de la ontología
                            DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                            docCL.GuardarIDXmlOntologia(ontologiaID, Guid.NewGuid());
                            docCL.Dispose();
                        }
                        else
                        {
                            throw new GnossException("The ontology does not exists in the community", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        throw new GnossException("The oauth user does not have permission to open community", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw new GnossException("The community does not exists", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Save a fraction of an xml configuration file. It's used in large ontologies. The XML file can be splitted in many files, and each file can contains the configuration for a number of entities. 
        /// </summary>
        /// <param name="parameters">Parameters of upload-partitioned-xml</param>
        /// /// <example>POST ontology/upload-partitioned-xml</example>
        [HttpPost, Route("upload-partitioned-xml")]
        public void GuardarXmlOntologiaFraccionado(FileOntology parameters)
        {
            if (!string.IsNullOrEmpty(parameters.community_short_name) && !string.IsNullOrEmpty(parameters.ontology_name) && !string.IsNullOrEmpty(parameters.file_name) && parameters.file != null && parameters.file.Length > 0)
            {
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid proyectoID = proyCN.ObtenerProyectoIDPorNombre(parameters.community_short_name);
                GestionProyecto gestorProy = new GestionProyecto(proyCN.ObtenerProyectoPorID(proyectoID), mLoggingService, mEntityContext);
                Proyecto proyecto = gestorProy.ListaProyectos[proyectoID];
                proyCN.Dispose();

                if (proyecto != null)
                {
                    if (proyecto.EsAdministradorUsuario(UsuarioOAuth))
                    {
                        mNombreCortoComunidad = parameters.community_short_name;

                        //identidad
                        GestorDocumental gestorDoc = CargarGestorDocumental(FilaProy);
                        Identidad identidad = CargarIdentidad(gestorDoc, FilaProy, UsuarioOAuth, true);

                        DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        Guid ontologiaID = docCN.ObtenerOntologiaAPartirNombre(FilaProy.ProyectoID, parameters.ontology_name);

                        if (!ontologiaID.Equals(Guid.Empty))
                        {
                            string servArchivosUrl = mConfigService.ObtenerUrlServicio("urlArchivos");
                            var item = new
                            {
                                pOntologiaID = ontologiaID,
                                pNombreFraccion = parameters.file_name,
                                pFichero = parameters.file
                            };
                            CallWebMethods.CallPostApi(servArchivosUrl, "GuardarXmlOntologiaFraccionado", item);

                            //borra la caché del xml de la ontología
                            DocumentacionCL docCL = new DocumentacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                            docCL.GuardarIDXmlOntologia(ontologiaID, Guid.NewGuid());
                            docCL.Dispose();
                        }
                        else
                        {
                            throw new GnossException("The ontology does not exists in the community", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        throw new GnossException("The oauth user does not have permission to open community", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw new GnossException("The community does not exists", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }
        }

    }
}
