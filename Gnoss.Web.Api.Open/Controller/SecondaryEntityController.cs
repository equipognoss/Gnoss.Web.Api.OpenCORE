﻿using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.AD.Facetado.Model;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
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
using Es.Riam.AbstractsOpen;
using BeetleX.Redis.Commands;
using Es.Riam.Gnoss.Logica.MVC;
using Microsoft.Azure.Amqp.Framing;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    /// <summary>
    /// Use it to create, modify or delete secondary entities
    /// </summary>
    [ApiController]
    [Route("secondary-entity")]
    public class SecondaryEntityController : ControlApiGnossBase
    {

        public SecondaryEntityController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {

        }

        #region Metodos web

        /// <summary>
        /// Método para agregar/modificar/eliminar triples de una entidad secundaria.
        /// * Modificar: Pasamos el objeto viejo y el objeto nuevo
        /// * Eliminar: Pasamos solo el objeto viejo
        /// * Agregar: Pasamos solo el objeto nuevo
        /// </summary>
        /// <param name="parameters">Parameters</param>
        [HttpPost, Route("modify-triple-list")]
        public void ModificarListaDeTripletasDeEntidadSecundaria(ModifyTripleListModel parameters)
        {
            try
            {
                mNombreCortoComunidad = parameters.community_short_name;

                //Acortamos la UrlOntología
                if (parameters.secondary_ontology_url.Contains('/'))
                {
                    parameters.secondary_ontology_url = parameters.secondary_ontology_url.Substring(parameters.secondary_ontology_url.LastIndexOf("/") + 1);
                }

                ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);
                GestionOWL.FicheroConfiguracionBD = "acid";
                Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.secondary_ontology_url, ProyectoTraerOntosID);

                if (docOnto == null)
                {
                    throw new Exception("No existe ningúna ontología de tesauro con la URL especificada");
                }

                Guid ontologiaID = docOnto.Clave;
                string nombreOntologia = docOnto.FilaDocumento.Enlace;
                GestionOWL gestorOWL = new GestionOWL();
                gestorOWL.UrlOntologia = BaseURLFormulariosSem + "/Ontologia/" + nombreOntologia + "#";
                gestorOWL.NamespaceOntologia = GestionOWL.NAMESPACE_ONTO_GNOSS;
                GestionOWL.FicheroConfiguracionBD = "acid";
                GestionOWL.URLIntragnoss = UrlIntragnoss;

                byte[] arrayOnto = ControladorDocumentacion.ObtenerOntologia(docOnto.Clave, FilaProy.ProyectoID);

                if (arrayOnto == null)
                {
                    throw new Exception("La ontología está vacía.");
                }

                Ontologia ontologia = new Ontologia(arrayOnto, true);
                ontologia.OntologiaID = ontologiaID;
                ontologia.LeerOntologia();

                string triplesInsertar = "";
                string triplesBorrar = "";
                List<TripleWrapper> triplesComBorrar = new List<TripleWrapper>();
                List<TripleWrapper> triplesComInsertar = new List<TripleWrapper>();

                foreach (string[] linea in parameters.triple_list)
                {
                    string objetoViejo = linea[0];
                    string predicado = linea[1];
                    string objetoNuevo = linea[2];

                    ModificarTripleEntidadSecundaria(ontologia, parameters.secondary_ontology_url, parameters.secondary_entity, predicado, objetoViejo, objetoNuevo, ref triplesInsertar, ref triplesBorrar, triplesComBorrar, triplesComInsertar);
                }

                //Genero las triples de la comunidad:
                string triplesComInsertarDef = "";
                List<TripleWrapper> triplesComBorrarDef = new List<TripleWrapper>();
                List<string> listaAux = new List<string>();

                FacetadoAD facetadoAD = new FacetadoAD(UrlIntragnoss, mLoggingService, mEntityContext, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                foreach (TripleWrapper triple in triplesComInsertar)
                {
                    triplesComInsertarDef += facetadoAD.GenerarTripletaSinConversionesAbsurdas(triple.Subject.ToLowerSearchGraph(), triple.Predicate, triple.Object.ToLowerSearchGraph(), triple.ObjectLanguage, triple.ObjectType);
                }

                foreach (TripleWrapper triple in triplesComBorrar)
                {
                    triplesComBorrarDef.Add(new TripleWrapper { Subject = triple.Subject.ToLowerSearchGraph(), Predicate = triple.Predicate, Object = triple.Object.ToLowerSearchGraph(), ObjectLanguage = triple.ObjectLanguage, ObjectType = triple.ObjectType });
                }

                FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

                //Guardo triples grafo:
                facCN.BorrarGrupoTripletasEnLista(nombreOntologia, triplesComBorrar, true);
                facCN.InsertaTripletas(nombreOntologia, triplesInsertar, (short)PrioridadBase.ApiRecursos, true);

                //Se añade el proyecto por si es un proyecto hijo, ya que en este caso la ontologia pertenece al padre y no se guardan los triples en el grafo de busqueda de la hija
                if (!docOnto.ListaProyectos.Contains(Proyecto.Clave))
                {
                    docOnto.ListaProyectos.Add(Proyecto.Clave);
                }
                //Guardo triples en los proyectos en los que está subido y compartido el grafo:
                foreach (Guid proyectoID in docOnto.ListaProyectos)
                {
                    facCN.BorrarGrupoTripletasEnLista(proyectoID.ToString().ToLower(), triplesComBorrarDef, true);
                    facCN.InsertaTripletas(proyectoID.ToString().ToLower(), triplesComInsertarDef, (short)PrioridadBase.ApiRecursos, true);
                }

                ontologia.Dispose();
                facCN.Dispose();
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Insert a secondary entity in a graph of a secondary ontology.
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST secondary-entity/create</example>
        [HttpPost, Route("create")]
        public void SubirEntidadSecundaria(SecondaryEntityModel parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            GuardarRDFEntidadSecundaria(parameters.ontology_url, parameters.rdf, false);
        }

        /// <summary>
        /// Modify a secondary entity in a graph of a secondary ontology.
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST secondary-entity/modify</example>
        [HttpPost, Route("modify")]
        public void ModificarEntidadSecundaria(SecondaryEntityModel parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            GuardarRDFEntidadSecundaria(parameters.ontology_url, parameters.rdf, true);
        }

        /// <summary>
        /// Delete a secondary entity in a graph of a secondary ontology.
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST secondary-entity/delete</example>
        [HttpPost, Route("delete")]
        public void BorrarEntidadSecundaria(DeleteSecondaryEntityModel parameters)
        {
            mNombreCortoComunidad = parameters.community_short_name;

            ComprobarUsuTienePermisoSobreEntSecundaria(UsuarioOAuth);

            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(parameters.ontology_url, ProyectoTraerOntosID);
            string nombreOntologia = docOnto.FilaDocumento.Enlace;

            string idHasEntidadPrincipal = parameters.entity_id;
            FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

            if (!idHasEntidadPrincipal.Contains("/entidadsecun_"))
            {
                List<string> sujetos = facCN.ObtenerSujetosConObjetoDePropiedad(nombreOntologia, idHasEntidadPrincipal, "http://gnoss/hasEntidad");

                if (sujetos.Count > 0 && sujetos[0].Contains("entidadsecun_"))
                {
                    idHasEntidadPrincipal = sujetos[0].Substring(sujetos[0].IndexOf("entidadsecun_"));
                }
                else
                {
                    if (idHasEntidadPrincipal.Contains("/"))
                    {
                        idHasEntidadPrincipal = idHasEntidadPrincipal.Substring(idHasEntidadPrincipal.LastIndexOf("/") + 1);
                    }

                    if (idHasEntidadPrincipal.Contains("#"))
                    {
                        idHasEntidadPrincipal = idHasEntidadPrincipal.Substring(idHasEntidadPrincipal.LastIndexOf("#") + 1);
                    }

                    idHasEntidadPrincipal = "entidadsecun_" + idHasEntidadPrincipal.ToLower();
                }
            }
            else
            {
                idHasEntidadPrincipal = idHasEntidadPrincipal.Substring(idHasEntidadPrincipal.LastIndexOf("/") + 1);
            }

            ControladorDocumentacion.BorrarRDFDeVirtuoso(idHasEntidadPrincipal, nombreOntologia, UrlIntragnoss, "acid", FilaProy.ProyectoID, true);

            foreach (Guid proyectoID in docOnto.ListaProyectos)
            {
                ControladorDocumentacion.BorrarRDFDeVirtuoso(idHasEntidadPrincipal, proyectoID.ToString().ToLower(), UrlIntragnoss, "acid", FilaProy.ProyectoID, true);
                facCN.BorrarTripleta(proyectoID.ToString().ToLower(), "<" + UrlIntragnoss + nombreOntologia.ToLower() + ">", "<http://gnoss/hasEntidad>", "<" + UrlIntragnoss + idHasEntidadPrincipal + ">", true);
            }

            facCN.Dispose();

            //Invalidar Cache del tesauro.
            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
            facetadoCL.Dispose();
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Guarda el RDF en virtuoso de la entidad secundaria.
        /// </summary>
        /// <param name="pUrlOntologia">Url de la ontología</param>
        /// <param name="pRdf">RDF</param>
        /// <param name="pEliminarRdfViejo">Indica si hay eliminar el antiguo RDF</param>
        private void GuardarRDFEntidadSecundaria(string pUrlOntologia, byte[] pRdf, bool pEliminarRdfViejo)
        {
            Documento docOnto = ControladorDocumentacion.ObtenerOntologiaDeEntidadSecundaria(pUrlOntologia, ProyectoTraerOntosID);

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

                StreamReader reader = new StreamReader(new MemoryStream(pRdf));
                string lineaRDF = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
                reader = null;

                List<ElementoOntologia> instanciasPrincipales = gestorOWL.LeerFicheroRDF(ontologia, lineaRDF, true, true);

                ControladorDocumentacion.GuardarRDFEntidadSecundaria(instanciasPrincipales, UrlIntragnoss, nombreOntologia, FilaProy.OrganizacionID, FilaProy.ProyectoID, docOnto, pEliminarRdfViejo);
                //Invalidar cache del tesauro
                FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                facetadoCL.InvalidarCacheTesauroFaceta(ProyectoTraerOntosID);
                facetadoCL.Dispose();
            }
            else
            {
                throw new Exception("La ontología está vacía.");
            }
        }

        /// <summary>
        /// Modifica un triple de una entidad secundaria.
        /// </summary>
        /// <param name="pOntologia">Ontología</param>
        /// <param name="pUrlOntologiaSecundaria">Url de la ontologia secundaria</param>
        /// <param name="pEntidadSecundariaID">URI de la entidad secundaria</param>
        /// <param name="pPredicado">Predicado</param>
        /// <param name="pObjetoViejo">Objeto a eliminar</param>
        /// <param name="pObjetoNuevo">Objeto a añadir</param>
        /// <param name="pTriplesInsertar">Cadena con los triples a insertar</param>
        /// <param name="pTriplesComBorrar">Lista con los triples a borrar</param>
        /// <param name="pTriplesComInsertar">Lista con los triples a insertar</param>
        private void ModificarTripleEntidadSecundaria(Ontologia pOntologia, string pUrlOntologiaSecundaria, string pEntidadSecundariaID, string pPredicado, string pObjetoViejo, string pObjetoNuevo, ref string pTriplesInsertar, ref string pTriplesBorrar, List<TripleWrapper> pTriplesComBorrar, List<TripleWrapper> pTriplesComInsertar)
        {
            //if (pPredicado.Contains("|"))
            //{
            //    throw new Exception("La versión actual del API no soporta la modificación de triples multiNivel.");
            //}

            


            List<string> ent = new List<string>();
            ent.Add(pEntidadSecundariaID);
            List<string> propiedades = new List<string>();
            propiedades.Add("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
            propiedades.Add(pPredicado);
            FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

            string grafoOnto = "";
            if (!string.IsNullOrEmpty(pUrlOntologiaSecundaria))
            {
                grafoOnto = UrlIntragnoss;

                if (!UrlIntragnoss.EndsWith("/"))
                {
                    grafoOnto += "/";
                }

                grafoOnto += pUrlOntologiaSecundaria.Substring(pUrlOntologiaSecundaria.LastIndexOf("/") + 1);
            }

            FacetadoDS dataSetEnt = facCN.ObtenerValoresPropiedadesEntidades(grafoOnto, ent, propiedades, true);
            facCN.Dispose();

            if (dataSetEnt.Tables[0].Rows.Count == 0)
            {
                throw new Exception("No existe una entidad cuya URI sea '" + pEntidadSecundariaID + "'.");
            }

            List<string> tipos = FacetadoCN.ObtenerObjetosDataSetSegunPropiedad(dataSetEnt, pEntidadSecundariaID, propiedades[0]);

            if (tipos.Count == 0)
            {
                throw new Exception("La entidad cuya URI sea '" + pEntidadSecundariaID + "' no tiene tipo en virtuoso, es corrupta.");
            }

            string rdfType = tipos[0];

            ElementoOntologia entidad = pOntologia.GetEntidadTipo(rdfType, false);

            if (entidad == null)
            {
                throw new Exception("La entidad indicada tiene un tipo que no pertenece a la ontología '" + rdfType + "'.");
            }

            ModificarInstanciaPorNivelEntidadSecundaria(entidad, dataSetEnt, grafoOnto, pEntidadSecundariaID, pPredicado, pObjetoViejo, pObjetoNuevo, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, pEntidadSecundariaID);
            //EscribirTripletasEntidadSecundaria(pEntidadSecundariaID, pPredicado, pObjetoViejo, pObjetoNuevo, pTriplesInsertar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);
            

            dataSetEnt.Dispose();
        }
        private void EscribirTripletasEntidadSecundaria(string pEntidadSecundariaID, string pPredicado, string pObjetoViejo, string pObjetoNuevo, ref string pTriplesInsertar, ref string pTriplesBorrar, List<TripleWrapper> pTriplesComBorrar, List<TripleWrapper> pTriplesComInsertar, Dictionary<string, List<string>> pValoresProp)
        {
            if (!string.IsNullOrEmpty(pObjetoViejo))
            {
                string idioma = "";

                if (UtilCadenas.EsMultiIdioma(pObjetoViejo))
                {
                    idioma = pObjetoViejo.Substring(pObjetoViejo.LastIndexOf("@") + 1);
                    pObjetoViejo = pObjetoViejo.Substring(0, pObjetoViejo.LastIndexOf("@"));
                }

                if (!pValoresProp.ContainsKey(idioma) || !pValoresProp[idioma].Contains(pObjetoViejo))
                {
                    throw new Exception("La propiedad '" + pPredicado + "' no contiene el valor '" + pObjetoViejo + "' con idioma '" + idioma + "'.");
                }

                ControladorDocumentacion.EscribirTripletaEntidad(pEntidadSecundariaID, pPredicado, pObjetoViejo, ref pTriplesBorrar, pTriplesComBorrar, false, idioma);
            }

            if (!string.IsNullOrEmpty(pObjetoNuevo))
            {
                string idioma = "";

                if (UtilCadenas.EsMultiIdioma(pObjetoNuevo))
                {
                    idioma = pObjetoNuevo.Substring(pObjetoNuevo.LastIndexOf("@") + 1);
                    pObjetoNuevo = pObjetoNuevo.Substring(0, pObjetoNuevo.LastIndexOf("@"));
                }

                if (pValoresProp.ContainsKey(idioma) && pValoresProp[idioma].Contains(pObjetoNuevo))
                {
                    throw new Exception("La propiedad '" + pPredicado + "' ya contiene el valor '" + pObjetoViejo + "' con idioma '" + idioma + "'.");
                }

                ControladorDocumentacion.EscribirTripletaEntidad(pEntidadSecundariaID, pPredicado, pObjetoNuevo, ref pTriplesInsertar, pTriplesComInsertar, false, idioma);
            }
        }

        private void ModificarInstanciaPorNivelEntidadSecundaria(ElementoOntologia pEntidad, FacetadoDS pDataSetEnt, string pGrafoOnto, string pEntidadSecundariaID, string pPredicado, string pObjetoViejo, string pObjetoNuevo, ref string pTriplesInsertar, ref string pTriplesBorrar, List<TripleWrapper> pTriplesComBorrar, List<TripleWrapper> pTriplesComInsertar, string pEntidadPadreID)
        {
            Propiedad propiedad = pEntidad.ObtenerPropiedadPorNombreOUri(pPredicado);

            Dictionary<string, List<string>> valoresProp = FacetadoCN.ObtenerObjetosDataSetSegunPropiedadConIdioma(pDataSetEnt, pEntidadSecundariaID, pPredicado);
            if (pPredicado.Contains("|"))
            {
                propiedad = pEntidad.ObtenerPropiedadPorNombreOUri(pPredicado.Substring(0, pPredicado.IndexOf('|')));

                if (propiedad == null)
                {
                    throw new Exception("La entidad '" + pEntidadSecundariaID + "' no posee la propiedad '" + pPredicado + "'.");
                }

                valoresProp = FacetadoCN.ObtenerObjetosDataSetSegunPropiedadConIdioma(pDataSetEnt, pEntidadSecundariaID, pPredicado.Substring(0, pPredicado.IndexOf('|')));

                if (!string.IsNullOrEmpty(pObjetoViejo) && string.IsNullOrEmpty(pObjetoNuevo) && propiedad.FunctionalProperty && valoresProp.Count == 1)
                {
                    throw new Exception("La propiedad '" + pPredicado.Substring(0, pPredicado.IndexOf('|') + 1) + "' es funcional y no puede quedarse sin valor.");
                }

                char[] delimiter = { '|' };
                string[] pred = pPredicado.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] objetoViejo = null;
                if (string.IsNullOrEmpty(pObjetoViejo) || !pObjetoViejo.Contains('|'))
                {
                    if (!string.IsNullOrEmpty(pObjetoNuevo) && pObjetoNuevo.Contains('|')) 
                    {
                        objetoViejo = pObjetoNuevo.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        throw new Exception($"Se ha configurado el predicado {pPredicado} multi valor ('\') pero no se ha dado tal valor al ObjetoViejo ni al objetoNuevo");
                    }
                }
                else
                {
                    objetoViejo = pObjetoViejo.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                }
                

                List<string> listaPredicados = new List<string>() { pred[0] };
                List<string> listaEntidades = new List<string>() { pEntidadSecundariaID };
                FacetadoCN facCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                FacetadoDS dataSetEnt = facCN.ObtenerValoresPropiedadesEntidades(pGrafoOnto, listaEntidades, listaPredicados, true);

                List<string> objetos = FacetadoCN.ObtenerObjetosDataSetSegunPropiedad(dataSetEnt, pEntidadSecundariaID, listaPredicados.First());

                if (!objetos.Contains(objetoViejo.First()))
                {
                    Propiedad prop = pEntidad.Propiedades.FirstOrDefault(p => p.Nombre.Equals(pred.First()));
                    string idEntidadAux = objetoViejo.First();

                    if (prop.ValoresUnificados.ContainsKey(idEntidadAux))
                    {
                        objetos.Add(idEntidadAux);
                    }
                    else
                    {
                        string sujeto = pEntidadSecundariaID.Replace("/items/", "/entidadsecun_").ToLower();
                        EscribirTripletasEntidadSecundaria(sujeto, "http://gnoss/hasEntidad", null, idEntidadAux, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);
                        EscribirTripletasEntidadSecundaria(pEntidadSecundariaID, prop.Nombre, null, idEntidadAux, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);

                        EscribirTripletasEntidadSecundaria(idEntidadAux, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", null, prop.Rango, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);
                        EscribirTripletasEntidadSecundaria(idEntidadAux, "http://www.w3.org/2000/01/rdf-schema#label", null, prop.Rango, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);

                        objetos.Add(idEntidadAux);

                        if (prop.FunctionalProperty)
                        {
                            prop.UnicoValor = new KeyValuePair<string, ElementoOntologia>(idEntidadAux, null);
                        }
                        else
                        {
                            prop.ListaValores.Add(idEntidadAux, null);
                        }
                    }
                }

                if (objetos.Contains(objetoViejo.First()))
                {
                    if (pObjetoViejo == null)
                    {
                        pObjetoViejo = "";
                    }
                    if (pred.Length > 2)
                    {
                        ModificarInstanciaPorNivelEntidadSecundaria(pEntidad, pDataSetEnt, pGrafoOnto, objetoViejo.First(), pPredicado.Substring(pPredicado.IndexOf('|') + 1), pObjetoViejo.Substring(pObjetoViejo.IndexOf('|') + 1), pObjetoNuevo.Substring(pObjetoNuevo.IndexOf('|') + 1), ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, pEntidadPadreID);
                    }
                    else
                    {
                        
                        if (!pObjetoViejo.Contains('|'))
                        {
                            pObjetoViejo = "";
                        }
                        else
                        {
                            pObjetoViejo = pObjetoViejo.Substring(pObjetoViejo.IndexOf('|') + 1);
                        }
                        if (!pObjetoNuevo.Contains('|'))
                        {
                            pObjetoNuevo = "";
                        }
                        else
                        {
                            pObjetoNuevo = pObjetoNuevo.Substring(pObjetoNuevo.IndexOf('|') + 1);
                        }
                        EscribirTripletasEntidadSecundaria(objetoViejo.First(), pPredicado.Substring(pPredicado.IndexOf('|') + 1), pObjetoViejo, pObjetoNuevo, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);//(pGrafoOnto, objetoViejo.First(), pPredicado.Substring(pPredicado.IndexOf('|') + 1), pObjetoViejo, pObjetoNuevo);
                    }
                }
            }
            else
            {
                propiedad = pEntidad.ObtenerPropiedadPorNombreOUri(pPredicado);

                if (propiedad == null)
                {
                    throw new Exception("La entidad '" + pEntidadSecundariaID + "' no posee la propiedad '" + pPredicado + "'.");
                }

                valoresProp = FacetadoCN.ObtenerObjetosDataSetSegunPropiedadConIdioma(pDataSetEnt, pEntidadSecundariaID, pPredicado);

                if (!string.IsNullOrEmpty(pObjetoViejo) && string.IsNullOrEmpty(pObjetoNuevo) && propiedad.FunctionalProperty && valoresProp.Count == 1)
                {
                    throw new Exception("La propiedad '" + pPredicado + "' es funcional y no puede quedarse sin valor.");
                }

                EscribirTripletasEntidadSecundaria(pEntidadSecundariaID, pPredicado, pObjetoViejo, pObjetoNuevo, ref pTriplesInsertar, ref pTriplesBorrar, pTriplesComBorrar, pTriplesComInsertar, valoresProp);

            }
        }
        #endregion
    }
       
}
