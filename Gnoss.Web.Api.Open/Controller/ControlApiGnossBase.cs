using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models.ParametroGeneralDS;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.AD.Identidad;
using Es.Riam.Gnoss.AD.Parametro;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Usuarios;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Amigos;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.CL.Identidad;
using Es.Riam.Gnoss.CL.ParametrosAplicacion;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Elementos.ParametroAplicacion;
using Es.Riam.Gnoss.Elementos.ServiciosGenerales;
using Es.Riam.Gnoss.Elementos.Tesauro;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Identidad;
using Es.Riam.Gnoss.Logica.ParametroAplicacion;
using Es.Riam.Gnoss.Logica.ParametrosProyecto;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Servicios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.Controles;
using Es.Riam.Gnoss.Web.Controles.Documentacion;
using Es.Riam.Gnoss.Web.Controles.ParametroAplicacionGBD;
using Es.Riam.Gnoss.Web.Controles.Proyectos;
using Es.Riam.Gnoss.Web.Controles.ServiciosGenerales;
using Es.Riam.Gnoss.Web.Controles.Solicitudes;
using Es.Riam.Gnoss.Web.MVC.Models;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    [Serializable]
    public class GnossException : Exception
    {
        public GnossException(string message, HttpStatusCode status) : base(message)
        {
            Data.Add("status", status);
        }
    }

    public class ControlApiGnossBase : Controller
    {
        protected Guid UsuarioOAuth = Guid.Empty;

        protected IHttpContextAccessor mHttpContextAccessor;
        protected LoggingService mLoggingService;
        protected EntityContext mEntityContext;
        protected ConfigService mConfigService;
        protected VirtuosoAD mVirtuosoAD;
        protected RedisCacheWrapper mRedisCacheWrapper;
        protected GnossCache mGnossCache;
        protected EntityContextBASE mEntityContextBASE;
        protected ControladorBase mControladorBase;
        protected IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;

        public ControlApiGnossBase(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
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
            mControladorBase = new ControladorBase(loggingService, configService, entityContext, redisCacheWrapper, gnossCache, virtuosoAD, httpContextAccessor, mServicesUtilVirtuosoAndReplication);
        }

        public async override void OnActionExecuting(ActionExecutingContext controllerContext)
        {
            try
            {
                GuardarLogDatosPeticion(await ObtenerDatosPeticionParaLog());

                UsuarioOAuth = ComprobarPermisosOauth(mHttpContextAccessor.HttpContext.Request);
                if (UsuarioOAuth.Equals(Guid.Empty))
                {
                    mLoggingService.GuardarLog($"Firma incorrecta: {UtilOAuth.ObtenerUrlGetDePeticionOAuth(Request)}");

                    controllerContext.Result = Unauthorized("Invalid OAuth signature");
                }

                base.OnActionExecuting(controllerContext);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
            }
        }

        private ControladorProyecto mControladorProyecto;
        protected ControladorProyecto ControladorProyecto
        {
            get
            {
                if (mControladorProyecto == null)
                {
                    mControladorProyecto = new ControladorProyecto(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                }

                return mControladorProyecto;
            }
        }

        private async Task<string> ObtenerDatosPeticionParaLog()
        {
            string urlPeticion = mHttpContextAccessor.HttpContext.Request.Path.ToString();
            string metodoPeticion = mHttpContextAccessor.HttpContext.Request.Method;

            //mHttpContextAccessor.HttpContext.Request.Body.Position = 0;
            var streamReader = new StreamReader(mHttpContextAccessor.HttpContext.Request.Body);
            string parametrosPeticion = await streamReader.ReadToEndAsync();
            if (parametrosPeticion.Contains("password"))
            {
                parametrosPeticion = "";
            }
            if (mHttpContextAccessor.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                //mLoggingService.GuardarLogError($"Cabeceras: {mHttpContextAccessor.HttpContext.Request.Headers["Authorization"]}");
            }
            else if (mHttpContextAccessor.HttpContext.Request.Query.ContainsKey("oauth_signature"))
            {
                //mLoggingService.GuardarLogError($"Cabeceras: {mHttpContextAccessor.HttpContext.Request.Query["oauth_signature"]}");
            }

            string urlPeticionOauth = UtilOAuth.ObtenerUrlGetDePeticionOAuth(mHttpContextAccessor.HttpContext.Request);



            string datosPeticion = Environment.NewLine;
            datosPeticion += $"URL: {urlPeticion}{Environment.NewLine}";
            datosPeticion += $"Metodo: {metodoPeticion}{Environment.NewLine}";
            datosPeticion += $"Params: {parametrosPeticion}{Environment.NewLine}";
            datosPeticion += $"OAuth: {urlPeticionOauth}{Environment.NewLine}";
            datosPeticion += $"UsuarioOAuth: {UsuarioOAuth}{Environment.NewLine}";

            return datosPeticion;
        }

        #region Miembros

        /// <summary>
        /// Obtiene la URL base de los formularios semánticos.
        /// </summary>
        private string mBaseURLFormulariosSem;

        /// <summary>
        /// Obtiene la URL principal de esta aplicación con httyp, con puerto y sin idioma (ej: para http://didactalia.net el dominio principal es http://gnoss.com:80)
        /// </summary>
        private string mUrlPrincipalConHttpConPuertoSinIdioma;

        /// <summary>
        /// URL de intraGnoss.
        /// </summary>
        private string mUrlIntragnoss;

        /// <summary>
        /// DataSet de parámetros de aplicación
        /// </summary>
        private GestorParametroAplicacion mParametrosAplicacionDS;

        /// <summary>
        /// Obtiene la fila de parámetro general de FilaProy
        /// </summary>
        ParametroGeneral mFilaParametroGeneral;

        /// <summary>
        /// Mensaje de tiempos.
        /// </summary>
        private string mMensajeTiempos = "";

        /// <summary>
        /// Indica si se deben tomar tiempos.
        /// </summary>
        public static bool TomarTiempos = false;

        /// <summary>
        /// Indica si se deben guardar los datos de cada peticiones.
        /// </summary>
        public static bool GuardarDatosPeticion = false;

        /// <summary>
        /// URL del los elementos de contenido de la página
        /// </summary>
        private string mBaseUrlContent;

        /// <summary>
        /// Nombre corto del proyecto proyecto.
        /// </summary>
        protected string mNombreCortoComunidad;

        /// <summary>
        /// Fila de proyecto.
        /// </summary>
        private Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS.Proyecto mFilaProy;

        /// <summary>
        /// Parametros de configuración del proyecto.
        /// </summary>
        private Dictionary<string, string> mParametroProyecto;

        /// <summary>
        /// Devuelve la URL del servicio de documentación
        /// </summary>
        private string mUrlServicioWebDocumentacion;

        /// <summary>
        /// Obtiene o establece la información sobre el idioma del usuario
        /// </summary>
        private UtilIdiomas mUtilIdiomas = null;

        /// <summary>
        /// Parametro para almacenar los parámtros de las peticiones.
        /// </summary>
        protected string mParametrosPeticion = "";

        /// <summary>
        /// Url de intragnoss para servicios.
        /// </summary>
        private string mUrlIntragnossServicios;

        /// <summary>
        /// Fila del evento interno de la comunidad
        /// </summary>
        Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS.ProyectoEvento mInvitacionAEventoComunidad;

        /// <summary>
        /// Indica el proyecto principal del ecosistema
        /// </summary>
        private Guid? mProyectoPrincipalUnico = null;

        /// <summary>
        /// Devuelve la URL de UrlIntragnossServicios
        /// </summary>
        private string mUrlServicioInterno = null;

        /// <summary>
        /// Devuelve la URL del servicio de imágenes
        /// </summary>
        private string mUrlServicioImagenes;

        private bool mTieneGoogleDriveConfigurado = false;

        #endregion

        #region Métodos Protected y Privados

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pUsuarioIDOAuth"></param>
        /// <param name="pGestorIdentidades"></param>
        /// <param name="pIdentidadPerfil"></param>
        /// <param name="pProyecto"></param>
        /// <param name="pUsuarioID"></param>
        /// <param name="pNombreCortoOrg"></param>
        /// <param name="pTipoIdentidad"></param>
        /// <returns></returns>
        protected string RegistrarUsuarioEnComunidad(Guid pUsuarioIDOAuth, GestionIdentidades pGestorIdentidades, Identidad pIdentidadPerfil, Proyecto pProyecto, Guid pUsuarioID, string pNombreCortoOrg, TiposIdentidad pTipoIdentidad, bool pActualizarLive, Dictionary<Guid, bool> pRecibirNewsletterDefectoProyectos)
        {
            string error = string.Empty;

            //Comprobar si es un perfil de tipo personal o uno de tipo organización o de un tipo organización
            if (pIdentidadPerfil.PersonaID.HasValue && pIdentidadPerfil.Tipo.Equals(TiposIdentidad.Personal))
            {
                ControladorIdentidades.RegistrarPerfilPersonalEnProyecto(pUsuarioID, pIdentidadPerfil, pProyecto, pActualizarLive);

                ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                if (pProyecto.ListaTipoProyectoEventoAccion.ContainsKey(TipoProyectoEventoAccion.Registro))
                {
                    proyectoCL.AgregarEventosAccionProyectoPorProyectoYUsuarioID(pProyecto.Clave, pUsuarioID, TipoProyectoEventoAccion.Registro);
                }
                proyectoCL.Dispose();
            }
            else if (!pIdentidadPerfil.PersonaID.HasValue && (pIdentidadPerfil.Tipo.Equals(TiposIdentidad.Organizacion)))
            {
                AD.EntityModel.Models.IdentidadDS.Perfil filaPerfil = pGestorIdentidades.DataWrapperIdentidad.ListaPerfil.FirstOrDefault(perfil => perfil.PerfilID.Equals(pIdentidadPerfil.PerfilID));
                AD.EntityModel.Models.IdentidadDS.PerfilOrganizacion filaPerfilOrganizacion = pGestorIdentidades.DataWrapperIdentidad.ListaPerfilOrganizacion.FirstOrDefault(perfilOrg => perfilOrg.PerfilID.Equals(pIdentidadPerfil.PerfilID));

                ControladorIdentidades.RegistrarOrganizacionEnProyecto(filaPerfil, filaPerfilOrganizacion, pProyecto);
            }
            else if (pIdentidadPerfil.PersonaID.HasValue && pIdentidadPerfil.Tipo.Equals(TiposIdentidad.Profesor))
            {
                ControladorIdentidades.RegistrarPerfilPersonalEnProyecto(pUsuarioID, pIdentidadPerfil, pProyecto, pActualizarLive);

                ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                if (pProyecto.ListaTipoProyectoEventoAccion.ContainsKey(TipoProyectoEventoAccion.Registro))
                {
                    proyectoCL.AgregarEventosAccionProyectoPorProyectoYUsuarioID(pProyecto.Clave, pUsuarioID, TipoProyectoEventoAccion.Registro);
                }
                proyectoCL.Dispose();
            }
            else if ((pIdentidadPerfil.PersonaID.HasValue && pIdentidadPerfil.Tipo.Equals(TiposIdentidad.ProfesionalPersonal)) || (pIdentidadPerfil.PersonaID.HasValue && pIdentidadPerfil.Tipo.Equals(TiposIdentidad.ProfesionalCorporativo)))
            {
                //administrador de org o administrador de mygnoss
                if (EsAdministradorProyectoMyGnoss(pUsuarioIDOAuth) || EsAdministradorOrganizacion(pUsuarioIDOAuth, pNombreCortoOrg))
                {
                    OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid organizacionID = orgCN.ObtenerOrganizacionesIDPorNombre(pNombreCortoOrg);
                    bool participaOrgEnProy = orgCN.ParticipaOrganizacionEnProyecto(pProyecto.Clave, organizacionID);
                    orgCN.Dispose();

                    if (participaOrgEnProy)
                    {
                        ControladorIdentidades.RegistrarUsuarioEnProyecto(pUsuarioID, pGestorIdentidades.ListaPerfiles[pIdentidadPerfil.PerfilID], pProyecto.Clave, pTipoIdentidad, pActualizarLive, pRecibirNewsletterDefectoProyectos);
                    }
                    else
                    {
                        return "La organización no participa en el proyecto";
                    }
                }
                else
                {
                    return "El usuario no administra el metaproyecto ni la organización";
                }
            }

            IdentidadCL identidadCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            if (!pUsuarioID.Equals(Guid.Empty))
            {
                //Recargamos el gestor de identidades de la identidad actual
                //Obtener las identidades del usuario actual
                identidadCL.EliminarCacheGestorTodasIdentidadesUsuario(pUsuarioID, pIdentidadPerfil.PersonaID.Value);
            }

            List<string> listaClavesInvalidar = new List<string>();
            string prefijoClave;

            if (!string.IsNullOrEmpty(identidadCL.Dominio))
            {
                prefijoClave = identidadCL.Dominio;
            }
            else
            {
                prefijoClave = IdentidadCL.DominioEstatico;
            }

            prefijoClave = prefijoClave + "_" + identidadCL.ClaveCache[0] + "_";
            prefijoClave = prefijoClave.ToLower();

            if (pIdentidadPerfil.PersonaID.HasValue)
            {
                string rawKey = string.Concat("IdentidadActual_", pIdentidadPerfil.PersonaID.Value, "_", pIdentidadPerfil.PerfilID);
                listaClavesInvalidar.Add(prefijoClave + rawKey.ToLower());
            }
            string rawKey2 = "PerfilMVC_" + pIdentidadPerfil.PerfilID;
            listaClavesInvalidar.Add(prefijoClave + rawKey2.ToLower());

            identidadCL.InvalidarCachesMultiples(listaClavesInvalidar);
            identidadCL.InvalidarCacheMiembrosComunidad(pProyecto.Clave);
            identidadCL.Dispose();

            //Limpiamos la cache de contactos para el proyecto
            AmigosCL amigosCL = new AmigosCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            amigosCL.InvalidarAmigosPertenecenProyecto(pProyecto.Clave);
            amigosCL.Dispose();

            if (!string.IsNullOrEmpty(mNombreCortoComunidad) && InvitacionAEventoComunidad != null && InvitacionAEventoComunidad.Interno)
            {
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid identidadID = pIdentidadPerfil.Clave;

                if (identidadID == UsuarioAD.Invitado)
                {
                    IdentidadCN idenCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    identidadID = idenCN.ObtenerIdentidadIDDePerfilEnProyecto(pProyecto.Clave, pIdentidadPerfil.PerfilID).Value;
                    idenCN.Dispose();
                }

                bool participaEnEvento = proyCN.ObtenerSiIdentidadParticipaEnEvento(InvitacionAEventoComunidad.EventoID, identidadID);

                if (!participaEnEvento)
                {
                    //Solo lo aceptamos si no hay restriccion o tiene restriccion de nuevo en comunidad y es nuevo en comunidad
                    ControladorIdentidades.AceptarExtrasInvitacionAComunidad(pIdentidadPerfil.Persona, null, pProyecto, UrlIntragnoss, InvitacionAEventoComunidad, UtilIdiomas.LanguageCode);
                }
                proyCN.Dispose();
            }

            return error;
        }

        private ControladorIdentidades mControladorIdentidades;
        protected ControladorIdentidades ControladorIdentidades
        {
            get
            {
                if (mControladorIdentidades == null)
                {
                    mControladorIdentidades = new ControladorIdentidades(new GestionIdentidades(new DataWrapperIdentidad(), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication), mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                }
                return mControladorIdentidades;
            }
        }

        /// <summary>
        /// Comprueba si el usuario tiene permiso sobre la entidad secundaria.
        /// </summary>
        /// <param name="usuarioID">ID de usuario</param>
        protected void ComprobarUsuTienePermisoSobreEntSecundaria(Guid usuarioID)
        {
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            bool tienePermiso = proyCN.EsUsuarioAdministradorProyecto(usuarioID, FilaProy.ProyectoID);
            proyCN.Dispose();

            if (!tienePermiso)
            {
                tienePermiso = EsAdministradorProyectoMyGnoss(UsuarioOAuth);
            }

            if (!tienePermiso)
            {
                throw new GnossException("El usuario no tiene permiso para realizar esta operación en la comunidad.", System.Net.HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Proyecto del que se deben traer las ontologías.
        /// </summary>
        protected Guid ProyectoTraerOntosID
        {
            get
            {
                if (ParametroProyecto.ContainsKey(ParametroAD.ProyectoIDPatronOntologias))
                {
                    return new Guid(ParametroProyecto[ParametroAD.ProyectoIDPatronOntologias]);
                }
                else if (Proyecto.FilaProyecto.ProyectoSuperiorID.HasValue)
                {
                    return Proyecto.FilaProyecto.ProyectoSuperiorID.Value;
                }
                else
                {
                    return FilaProy.ProyectoID;
                }
            }
        }

        public string BaseURLFormulariosSem
        {
            get
            {
                if (mBaseURLFormulariosSem == null)
                {
                    string url = UrlPrincipalConHttpConPuertoSinIdioma;

                    url = url.Replace("www.", "").Replace("http://", "");
                    mBaseURLFormulariosSem = url;

                    if (url.Contains(":"))
                    {
                        mBaseURLFormulariosSem = url.Substring(0, url.IndexOf(":"));
                        url = url.Substring(url.IndexOf(":"));
                        url = url.Substring(url.IndexOf("/"));
                        mBaseURLFormulariosSem += url;
                    }

                    mBaseURLFormulariosSem = "http://" + mBaseURLFormulariosSem;
                }

                return mBaseURLFormulariosSem;
            }
        }

        /// <summary>
        /// Obtiene la URL principal de esta aplicación con httyp, con puerto y sin idioma (ej: para http://didactalia.net el dominio principal es http://gnoss.com:80)
        /// </summary>
        public string UrlPrincipalConHttpConPuertoSinIdioma
        {
            get
            {
                if (mUrlPrincipalConHttpConPuertoSinIdioma == null)
                {
                    try
                    {
                        //mUrlPrincipalConHttpConPuertoSinIdioma = Conexion.ObtenerUrlBase();
                        mUrlPrincipalConHttpConPuertoSinIdioma = UrlIntragnoss;

                        if (mUrlPrincipalConHttpConPuertoSinIdioma[mUrlPrincipalConHttpConPuertoSinIdioma.Length - 1] == '/')
                        {
                            mUrlPrincipalConHttpConPuertoSinIdioma = mUrlPrincipalConHttpConPuertoSinIdioma.Substring(0, mUrlPrincipalConHttpConPuertoSinIdioma.Length - 1);
                        }
                    }
                    catch (Exception) { }

                    if (mUrlPrincipalConHttpConPuertoSinIdioma.LastIndexOf("/") == mUrlPrincipalConHttpConPuertoSinIdioma.Length - 1)
                    {
                        mUrlPrincipalConHttpConPuertoSinIdioma = mUrlPrincipalConHttpConPuertoSinIdioma.Substring(0, mUrlPrincipalConHttpConPuertoSinIdioma.Length - 1);
                    }
                }
                return mUrlPrincipalConHttpConPuertoSinIdioma;
            }
        }

        private ControladorDocumentacion mControladorDocumentacion;

        protected ControladorDocumentacion ControladorDocumentacion
        {
            get
            {
                if (mControladorDocumentacion == null)
                {
                    mControladorDocumentacion = new ControladorDocumentacion(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                }
                return mControladorDocumentacion;
            }
        }

        private ControladorGrupos mControladorGrupos;
        protected ControladorGrupos ControladorGrupos
        {
            get
            {
                if (mControladorGrupos == null)
                {
                    mControladorGrupos = new ControladorGrupos(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                }
                return mControladorGrupos;
            }
        }

        protected string PasarObjetoALower(string pObjeto, List<string> pListaTipos)
        {
            if ((!pObjeto.StartsWith("<http://gnoss/")) && (!pListaTipos.Contains(pObjeto)))
            {
                return pObjeto.ToLower();
            }
            return pObjeto;
        }

        protected Guid ComprobarUsuarioOauthHttpHttps(HttpRequest pPeticion, string pUrlApi)
        {
            string UrlPeticionOauthOriginal = UtilOAuth.ObtenerUrlGetDePeticionOAuth(pPeticion, pUrlApi);
            string urlPeticionOauth = WebUtility.UrlEncode(UrlPeticionOauthOriginal);
            string servicioOauthUrl = mConfigService.ObtenerUrlServicio("urlOauth");
            string result = CallWebMethods.CallGetApi(servicioOauthUrl, $"ServicioOauth/ObtenerUsuarioAPartirDeUrl?pUrl={urlPeticionOauth}&pMetodoHttp=GET");
            Guid usuarioID = JsonConvert.DeserializeObject<Guid>(result);

            if (usuarioID == Guid.Empty)
            {
                UrlPeticionOauthOriginal = UtilOAuth.ObtenerUrlGetDePeticionOAuth(pPeticion, pUrlApi, false);
                urlPeticionOauth = WebUtility.UrlEncode(UrlPeticionOauthOriginal);
                result = CallWebMethods.CallGetApi(servicioOauthUrl, $"ServicioOauth/ObtenerUsuarioAPartirDeUrl?pUrl={urlPeticionOauth}&pMetodoHttp=GET");
                usuarioID = JsonConvert.DeserializeObject<Guid>(result);
            }
            return usuarioID;
        }

        /// <summary>
        /// Comprueba si la petición tiene permisos para realizarse
        /// </summary>
        /// <param name="pPeticion">Petición Http</param>
        /// <returns>TRUE si el oauth mandado en la cabecera de la petición retorna un usuario. FALSE en caso contrario</returns>
        protected Guid ComprobarPermisosOauth(HttpRequest pPeticion)
        {
            Guid salida = Guid.Empty;

            if (EsPeticionOAuth(pPeticion))
            {
                string urlPeticionOauthOriginal = "";

                try
                {
                    string urlApi = mConfigService.ObtenerUrlServicio("urlApi");
                    Guid usuarioID = ComprobarUsuarioOauthHttpHttps(pPeticion, urlApi);
                    mLoggingService.GuardarLogError($"valor del usuarioID al hacer la llamada tal y como le llega {usuarioID} || valor del urlApi {urlApi}");
                    mLoggingService.GuardarLogError($"Authority: {new Uri(UriHelper.GetEncodedUrl(pPeticion.HttpContext.Request)).Authority} || scheme: {pPeticion.HttpContext.Request.Scheme} || Host: {new Uri(UriHelper.GetEncodedUrl(pPeticion.HttpContext.Request)).Host} || URI: {new Uri(UriHelper.GetEncodedUrl(pPeticion.HttpContext.Request))}");
                    if (usuarioID == Guid.Empty)
                    {
                        mLoggingService.GuardarLogError($"Uri de llamada sin https {urlApi}");
                        if (!pPeticion.IsHttps && string.IsNullOrEmpty(urlApi))
                        {       
                            urlApi = $"https://{new Uri(UriHelper.GetEncodedUrl(pPeticion.HttpContext.Request)).Authority}";
                            mLoggingService.GuardarLogError($"Uri de llamada con https {urlApi}");
                            usuarioID = ComprobarUsuarioOauthHttpHttps(pPeticion, urlApi);
                        }
                    }
                    if (usuarioID != Guid.Empty)
                    {
                        salida = usuarioID;
                    }
                }
                catch (Exception ex)
                {
                    mLoggingService.GuardarLogError(ex, $"Error al ComprobarPermisosOauth: {urlPeticionOauthOriginal}");
                }
            }
            else
            {
                mLoggingService.GuardarLogError("No es una petición Oauth válida");
            }

            return salida;
        }

        /// <summary>
        /// Indica si la petición es OAuth o no.
        /// </summary>
        private bool EsPeticionOAuth(HttpRequest pPeticion)
        {
            return (pPeticion.Headers.ContainsKey("oauth_token") || (pPeticion.Headers.ContainsKey("Authorization") && pPeticion.Headers["Authorization"].FirstOrDefault().Contains("oauth_token=")));
        }

        /// <summary>
        /// Comprueba si el parámetro llega en la petición
        /// </summary>
        /// <param name="pNombreParametro">Nombre del parámetro</param>
        /// <param name="pObligatorio">Indica si el parámetro es requerido</param>
        /// <returns>Cadena con el valor del parámetro pedido</returns>
        protected string ComprobarParametros(string pNombreParametro, bool pObligatorio)
        {
            return (string)ComprobarParametros(pNombreParametro, pObligatorio, typeof(string));
        }

        /// <summary>
        /// Comprueba si el parámetro llega en la petición
        /// </summary>
        /// <param name="pNombreParametro">Nombre del parámetro</param>
        /// <param name="pObligatorio">Indica si el parámetro es requerido</param>
        /// <param name="pTipoObjecto">Tipo de objeto pedido</param>
        /// <returns>Object con el valor del parámetro pedido</returns>
        protected object ComprobarParametros(string pNombreParametro, bool pObligatorio, Type pTipoObjecto)
        {
            object salida = null;
            string valorParam = mHttpContextAccessor.HttpContext.Request.Query[pNombreParametro];

            if (valorParam == null && pObligatorio)
            {
                throw new GnossException($"Falta el parámetro: {pNombreParametro}", HttpStatusCode.NotFound);
            }

            if (pTipoObjecto.Equals(typeof(List<Guid>)))
            {
                List<Guid> lista = JsonConvert.DeserializeObject<List<Guid>>(valorParam);
                foreach (Guid id in lista)
                {
                    try
                    {
                        ComprobarParametros(pNombreParametro, pObligatorio, typeof(Guid));
                    }
                    catch (GnossException ex)
                    {
                        throw new GnossException($"The parameter {pNombreParametro} contains some empty or wrong formed guid.", HttpStatusCode.BadRequest);
                    }
                }
            }
            else if (pTipoObjecto.Equals(typeof(Guid)))
            {
                Guid id = Guid.Empty;
                if (Guid.TryParse(valorParam, out id))
                {
                    if (id.Equals(Guid.Empty))
                    {
                        throw new GnossException($"The parameter {pNombreParametro} can not be empty.", HttpStatusCode.BadRequest);
                    }
                    salida = id;
                }
                else
                {
                    throw new GnossException($"The parameter {pNombreParametro} has not the correct format.", HttpStatusCode.BadRequest);
                }

            }
            else if (pTipoObjecto.Equals(typeof(string)))
            {
                if (string.IsNullOrEmpty(valorParam))
                {
                    throw new GnossException($"The parameter {pNombreParametro} can not be empty.", HttpStatusCode.BadRequest);
                }
                salida = valorParam;
            }

            return salida;
        }

        /// <summary>
        /// Extra el parámetro para el log.
        /// </summary>
        /// <param name="pParametro">Parámetro</param>
        /// <returns>Parámetro para el log</returns>
        protected string ExtraerParametroParaLog(object pParametro)
        {
            return ExtraerParametroParaLog(pParametro, false);
        }

        /// <summary>
        /// Extra el parámetro para el log.
        /// </summary>
        /// <param name="pParametro">Parámetro</param>
        /// <param name="pForzarEscribirTexto">Fuerza a escribir en texto</param>
        /// <returns>Parámetro para el log</returns>
        protected string ExtraerParametroParaLog(object pParametro, bool pForzarEscribirTexto)
        {
            if (pParametro == null)
            {
                return "<NULL>";
            }
            else if (pParametro is string)
            {
                return (string)pParametro;
            }
            else if (pParametro is string[])
            {
                string cadenas = "";

                foreach (string cadena in (string[])pParametro)
                {
                    if (cadena != null)
                    {
                        cadenas += cadena + ",";
                    }
                    else
                    {
                        cadenas += "<NULL>,";
                    }
                }

                return cadenas;
            }
            else if (pParametro is string[][])
            {
                string cadenas = "";

                foreach (string[] array in (string[][])pParametro)
                {
                    cadenas += "[IncioArray]";
                    foreach (string cadena in array)
                    {
                        if (cadena != null)
                        {
                            cadenas += cadena + ",";
                        }
                        else
                        {
                            cadenas += "<NULL>,";
                        }
                    }

                    cadenas += "[FinArray]";
                }

                return cadenas;
            }
            else if (pForzarEscribirTexto && pParametro is byte[])
            {
                StreamReader reader = new StreamReader(new MemoryStream((byte[])pParametro));
                string lineaRDF = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
                reader = null;

                return lineaRDF;
            }
            else if (pParametro is byte[])
            {
                return $"<Byte[{((byte[])pParametro).Length}]>";
            }
            else if (pParametro is byte[][])
            {
                string archs = "";

                foreach (byte[] arch in (byte[][])pParametro)
                {
                    if (arch == null)
                    {
                        archs += "<NULL>,";
                    }
                    else
                    {
                        archs += $"<Byte[{arch.Length}]>,";
                    }
                }

                return archs;
            }
            else if (pParametro is int[])
            {
                return $"<int[{((int[])pParametro).Length}]>";
            }
            else
            {
                return pParametro.ToString();
            }
        }

        protected void GuardarLogTiempos(string pMensaje)
        {
            if (TomarTiempos)
            {
                mMensajeTiempos += $"{DateTime.Now.ToString()}:{DateTime.Now.Millisecond} {pMensaje}{Environment.NewLine}";
                EscribirLogTiempos(Guid.Empty);
                mMensajeTiempos = "";
            }
        }

        protected void GuardarLogDatosPeticion(string pDatos)
        {
            try
            {
                if (GuardarDatosPeticion)
                {
                    string directorio = $"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}logs";
                    Directory.CreateDirectory(directorio);
                    string rutaFichero = $"{directorio}\\logDatosPeticion_apiRecursos_{DateTime.Now.ToString("yyyy-MM-dd")}.log";

                    //Si el fichero supera el tamaño máximo lo renombro
                    if (System.IO.File.Exists(rutaFichero))
                    {

                        FileInfo fichero = new FileInfo(rutaFichero);
                        if (fichero.Length > 10000000)
                        {
                            fichero.CopyTo(rutaFichero.Replace(".log", $"_bk_{DateTime.Now.ToString("hh-mm-ss")}.log"));
                            fichero.Delete();
                        }
                    }

                    //Añado el error al fichero
                    using (StreamWriter sw = new StreamWriter(rutaFichero, true, Encoding.Default))
                    {
                        sw.WriteLine("Fecha: " + DateTime.Now);
                        // Escribo los datos
                        sw.Write(pDatos);
                        sw.WriteLine($"{Environment.NewLine}___________________________________________________________________________________________{Environment.NewLine}");
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Lee los nodos en los que podemos recibir un valor de tipo string o short
        /// </summary>
        /// <param name="nodo">Nodo del XML</param>
        /// <param name="nom">Nombre del nodo</param>
        /// <param name="pTipo">Tipo que se espera recibir</param>
        /// <returns>Object que contendrá una string o un short</returns>
        protected object LeerNodo(XmlNode nodo, string nom, Type pTipo)
        {
            return UtilXML.LeerNodo(nodo, nom, pTipo);
        }

        protected void EscribirLogTiempos(Guid pDocumentoID)
        {
            if (TomarTiempos)
            {
                try
                {
                    string directorio = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "logs";
                    Directory.CreateDirectory(directorio);
                    string rutaFichero = $"{directorio}\\logTiempos_apiRecursos_{DateTime.Now.ToString("yyyy-MM-dd")}.log";

                    //Si el fichero supera el tamaño máximo lo renombro
                    if (System.IO.File.Exists(rutaFichero))
                    {
                        try
                        {
                            FileInfo fichero = new FileInfo(rutaFichero);
                            if (fichero.Length > 10000000)
                            {
                                fichero.CopyTo(rutaFichero.Replace(".log", $"_bk_{DateTime.Now.ToString("hh-mm-ss")}.log"));
                                fichero.Delete();
                            }
                        }
                        catch (Exception) { }
                    }

                    //Añado el error al fichero
                    using (StreamWriter sw = new StreamWriter(rutaFichero, true, System.Text.Encoding.Default))
                    {
                        sw.WriteLine($"{Environment.NewLine}Doc: {pDocumentoID} Fecha: {DateTime.Now}{Environment.NewLine}{Environment.NewLine}");
                        // Escribo el error
                        sw.Write(mMensajeTiempos);
                        sw.WriteLine($"{Environment.NewLine}{Environment.NewLine}___________________________________________________________________________________________{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}");
                    }
                }
                catch (Exception) { }
            }
        }

        protected bool EsAdministradorProyectoMyGnoss(Guid pUsuarioID)
        {
            return EsAdministradorProyecto(pUsuarioID, ProyectoAD.MetaProyecto);
        }

        protected bool EsAdministradorProyecto(Guid pUsuarioID, Guid pProyectoID)
        {
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            bool esAdminProyecto = proyCN.EsUsuarioAdministradorProyecto(pUsuarioID, pProyectoID);
            proyCN.Dispose();
            return esAdminProyecto;
        }

        protected bool EsAdministradorOrganizacion(Guid pUsuarioID, string pNombreCortoOrg)
        {
            bool salida = false;
            if (!string.IsNullOrEmpty(pNombreCortoOrg) && !pUsuarioID.Equals(Guid.Empty))
            {
                OrganizacionCN organizacionCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid orgID = organizacionCN.ObtenerOrganizacionesIDPorNombre(pNombreCortoOrg);
                DataWrapperOrganizacion orgDW = organizacionCN.CargarAdministradoresdeOrganizacion(orgID);
                organizacionCN.Dispose();

                if (orgDW != null && orgDW.ListaAdministradorOrganizacion != null)
                {
                    salida = !orgDW.ListaAdministradorOrganizacion.Where(item => item.UsuarioID.Equals(pUsuarioID) && item.OrganizacionID.Equals(orgID) && item.Tipo.Equals((short)TipoAdministradoresOrganizacion.Administrador)).Equals(null);
                }
            }

            return salida;
        }

        private DataWrapperProyecto mDataWrapperProyecto;

        private ConcurrentDictionary<string, Guid> mDictionary;

        private DataWrapperProyecto DataWrapperProyecto
        {
            get
            {
                if (mDataWrapperProyecto == null)
                {
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid proyectoID = proyCN.ObtenerProyectoIDPorNombre(mNombreCortoComunidad);
                    mDataWrapperProyecto = proyCN.ObtenerProyectoPorID(proyectoID);
                }
                return mDataWrapperProyecto;
            }
        }

        private UtilUsuario mUtilUsuario;

        protected UtilUsuario UtilUsuario
        {
            get
            {
                if (mUtilUsuario == null)
                {
                    mUtilUsuario = new UtilUsuario(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper);
                }
                return mUtilUsuario;
            }
        }

        private Proyecto mProyecto;
        public Proyecto Proyecto
        {
            get
            {
                if (mProyecto == null)
                {
                    mProyecto = new Proyecto(FilaProy, new GestionProyecto(DataWrapperProyecto, mLoggingService, mEntityContext), mLoggingService, mEntityContext);
                }
                return mProyecto;
            }
        }

        private AD.EntityModel.Models.ProyectoDS.Proyecto ObtenerFilaProyecto()
        {
            return DataWrapperProyecto.ListaProyecto.FirstOrDefault();
        }

        /// <summary>
        /// Agrega traza.
        /// </summary>
        /// <param name="pMensaje">Mensaje</param>
        protected void AgregarTraza(string pMensaje)
        {
            mLoggingService.AgregarEntrada(pMensaje);
        }

        /// <summary>
        /// Obtiene la url base idioma.
        /// </summary>
        /// <param name="pIdioma">Idioma usuario</param>
        /// <returns>Url base idioma</returns>
        protected string ObtenerUrlBaseIdioma(string pIdioma)
        {
            ParametroAplicacionCL paramCL = new ParametroAplicacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            List<ParametroAplicacion> parametrosAplicacionDS = paramCL.ObtenerParametrosAplicacionPorContext();


            //Obtenenos la urlIntragnoss (acaba en "/")
            string baseUrlIdioma = parametrosAplicacionDS.Find(parametroApp => parametroApp.Parametro.Equals("UrlIntragnoss")).Valor;

            if (pIdioma != "es")
            {
                baseUrlIdioma = $"{baseUrlIdioma}{pIdioma}/";
            }
            baseUrlIdioma = baseUrlIdioma.Substring(0, baseUrlIdioma.Length - 1);

            return baseUrlIdioma;
        }

        /// <summary>
        /// Método que transforma la petición para agregar al servicio módulo base en un string con los parámetros que necesita el servicio de replicación para enviar la solicitud.
        /// </summary>
        /// <param name="pDocumentoID">DocumentoID que se ha creado/editado</param>
        /// <param name="pTipoDoc">Tipo de documento que se ha creado.</param>
        /// <param name="pProyectoID">Proyecto donde se ha creado el documento.</param>
        /// <param name="pPrioridadBase">Prioridad para procesarlo por el servicio modulo base.</param>
        /// <returns>Cadena de parámetros necesarios para que el servicio de replicación inserte en el módulo base.</returns>
        protected string ObtenerInfoExtraBaseDocumentoAgregar(Guid pDocumentoID, short pTipoDoc, Guid pProyectoID, short pPrioridadBase)
        {
            return ObtenerInfoExtraBaseDocumento(pDocumentoID, pTipoDoc, pProyectoID, pPrioridadBase, 0);
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
            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            int id = proyCL.ObtenerTablaBaseProyectoIDProyectoPorID(pProyectoID);
            proyCL.Dispose();

            string tempString = "";

            //TablaBaseProyectoID
            tempString += $"{id}|";

            //Tag
            tempString += $"{Constantes.ID_TAG_DOCUMENTO}{pDocumentoID}{Constantes.ID_TAG_DOCUMENTO},{Constantes.TIPO_DOC}{pTipoDoc}{Constantes.TIPO_DOC}|";

            //Tipo de acción (0 agregado) (1 eliminado)
            tempString += $"{pAccion}|";

            //Prioridad de procesado por el servicio base.
            tempString += $"{pPrioridadBase}|";

            //pOtrosArgumentos;
            return tempString;
        }


        protected Identidad CargarIdentidad(GestorDocumental pGestorDocumental, AD.EntityModel.Models.ProyectoDS.Proyecto pFilaProy, Guid pUsuarioID, bool pCargarGnossIdentity)
        {
            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            pGestorDocumental.GestorIdentidades = new GestionIdentidades(identCN.ObtenerPerfilesDeUsuarioEnProyecto(pUsuarioID, pFilaProy.ProyectoID), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
            identCN.Dispose();

            Identidad identidad = null;

            foreach (AD.EntityModel.Models.IdentidadDS.Identidad filaIdent in pGestorDocumental.GestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Where(ident => ident.ProyectoID.Equals(pFilaProy.ProyectoID)).ToList())
            {
                if (!filaIdent.FechaBaja.HasValue)
                {
                    identidad = pGestorDocumental.GestorIdentidades.ListaIdentidades[filaIdent.IdentidadID];

                    IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    pGestorDocumental.GestorIdentidades.DataWrapperIdentidad.Merge(identidadCN.ObtenerGruposParticipaIdentidad(identidad.Clave, true));
                    pGestorDocumental.GestorIdentidades.DataWrapperIdentidad.Merge(identidadCN.ObtenerGruposParticipaIdentidad(identidad.IdentidadMyGNOSS.Clave, true));
                    identidadCN.Dispose();
                    break;
                }
            }

            if (identidad == null)
            {
                return identidad;
            }

            CompletarCargaIdentidad(identidad.Clave, pGestorDocumental.GestorIdentidades);

            if (pCargarGnossIdentity)
            {
                #region Cargo GnossIdentity para poder comprobar permisos de organización

                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
                dataWrapperUsuario.ListaUsuario.Add(usuCN.ObtenerUsuarioPorID(pUsuarioID));
                identidad.GestorIdentidades.GestorPersonas.GestorUsuarios = new GestionUsuarios(dataWrapperUsuario, mLoggingService, mEntityContext, mConfigService);
                usuCN.Dispose();

                UtilUsuario.ValidarUsuario(identidad.Usuario.FilaUsuario.Login, FilaProy.OrganizacionID, FilaProy.ProyectoID);

                #endregion
            }

            return identidad;
        }

        /// <summary>
        /// Carga la persona y la organización de la identidad si aún no las tiene cargadas
        /// </summary>
        /// <param name="pGestorIdentidades">Gestor de identidades</param>
        /// <param name="pIdentidadID">Identificador de la identidad que se desea cargar</param>
        protected void CompletarCargaIdentidad(Guid pIdentidadID, GestionIdentidades pGestorIdentidades)
        {
            Identidad ident = pGestorIdentidades.ListaIdentidades[pIdentidadID];
            //Si la identidad que comparte no está obtenida por completo, la obtengo ahora
            if ((!ident.EsOrganizacion) && (ident.Persona == null))
            {
                //Cargo la persona
                PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperPersona persDW = new DataWrapperPersona();
                persDW.ListaPersona.Add(persCN.ObtenerPersonaPorIdentidadCargaLigera(pIdentidadID));
                persCN.Dispose();

                if (pGestorIdentidades.GestorPersonas == null)
                {
                    pGestorIdentidades.GestorPersonas = new GestionPersonas(new DataWrapperPersona(), mLoggingService, mEntityContext);
                }

                pGestorIdentidades.GestorPersonas.DataWrapperPersonas.Merge(persDW);
                pGestorIdentidades.GestorPersonas.RecargarPersonas();
            }

            if (ident.TrabajaConOrganizacion && (ident.OrganizacionPerfil == null))
            {
                IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                pGestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerIdentidadDeOrganizacion(ident.OrganizacionID.Value, ident.FilaIdentidad.ProyectoID, true));
                identCN.Dispose();

                //Cargo la organización
                OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperOrganizacion orgDW = orgCN.ObtenerOrganizacionDeIdentidad(pIdentidadID);
                orgCN.Dispose();

                if (pGestorIdentidades.GestorOrganizaciones == null)
                {
                    pGestorIdentidades.GestorOrganizaciones = new GestionOrganizaciones(new DataWrapperOrganizacion(), mLoggingService, mEntityContext);
                }

                pGestorIdentidades.GestorOrganizaciones.OrganizacionDW.Merge(orgDW);
                pGestorIdentidades.GestorOrganizaciones.CargarOrganizaciones();
            }
        }

        protected GestorDocumental CargarGestorDocumental(AD.EntityModel.Models.ProyectoDS.Proyecto pFilaProy)
        {
            DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestorDocumental gestorDoc = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext);
            docCN.ObtenerBaseRecursosProyecto(gestorDoc.DataWrapperDocumentacion, pFilaProy.ProyectoID, pFilaProy.OrganizacionID, Guid.Empty);
            docCN.Dispose();

            TesauroCL tesCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            gestorDoc.GestorTesauro = new GestionTesauro(tesCL.ObtenerTesauroDeProyecto(pFilaProy.ProyectoID), mLoggingService, mEntityContext);
            tesCL.Dispose();

            return gestorDoc;
        }

        private ControladorDeSolicitudes mControladorDeSolicitudes;
        protected ControladorDeSolicitudes ControladorDeSolicitudes
        {
            get
            {
                if (mControladorDeSolicitudes == null)
                {
                    mControladorDeSolicitudes = new ControladorDeSolicitudes(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                }
                return mControladorDeSolicitudes;
            }
        }

        private UtilidadesVirtuoso mUtilidadesVirtuoso;
        protected UtilidadesVirtuoso UtilidadesVirtuoso
        {
            get
            {
                if (mUtilidadesVirtuoso == null)
                {
                    mUtilidadesVirtuoso = new UtilidadesVirtuoso(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mEntityContextBASE, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                }
                return mUtilidadesVirtuoso;
            }
        }

        private UtilServicios mUtilServicios;
        protected UtilServicios UtilServicios
        {
            get
            {
                if (mUtilServicios == null)
                {
                    mUtilServicios = new UtilServicios(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mServicesUtilVirtuosoAndReplication);
                }
                return mUtilServicios;
            }
        }

        protected void ComprobacionCambiosCachesLocales(Guid pProyectoID)
        {
            UtilServicios.ComprobacionCambiosCachesLocales(pProyectoID);
        }

        protected bool ComprobarFechaISO8601(string pFecha)
        {
            bool esValida = false;
            try
            {
                if (!pFecha.Contains(" ") && pFecha.Contains("T"))
                {
                    DateTime.Parse(pFecha, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    esValida = true;
                }
            }
            catch (FormatException fex)
            {
                mLoggingService.GuardarLogError(fex, "Error al ComprobarFechaISO8601: " + pFecha);
            }

            return esValida;
        }

        protected DateTime ConvertirFechaAISO8601(DateTime pFecha)
        {
            return DateTime.Parse(pFecha.ToString("yyyy-MM-ddTHH:mm:ssZ"), null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Obtiene la lista de items extra que se obtendrá de la búsqueda y su prefijo (recetas, peliculas, etc)
        /// </summary>
        protected Dictionary<string, List<string>> ObtenerInformacionOntologias(Guid pOrganizacionID, Guid pProyectoID)
        {
            FacetaCL facetaCL = new FacetaCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            List<AD.EntityModel.Models.Faceta.OntologiaProyecto> listaOntologiaProyecto = facetaCL.ObtenerOntologiasProyecto(pOrganizacionID, pProyectoID);

            return FacetadoAD.ObtenerInformacionOntologias(listaOntologiaProyecto);
        }

        #endregion

        #region Propiedades

        public string UrlServicioInterno
        {
            get
            {
                if (mUrlServicioInterno == null)
                {
                    mUrlServicioInterno = mConfigService.ObtenerUrlServicioInterno();
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
        /// Obtiene la url interna de Gnoss.
        /// </summary>
        public string UrlIntragnoss
        {
            get
            {
                if (string.IsNullOrEmpty(mUrlIntragnoss))
                {
                    mUrlIntragnoss = ParametrosAplicacionDS.ParametroAplicacion.Find(parametroApp => parametroApp.Parametro.Equals("UrlIntragnoss")).Valor;
                }
                return mUrlIntragnoss;
            }
        }

        /// <summary>
        /// Indica si los recursos que se suban deben ir al live o no.
        /// </summary>
        public bool AgregarColaLive
        {
            get
            {
                string param = mConfigService.ObtenerColaLive();
                return (string.IsNullOrEmpty(param) || param == "1");
            }
        }

        /// <summary>
        /// Obtiene el dataSet de parámetros de aplicación
        /// </summary>
        public GestorParametroAplicacion ParametrosAplicacionDS
        {
            get
            {
                if (mParametrosAplicacionDS == null)
                {

                    mParametrosAplicacionDS = new GestorParametroAplicacion();
                    ParametroAplicacionGBD prametroAplicacionGBD = new ParametroAplicacionGBD(mLoggingService, mEntityContext, mConfigService);
                    prametroAplicacionGBD.ObtenerConfiguracionGnoss(mParametrosAplicacionDS);
                }

                return mParametrosAplicacionDS;
            }
        }

        /// <summary>
        /// Obtiene la fila de parámetro general de FilaProy
        /// </summary>
        public ParametroGeneral FilaParametroGeneral
        {
            get
            {
                if (mFilaParametroGeneral == null)
                {
                    ParametroGeneralCN paramGralCN = new ParametroGeneralCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    mFilaParametroGeneral = paramGralCN.ObtenerFilaParametrosGeneralesDeProyecto(FilaProy.ProyectoID);
                    paramGralCN.Dispose();
                }

                return mFilaParametroGeneral;
            }
        }

        /// <summary>
        /// URL del los elementos de contenido de la página
        /// </summary>
        public string BaseUrlContent
        {
            get
            {
                if (string.IsNullOrEmpty(mBaseUrlContent))
                {
                    ParametroAplicacionCN paramApliCN = new ParametroAplicacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid proyectoID = Guid.Empty;
                    if (mNombreCortoComunidad != null && FilaProy != null)
                    {
                        proyectoID = FilaProy.ProyectoID;
                    }
                    mBaseUrlContent = paramApliCN.ObtenerUrlContent(proyectoID);

                    paramApliCN.Dispose();
                }

                return mBaseUrlContent;
            }
        }

        /// <summary>
        /// Fila de proyecto.
        /// </summary>
        public Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS.Proyecto FilaProy
        {
            get
            {
                if (mFilaProy == null)
                {
                    if (string.IsNullOrEmpty(mNombreCortoComunidad))
                    {
                        throw new GnossException("The community short name can not be null or empty.", HttpStatusCode.BadRequest);
                    }

                    mFilaProy = ObtenerFilaProyecto();

                    if (mFilaProy == null)
                    {
                        throw new GnossException($"The community '{mNombreCortoComunidad}' does not exist. ", HttpStatusCode.BadRequest);
                    }
                }
                return mFilaProy;
            }
        }

        /// <summary>
        /// Parametros de configuración del proyecto.
        /// </summary>
        protected Dictionary<string, string> ParametroProyecto
        {
            get
            {
                if (mParametroProyecto == null)
                {
                    ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                    mParametroProyecto = proyectoCL.ObtenerParametrosProyecto(FilaProy.ProyectoID);
                    proyectoCL.Dispose();
                }

                return mParametroProyecto;
            }
            set { mParametroProyecto = value; }
        }

        /// <summary>
        /// Devuelve la URL del servicio de documentación
        /// </summary>
        public string UrlServicioWebDocumentacion
        {
            get
            {
                if (mUrlServicioWebDocumentacion == null)
                {
                    mUrlServicioWebDocumentacion = mConfigService.ObtenerUrlServicioDocumental();
                }
                return mUrlServicioWebDocumentacion;
            }
        }

        /// <summary>
        /// Url de intragnoss para servicios.
        /// </summary>
        public string UrlIntragnossServicios
        {
            get
            {
                if (string.IsNullOrEmpty(mUrlIntragnossServicios))
                {
                    if (ParametroProyecto != null && ParametroProyecto.ContainsKey(TiposParametrosAplicacion.UrlIntragnossServicios))
                    {
                        mUrlIntragnossServicios = ParametroProyecto[TiposParametrosAplicacion.UrlIntragnossServicios];
                    }
                    else
                    {
                        List<ParametroAplicacion> filas = ParametrosAplicacionDS.ParametroAplicacion.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.UrlIntragnossServicios)).ToList();

                        if (filas.Count > 0)
                        {
                            mUrlIntragnossServicios = filas[0].Valor;
                        }
                        else
                        {
                            mUrlIntragnossServicios = "";
                        }
                    }

                    mUrlIntragnossServicios = ParametrosAplicacionDS.ParametroAplicacion.Find(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.UrlIntragnossServicios)).Valor;
                }
                return mUrlIntragnossServicios;
            }
        }

        /// <summary>
        /// Obtiene o establece la información sobre el idioma del usuario
        /// </summary>
        public UtilIdiomas UtilIdiomas
        {
            get
            {
                if (mUtilIdiomas == null)
                {
                    mUtilIdiomas = new UtilIdiomas($"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}{Path.DirectorySeparatorChar}languages", mHttpContextAccessor.HttpContext.Request.Headers["Accept-Language"], "es", Guid.Empty, Guid.Empty, Guid.Empty, mLoggingService, mEntityContext, mConfigService);
                }
                return mUtilIdiomas;
            }
            set
            {
                mUtilIdiomas = value;
            }
        }

        /// <summary>
        /// Obtiene la fila del evento interno activo de la comunidad
        /// </summary>
        public Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS.ProyectoEvento InvitacionAEventoComunidad
        {
            get
            {
                if (mInvitacionAEventoComunidad == null)
                {
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    DataWrapperProyecto proyDS = proyCN.ObtenerEventosProyectoPorProyectoID(FilaProy.ProyectoID);
                    proyCN.Dispose();
                    mInvitacionAEventoComunidad = proyDS.ListaProyectoEvento.FirstOrDefault(proyecto => proyecto.Interno && proyecto.Activo);
                }

                return mInvitacionAEventoComunidad;
            }
        }

        /// <summary>
        /// Obtiene el proyecto principal de un ecosistema sin metaproyecto
        /// </summary>
        public Guid ProyectoPrincipalUnico
        {
            get
            {
                if (mProyectoPrincipalUnico == null)
                {
                    mProyectoPrincipalUnico = ProyectoAD.MetaProyecto;
                    List<ParametroAplicacion> parametrosAplicacion = ParametrosAplicacionDS.ParametroAplicacion.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.ComunidadPrincipalID.ToString())).ToList();
                    if (parametrosAplicacion.Count > 0)
                    {
                        mProyectoPrincipalUnico = new Guid(parametrosAplicacion[0].Valor.ToString());
                    }
                }
                return mProyectoPrincipalUnico.Value;
            }
        }

        /// <summary>
        /// Indica si hay que subir los recursos a GoogleDrive
        /// </summary>
        public bool TieneGoogleDriveConfigurado
        {
            get
            {
                if (ParametroProyecto != null)
                {
                    mTieneGoogleDriveConfigurado = ParametroProyecto.ContainsKey(TiposParametrosAplicacion.GoogleDrive) && (ParametroProyecto[TiposParametrosAplicacion.GoogleDrive].Equals("1") || ParametroProyecto[TiposParametrosAplicacion.GoogleDrive].ToLower().Equals("true"));
                }

                List<ParametroAplicacion> parametrosAplicacion = ParametrosAplicacionDS.ParametroAplicacion.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.GoogleDrive)).ToList();
                if (!mTieneGoogleDriveConfigurado && parametrosAplicacion.FirstOrDefault() != null)
                {
                    mTieneGoogleDriveConfigurado = parametrosAplicacion.Count > 0 && (parametrosAplicacion[0].Equals("1") || parametrosAplicacion[0].Valor.ToString().ToLower().Equals("true"));
                }

                return mTieneGoogleDriveConfigurado;
            }
        }

        #endregion
    }
}
