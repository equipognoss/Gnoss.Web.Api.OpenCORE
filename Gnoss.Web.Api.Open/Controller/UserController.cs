using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.BASE_BD;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models;
using Es.Riam.Gnoss.AD.EntityModel.Models.Pais;
using Es.Riam.Gnoss.AD.EntityModel.Models.ParametroGeneralDS;
using Es.Riam.Gnoss.AD.EntityModel.Models.Solicitud;
using Es.Riam.Gnoss.AD.EntityModel.Models.UsuarioDS;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.AD.Identidad;
using Es.Riam.Gnoss.AD.Live;
using Es.Riam.Gnoss.AD.Live.Model;
using Es.Riam.Gnoss.AD.Notificacion;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.Peticion;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Suscripcion;
using Es.Riam.Gnoss.AD.Usuarios;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Amigos;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.CL.Identidad;
using Es.Riam.Gnoss.CL.ParametrosAplicacion;
using Es.Riam.Gnoss.CL.ParametrosProyecto;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.CL.Usuarios;
using Es.Riam.Gnoss.Elementos.Amigos;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Elementos.Notificacion;
using Es.Riam.Gnoss.Elementos.ParametroAplicacion;
using Es.Riam.Gnoss.Elementos.ParametroGeneralDSEspacio;
using Es.Riam.Gnoss.Elementos.ServiciosGenerales;
using Es.Riam.Gnoss.Elementos.Suscripcion;
using Es.Riam.Gnoss.Elementos.Tesauro;
using Es.Riam.Gnoss.Logica;
using Es.Riam.Gnoss.Logica.Amigos;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.Identidad;
using Es.Riam.Gnoss.Logica.Live;
using Es.Riam.Gnoss.Logica.Notificacion;
using Es.Riam.Gnoss.Logica.Peticion;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Suscripcion;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.Controles;
using Es.Riam.Gnoss.Web.Controles.Amigos;
using Es.Riam.Gnoss.Web.Controles.Documentacion;
using Es.Riam.Gnoss.Web.Controles.ServiciosGenerales;
using Es.Riam.Gnoss.Web.Controles.Solicitudes;
using Es.Riam.Gnoss.Web.MVC.Models.ViewModels;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    /// <summary>
    /// Use it to query / create / modify / delete users
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public partial class UserController : ControlApiGnossBase
    {

        public UserController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {
        }

        #region Métodos Públicos

        /// <summary>
        /// Checks if the emails already exists in the database
        /// </summary>
        /// <param name="emails">Email list that you want to check</param>
        /// <returns>Email list that already exists in the database</returns>
        /// <example>POST user/exists-email-in-database</example>
        [HttpPost, Route("exists-email-in-database")]
        public List<string> ExistenEmailsEnBaseDatos(List<string> emails)
        {
            if (emails == null || emails.Count == 0)
            {
                throw new GnossException("The parameter 'emails' can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {

                PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                List<string> correosPersonas = personaCN.EmailYaPerteneceAPersona(emails.ToArray());
                personaCN.Dispose();

                return correosPersonas;
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Change email a user
        /// </summary>
        /// <param name="user_id">User's identificator</param>
        /// <param name="email">Email to change</param>
        /// <example>POST community/block-member</example>
        [HttpPost, ActionName("change-user-email")]
        public void CambiarEmailUsuario(Guid user_id, string email)
        {
            CambiarEmailUser(user_id, email);
        }


        /// <summary>
        /// Gets the short names of the communities in which the user participates.
        /// </summary>
        /// <returns>Short names of the communities in which the user participates</returns>
        /// <example>GET user/get-communities</example>
        [HttpGet, Route("get-communities")]
        public List<string> ObtenerNombreCortoProyectosParticipaUsuario()
        {
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Dictionary<Guid, string> proysPart = proyCN.ObtenerNombresCortosProyectosParticipaUsuarioSinBloquearNiAbandonarYConfigurables(UsuarioOAuth);
            proyCN.Dispose();

            proysPart.Remove(ProyectoAD.MetaProyecto);

            List<string> nombresProy = new List<string>(proysPart.Values);
            List<string> nombresProyAlter = new List<string>(proysPart.Values);

            UtilUsuario utilUsuario = new UtilUsuario(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper);
            foreach (Guid proyID in proysPart.Keys)
            {
                ParametroGeneral pgr = utilUsuario.ObtenerFilaParametrosGeneralesDeProyecto(proyID);
                if (!pgr.CargasMasivasDisponibles)
                {
                    nombresProy.Remove(proysPart[proyID]);
                }
            }

            return nombresProy;
        }

        /// <summary>
        /// Gets the short name of the communities that manages the user.
        /// </summary>
        /// <returns>Short name of the communities that manages the user</returns>
        /// <example>GET user/get-admin-communities</example>
        [HttpGet, Route("get-admin-communities")]
        public List<string> ObtenerNombreCortoProyectosAdministraUsuario(string login)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {

                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid? userID = usuCN.ObtenerUsuarioIDPorLoginOEmail(login);

                List<string> nombresProy;

                if (userID.HasValue)
                {
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Dictionary<Guid, string> proysPart = proyCN.ObtenerNombresCortosProyectosAdministraUsuarioSinBloquearNiAbandonar(UsuarioOAuth);
                    proyCN.Dispose();

                    proysPart.Remove(ProyectoAD.MetaProyecto);

                    nombresProy = new List<string>(proysPart.Values);
                }
                else
                {
                    nombresProy = new List<string>();
                }

                return nombresProy;
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets the name of the user making the request OAuth
        /// </summary>
        /// <returns>Name of the user making the request OAuth</returns>
        /// <example>GET user/get-complete-name</example>
        [HttpGet, Route("get-complete-name")]
        public string GetCompleteName()
        {
            PersonaCN perCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperPersona dataWrapperPersona = perCN.ObtenerPersonaPorUsuario(UsuarioOAuth);
            perCN.Dispose();

            if (dataWrapperPersona.ListaPersona.Count == 0)
            {
                throw new GnossException("There is no user with the OAuth signature", HttpStatusCode.NotFound);
            }
            string nombre = dataWrapperPersona.ListaPersona.First().Nombre + " " + dataWrapperPersona.ListaPersona.First().Apellidos;

            return nombre;
        }

        /// <summary>
        /// Gets the data user by user ID and the community short name
        /// </summary>
        /// <param name="user_id">User ID you want to get data</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>User data that has been requested</returns>
        /// <example>GET user/get-by-id</example>
        [HttpGet, Route("get-by-id")]
        public User GetUsuarioPorID(Guid user_id, string community_short_name)
        {
            User jsonUsuario = ObtenerJsonUsuarioProyecto("", "", UsuarioOAuth, user_id, community_short_name, false);

            return jsonUsuario;
        }

        /// <summary>
        /// Get the data a user by user short name and the community short name
        /// </summary>
        /// <param name="user_short_name">User short name you want to get data</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>User data that has been requested</returns>
        /// <example>GET user/get-by-short-name</example>
        [HttpGet, Route("get-by-short-name")]
        public User GetUsuarioPorNombreCorto(string user_short_name, string community_short_name)
        {
            User jsonUsuario = ObtenerJsonUsuarioProyecto(user_short_name, "", UsuarioOAuth, Guid.Empty, community_short_name, false);

            return jsonUsuario;
        }

        /// <summary>
        /// Get the data a user by user short name and the community short name
        /// </summary>
        /// <param name="user_id">User ID you want to get data</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>User data that has been requested</returns>
        /// <example>GET user/get-by-short-name</example>
        [HttpGet, Route("get-groups-per-community")]
        public List<string> GetGroupsPerCommunity(Guid user_id, string community_short_name)
        {
            if (string.IsNullOrEmpty(community_short_name) || user_id.Equals(Guid.Empty))
            {
                throw new GnossException("The parameters can't be null or empty", HttpStatusCode.BadRequest);
            }

            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            Guid proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(community_short_name);

            if (proyectoID.Equals(Guid.Empty))
            {
                throw new GnossException($"The community {community_short_name} does not belong to this platform", HttpStatusCode.BadRequest);
            }

            try
            {
                IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                return identCN.ObtenerGruposDeUsuarioEnProyecto(user_id, proyectoID);
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, $"GetGroupsPerCommunity: Parametros: user_id={user_id} community_short_name={community_short_name}");
                throw new GnossException($"There were an error trying to perform your request. Please, try again later", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get the data a user by user email and the community short name
        /// </summary>
        /// <param name="email">User email you want to get data</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>User data that has been requested</returns>
        /// <example>GET user/get-by-email</example>
        [Route("get-by-email")]
        [HttpGet]
        public User GetUsuarioPorEmail(string email, string community_short_name)
        {
            User jsonUsuario = ObtenerJsonUsuarioProyecto("", email, UsuarioOAuth, Guid.Empty, community_short_name, true);

            return jsonUsuario;
        }

        /// <summary>
        /// Validate the login and password of an user
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>Returns true if the data is correct</returns>
        /// <example>POST user/validate-password</example>
        [HttpPost, Route("validate-password")]
        public bool GetValidarUsuarioYContraseña(ParamsLoginPassword parameters)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                if (!string.IsNullOrEmpty(parameters.login) && !string.IsNullOrEmpty(parameters.password))
                {
                    return mControladorBase.ValidarUsuario(parameters.login, parameters.password) != null;
                }
                else
                {
                    throw new GnossException("The params can not be empty", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The oauth user does not have permission to validate password", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets the position of an organization profile in a community
        /// </summary>
        /// <param name="profile_id">Organization profile ID</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>Position of the organization profile in a community</returns>
        /// <example>GET user/get-profile-roll-in-organization</example>
        [Route("get-profile-role-in-organization")]
        [HttpGet]
        public string GetCargoPerfilEnOrganizacion(Guid profile_id, string community_short_name)
        {
            string cargo = string.Empty;

            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(community_short_name);

            if (proyectoID != Guid.Empty)
            {
                IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                Guid? identidadID = identidadCN.ObtenerIdentidadIDDePerfilEnProyecto(proyectoID, profile_id);

                if (identidadID.HasValue)
                {
                    GestionIdentidades gestorIdentidades = new GestionIdentidades(identidadCN.ObtenerIdentidadPorID(identidadID.Value, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID.Value];

                    if (identidad.TrabajaConOrganizacion)
                    {
                        OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        cargo = orgCN.ObtenerCargoPersonaVinculoOrganizacion(identidad.OrganizacionID.Value, identidad.PersonaID.Value);
                        orgCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The profile does not participate in the community", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The profile does not participate in the community", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
            }

            return cargo;
        }

        /// <summary>
        /// Create a user awaiting activation
        /// </summary>
        /// <param name="user">User data you want to create</param>
        /// <returns>User data</returns>
        /// <example>POST user/create-user-waiting-for-activate</example>
        [HttpPost, Route("create-user-waiting-for-activate")]
        public User CrearUsuarioSinActivar(User user)
        {
            if (user != null && !string.IsNullOrEmpty(user.name) && !string.IsNullOrEmpty(user.last_name) && !string.IsNullOrEmpty(user.community_short_name))
            {
                if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                {
                    ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid proyectoID = proyectoCN.ObtenerProyectoIDPorNombreCorto(user.community_short_name);
                    proyectoCN.Dispose();

                    if (proyectoID != Guid.Empty)
                    {
                        user.community_id = proyectoID;
                        int hashNumUsu = 1;
                        string loginUsuario = GenerarLoginUsuario(user.name, user.last_name, ref hashNumUsu);
                        //Antes de la migracion del V2 Incidencia LRE-145
                        //string nombreCortoUsuario = GenerarNombreCortoUsuario(loginUsuario, ref hashNumUsu);
                        DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
                        string nombreCortoUsuario = GenerarNombreCortoUsuario(ref loginUsuario, user.name, user.last_name, dataWrapperUsuario);

                        string password = DateTime.Now.ToString("MMddHHmmss") + "a";
                        if (!string.IsNullOrEmpty(user.password))
                        {
                            if (user.password.Length < 6 || user.password.Length > 12)
                            {
                                throw new GnossException("The user password must contain between 6 and 12 characters", HttpStatusCode.BadRequest);
                            }
                            password = user.password;
                        }

                        //Usuario
                        //UsuarioDS usuarioDS = new UsuarioDS();

                        ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        AD.EntityModel.Models.ProyectoDS.Proyecto filaProyecto = proyCN.ObtenerProyectoPorIDCargaLigera(user.community_id);
                        proyCN.Dispose();
                        GestionUsuarios gestorUsuarios = new GestionUsuarios(dataWrapperUsuario, mLoggingService, mEntityContext, mConfigService);
                        UsuarioGnoss usuario = gestorUsuarios.AgregarUsuario(loginUsuario, nombreCortoUsuario, password /*HashHelper.CalcularHash(password, true)*/, true);
                        user.user_id = usuario.Clave;
                        user.user_short_name = nombreCortoUsuario;
                        Usuario filaUsuario = usuario.FilaUsuario;
                        filaUsuario.EstaBloqueado = false;

                        DataWrapperSolicitud solicitudDW = new DataWrapperSolicitud();
                        ParametroGeneralCL paramCL = new ParametroGeneralCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                        GestorParametroGeneral gestorParametroGeneral = paramCL.ObtenerParametrosGeneralesDeProyecto(user.community_id);
                        paramCL.Dispose();
                        //ParametroGeneral parametrosGeneralesRow = gestorParametroGeneral.ListaParametroGeneral.FindByOrganizacionIDProyectoID(filaProyecto.OrganizacionID, filaProyecto.ProyectoID);
                        ParametroGeneral parametrosGeneralesRow = gestorParametroGeneral.ListaParametroGeneral.Find(parametroGen => parametroGen.OrganizacionID.Equals(filaProyecto.OrganizacionID) && parametroGen.ProyectoID.Equals(filaProyecto.ProyectoID));

                        Solicitud filaSolicitud = new Solicitud();
                        filaSolicitud.Estado = (short)EstadoSolicitud.Espera;
                        filaSolicitud.FechaSolicitud = DateTime.Now;
                        filaSolicitud.FechaProcesado = filaSolicitud.FechaSolicitud;
                        filaSolicitud.OrganizacionID = filaProyecto.OrganizacionID;
                        filaSolicitud.ProyectoID = user.community_id;
                        filaSolicitud.SolicitudID = Guid.NewGuid();
                        solicitudDW.ListaSolicitud.Add(filaSolicitud);
                        mEntityContext.Solicitud.Add(filaSolicitud);

                        SolicitudNuevoUsuario filaNuevoUsuario = new SolicitudNuevoUsuario();
                        filaNuevoUsuario.SolicitudID = filaSolicitud.SolicitudID;
                        filaNuevoUsuario.UsuarioID = filaUsuario.UsuarioID;
                        filaNuevoUsuario.NombreCorto = nombreCortoUsuario;
                        filaNuevoUsuario.Nombre = user.name;
                        filaNuevoUsuario.Apellidos = user.last_name;
                        filaNuevoUsuario.Email = user.email;
                        filaNuevoUsuario.EsBuscable = true;
                        filaNuevoUsuario.EsBuscableExterno = false;

                        if (parametrosGeneralesRow != null && !parametrosGeneralesRow.PrivacidadObligatoria)
                        {
                            filaNuevoUsuario.EsBuscable = false;
                            filaNuevoUsuario.EsBuscableExterno = false;
                        }
                        if (user.country_id != null && !user.country_id.Equals(Guid.Empty))
                        {
                            filaNuevoUsuario.PaisID = user.country_id;
                        }
                        if (user.province_id != null && !user.province_id.Equals(Guid.Empty))
                        {
                            filaNuevoUsuario.ProvinciaID = user.province_id;
                        }
                        if (user.provice == null)
                        {
                            user.provice = "";
                        }
                        filaNuevoUsuario.Provincia = user.provice;
                        filaNuevoUsuario.Poblacion = user.city;
                        filaNuevoUsuario.Sexo = user.sex;
                        filaNuevoUsuario.FaltanDatos = false;
                        filaNuevoUsuario.Idioma = "es";
                        filaNuevoUsuario.FechaNacimiento = user.born_date;

                        if (user.extra_data != null && user.extra_data.Count > 0)
                        {
                            filaNuevoUsuario.ClausulasAdicionales = ObtenerClausulasAdicionales(user.community_id, user.extra_data);

                            GuardarDatosExtraSolicitud(solicitudDW, filaSolicitud, user.extra_data, filaSolicitud.OrganizacionID, user.community_id, user.community_id.Equals(ProyectoAD.MyGnoss));
                        }

                        solicitudDW.ListaSolicitudNuevoUsuario.Add(filaNuevoUsuario);
                        mEntityContext.SolicitudNuevoUsuario.Add(filaNuevoUsuario);

                        mEntityContext.SaveChanges();

                        //enviar correo de invitación
                        GestionNotificaciones gestorNotificaciones = new GestionNotificaciones(new DataWrapperNotificacion(), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                        UtilIdiomas utilIdiomas = new UtilIdiomas("es", user.community_id, mLoggingService, mEntityContext, mConfigService);
                        string urlEnlace = mControladorBase.UrlsSemanticas.ObtenerURLComunidad(utilIdiomas, UrlIntragnoss, filaProyecto.NombreCorto);
                        urlEnlace += "/" + utilIdiomas.GetText("URLSEM", "REGISTROUSUARIO") + "/" + utilIdiomas.GetText("URLSEM", "PREACTIVACION") + "/" + filaNuevoUsuario.SolicitudID;
                        gestorNotificaciones.AgregarNotificacionRegistroParcialComunidad(filaNuevoUsuario.SolicitudID, user.name, TiposNotificacion.InvitacionRegistroParcialComunidad, user.email, null, UrlIntragnoss, "es", filaProyecto.Nombre, urlEnlace, user.community_id);

                        NotificacionCN notificacionCN = new NotificacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        notificacionCN.ActualizarNotificacion();
                        notificacionCN.Dispose();
                        gestorNotificaciones.Dispose();

                        return user;

                    }
                    else
                    {
                        throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
                }
            }
            else
            {
                throw new GnossException("The user data are not valid", HttpStatusCode.BadRequest);
            }
        }


        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="user">User data you want to create</param>
        /// <returns>User data</returns>
        /// <example>POST user/create-user</example>
        [HttpPost, Route("create-user")]
        public User CrearUsuario(User user)
        {
            if (user != null && !string.IsNullOrEmpty(user.name) && !string.IsNullOrEmpty(user.last_name) && !string.IsNullOrEmpty(user.email))
            {
                if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                {
                    if (!string.IsNullOrEmpty(user.password))
                    {
                        if (user.password.Length < 6 || user.password.Length > 12)
                        {
                            throw new GnossException("The user password must contain between 6 and 12 characters", HttpStatusCode.BadRequest);
                        }
                    }

                    Dictionary<string, Guid> dicUsuario = AltaUsuarioEnGnossYComunidades(user);
                    if (dicUsuario.Count > 0)
                    {
                        user.user_short_name = dicUsuario.Keys.First();
                        user.user_id = dicUsuario[user.user_short_name];
                    }

                    return user;
                }
                else
                {
                    throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
                }
            }
            else
            {
                throw new GnossException("The user data are not valid", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Gets the URL to recover the password of a user
        /// </summary>
        /// <param name="login">Login o email of the user</param>
        /// <param name="community_short_name">Community short name you want to get data</param>
        /// <returns>URL to recover the password of a user</returns>
        /// <example>GET user/generate-forgotten-password-url</example>
        [HttpGet, Route("generate-forgotten-password-url")]
        public string GenerarURLOlvidePassword(string login, string community_short_name)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                if (!login.Equals(string.Empty) && !community_short_name.Equals(string.Empty))
                {
                    UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    PeticionCN peticionCN = new PeticionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                    DataWrapperUsuario dataWrapperUsuario = usuarioCN.ObtenerUsuarioPorLoginOEmail(login, false);
                    Guid idProyecto = proyectoCN.ObtenerProyectoIDPorNombre(community_short_name);

                    if (dataWrapperUsuario.ListaUsuario.Count > 0 && !idProyecto.Equals(Guid.Empty))
                    {
                        AD.EntityModel.Models.UsuarioDS.Usuario filaUsuario = dataWrapperUsuario.ListaUsuario.First();
                        Guid idUsuario = filaUsuario.UsuarioID;
                        Guid idPersona = personaCN.ObtenerPersonaIDPorUsuarioID(idUsuario).Value;
                        string idiomaPersona = personaCN.ObtenerIdiomaDePersonaID(idPersona);
                        string urlPropiaProyecto = proyectoCN.ObtenerURLPropiaProyecto(idProyecto);
                        UtilIdiomas UtilIdiomasAux = new UtilIdiomas(idiomaPersona, mLoggingService, mEntityContext, mConfigService);

                        DataWrapperPeticion peticionDW = new DataWrapperPeticion();
                        AD.EntityModel.Models.Peticion.Peticion filaPeticion = new AD.EntityModel.Models.Peticion.Peticion();
                        filaPeticion.Estado = (short)EstadoPeticion.Pendiente;
                        filaPeticion.FechaPeticion = DateTime.Now;
                        filaPeticion.PeticionID = Guid.NewGuid();
                        filaPeticion.UsuarioID = filaUsuario.UsuarioID;
                        filaPeticion.Tipo = (int)TipoPeticion.CambioPassword;
                        peticionDW.ListaPeticion.Add(filaPeticion);
                        mEntityContext.Peticion.Add(filaPeticion);

                        peticionCN.ActualizarBD();

                        //Cojo el idioma del usuario de bbdd
                        string urlCambioPass = urlCambioPass = mControladorBase.UrlsSemanticas.ObtenerURLComunidad(UtilIdiomasAux, urlPropiaProyecto, community_short_name) + "/" + UtilIdiomasAux.GetText("URLSEM", "CAMBIARPASSWORD") + "/" + UtilIdiomasAux.GetText("URLSEM", "PETICION") + "/" + filaPeticion.PeticionID.ToString() + "/" + UtilIdiomasAux.GetText("URLSEM", "USUARIO") + "/" + filaUsuario.UsuarioID;

                        return urlCambioPass;
                    }
                    else
                    {
                        if (dataWrapperUsuario.ListaUsuario.Count == 0)
                        {
                            throw new GnossException("The user does not exists", HttpStatusCode.BadRequest);
                        }
                        else
                        {
                            throw new GnossException("The community does not exists", HttpStatusCode.BadRequest);
                        }
                    }
                }
                else
                {
                    throw new GnossException("The login and the community can not be empty", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Modify a user
        /// </summary>
        /// <param name="user">User data that we modify</param>
        /// <example>POST user/modify-user</example>
        [HttpPost, Route("modify-user")]
        public void ModificarUsuario(User user)
        {
            try
            {

                if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                {
                    if (user != null && user.user_id != null && user.user_id != Guid.Empty && user.community_id != null && user.community_id != Guid.Empty)
                    {
                        UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        if (usuarioCN.EstaUsuarioEnProyecto(user.user_id, user.community_id))
                        {
                            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                            Guid identidadID = identCN.ObtenerIdentidadUsuarioEnProyecto(user.user_id, user.community_id);

                            if (identidadID != Guid.Empty)
                            {
                                GestionIdentidades gestorIdentidades = new GestionIdentidades(identCN.ObtenerIdentidadPorID(identidadID, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);

                                if (gestorIdentidades.ListaIdentidades.ContainsKey(identidadID))
                                {
                                    Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID];
                                    gestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerDatosExtraProyectoOpcionIdentidadPorIdentidadID(identidadID));
                                    gestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerIdentidadesDePerfil(identidad.PerfilID));
                                    gestorIdentidades.RecargarHijos();
                                    identidad = gestorIdentidades.ListaIdentidades[identidadID];

                                    PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                    gestorIdentidades.GestorPersonas = new GestionPersonas(personaCN.ObtenerPersonaPorID(identidad.PersonaID.Value), mLoggingService, mEntityContext);
                                    personaCN.Dispose();
                                    gestorIdentidades.GestorPersonas.CargarGestor();

                                    //Empieza la edición
                                    RellenarDatosPersona(identidad, user, "es");

                                    Dictionary<int, string> dicDatosExtraProyectoVirtuoso = new Dictionary<int, string>();
                                    Dictionary<int, string> dicDatosExtraEcosistemaVirtuoso = new Dictionary<int, string>();
                                    GuardarDatosExtra(user.extra_data, identidad, dicDatosExtraProyectoVirtuoso, dicDatosExtraEcosistemaVirtuoso, user.community_id.Equals(ProyectoAD.MyGnoss));

                                    ParametroAplicacionCL paramCL = new ParametroAplicacionCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                                    GestorParametroAplicacion paramApliDS = new GestorParametroAplicacion();
                                    paramApliDS.ParametroAplicacion = paramCL.ObtenerParametrosAplicacionPorContext();
                                    paramCL.Dispose();



                                    if (paramApliDS.ListaAccionesExternas != null && paramApliDS.ListaAccionesExternas.Where(accionesExt => accionesExt.TipoAccion.Equals((short)TipoAccionExterna.Edicion)).ToList().Count == 1)
                                    {
                                        ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                        DataWrapperProyecto dataWrapperProyecto = proyectoCN.ObtenerDatosExtraProyectoPorID(user.community_id);
                                        proyectoCN.Dispose();

                                        if (user.community_id.Equals(ProyectoAD.MyGnoss))
                                        {
                                            ControladorIdentidades.AccionEnServicioExternoEcosistema(TipoAccionExterna.Edicion, user.community_id, user.user_id, user.name, user.last_name, user.email, user.password, paramApliDS, dataWrapperProyecto, dicDatosExtraEcosistemaVirtuoso, dicDatosExtraProyectoVirtuoso, user.aux_data);
                                        }
                                        else
                                        {
                                            ControladorIdentidades.AccionEnServicioExternoProyecto(TipoAccionExterna.Edicion, user.community_id, identidadID, user.user_id, user.name, user.last_name, user.email, user.password, user.aux_data, user.born_date, user.country_id, user.city, user.sex, user.join_community_date, dataWrapperProyecto, user.province_id, user.provice, user.postal_code);
                                        }
                                    }

                                    if (user.preferences != null)
                                    {
                                        EditarSuscripciones(user.preferences.Select(preference => preference.category_id).ToList(), identidad);
                                    }

                                    mEntityContext.SaveChanges();

                                    List<Guid> listaProyectos = new List<Guid>();
                                    listaProyectos.Add(user.community_id);
                                    ControladorPersonas contrPers = new ControladorPersonas(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                                    contrPers.ActualizarModeloBaseSimpleMultiple(identidad.Persona.Clave, listaProyectos);
                                    EliminarCaches(identidad);

                                }
                            }

                            identCN.Dispose();
                            usuarioCN.Dispose();
                        }
                        else
                        {
                            throw new GnossException("The user does not participate in the community", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        throw new GnossException("The user and the community can not be empty", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Delete a user from a community
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST user/delete-user-from-community</example>
        [HttpPost, Route("delete-user-from-community")]
        public void BorrarUsuarioDeComunidad(ParamsUserCommunity parameters)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                User jsonUsuario = ObtenerJsonUsuarioProyecto(parameters.user_short_name, "", UsuarioOAuth, Guid.Empty, parameters.community_short_name, false);

                if (jsonUsuario != null && jsonUsuario.user_id != null && jsonUsuario.community_id != null)
                {
                    IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid identidadID = identidadCN.ObtenerIdentidadUsuarioEnProyecto(jsonUsuario.user_id, jsonUsuario.community_id);
                    if (identidadID != Guid.Empty)
                    {
                        GestionIdentidades gestorIdentidades = new GestionIdentidades(identidadCN.ObtenerIdentidadPorID(identidadID, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                        if (gestorIdentidades.ListaIdentidades.ContainsKey(identidadID))
                        {
                            Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID];
                            gestorIdentidades.CargarGestor();
                            gestorIdentidades.GestorPersonas = new GestionPersonas(personaCN.ObtenerPersonaPorID(identidad.PersonaID.Value), mLoggingService, mEntityContext);
                            gestorIdentidades.GestorPersonas.CargarGestor();

                            identidad.FilaIdentidad.FechaBaja = DateTime.Now;
                            mEntityContext.SaveChanges();
                            ControladorPersonas contrPers = new ControladorPersonas(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                            contrPers.ActualizarEliminacionModeloBaseSimple(identidad.Persona.Clave, jsonUsuario.community_id, PrioridadBase.ApiRecursos);
                            EliminarCaches(identidad);
                        }
                    }
                    identidadCN.Dispose();
                    personaCN.Dispose();
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="user_id">User ID to delete</param>
        /// <example>POST user/delete-user</example>
        [HttpPost, Route("delete-user")]
        public void BorrarUsuario([FromBody] Guid user_id)
        {
            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                if (usuarioCN.EstaUsuarioEnProyecto(user_id, ProyectoAD.MetaProyecto))
                {
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    string nombreCortoProy = proyCN.ObtenerNombreCortoProyecto(ProyectoAD.MetaProyecto);
                    proyCN.Dispose();

                    User jsonUsuarioEliminar = ObtenerJsonUsuarioProyecto("", "", UsuarioOAuth, user_id, nombreCortoProy, false);

                    if (jsonUsuarioEliminar != null && jsonUsuarioEliminar.user_id != null && jsonUsuarioEliminar.community_id != null)
                    {
                        IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        Guid identidadID = identidadCN.ObtenerIdentidadUsuarioEnProyecto(jsonUsuarioEliminar.user_id, jsonUsuarioEliminar.community_id);
                        identidadCN.Dispose();
                        if (identidadID != Guid.Empty)
                        {
                            EliminarUsuario(identidadID);
                        }
                    }
                }
                else
                {
                    throw new GnossException("The user does not exist", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Add a user to an organization
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST add-user-to-organization</example>
        [HttpPost, Route("add-user-to-organization")]
        public void AltaUsuarioEnOrganizacion(ParamsAddUserOrg parameters)
        {
            if (!string.IsNullOrEmpty(parameters.organization_short_name) && !string.IsNullOrEmpty(parameters.position))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                if (usuCN.ExisteUsuarioEnBD(parameters.user_id))
                {
                    OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    if (orgCN.ExisteNombreCortoEnBD(parameters.organization_short_name))
                    {
                        if (EsAdministradorProyectoMyGnoss(UsuarioOAuth) || EsAdministradorOrganizacion(UsuarioOAuth, parameters.organization_short_name))
                        {
                            PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                            Guid personaID = persCN.ObtenerPersonaIDPorUsuarioID(parameters.user_id).Value;
                            string emailPersona = persCN.ObtenerEmailPersonalPorUsuario(parameters.user_id);

                            Guid organizacionID = orgCN.ObtenerOrganizacionesIDPorNombre(parameters.organization_short_name);
                            List<Guid> listaProyectosID = new List<Guid>();
                            ValidarNombresProyectosYOrganizacion(organizacionID, parameters.communities_short_names, out listaProyectosID);

                            //Necesitamos cargar la tabla: OrganizacionParticipaProy para registrar a la org en esas comunidades...
                            GestionOrganizaciones gestOrg = new GestionOrganizaciones(orgCN.ObtenerOrganizacionPorID(organizacionID), mLoggingService, mEntityContext);
                            Organizacion organizacion = gestOrg.ListaOrganizaciones[organizacionID];

                            DatosTrabajoPersonaOrganizacion perfilPersonaOrganizacion = gestOrg.VincularPersonaOrganizacion(organizacion, personaID);
                            AD.EntityModel.Models.OrganizacionDS.PersonaVinculoOrganizacion filaOrgPersona = perfilPersonaOrganizacion.FilaVinculo;
                            filaOrgPersona.EmailTrabajo = emailPersona;
                            filaOrgPersona.Cargo = parameters.position;

                            GestionIdentidades gestorIdentidades = new GestionIdentidades(new DataWrapperIdentidad(), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                            gestorIdentidades.GestorOrganizaciones = new GestionOrganizaciones(gestOrg.OrganizacionDW, mLoggingService, mEntityContext);
                            gestorIdentidades.GestorOrganizaciones.CargarOrganizaciones();
                            gestorIdentidades.GestorPersonas = new GestionPersonas(persCN.ObtenerPersonaPorID(personaID), mLoggingService, mEntityContext);
                            gestorIdentidades.GestorPersonas.CargarGestor();
                            Persona persona = gestorIdentidades.GestorPersonas.ListaPersonas[personaID];

                            //Creamos el perfil persona + organización o lo retomamos si ya existía
                            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                            DataWrapperIdentidad identDW = identCN.ObtenerPerfilesDePersona(personaID, false);

                            Perfil perfil = null;
                            Guid perfilID = Guid.Empty;

                            if (!persona.UsuarioCargado)
                            {
                                if (persona.GestorPersonas.GestorUsuarios == null)
                                {
                                    persona.GestorPersonas.GestorUsuarios = new GestionUsuarios(new DataWrapperUsuario(), mLoggingService, mEntityContext, mConfigService);
                                }

                                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
                                dataWrapperUsuario.ListaUsuario.Add(usuarioCN.ObtenerUsuarioPorID(persona.UsuarioID));

                                if (dataWrapperUsuario.ListaUsuario.Count.Equals(1))
                                {
                                    persona.GestorPersonas.GestorUsuarios.DataWrapperUsuario.Merge(dataWrapperUsuario);
                                    persona.GestorPersonas.GestorUsuarios.RecargarUsuarios();
                                }
                                usuarioCN.Dispose();
                            }

                            DataWrapperUsuario usuDS = gestorIdentidades.GestorPersonas.GestorUsuarios.DataWrapperUsuario;
                            GestionUsuarios gestorUsuario = new GestionUsuarios(usuDS, mLoggingService, mEntityContext, mConfigService);
                            gestorIdentidades.GestorUsuarios = gestorUsuario;
                            gestorIdentidades.GestorPersonas.GestorUsuarios = gestorUsuario;

                            AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg filaPerfilPersonaOrg = null;
                            List<string> listaContactosOrganizacion = new List<string>();
                            string identidadReal = "";

                            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                            Dictionary<Guid, bool> recibirNewsletterDefectoProyectos = proyCN.ObtenerProyectosConConfiguracionNewsletterPorDefecto();
                            proyCN.Dispose();

                            AD.EntityModel.Models.UsuarioDS.Usuario filaUsuario = gestorUsuario.DataWrapperUsuario.ListaUsuario.Find(usuario => usuario.UsuarioID.Equals(parameters.user_id));
                            if (identDW.ListaPerfilPersonaOrg.Count(perfilOrg => perfilOrg.OrganizacionID.Equals(organizacionID) && perfilOrg.PersonaID.Equals(personaID)) == 0)
                            {
                                //Lo creamos nuevo
                                LiveCN liveCN = new LiveCN("base", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                LiveDS liveDS = new LiveDS();

                                gestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerIdentidadesDeOrganizacion(organizacionID, ProyectoAD.MetaProyecto));

                                perfil = ControladorIdentidades.AgregarPerfilPersonaOrganizacion(gestorIdentidades, gestorIdentidades.GestorOrganizaciones, gestorIdentidades.GestorUsuarios, persona, organizacion, true, ProyectoAD.MetaOrganizacion, ProyectoAD.MetaProyecto, liveDS, recibirNewsletterDefectoProyectos);

                                liveCN.ActualizarBD(liveDS);
                                liveCN.Dispose();

                                filaPerfilPersonaOrg = gestorIdentidades.DataWrapperIdentidad.ListaPerfilPersonaOrg.FirstOrDefault(perfilPersOrg => perfilPersOrg.OrganizacionID.Equals(organizacionID) && perfilPersOrg.PersonaID.Equals(personaID));

                                gestorUsuario.AgregarUsuarioAProyecto(filaUsuario, ProyectoAD.MetaOrganizacion, ProyectoAD.MetaProyecto, perfil.IdentidadMyGNOSS.Clave, false);

                                foreach (Guid proyectoID in listaProyectosID)
                                {
                                    if (!proyectoID.Equals(ProyectoAD.MetaProyecto) && gestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Count(ident => ident.ProyectoID.Equals(proyectoID) && ident.Tipo != 3) == 0)
                                    {
                                        //Hay que añadir al usuario al proyecto pProyectoID
                                        ControladorIdentidades.AgregarIdentidadPerfilYUsuarioAProyecto(gestorIdentidades, gestorUsuario, ProyectoAD.MetaOrganizacion, proyectoID, filaUsuario, perfil, recibirNewsletterDefectoProyectos);
                                    }
                                }

                                perfilID = filaPerfilPersonaOrg.PerfilID;
                                gestorIdentidades.RecargarHijos();

                                //Agregamos al modelo base las comunidades en las que se ha hecho miembro el usuario:
                                foreach (LiveDS.ColaRow colaLiveRow in liveDS.Cola)
                                {//"ProyectoID='" + colaLiveRow.ProyectoId + "' AND PerfilID='" + perfil.Clave + "'"
                                    List<AD.EntityModel.Models.IdentidadDS.Identidad> filasIdentidad = perfil.GestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Where(ident => ident.ProyectoID.Equals(colaLiveRow.ProyectoId) && ident.PerfilID.Equals(perfil.Clave)).ToList();

                                    if (filasIdentidad.Count > 0)
                                    {
                                        ControladorPersonas controladorPersonas = new ControladorPersonas(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                                        controladorPersonas.ActualizarModeloBASE(perfil.GestorIdentidades.ListaIdentidades[filasIdentidad.First().IdentidadID], colaLiveRow.ProyectoId, true, true, PrioridadBase.Alta);
                                    }
                                }

                                //Agregamos como contactos a las personas de la organización y a la propia organización
                                ControladorAmigos controlador = new ControladorAmigos(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                                listaContactosOrganizacion = controlador.AgregarContactosOrganizacion(organizacionID, perfil.IdentidadMyGNOSS.Clave, gestorIdentidades);
                                identidadReal = perfil.IdentidadMyGNOSS.Clave.ToString();
                            }
                            else
                            {
                                //Lo tenemos que retomar
                                filaPerfilPersonaOrg = identDW.ListaPerfilPersonaOrg.FirstOrDefault(perfilPersOrg => perfilPersOrg.OrganizacionID.Equals(organizacionID) && perfilPersOrg.PersonaID.Equals(personaID));
                                AD.EntityModel.Models.IdentidadDS.Perfil filaPerfil = identDW.ListaPerfil.Find(perf => perf.PerfilID.Equals(filaPerfilPersonaOrg.PerfilID));
                                AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad = identDW.ListaIdentidad.FirstOrDefault(ident => ident.PerfilID.Equals(filaPerfil.PerfilID) && ident.ProyectoID.Equals(ProyectoAD.MetaProyecto));

                                if (filaPerfil.Eliminado)
                                {
                                    gestorIdentidades.DataWrapperIdentidad.ListaPerfil.Add(filaPerfil);
                                    gestorIdentidades.DataWrapperIdentidad.ListaPerfilPersonaOrg.Add(filaPerfilPersonaOrg);
                                    gestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Add(filaIdentidad);
                                    gestorIdentidades.RecargarHijos();

                                    //Agregamos como contactos a las personas de la organización y a la propia organización
                                    ControladorAmigos controlador = new ControladorAmigos(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
                                    listaContactosOrganizacion = controlador.AgregarContactosOrganizacion(organizacionID, filaIdentidad.IdentidadID, gestorIdentidades);
                                    identidadReal = filaIdentidad.IdentidadID.ToString();
                                    //Retomamos el perfil y la identidad en el metaproyecto
                                    ControladorIdentidades.RetomarPerfil(filaPerfilPersonaOrg.PerfilID, gestorIdentidades, gestorUsuario, gestorIdentidades.GestorOrganizaciones, true, ProyectoAD.MetaProyecto, recibirNewsletterDefectoProyectos, parameters.user_id);

                                    //Agregamos ProyectoUsuarioIdentidad en el metaproyecto
                                    gestorUsuario.AgregarUsuarioAProyecto(gestorUsuario.ListaUsuarios[parameters.user_id].FilaUsuario, ProyectoAD.MetaOrganizacion, ProyectoAD.MetaProyecto, filaIdentidad.IdentidadID, false);

                                    //si el perfil existía hay que retomarlo, pero si en la lista de proyectos hay proyectos nuevos en los que no participaba el usuario cuando abandonó la organización, hay que agregarlo a dichos proyectos
                                    foreach (Guid proyectoID in listaProyectosID)
                                    {//"ProyectoID = '" + proyectoID + "' AND Tipo <> 3"
                                        if (!proyectoID.Equals(ProyectoAD.MetaProyecto) && gestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Count(ident => ident.ProyectoID.Equals(proyectoID) && ident.Tipo != 3) == 0)
                                        {
                                            //Hay que añadir al usuario al proyecto
                                            ControladorIdentidades.AgregarIdentidadPerfilYUsuarioAProyecto(gestorIdentidades, gestorUsuario, ProyectoAD.MetaOrganizacion, proyectoID, filaUsuario, perfil, recibirNewsletterDefectoProyectos);
                                        }
                                    }

                                    perfilID = filaIdentidad.PerfilID;
                                    gestorIdentidades.RecargarHijos();
                                }
                            }

                            gestorUsuario.AgregarOrganizacionRolUsuario(parameters.user_id, organizacionID);

                            mEntityContext.SaveChanges();

                            //Agregamos a Virtuoso e Insertamos una fila en BASE
                            ControladorContactos contrContactos = new ControladorContactos(mLoggingService, mEntityContext, mConfigService, mEntityContextBASE, mRedisCacheWrapper, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, "contactos/", mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                            if (listaContactosOrganizacion.Count > 0)
                            {
                                foreach (string idAmigo in listaContactosOrganizacion)
                                {
                                    facetadoCN.InsertarNuevoContacto(identidadReal, idAmigo);
                                    contrContactos.ActualizarModeloBaseSimple(new Guid(identidadReal), new Guid(idAmigo));
                                }
                            }

                            facetadoCN.Dispose();
                            identCN.Dispose();

                            //Invalido la cache de Mis comunidades
                            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                            proyCL.InvalidarMisProyectos(perfilID);
                            proyCL.Dispose();

                            //Borro la caché para que aparezca la identidad en el menú superior:
                            IdentidadCL identCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                            identCL.EliminarCacheGestorTodasIdentidadesUsuario(parameters.user_id, personaID);
                            identCL.InvalidarCacheMiembrosOrganizacionParaFiltroGrupos(organizacionID);
                            identCL.Dispose();

                            UsuarioCL usuarioCL = new UsuarioCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                            usuarioCL.EliminarCacheUsuariosCargaLigeraParaFiltros(organizacionID);
                            usuarioCL.Dispose();
                            persCN.Dispose();

                        }
                        else
                        {
                            throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
                        }
                    }
                    else
                    {
                        throw new GnossException("The organization does not exist", HttpStatusCode.BadRequest);
                    }

                    orgCN.Dispose();
                }
                else
                {
                    throw new GnossException("The user does not exist", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The parameters organization_short_name and position cannot be null or empty", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Add a user to organization groups
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <example>POST add-user-to-organization-group</example>
        [HttpPost, Route("add-user-to-organization-group")]
        public void AltaUsuarioGrupoOrganizacion(ParamsAddUserOrgGroups parameters)
        {
            if (!string.IsNullOrEmpty(parameters.organization_short_name))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                if (usuCN.ExisteUsuarioEnBD(parameters.user_id))
                {
                    OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    if (orgCN.ExisteNombreCortoEnBD(parameters.organization_short_name))
                    {
                        if (EsAdministradorProyectoMyGnoss(UsuarioOAuth) || EsAdministradorOrganizacion(UsuarioOAuth, parameters.organization_short_name))
                        {
                            Guid organizacionID = orgCN.ObtenerOrganizacionesIDPorNombre(parameters.organization_short_name);
                            DataWrapperOrganizacion orgDW = orgCN.ObtenerOrganizacionesParticipaUsuario(parameters.user_id);

                            if (orgDW.ListaOrganizacion.Where(item => item.OrganizacionID.Equals(organizacionID)).FirstOrDefault() != null)
                            {
                                Dictionary<string, Guid> dicGrupos = new Dictionary<string, Guid>();
                                ValidacionNombresGruposYOrganizacion(organizacionID, parameters.groups_short_names, dicGrupos);

                                PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                Guid personaID = persCN.ObtenerPersonaIDPorUsuarioID(parameters.user_id).Value;
                                GestionIdentidades gestorIdentidades = new GestionIdentidades(identCN.ObtenerPerfilesDeUsuario(parameters.user_id), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                                List<AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg> filasPerfilPersonaOrg = gestorIdentidades.DataWrapperIdentidad.ListaPerfilPersonaOrg.Where(item => item.OrganizacionID.Equals(organizacionID) && item.PersonaID.Equals(personaID)).ToList();

                                if (filasPerfilPersonaOrg != null && filasPerfilPersonaOrg.Count > 0)
                                {
                                    AD.EntityModel.Models.IdentidadDS.PerfilPersonaOrg filaPerfilPersonaOrg = filasPerfilPersonaOrg[0];
                                    List<Guid> listaPerfiles = new List<Guid>();
                                    listaPerfiles.Add(filaPerfilPersonaOrg.PerfilID);
                                    List<Guid> listaIdentidades = identCN.ObtenerIdentidadesIDDePerfilEnProyecto(ProyectoAD.MyGnoss, listaPerfiles);

                                    if (listaIdentidades.Count > 0)
                                    {//"PerfilID = '" + filaPerfilPersonaOrg.PerfilID + "' AND ProyectoID = '" + ProyectoAD.MetaProyecto + "'"
                                        gestorIdentidades.GestorOrganizaciones = new GestionOrganizaciones(orgDW, mLoggingService, mEntityContext);
                                        gestorIdentidades.GestorPersonas = new GestionPersonas(persCN.ObtenerPersonaPorPerfil(filaPerfilPersonaOrg.PerfilID), mLoggingService, mEntityContext);
                                        AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad = (gestorIdentidades.DataWrapperIdentidad.ListaIdentidad.FirstOrDefault(ident => ident.PerfilID.Equals(filaPerfilPersonaOrg.PerfilID) && ident.ProyectoID.Equals(ProyectoAD.MetaProyecto)));
                                        Identidad identidad = new Identidad(filaIdentidad, gestorIdentidades.ListaPerfiles[filaPerfilPersonaOrg.PerfilID], mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);

                                        foreach (string nombreGrupo in dicGrupos.Keys)
                                        {
                                            gestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerGrupoPorNombreCortoYOrganizacion(nombreGrupo, organizacionID, false));
                                            gestorIdentidades.CargarGestor();
                                            Guid grupoID = dicGrupos[nombreGrupo];

                                            if (gestorIdentidades.ListaGrupos.ContainsKey(grupoID))
                                            {
                                                GrupoIdentidades grupoIdentidades = gestorIdentidades.ListaGrupos[grupoID];
                                                AgregarParticipantesGrupoOrganizacion(organizacionID, listaIdentidades, grupoIdentidades);
                                                AgregarParticipanteComunidadesParticipaGrupo(grupoID, parameters.user_id, gestorIdentidades, identidad, parameters.organization_short_name);
                                            }
                                        }
                                    }
                                }

                                persCN.Dispose();
                            }
                            else
                            {
                                throw new GnossException("El usuario no participa en la organizacion", HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("No tienes permisos para dar de alta al usuario en la organización", HttpStatusCode.Unauthorized);
                        }
                    }
                    else
                    {
                        throw new GnossException("La organizacion no existe", HttpStatusCode.BadRequest);
                    }

                    orgCN.Dispose();
                    usuCN.Dispose();
                }
                else
                {
                    throw new GnossException("El usuario no existe", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("El nombrecortoorg y el cargo no pueden ser vacios", HttpStatusCode.BadRequest);
            }
        }


        /// <summary>
        /// Blocks a user at the platform
        /// </summary>
        /// <param name="user_id">User's identificator</param>
        /// <example>POST community/block-member</example>
        [HttpPost, Route("block")]
        public void BloquearUsuario(Guid user_id)
        {
            BloquearDesbloquearUsuario(user_id, true);
        }

        /// <summary>
        /// Blocks a user at the platform
        /// </summary>
        /// <param name="user_id">User's identificator</param>
        /// <example>POST community/block-member</example>
        [HttpPost, Route("unblock")]
        public void DesbloquearUsuario(Guid user_id)
        {
            BloquearDesbloquearUsuario(user_id, false);
        }

        /// <summary>
        /// Blocks a user at the platform
        /// </summary>
        /// <param name="user_id">User's identificator</param>
        /// <param name="blocked">True if the user has been blocked, false if the user has been unblocked</param>
        private void BloquearDesbloquearUsuario(Guid user_id, bool blocked)
        {
            try
            {
                if (!user_id.Equals(Guid.Empty))
                {
                    //es administrador quien realiza la petición
                    if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                    {
                        UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
                        dataWrapperUsuario.ListaUsuario.Add(usuarioCN.ObtenerUsuarioPorID(user_id));

                        AD.EntityModel.Models.UsuarioDS.Usuario filaUsuario = dataWrapperUsuario?.ListaUsuario.Find(usuario => usuario.UsuarioID.Equals(user_id));

                        if (filaUsuario != null)
                        {
                            filaUsuario.EstaBloqueado = blocked;
                            usuarioCN.GuardarActualizaciones(dataWrapperUsuario);
                        }
                        else
                        {
                            throw new GnossException("The user doesn't exists", HttpStatusCode.BadRequest);
                        }

                        usuarioCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The oauth user does not have permission in the community", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw new GnossException("The user ID can not be empty", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw new GnossException("Unexpected error. Try it again later. ", HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Nuevos Métodos Api V3


        /// <summary>
        /// Get the data a user by user id
        /// </summary>
        /// <param name="listaIds">List Ids Users</param>
        /// <returns>User data that has been requested</returns>
        /// <example>GET user/get-users-by-id</example>
        [HttpPost, Route("get-users-by-id")]
        public Dictionary<Guid, Userlite> GetUsuariosPorIds(List<Guid> listaIds)
        {
            //Usuario, Nombre, apellidos, IDUsuario, NombreCorto, Numero de Recursos, Numero de Comentarios, Grupos a los que pertenece en la comunidad
            Dictionary<Guid, Userlite> lista = new Dictionary<Guid, Userlite>();
            lista = ObtenerUsuariosLitePorID(UsuarioOAuth, listaIds);
            return lista;
        }

        private Dictionary<Guid, Userlite> ObtenerUsuariosLitePorID(Guid pUsuarioIDOauth, List<Guid> listaIds)
        {
            Dictionary<Guid, Userlite> devolver = new Dictionary<Guid, Userlite>();
            //como esta función se usa en varios métodos, en la de eliminación la petición Oauth ya se ha realizado y ya tenemos el usuario
            if (pUsuarioIDOauth.Equals(Guid.Empty))
            {
                pUsuarioIDOauth = ComprobarPermisosOauth(mHttpContextAccessor.HttpContext.Request);
                if (UsuarioOAuth.Equals(Guid.Empty))
                {
                    throw new GnossException("Invalid OAuth signature", HttpStatusCode.Unauthorized);
                }
            }

            if (EsAdministradorProyectoMyGnoss(pUsuarioIDOauth))
            {
                foreach (Guid userID in listaIds)
                {
                    Userlite userDevolver = new Userlite();
                    userDevolver = mEntityContext.Usuario.JoinPersona().JoinPerfil().JoinIdentidad().Where(item => item.Usuario.UsuarioID.Equals(userID)).ToList().Select(item =>
                    new Userlite
                    {
                        name = item.Persona.Nombre,
                        last_name = item.Persona.Apellidos,
                        user_short_name = item.Usuario.NombreCorto,
                        image = item.Identidad.Foto,
                        born_date = item.Persona.FechaNacimiento
                    }).FirstOrDefault();
                    if (userDevolver != null)
                    {
                        devolver.Add(userID, userDevolver);
                    }
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }

            return devolver;
        }


        /// <summary>
        /// Get a list of users identifiers from a specific community whose information have been modified from the date provided
        /// </summary>
        /// <param name="search_date">String of date with ISO 8601 format from which the search will filter to get the results</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="community_id">Community identifier</param>
        /// <returns>List of the identifiers of modified users</returns>
        /// <example>GET resource/get-modified-users?community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-modified-users")]
        public List<Guid> GetModifiedUsersFromDate(string search_date, string community_short_name = null, Guid? community_id = null)
        {
            List<Guid> listaIDs = null;
            DateTime fechaBusqueda = DateTime.MinValue;
            bool esFecha = DateTime.TryParse(search_date, out fechaBusqueda);

            if (string.IsNullOrEmpty(community_short_name) && community_id.HasValue)
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                community_short_name = proyCL.ObtenerNombreCortoProyecto(community_id.Value);
            }

            if (!string.IsNullOrEmpty(community_short_name) && esFecha && !fechaBusqueda.Equals(DateTime.MinValue) && !fechaBusqueda.Equals(DateTime.MaxValue))
            {
                if (!ComprobarFechaISO8601(search_date))
                {
                    throw new GnossException("The parameter search_date has not the ISO8601 format", HttpStatusCode.BadRequest);
                }

                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
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
                    //buscar novedades en los usuarios: suscripciones a tesauro comunidad, a otros usuarios, altas en la comunidad...
                    UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    listaIDs = usuCN.ObtenerUsuariosActivosEnFecha(proyectoID, fechaBusqueda);
                    usuCN.Dispose();
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

            return listaIDs;
        }

        /// <summary>
        /// Get the novelties from a user by its user identifier. The novelties of the user can also be obtained providing either a community identifier or a community short name.
        /// </summary>
        /// <param name="user_id"">User identifier</param>
        /// <param name="search_date">String of date with ISO 8601 format from which the search will filter to get the results</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="community_id">Community identifier</param>
        /// <example>GET resource/get-user-novelties?user_id={user_id}&community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpGet, Route("get-user-novelties")]
        public UserNoveltiesModel GetUserNoveltiesFromDate(Guid user_id, string search_date, string community_short_name = null, Guid? community_id = null)
        {
            UserNoveltiesModel novedadesUsuario = null;
            DateTime fechaBusqueda = DateTime.MinValue;
            bool esFecha = DateTime.TryParse(search_date, out fechaBusqueda);

            if (string.IsNullOrEmpty(community_short_name) && community_id.HasValue)
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                community_short_name = proyCL.ObtenerNombreCortoProyecto(community_id.Value);
            }

            if (!user_id.Equals(Guid.Empty) && !string.IsNullOrEmpty(community_short_name) && esFecha && !fechaBusqueda.Equals(DateTime.MinValue) && !fechaBusqueda.Equals(DateTime.MaxValue))
            {
                if (!ComprobarFechaISO8601(search_date))
                {
                    throw new GnossException("The parameter search_date has not the ISO8601 format", HttpStatusCode.BadRequest);
                }

                mNombreCortoComunidad = community_short_name;
                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                if (usuarioCN.EstaUsuarioEnProyecto(user_id, FilaProy.ProyectoID))
                {
                    IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    Guid identidadIDMyGnoss = identCN.ObtenerIdentidadUsuarioEnProyecto(user_id, ProyectoAD.MetaProyecto);
                    Guid identidadID = identCN.ObtenerIdentidadUsuarioEnProyecto(user_id, FilaProy.ProyectoID);
                    if (identidadID.Equals(Guid.Empty))
                    {
                        throw new GnossException($"The user does not participate in the community {community_short_name}", HttpStatusCode.BadRequest);
                    }

                    SuscripcionCN suscCN = new SuscripcionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    DataWrapperSuscripcion suscDW = suscCN.ObtenerSuscripcionesDeIdentidad(identidadID, true);
                    //obtengo las suscripciones a usuarios con la identidad de MyGnoss porque el usuario está suscrito a otros usuarios con su identidad de MyGnoss
                    suscDW.Merge(suscCN.ObtenerSuscripcionesDeIdentidad(identidadIDMyGnoss, true));
                    GestionSuscripcion gestorSuscripciones = new GestionSuscripcion(suscDW, mLoggingService, mEntityContext);
                    suscCN.Dispose();
                    //Suscripcion suscripcion = gestorSuscripciones.ObtenerSuscripcionAProyecto(FilaProy.ProyectoID);

                    TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                    GestionTesauro gestorTesauro = new GestionTesauro(tesauroCL.ObtenerTesauroDeProyecto(FilaProy.ProyectoID), mLoggingService, mEntityContext);
                    gestorTesauro.CargarCategorias();
                    tesauroCL.Dispose();

                    novedadesUsuario = new UserNoveltiesModel();
                    novedadesUsuario.user_id = user_id;

                    //miembro comunidad
                    GestionIdentidades gestorIdentidades = new GestionIdentidades(identCN.ObtenerIdentidadPorID(identidadID, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                    if (gestorIdentidades.ListaIdentidades.ContainsKey(identidadID))
                    {
                        Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID];
                        novedadesUsuario.user_community_membership = new UserCommunityMembership();
                        novedadesUsuario.user_community_membership.community_short_name = community_short_name;
                        novedadesUsuario.user_community_membership.user_id = user_id;
                        novedadesUsuario.user_community_membership.registration_date = identidad.FilaIdentidad.FechaAlta;
                        novedadesUsuario.user_community_membership.administrator_rol = false;
                    }

                    foreach (Guid suscripcionID in gestorSuscripciones.ListaSuscripciones.Keys)
                    {
                        DateTime fechaSuscripcion = gestorSuscripciones.ListaSuscripciones[suscripcionID].Fecha;

                        if (fechaSuscripcion >= fechaBusqueda)
                        {
                            //Suscripciones tesauro comunidad
                            if (gestorSuscripciones.ListaSuscripciones[suscripcionID].FilasCategoriasVinculadas != null)
                            {
                                novedadesUsuario.community_subscriptions = new CommunitySubscriptionModel();
                                novedadesUsuario.community_subscriptions.user_id = user_id;
                                novedadesUsuario.community_subscriptions.community_short_name = community_short_name;
                                novedadesUsuario.community_subscriptions.category_list = new List<ThesaurusCategory>();

                                foreach (AD.EntityModel.Models.Suscripcion.CategoriaTesVinSuscrip filaCat in gestorSuscripciones.ListaSuscripciones[suscripcionID].FilasCategoriasVinculadas)
                                {
                                    string nomCat = gestorTesauro.ListaCategoriasTesauro[filaCat.CategoriaTesauroID].Nombre["es"];
                                    novedadesUsuario.community_subscriptions.category_list.Add(ObtenerCategoriasJerarquicas(filaCat.CategoriaTesauroID, gestorTesauro.ListaCategoriasTesauro));
                                }
                            }

                            //suscripciones usuario
                            if (gestorSuscripciones.ListaSuscripciones[suscripcionID].FilaSuscripcionIdentidadProyecto != null)
                            {
                                if (novedadesUsuario.user_subscriptions == null)
                                {
                                    novedadesUsuario.user_subscriptions = new List<UserSubscriptionModel>();
                                }

                                foreach (AD.EntityModel.Models.Suscripcion.SuscripcionIdentidadProyecto filaSusIdentProy in gestorSuscripciones.ListaSuscripciones[suscripcionID].FilaSuscripcionIdentidadProyecto)
                                {
                                    UserSubscriptionModel suscripcionUsuario = new UserSubscriptionModel();
                                    suscripcionUsuario.user_id = user_id;
                                    suscripcionUsuario.community_short_name = community_short_name;
                                    suscripcionUsuario.user_followed_id = filaSusIdentProy.IdentidadID;
                                    suscripcionUsuario.subscription_date = fechaSuscripcion;
                                    novedadesUsuario.user_subscriptions.Add(suscripcionUsuario);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new GnossException($"The user does not exist in the community {community_short_name}", HttpStatusCode.BadRequest);
                }

                usuarioCN.Dispose();
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }

            return novedadesUsuario;
        }

        /// <summary>
        /// Add a user's social network login based on their social network type data and user identifier. 
        /// </summary>
        /// <param name="user_id">User identifier</param>
        /// <param name="social_network_user_id">Social network user identifier</param>
        /// <param name="social_network">Social network name</param>
        /// <example>GET resource/get-user-novelties?user_id={user_id}&community_short_name={community_short_name}&search_date={ISO8601 search_date}</example>
        [HttpPost, Route("add-social-network-login")]
        public void AddSocialNetworkLogin(Guid user_id, string social_network_user_id, string social_network)
        {
            if (string.IsNullOrEmpty(social_network_user_id))
            {
                throw new GnossException("The parameter \"social_network_user_id\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                if (usuCN.ExisteUsuarioEnBD(user_id))
                {
                    TipoRedSocialLogin tipoRedSocial;
                    if (!Enum.TryParse(social_network, true, out tipoRedSocial))
                    {
                        tipoRedSocial = TipoRedSocialLogin.Otros;
                    }

                    DataWrapperUsuario usuDW = new DataWrapperUsuario();

                    UsuarioVinculadoLoginRedesSociales filaLoginRedSocial = new UsuarioVinculadoLoginRedesSociales();

                    filaLoginRedSocial.UsuarioID = user_id;
                    filaLoginRedSocial.IDenRedSocial = social_network_user_id;
                    filaLoginRedSocial.TipoRedSocial = (short)tipoRedSocial;

                    usuDW.ListaUsuarioVinculadoLoginRedesSociales.Add(filaLoginRedSocial);
                    mEntityContext.UsuarioVinculadoLoginRedesSociales.Add(filaLoginRedSocial);

                    usuCN.ActualizarUsuario(false);
                }
                else
                {
                    throw new GnossException($"The user {user_id} does not belong to the database", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Modifies the social network user identifier
        /// </summary>
        /// <param name="user_id">User identifier</param>
        /// <param name="social_network_user_id">User identifier in the social network</param>
        /// <param name="social_network">Name of the social network</param>
        /// <example>POST resource/modify-social-network-login?user_id={user_id}social_network_user_id={social_network_user_id}social_network={social_network}</example>
        [HttpPost, Route("modify-social-network-login")]
        public void ModifySocialNetworkLogin(Guid user_id, string social_network_user_id, string social_network)
        {
            if (string.IsNullOrEmpty(social_network_user_id))
            {
                throw new GnossException("The parameter \"social_network_user_id\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                if (usuCN.ExisteUsuarioEnBD(user_id))
                {
                    TipoRedSocialLogin tipoRedSocial;
                    if (!Enum.TryParse(social_network, true, out tipoRedSocial))
                    {
                        tipoRedSocial = TipoRedSocialLogin.Otros;
                    }

                    string socialNetworkLogin = usuCN.ObtenerLoginEnRedSocialPorUsuarioId(tipoRedSocial, user_id);
                    if (string.IsNullOrEmpty(socialNetworkLogin))
                    {
                        throw new GnossException($"Could not find any social network login with user {user_id} at {social_network}", HttpStatusCode.BadRequest);
                    }

                    usuCN.ActualizarLoginEnRedSocialPorUsuario(tipoRedSocial, user_id, social_network_user_id);
                    usuCN.Dispose();
                }
                else
                {
                    throw new GnossException($"The user {user_id} does not belong to the database", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets a user by a social network login
        /// </summary>
        /// <param name="socialNetworkUserId">Social network user's identifier</param>
        /// <param name="socialNetwork">Social network (Facebook, twitter, instagram...)</param>
        [HttpGet, Route("get-user_id-by-social-network-login")]
        public Guid? GetUserBySocialNetworkLogin(string social_network_user_id, string social_network)
        {
            Guid? user_id = null;

            if (string.IsNullOrEmpty(social_network_user_id))
            {
                throw new GnossException("The parameter \"social_network_user_id\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                TipoRedSocialLogin tipoRedSocial;
                if (!Enum.TryParse(social_network, true, out tipoRedSocial))
                {
                    tipoRedSocial = TipoRedSocialLogin.Otros;
                }

                Guid resultado = usuCN.ObtenerUsuarioPorLoginEnRedSocial(tipoRedSocial, social_network_user_id);
                if (!resultado.Equals(Guid.Empty))
                {
                    user_id = resultado;
                }
                else
                {
                    throw new GnossException($"Could not find any user with social network id {social_network_user_id} at {social_network}", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }

            return user_id;
        }

        /// <summary>
        /// Checks if a user id in a social network exists in the system
        /// </summary>
        /// <param name="social_network_user_id">Social network user's identifier</param>
        /// <param name="social_network">Social network (Facebook, twitter, instagram...)</param>
        [HttpGet, Route("exists-social-network-login")]
        public bool ExistsSocialNetworkLogin(string social_network_user_id, string social_network)
        {
            if (string.IsNullOrEmpty(social_network_user_id))
            {
                throw new GnossException("The parameter \"social_network_user_id\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                TipoRedSocialLogin tipoRedSocial;
                if (!Enum.TryParse(social_network, true, out tipoRedSocial))
                {
                    tipoRedSocial = TipoRedSocialLogin.Otros;
                }

                Guid resultado = usuCN.ObtenerUsuarioPorLoginEnRedSocial(tipoRedSocial, social_network_user_id);
                return !resultado.Equals(Guid.Empty);
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets the user
        /// </summary>
        /// <param name="user_id">User identifier</param>
        /// <param name="social_network">Social network short name</param>
        /// <returns>Social network login of the user</returns>
        [HttpGet, Route("get-social-network-login-by-user_id")]
        public string GetSocialNetworkLoginByUserId(string social_network, Guid user_id)
        {
            string socialNetworkLogin;

            if (string.IsNullOrEmpty(social_network))
            {
                throw new GnossException("The parameter \"social_network\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (user_id.Equals(Guid.Empty))
            {
                throw new GnossException("The parameter \"user_id\" can not be empty", HttpStatusCode.BadRequest);
            }

            if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
            {
                UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                TipoRedSocialLogin tipoRedSocial;
                if (!Enum.TryParse(social_network, true, out tipoRedSocial))
                {
                    tipoRedSocial = TipoRedSocialLogin.Otros;
                }

                string resultado = usuCN.ObtenerLoginEnRedSocialPorUsuarioId(tipoRedSocial, user_id);
                if (!string.IsNullOrEmpty(resultado))
                {
                    socialNetworkLogin = resultado;
                }
                else
                {
                    throw new GnossException($"Could not find any social network login with user {user_id} at {social_network}", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }

            return socialNetworkLogin;
        }

        /// <summary>
        /// Add CMS Admin rol privilege to a specific user.
        /// </summary>
        /// <param name="user_id">User identifier</param>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="admin_page_type">Administation page type</param>
        /// <example>POST user/add-permission?user_id={user_id}&community_short_name={community_short_name}&admin_page_type={admin_page_type}</example>  
        [HttpPost, Route("add-permission")]
        public void AddPermissionToUser(Guid user_id, string community_short_name, string admin_page_type)
        {
            if (!user_id.Equals(Guid.Empty) && !string.IsNullOrEmpty(community_short_name))
            {
                TipoPaginaAdministracion tipoPaginaAdministracion;
                if (!Enum.TryParse(admin_page_type, out tipoPaginaAdministracion))
                {
                    throw new GnossException($"The required parameter 'admin_page_type' its not a valid administation page type", HttpStatusCode.BadRequest);
                }

                mNombreCortoComunidad = community_short_name;
                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                if (usuarioCN.EstaUsuarioEnProyecto(user_id, FilaProy.ProyectoID))
                {
                    ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                    Guid identidadID = identidadCN.ObtenerIdentidadUsuarioEnProyecto(user_id, FilaProy.ProyectoID);
                    List<Guid> identidadesSupervisores = proyCN.ObtenerListaIdentidadesSupervisoresPorProyecto(FilaProy.ProyectoID);

                    //si el usuario no es supervisor se le cambia el rol
                    if (!identidadesSupervisores.Contains(identidadID))
                    {
                        if (!CambiarRolUsuarioEnProyecto(user_id, FilaProy.ProyectoID, TipoRolUsuario.Supervisor))
                        {
                            throw new GnossException($"Could not assign the 'Supervisor' role to user {user_id}", HttpStatusCode.BadRequest);
                        }
                    }

                    List<PermisosPaginasUsuarios> filasPermisoProyectoUsuario = mEntityContext.PermisosPaginasUsuarios.Where(fila => fila.ProyectoID.Equals(FilaProy.ProyectoID) && fila.Pagina.Equals((short)tipoPaginaAdministracion) && fila.UsuarioID.Equals(user_id)).ToList();
                    List<Guid> idsUsuariosBD = filasPermisoProyectoUsuario.Select(fila => fila.UsuarioID).ToList();

                    if (!idsUsuariosBD.Contains(user_id))
                    {
                        PermisosPaginasUsuarios filaNuevoUsuario = CrearFilaPermisosPaginasUsuarios(tipoPaginaAdministracion, user_id, FilaProy.OrganizacionID, FilaProy.ProyectoID);
                        mEntityContext.PermisosPaginasUsuarios.Add(filaNuevoUsuario);
                        mEntityContext.SaveChanges();
                    }
                }
                else
                {
                    throw new GnossException($"The user does not exist in the community {community_short_name}", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Remove CMS Admin rol privilege to a specific user.
        /// </summary>
        /// <param name="community_short_name">Community short name</param>
        /// <param name="admin_page_type">Administation page type</param>
        /// <example>POST user/remove-permission?user_id={user_id}&community_short_name={community_short_name}&admin_page_type={admin_page_type}</example>
        [HttpPost, Route("remove-permission")]
        public void RemovePermissionToUser(Guid user_id, string community_short_name, string admin_page_type)
        {
            if (!user_id.Equals(Guid.Empty) && !string.IsNullOrEmpty(community_short_name))
            {
                TipoPaginaAdministracion tipoPaginaAdministracion;
                if (!Enum.TryParse(admin_page_type, out tipoPaginaAdministracion))
                {
                    throw new GnossException($"The required parameter 'admin_page_type' its not a valid administation page type", HttpStatusCode.BadRequest);
                }

                mNombreCortoComunidad = community_short_name;
                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                if (usuarioCN.EstaUsuarioEnProyecto(user_id, FilaProy.ProyectoID))
                {
                    PermisosPaginasUsuarios filaUsuarioEliminar = mEntityContext.PermisosPaginasUsuarios.FirstOrDefault(fila => fila.ProyectoID.Equals(FilaProy.ProyectoID) && fila.Pagina.Equals((short)tipoPaginaAdministracion) && fila.UsuarioID.Equals(user_id));

                    if (filaUsuarioEliminar != null)
                    {
                        mEntityContext.PermisosPaginasUsuarios.Remove(filaUsuarioEliminar);
                        mEntityContext.SaveChanges();
                    }
                }
                else
                {
                    throw new GnossException($"The user does not exist in the community {community_short_name}", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The required parameters can not be empty", HttpStatusCode.BadRequest);
            }
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Da de alta a un nuevo usuario en Gnoss y en una serie de comunidades.
        /// </summary>
        /// <param name="pUsuarioJSON">UsuarioJson</param>
        /// <returns></returns>
        private Dictionary<string, Guid> AltaUsuarioEnGnossYComunidades(User pUsuarioJSON)
        {
            Dictionary<string, Guid> salida = new Dictionary<string, Guid>();
            AgregarTraza("Empieza AltaUsuarioEnGnossYComunidades");

            #region Comprobar incoherencias

            if (string.IsNullOrEmpty(pUsuarioJSON.name) || string.IsNullOrEmpty(pUsuarioJSON.last_name) || string.IsNullOrEmpty(pUsuarioJSON.email)/* || string.IsNullOrEmpty(pSexo)*/)
            {
                throw new GnossException("Missing one or more parameters", HttpStatusCode.BadRequest);
            }

            //Comprobar buen email:
            if (!UtilCadenas.ValidarEmail(pUsuarioJSON.email))
            {
                throw new GnossException("The email does not have the correct format", HttpStatusCode.BadRequest);
            }

            string RegExPatternPass = "(?!^[0-9]*$)(?!^[a-zA-ZñÑüÜ]*$)^([a-zA-ZñÑüÜ0-9#_$*]{6,12})$";
            Regex r = new Regex(RegExPatternPass);

            //se quita la validación de la contraseña porque para integrar el método en entornos en los que la contraseña nos viene heredada,
            //no podemos imponer nuestras restricciones
            //if (!string.IsNullOrEmpty(pPassword) && !r.IsMatch(pPassword))
            //{
            //    error = "Error: El dato 'password' no tiene un formato correcto. La contraseña debe tener entre 6 y 12 caracteres, al menos una letra y un número, y no puede contener caracteres especiales, excepto: '#', '_', '$' y '*'.";
            //    GuardarLogError(error);
            //    throw new Exception(error);
            //}
            /*
            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService);
            if (personaCN.ExisteEmail(pUsuarioJSON.email))
            {
                throw new GnossException("The email is already being used", HttpStatusCode.BadRequest);
            }
            personaCN.Dispose();*/

            //Comprobar sexo
            if (!string.IsNullOrEmpty(pUsuarioJSON.sex) && pUsuarioJSON.sex != "H" && pUsuarioJSON.sex != "M")
            {
                throw new GnossException("The GenderCode does not have the correct format (H/M)", HttpStatusCode.BadRequest);
            }

            int hashNumUsu = 1;
            string login = GenerarLoginUsuario(pUsuarioJSON.name, pUsuarioJSON.last_name, ref hashNumUsu);
            //comprobar el email despues del login.

            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            if (personaCN.ExisteEmail(pUsuarioJSON.email))
            {
                throw new GnossException("The email is already being used", HttpStatusCode.BadRequest);
            }
            personaCN.Dispose();

            //Comprobamos que no exista el usuario:
            int count = 0;
            UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            //no es necesario esto
            /*while (usuarioCN.ExisteUsuarioEnBD(login))
            {
                if (count > 0)
                {
                    login = login.Substring(0, login.Length - count.ToString().Length);
                }

                count++;
                login += count.ToString();
            }*/

            string password = "";
            if (!string.IsNullOrEmpty(pUsuarioJSON.password))
            {
                if (pUsuarioJSON.password.Length < 6 || pUsuarioJSON.password.Length > 12)
                {
                    throw new GnossException("The user password must contain between 6 and 12 characters", HttpStatusCode.BadRequest);
                }

                password = pUsuarioJSON.password;
            }

            #endregion

            #region Generar solicitud usuario

            AgregarTraza("Empiezo a Generar solicitud usuario");

            string nombre = pUsuarioJSON.name.Substring(0, 1).ToUpper() + pUsuarioJSON.name.Substring(1);
            //Antes de la migracion del V2 Incidencia LRE-145
            //string nombreCorto = GenerarNombreCortoUsuario(login, ref hashNumUsu);
            DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
            string nombreCorto = GenerarNombreCortoUsuario(ref login, pUsuarioJSON.name, pUsuarioJSON.last_name, dataWrapperUsuario);

            //Usuario
            //UsuarioDS usuarioDS = new UsuarioDS();
            GestionUsuarios gestorUsuarios = new GestionUsuarios(dataWrapperUsuario, mLoggingService, mEntityContext, mConfigService);
            UsuarioGnoss usuario = gestorUsuarios.AgregarUsuario(login, nombreCorto, HashHelper.CalcularHash(password, true), true);
            AD.EntityModel.Models.UsuarioDS.Usuario filaUsuario = usuario.FilaUsuario;
            filaUsuario.EstaBloqueado = true;

            //Solicitud
            DataWrapperSolicitud solicitudDW = new DataWrapperSolicitud();
            Guid organizacionID = ProyectoAD.MetaOrganizacion;
            Guid proyectoID = ProyectoAD.MetaProyecto;

            if (!pUsuarioJSON.community_id.Equals(Guid.Empty))
            {
                proyectoID = pUsuarioJSON.community_id;
            }
            else if (!string.IsNullOrEmpty(pUsuarioJSON.community_short_name))
            {
                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(pUsuarioJSON.community_short_name);

                if (proyectoID.Equals(Guid.Empty))
                {
                    throw new ArgumentException($"The community {pUsuarioJSON.community_short_name} does not exists", "community_short_name");
                }
            }

            Solicitud filaSolicitud = new Solicitud();
            filaSolicitud.Estado = (short)EstadoSolicitud.Espera;
            filaSolicitud.FechaSolicitud = DateTime.Now;
            filaSolicitud.OrganizacionID = organizacionID;
            filaSolicitud.ProyectoID = proyectoID;
            filaSolicitud.SolicitudID = Guid.NewGuid();

            solicitudDW.ListaSolicitud.Add(filaSolicitud);
            mEntityContext.Solicitud.Add(filaSolicitud);

            SolicitudNuevoUsuario filaNuevoUsuario = new SolicitudNuevoUsuario();
            filaNuevoUsuario.SolicitudID = filaSolicitud.SolicitudID;
            filaNuevoUsuario.UsuarioID = filaUsuario.UsuarioID;
            filaNuevoUsuario.Nombre = nombre;
            filaNuevoUsuario.NombreCorto = nombreCorto;
            filaNuevoUsuario.Apellidos = pUsuarioJSON.last_name;
            filaNuevoUsuario.Email = pUsuarioJSON.email;
            filaNuevoUsuario.EsBuscable = true;
            filaNuevoUsuario.EsBuscableExterno = false;

            try
            {
                ParametroGeneralCL paramCL = new ParametroGeneralCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                GestorParametroGeneral parametroGeneralDS = paramCL.ObtenerParametrosGeneralesDeProyecto(pUsuarioJSON.community_id);
                paramCL.Dispose();
                //ParametroGeneral parametrosGeneralesRow = parametroGeneralDS.ListaParametroGeneral.FindByOrganizacionIDProyectoID(ProyectoAD.MyGnoss, pUsuarioJSON.community_id);
                ParametroGeneral parametrosGeneralesRow = parametroGeneralDS.ListaParametroGeneral.Find(parametroGen => parametroGen.OrganizacionID.Equals(ProyectoAD.MyGnoss) && parametroGen.ProyectoID.Equals(pUsuarioJSON.community_id));
                if (parametrosGeneralesRow != null && !parametrosGeneralesRow.PrivacidadObligatoria)
                {
                    filaNuevoUsuario.EsBuscable = false;
                    filaNuevoUsuario.EsBuscableExterno = false;
                }
            }
            catch { }

            if (pUsuarioJSON.country_id != null)
            {
                filaNuevoUsuario.PaisID = pUsuarioJSON.country_id;
            }
            if (pUsuarioJSON.province_id != null)
            {
                filaNuevoUsuario.ProvinciaID = pUsuarioJSON.province_id;
            }

            filaNuevoUsuario.Provincia = "";
            if (pUsuarioJSON.provice != null)
            {
                filaNuevoUsuario.Provincia = pUsuarioJSON.provice;
            }

            filaNuevoUsuario.Poblacion = "";
            if (pUsuarioJSON.city != null)
            {
                filaNuevoUsuario.Poblacion = pUsuarioJSON.city;
            }

            filaNuevoUsuario.Direccion = "";
            if (pUsuarioJSON.address != null)
            {
                filaNuevoUsuario.Direccion = pUsuarioJSON.address;
            }

            filaNuevoUsuario.Sexo = "";
            if (pUsuarioJSON.sex != null)
            {
                filaNuevoUsuario.Sexo = pUsuarioJSON.sex;
            }

            filaNuevoUsuario.FaltanDatos = false;

            if (pUsuarioJSON.born_date != null && !pUsuarioJSON.born_date.Equals(DateTime.MinValue) && !pUsuarioJSON.born_date.Equals(DateTime.MaxValue))
            {
                filaNuevoUsuario.FechaNacimiento = pUsuarioJSON.born_date;
            }

            string dni = "";
            if (!string.IsNullOrEmpty(pUsuarioJSON.id_card))
            {
                dni = pUsuarioJSON.id_card;
            }

            if (pUsuarioJSON.extra_data != null && pUsuarioJSON.extra_data.Count > 0)
            {
                filaNuevoUsuario.ClausulasAdicionales = ObtenerClausulasAdicionales(pUsuarioJSON.community_id, pUsuarioJSON.extra_data);

                GuardarDatosExtraSolicitud(solicitudDW, filaSolicitud, pUsuarioJSON.extra_data, filaSolicitud.OrganizacionID, pUsuarioJSON.community_id, pUsuarioJSON.community_id.Equals(ProyectoAD.MyGnoss));
            }

            if (!string.IsNullOrEmpty(pUsuarioJSON.languaje))
            {
                filaNuevoUsuario.Idioma = pUsuarioJSON.languaje;
            }
            else
            {
                filaNuevoUsuario.Idioma = "es";
            }

            filaNuevoUsuario.CrearClase = false;
            filaNuevoUsuario.CambioPassword = false;

            solicitudDW.ListaSolicitudNuevoUsuario.Add(filaNuevoUsuario);
            mEntityContext.SolicitudNuevoUsuario.Add(filaNuevoUsuario);

            AgregarTraza("Antes guardar solicitud.");

            //Guardado

            usuarioCN.ActualizarUsuario(false);
            mEntityContext.SaveChanges();

            AgregarTraza("Solicitud guardada.");

            #endregion

            AceptarUsuario(filaSolicitud.SolicitudID, TipoDocumentoAcreditativo.DNI, dni, password, filaNuevoUsuario.Idioma, dataWrapperUsuario, solicitudDW);
            salida.Add(login, filaNuevoUsuario.UsuarioID);

            AgregarTraza("Acabado AceptarUsuario.");
            usuarioCN.Dispose();

            return salida;
        }

        /// <summary>
        /// Método para aceptar el usuario
        /// </summary>
        /// <param name="pIdSolicitud">Identificador de solicitud</param>
        /// <param name="pPassword">Contraseña</param>
        private void AceptarUsuario(Guid pIdSolicitud, TipoDocumentoAcreditativo pTipoDocumentoAcreditativo, string pDNI, string pPassword, string pIdioma, DataWrapperUsuario pDataWrapperUsuario, DataWrapperSolicitud pDataWrapperSolicitud)
        {
            AgregarTraza("Empiezo AceptarUsuario");

            UtilIdiomas utilIdiomas = new UtilIdiomas(pIdioma, mLoggingService, mEntityContext, mConfigService);
            string baseUrlIdioma = ObtenerUrlBaseIdioma(pIdioma);

            SolicitudNuevoUsuario filaSU = pDataWrapperSolicitud.ListaSolicitudNuevoUsuario.Where(item => item.SolicitudID.Equals(pIdSolicitud)).FirstOrDefault();

            Solicitud fila = pDataWrapperSolicitud.ListaSolicitud.Where(item => item.SolicitudID.Equals(pIdSolicitud)).FirstOrDefault();

            Usuario filaUsuario = pDataWrapperUsuario.ListaUsuario.First();

            filaUsuario.EstaBloqueado = false;

            GestionPersonas gestorPersonas = null;

            GeneralCN generalCN = new GeneralCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DateTime fechaHoy = generalCN.HoraServidor;

            fila.Estado = (short)EstadoSolicitud.Aceptada;
            fila.FechaProcesado = fechaHoy;

            //Identidad
            DataWrapperIdentidad dataWrapperIdentidad = new DataWrapperIdentidad();

            //Persona
            DataWrapperPersona dataWrapperPersona = new DataWrapperPersona();
            gestorPersonas = new GestionPersonas(dataWrapperPersona, mLoggingService, mEntityContext);
            Persona persona = gestorPersonas.AgregarPersona();

            AD.EntityModel.Models.PersonaDS.Persona filaPersona = persona.FilaPersona;
            filaPersona.UsuarioID = filaSU.UsuarioID;
            filaPersona.Usuario = filaUsuario;
            filaPersona.Apellidos = filaSU.Apellidos;
            filaPersona.CPPersonal = filaSU.CP;
            filaPersona.DireccionPersonal = filaSU.Direccion;
            filaPersona.Email = filaSU.Email;
            filaPersona.EsBuscable = filaSU.EsBuscable;
            filaPersona.EsBuscableExternos = filaSU.EsBuscableExterno;
            if (filaSU.FechaNacimiento.HasValue)
            {
                filaPersona.FechaNacimiento = filaSU.FechaNacimiento;
            }
            filaPersona.LocalidadPersonal = filaSU.Poblacion;
            filaPersona.Nombre = filaSU.Nombre;
            if (filaSU.PaisID.HasValue)
            {
                filaPersona.PaisPersonalID = filaSU.PaisID;
            }
            filaPersona.EstadoCorreccion = (short)EstadoCorreccion.NoCorreccion;

            if (!string.IsNullOrEmpty(pDNI))
            {
                filaPersona.TipoDocumentoAcreditativo = (short)pTipoDocumentoAcreditativo;
                filaPersona.ValorDocumentoAcreditativo = pDNI;
            }

            if (!filaSU.ProvinciaID.HasValue)
            {
                filaPersona.ProvinciaPersonal = filaSU.Provincia;
            }
            else
            {
                filaPersona.ProvinciaPersonalID = filaSU.ProvinciaID;
            }
            filaPersona.Sexo = filaSU.Sexo;
            if (!string.IsNullOrEmpty(pIdioma))
            {
                filaPersona.Idioma = pIdioma;
            }
            AD.EntityModel.Models.PersonaDS.ConfiguracionGnossPersona filaConfigPers = gestorPersonas.AgregarConfiguracionGnossPersona(filaPersona.PersonaID);

            if (filaSU.EsBuscable)
            {
                filaConfigPers.VerRecursos = true;
                filaConfigPers.VerAmigos = true;
                filaConfigPers.VerRecursos = true;

            }

            if (filaSU.EsBuscableExterno)
            {
                filaConfigPers.VerRecursos = true;
                filaConfigPers.VerAmigos = true;
                filaConfigPers.VerRecursos = true;
                filaConfigPers.VerRecursosExterno = true;
                filaConfigPers.VerAmigos = true;
                filaConfigPers.VerRecursosExterno = true;
            }

            AgregarTraza("Persona creada");

            GestionIdentidades gestorIdentidades = new GestionIdentidades(dataWrapperIdentidad, gestorPersonas, new GestionOrganizaciones(new DataWrapperOrganizacion(), mLoggingService, mEntityContext), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestionUsuarios gestorUsuarios = new GestionUsuarios(pDataWrapperUsuario, mLoggingService, mEntityContext, mConfigService);

            if (!persona.UsuarioCargado)
            {
                if (persona.GestorPersonas.GestorUsuarios == null)
                {
                    persona.GestorPersonas.GestorUsuarios = new GestionUsuarios(new DataWrapperUsuario(), mLoggingService, mEntityContext, mConfigService);
                }
            }

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Dictionary<Guid, bool> recibirNewsletterDefectoProyectos = proyCN.ObtenerProyectosConConfiguracionNewsletterPorDefecto();

            Perfil perfilPersona = gestorIdentidades.AgregarPerfilPersonal(filaPersona, true, ProyectoAD.MetaOrganizacion, ProyectoAD.MetaProyecto, recibirNewsletterDefectoProyectos);
            Identidad objetoIdentidad = (Identidad)perfilPersona.Hijos[0];
            AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad = objetoIdentidad.FilaIdentidad;

            AgregarTraza("Identidad creada");

            if (!proyCN.ParticipaUsuarioEnProyecto(ProyectoAD.MetaProyecto, filaUsuario.UsuarioID))
            {
                gestorUsuarios.AgregarUsuarioAProyecto(filaUsuario, ProyectoAD.MetaOrganizacion, ProyectoAD.MetaProyecto, filaIdentidad.IdentidadID);
            }

            AgregarTraza("Agregada a MyGnoss");

            ControladorDeSolicitudes.RegistrarUsuarioEnProyectosObligatorios(filaSU.Solicitud.OrganizacionID, filaSU.Solicitud.ProyectoID, filaPersona.PersonaID, perfilPersona, filaUsuario, gestorUsuarios, gestorIdentidades);
            gestorIdentidades.RecargarHijos();

            if (!fila.ProyectoID.Equals(ProyectoAD.MetaProyecto) && !fila.ProyectoID.Equals(ProyectoAD.ProyectoFAQ) && !fila.ProyectoID.Equals(ProyectoAD.ProyectoNoticias) && !fila.ProyectoID.Equals(ProyectoAD.ProyectoDidactalia))
            {
                Guid organizacionID = fila.OrganizacionID;
                Guid proyectoID = fila.ProyectoID;

                //si el usuario aún no participa en el proyecto se agrega
                if (!proyCN.ParticipaUsuarioEnProyecto(proyectoID, filaUsuario.UsuarioID) && gestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Count(identidad => identidad.ProyectoID.Equals(proyectoID)) == 0)
                {
                    Identidad ObjetoIdentidadProy = ControladorIdentidades.AgregarIdentidadPerfilYUsuarioAProyecto(gestorIdentidades, gestorUsuarios, organizacionID, proyectoID, filaUsuario, perfilPersona, recibirNewsletterDefectoProyectos);

                    gestorIdentidades.RecargarHijos();
                }
            }

            //Invalido la cache de Mis comunidades
            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            proyCL.InvalidarMisProyectos(filaIdentidad.PerfilID);
            proyCL.Dispose();

            AgregarTraza("Agregada a Ayuda y Noticias");

            //Invalido la cache de Mis comunidades
            DataWrapperIdentidad idenDW = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication).ObtenerIdentidadPorID(gestorIdentidades.ObtenerIdentidadDeProyecto(ProyectoAD.ProyectoFAQ, filaPersona.PersonaID), true);

            if (idenDW.ListaIdentidad.Count > 0)
            {
                proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                proyCL.InvalidarMisProyectos(idenDW.ListaIdentidad.FirstOrDefault().PerfilID);
                proyCL.Dispose();
            }

            AgregarTraza("Borrada caché identidades");

            gestorPersonas.CrearDatosTrabajoPersonaLibre(persona);
            gestorUsuarios.GestorTesauro = new GestionTesauro(new DataWrapperTesauro(), mLoggingService, mEntityContext);
            gestorUsuarios.GestorDocumental = new GestorDocumental(new DataWrapperDocumentacion(), mLoggingService, mEntityContext);
            gestorUsuarios.CompletarUsuarioNuevo(filaUsuario, utilIdiomas.GetText("TESAURO", "RECURSOSPUBLICOS"), utilIdiomas.GetText("TESAURO", "RECURSOSPRIVADOS"));

            AgregarTraza("Completado usuario");

            GuardarDatosExtraDeSolicitudEnIdentidad(dataWrapperIdentidad, filaIdentidad.PerfilID, pIdSolicitud, pDataWrapperSolicitud);

            mEntityContext.SaveChanges();

            AgregarTraza("Datos guardados");

            try
            {
                ControladorIdentidades.NotificarEdicionPerfilEnProyectos(TipoAccionExterna.Registro, filaPersona.PersonaID, "", "");
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex, " Error al enviar el usuario: " + filaPersona.UsuarioID + " a Smart Focus");
            }

            IdentidadCN idenCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            IdentidadCL idenCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            Identidad identidadnuevo = new GestionIdentidades(idenCN.ObtenerIdentidadPorID(filaIdentidad.IdentidadID, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication).ListaIdentidades[filaIdentidad.IdentidadID];
            PersonaCN persCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            identidadnuevo.GestorIdentidades.GestorPersonas = new GestionPersonas(persCN.ObtenerPersonaPorID(identidadnuevo.PersonaID.Value), mLoggingService, mEntityContext);
            identidadnuevo.GestorIdentidades.GestorPersonas.CargarGestor();

            //Actualizo el modelo base:
            ControladorPersonas controladorPersonas = new ControladorPersonas(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
            foreach (Identidad iden in gestorIdentidades.ListaIdentidades.Values)
            {
                //controladorPersonas.ActualizarModeloBaseSimple(filaPersona.PersonaID, iden.FilaIdentidad.ProyectoID, PrioridadBase.Alta);
                controladorPersonas.ActualizarModeloBaseSimple(iden, iden.FilaIdentidad.ProyectoID, UrlIntragnoss);
            }

            idenCL.Dispose();

            AgregarTraza("Modelo base de identidad actualizado");

            #region Actualizar cola GnossLIVE
            LiveCN liveCN = new LiveCN("base", mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            LiveDS liveDS = new LiveDS();
            foreach (Identidad iden in gestorIdentidades.ListaIdentidades.Values)
            {
                liveDS.Cola.AddColaRow(iden.FilaIdentidad.ProyectoID, iden.FilaIdentidad.PerfilID, (int)AccionLive.Agregado, (int)TipoLive.Miembro, 0, DateTime.Now, false, (short)PrioridadLive.Alta, null);
            }
            liveCN.ActualizarBD(liveDS);
            liveCN.Dispose();
            liveDS.Dispose();

            #endregion

            AgregarTraza("Live actualizado");

            //#region Actualizo el Base

            //if (correoID != Guid.Empty)
            //{
            //    foreach (Guid destinatario in listaDestinatarios)
            //    {
            //        ControladorDocumentacion.AgregarMensajeFacModeloBaseSimple(correoID, Guid.Empty, ProyectoAD.MetaProyecto, "base", destinatario.ToString(), null, PrioridadBase.Alta);
            //    }
            //}

            //AgregarTraza("Fila de mensaje actualizada en el base");

            //#endregion
        }
        [NonAction]
        private void EliminarUsuario(Guid pIdentidadID)
        {
            //código obtenido de la funcionalidad de la web al eliminar un usuario en mygnoss
            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestionPersonas GestorPersonas = new GestionPersonas(personaCN.ObtenerPersonasPorIdentidad(pIdentidadID), mLoggingService, mEntityContext);
            personaCN.Dispose();

            OrganizacionCN organizacionCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestionOrganizaciones GestorOrganizaciones = new GestionOrganizaciones(organizacionCN.ObtenerOrganizacionesPorIdentidad(pIdentidadID), mLoggingService, mEntityContext);
            personaCN.Dispose();

            IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestionIdentidades GestorIdentidades = new GestionIdentidades(identidadCN.ObtenerIdentidadPorID(pIdentidadID, true), GestorPersonas, GestorOrganizaciones, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
            identidadCN.Dispose();

            Identidad identidadInvitado = GestorIdentidades.ListaIdentidades[pIdentidadID];

            ControladorAmigos contrAmigos = new ControladorAmigos(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
            contrAmigos.CargarAmigos(identidadInvitado, false);

            //Obtengo usuario:
            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperUsuario dataWrapperUsuario = usuCN.ObtenerUsuarioCompletoPorID(identidadInvitado.Persona.UsuarioID);
            dataWrapperUsuario.Merge(usuCN.ObtenerFilaUsuarioVincRedSocialPorUsuarioID(identidadInvitado.Persona.UsuarioID));
            usuCN.Dispose();

            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            identidadInvitado.GestorIdentidades.DataWrapperIdentidad.Merge(identCN.ObtenerPerfilesDePersona(identidadInvitado.PersonaID.Value, false));
            identCN.Dispose();

            //Boqueo Usuario:
            dataWrapperUsuario.ListaUsuario.FirstOrDefault().EstaBloqueado = true;

            //Borro ProyectoUsuarioIdentidad:
            foreach (AD.EntityModel.Models.UsuarioDS.ProyectoUsuarioIdentidad filaProyUsuIdent in dataWrapperUsuario.ListaProyectoUsuarioIdentidad)
            {
                mEntityContext.Entry(filaProyUsuIdent).State = EntityState.Deleted;
            }

            //Bloqueo ProyectoRolUsuario:
            foreach (AD.EntityModel.Models.UsuarioDS.ProyectoRolUsuario filaProyRolUsu in dataWrapperUsuario.ListaProyectoRolUsuario)
            {
                filaProyRolUsu.EstaBloqueado = true;
            }

            // Borro UsuarioVinculadoLoginRedesSociales
            foreach (AD.EntityModel.Models.UsuarioDS.UsuarioVinculadoLoginRedesSociales filaRedSocial in dataWrapperUsuario.ListaUsuarioVinculadoLoginRedesSociales)
            {
                mEntityContext.Entry(filaRedSocial).State = EntityState.Deleted;
            }

            //Elimino Persona:
            identidadInvitado.Persona.FilaPersona.Eliminado = true;

            //Borro perfiles:
            List<Guid> perfilesEliminados = new List<Guid>();
            foreach (AD.EntityModel.Models.IdentidadDS.Perfil filaPerfil in identidadInvitado.GestorIdentidades.DataWrapperIdentidad.ListaPerfil.Where(perfil => perfil.PersonaID.Equals(identidadInvitado.PersonaID)).ToList())
            {
                perfilesEliminados.Add(filaPerfil.PerfilID);
                filaPerfil.Eliminado = true;
            }

            //Pongo como expulsadas las identidades:
            List<Guid> listaProyectosEliminados = new List<Guid>();
            foreach (Guid perfilID in perfilesEliminados)
            {
                List<AD.EntityModel.Models.IdentidadDS.Identidad> filasIdent = identidadInvitado.GestorIdentidades.DataWrapperIdentidad.ListaIdentidad.Where(identidad => identidad.PerfilID.Equals(perfilID)).ToList();
                foreach (AD.EntityModel.Models.IdentidadDS.Identidad filaIdent in filasIdent)
                {
                    listaProyectosEliminados.Add(filaIdent.ProyectoID);
                    filaIdent.FechaExpulsion = DateTime.Now;
                    filaIdent.FechaBaja = DateTime.Now;
                }
            }

            #region Elimino Contactos

            List<Guid> listaContactosEliminados = new List<Guid>();

            foreach (Identidad amigo in identidadInvitado.GestorAmigos.ListaContactos.Values)
            {
                listaContactosEliminados.Add(amigo.Clave);
                identidadInvitado.GestorAmigos.EliminarAmigos(identidadInvitado.IdentidadMyGNOSS, amigo.IdentidadMyGNOSS);
            }

            #endregion

            //GestionNotificaciones gestorNotificaciones = new GestionNotificaciones(new NotificacionDS());
            //gestorNotificaciones.AgregarNotificacionEliminacionDeUsuario(identidadInvitado.Persona);

            //Guardo:
            mEntityContext.SaveChanges();

            //Actualizo Live:
            foreach (Guid proyectoID in listaProyectosEliminados)
            {
                ControladorDocumentacion.ActualizarGnossLive(proyectoID, identidadInvitado.FilaIdentidad.PerfilID, AccionLive.Eliminado, (int)TipoLive.Miembro, false, PrioridadLive.Alta);
            }

            //Actualizo modelo base:
            ControladorPersonas controPer = new ControladorPersonas(mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper, mGnossCache, mEntityContextBASE, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);
            foreach (Guid proyectoID in listaProyectosEliminados)
            {
                controPer.ActualizarEliminacionModeloBaseSimple(identidadInvitado.PersonaID.Value, proyectoID, PrioridadBase.Alta);
            }

            //Limpio Caches:
            try
            {
                foreach (Guid perfilID in perfilesEliminados)
                {
                    //Invalido la cache de Mis comunidades
                    ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                    proyCL.InvalidarMisProyectos(perfilID);
                    proyCL.Dispose();
                }

                foreach (Guid proyectoID in listaProyectosEliminados)
                {
                    //Invalidamos la cache de amigos en la comunidad
                    AmigosCL amigosCL = new AmigosCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                    amigosCL.InvalidarAmigosPertenecenProyecto(proyectoID);
                    amigosCL.Dispose();
                }

                foreach (Guid identidadID in listaContactosEliminados)
                {
                    //Limpiamos la cache de los contactos
                    AmigosCL amigosCL = new AmigosCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                    amigosCL.InvalidarAmigos(identidadID);
                    amigosCL.Dispose();
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
            }
        }
        [NonAction]
        private User ObtenerJsonUsuarioProyecto(string pNombreCortoUsuario, string pEmail, Guid pUsuarioIDOauth, Guid pUsuarioID, string pNombreCortoComunidad, bool pObtenerPorEmail)
        {
            //como esta función se usa en varios métodos, en la de eliminación la petición Oauth ya se ha realizado y ya tenemos el usuario
            if (pUsuarioIDOauth.Equals(Guid.Empty))
            {
                pUsuarioIDOauth = ComprobarPermisosOauth(mHttpContextAccessor.HttpContext.Request);
                if (UsuarioOAuth.Equals(Guid.Empty))
                {
                    throw new GnossException("Invalid OAuth signature", HttpStatusCode.Unauthorized);
                }
            }

            if (EsAdministradorProyectoMyGnoss(pUsuarioIDOauth))
            {
                PersonaCN personaCN = null;
                ProyectoCN proyectoCN = null;
                UsuarioCN usuarioCN = null;

                if ((!string.IsNullOrEmpty(pNombreCortoUsuario) || !string.IsNullOrEmpty(pEmail) || !pUsuarioID.Equals(Guid.Empty)) && !string.IsNullOrEmpty(pNombreCortoComunidad))
                {
                    personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    if (usuarioCN.ExisteNombreCortoEnBD(pNombreCortoUsuario) || personaCN.ExisteEmail(pEmail) || usuarioCN.ExisteUsuarioEnBD(pUsuarioID))
                    {
                        Guid proyectoID = Guid.Empty;
                        List<Guid> lista = proyectoCN.ObtenerProyectoIDOrganizacionIDPorNombreCorto(pNombreCortoComunidad);
                        if (lista != null && lista.Count > 1)
                        {
                            proyectoID = lista[1];
                        }
                        if (proyectoID != Guid.Empty)
                        {
                            Guid usuarioID = Guid.Empty;
                            //si se quiere obtener el usuario por usuarioID
                            if (!pUsuarioID.Equals(Guid.Empty))
                            {
                                usuarioID = pUsuarioID;
                            }
                            else
                            {
                                //si se quiere obtener el usuario por NombreCorto o Email
                                List<AD.EntityModel.Models.UsuarioDS.Usuario> tablaUsuario = null;

                                if (pObtenerPorEmail && !string.IsNullOrEmpty(pEmail))
                                {
                                    tablaUsuario = usuarioCN.ObtenerUsuarioPorLoginOEmail(pEmail, proyectoID.Equals(ProyectoAD.MyGnoss)).ListaUsuario;
                                }
                                else if (!pObtenerPorEmail && !string.IsNullOrEmpty(pNombreCortoUsuario))
                                {
                                    tablaUsuario = usuarioCN.ObtenerUsuarioPorLoginOEmail(usuarioCN.ObtenerLoginUsuarioPorNombreCorto(pNombreCortoUsuario), proyectoID.Equals(ProyectoAD.MyGnoss)).ListaUsuario;
                                }

                                if (tablaUsuario.Count > 0)
                                {
                                    usuarioID = tablaUsuario[0].UsuarioID;
                                }
                            }

                            if (usuarioID != Guid.Empty)
                            {
                                IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                Guid identidadID = identidadCN.ObtenerIdentidadUsuarioEnProyecto(usuarioID, proyectoID);
                                if (identidadID != Guid.Empty)
                                {
                                    GestionIdentidades gestorIdentidades = new GestionIdentidades(identidadCN.ObtenerIdentidadPorID(identidadID, false), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                                    if (gestorIdentidades.ListaIdentidades.ContainsKey(identidadID))
                                    {
                                        Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID];
                                        gestorIdentidades.DataWrapperIdentidad.Merge(identidadCN.ObtenerDatosExtraProyectoOpcionIdentidadPorIdentidadID(identidadID));
                                        gestorIdentidades.CargarGestor();
                                        gestorIdentidades.GestorPersonas = new GestionPersonas(personaCN.ObtenerPersonaPorID(identidad.PersonaID.Value), mLoggingService, mEntityContext);
                                        gestorIdentidades.GestorPersonas.CargarGestor();

                                        DataWrapperProyecto dataWrapperProyecto = proyectoCN.ObtenerDatosExtraProyectoPorID(proyectoID);
                                        User jsonUsuario = MontarJsonUsuario(identidad, dataWrapperProyecto, pNombreCortoComunidad);

                                        return jsonUsuario;
                                    }
                                    else
                                    {
                                        throw new GnossException("The user does not participate in this community", HttpStatusCode.BadRequest);
                                    }
                                }
                                else
                                {
                                    throw new GnossException("The user does not participate in this community", HttpStatusCode.BadRequest);
                                }
                            }
                            else
                            {
                                throw new GnossException("The user does not participate in this community", HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The community does not exist", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        throw new GnossException("The user does not exist", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("The requested params can not be empty", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                throw new GnossException("The OAuth user does not have permission to perform this action", HttpStatusCode.Unauthorized);
            }

        }
        [NonAction]
        private User MontarJsonUsuario(Identidad pIdentidad, DataWrapperProyecto pDataWrapperProyecto, string pNombreCortoComunidad)
        {
            User usuario = new User();
            usuario.name = pIdentidad.Persona.Nombre;
            usuario.last_name = pIdentidad.Persona.Apellidos;
            usuario.email = pIdentidad.Persona.Mail;
            usuario.sex = pIdentidad.Persona.Sexo;
            usuario.community_id = pIdentidad.FilaIdentidad.ProyectoID;
            usuario.community_short_name = pNombreCortoComunidad;
            usuario.user_id = pIdentidad.Persona.UsuarioID;
            if (pIdentidad.PerfilUsuario.NombreCortoUsu != null)
            {
                usuario.user_short_name = pIdentidad.PerfilUsuario.NombreCortoUsu;
            }

            if (pIdentidad.Persona.Fecha != null)
            {
                usuario.born_date = pIdentidad.Persona.Fecha;
            }

            if (pIdentidad.Persona.PaisID != null && pIdentidad.Persona.PaisID != Guid.Empty)
            {
                usuario.country_id = pIdentidad.Persona.PaisID;
            }

            if (pIdentidad.FilaIdentidad.FechaAlta != null)
            {
                usuario.join_community_date = pIdentidad.FilaIdentidad.FechaAlta;
            }
            if (pIdentidad.Persona.ProvinciaID != null && pIdentidad.Persona.ProvinciaID != Guid.Empty)
            {
                usuario.province_id = pIdentidad.Persona.ProvinciaID;
            }

            if (pIdentidad.Persona.FilaPersona != null && !string.IsNullOrEmpty(pIdentidad.Persona.ValorDocumentoAcreditativo))
            {
                usuario.id_card = pIdentidad.Persona.ValorDocumentoAcreditativo;
            }

            PaisCL paisCL = new PaisCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperPais paisDW = paisCL.ObtenerPaisesProvincias();
            paisCL.Dispose();

            if (usuario.country_id != null)
            {
                Pais filaPais = paisDW.ListaPais.Where(item => item.PaisID.Equals(usuario.country_id)).FirstOrDefault();
                if (filaPais != null)
                {
                    usuario.country = filaPais.Nombre;
                }
            }
            if (usuario.province_id != null)
            {
                Provincia filaProvincia = paisDW.ListaProvincia.Where(item => item.ProvinciaID.Equals(usuario.province_id)).FirstOrDefault();
                if (filaProvincia != null)
                {
                    usuario.provice = filaProvincia.Nombre;
                }
            }
            else
            {
                usuario.provice = pIdentidad.Persona.Provincia;
            }
            usuario.city = pIdentidad.Persona.Localidad;
            usuario.postal_code = pIdentidad.Persona.CodPostal;

            List<UserEvent> listadoEventosusuarioProyecto;
            List<ExtraUserData> listaDatosExtra = new List<ExtraUserData>();
            //Obtención de las cláusulas del registro y eventos activos
            ObtenerClausulasYEventosUsuarioEnProyecto(usuario.user_id, usuario.community_id, ref listaDatosExtra, out listadoEventosusuarioProyecto);
            //Obtención de DatosExtra
            ObtenerListaDatosExtraUsuario(pIdentidad, pDataWrapperProyecto, ref listaDatosExtra);

            if (listaDatosExtra.Count > 0)
            {
                usuario.extra_data = listaDatosExtra;
            }
            usuario.user_events = listadoEventosusuarioProyecto;

            //Suscripciones
            SuscripcionCN suscCN = new SuscripcionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperSuscripcion suscDW = suscCN.ObtenerSuscripcionesDeIdentidad(pIdentidad.Clave, true);
            GestionSuscripcion gestorSuscripciones = new GestionSuscripcion(suscDW, mLoggingService, mEntityContext);
            suscCN.Dispose();
            Suscripcion suscripcion = gestorSuscripciones.ObtenerSuscripcionAProyecto(usuario.community_id);

            if (suscripcion != null && suscripcion.FilasCategoriasVinculadas != null)
            {
                usuario.preferences = new List<ThesaurusCategory>();

                TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                GestionTesauro gestorTesauro = new GestionTesauro(tesauroCL.ObtenerTesauroDeProyecto(usuario.community_id), mLoggingService, mEntityContext);
                gestorTesauro.CargarCategorias();
                tesauroCL.Dispose();

                foreach (AD.EntityModel.Models.Suscripcion.CategoriaTesVinSuscrip filaCat in suscripcion.FilasCategoriasVinculadas)
                {
                    string nomCat = gestorTesauro.ListaCategoriasTesauro[filaCat.CategoriaTesauroID].Nombre["es"];
                    usuario.preferences.Add(ObtenerCategoriasJerarquicas(filaCat.CategoriaTesauroID, gestorTesauro.ListaCategoriasTesauro));
                }
            }

            return usuario;
        }
        [NonAction]
        private static ThesaurusCategory ObtenerCategoriasJerarquicas(Guid pCategoriaID, SortedList<Guid, CategoriaTesauro> pListaCategoriasTesauro)
        {
            ThesaurusCategory preferenciaJerarquica = new ThesaurusCategory();
            preferenciaJerarquica.category_id = pCategoriaID;
            preferenciaJerarquica.category_name = pListaCategoriasTesauro[pCategoriaID].Nombre["es"];

            if (pListaCategoriasTesauro[pCategoriaID].Padre != null && pListaCategoriasTesauro[pCategoriaID].Padre is CategoriaTesauro)
            {
                ThesaurusCategory padre = ObtenerCategoriasJerarquicas(((CategoriaTesauro)pListaCategoriasTesauro[pCategoriaID].Padre).Clave, pListaCategoriasTesauro);

                preferenciaJerarquica.parent_category_id = padre.category_id;
            }

            return preferenciaJerarquica;
        }
        [NonAction]
        private static void ObtenerListaDatosExtraUsuario(Identidad pIdentidad, DataWrapperProyecto pDataWrapperProyecto, ref List<ExtraUserData> pListaJsonDatosExtra)
        {
            foreach (AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad filaDatoExtraProyIdent in pIdentidad.GestorIdentidades.DataWrapperIdentidad.ListaDatoExtraProyectoOpcionIdentidad)
            {
                if (pDataWrapperProyecto.ListaDatoExtraProyecto != null && pDataWrapperProyecto.ListaDatoExtraProyecto.Count > 0)
                {
                    List<AD.EntityModel.Models.ProyectoDS.DatoExtraProyecto> filasDatoExtraProy = pDataWrapperProyecto.ListaDatoExtraProyecto.Where(dato => dato.ProyectoID.Equals(pIdentidad.FilaIdentidad.ProyectoID) && dato.DatoExtraID.Equals(filaDatoExtraProyIdent.DatoExtraID)).ToList();
                    List<AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcion> filasDatoExtraProyOpcion = pDataWrapperProyecto.ListaDatoExtraProyectoOpcion.Where(dato => dato.ProyectoID.Equals(pIdentidad.FilaIdentidad.ProyectoID) && dato.OpcionID.Equals(filaDatoExtraProyIdent.OpcionID)).ToList();

                    if (filasDatoExtraProy.Count > 0 && filasDatoExtraProyOpcion.Count > 0)
                    {
                        ExtraUserData datosExtraUsuario = new ExtraUserData();
                        datosExtraUsuario.name = filasDatoExtraProy[0].Titulo;
                        datosExtraUsuario.name_id = filasDatoExtraProy[0].DatoExtraID;
                        datosExtraUsuario.value = filasDatoExtraProyOpcion[0].Opcion;
                        datosExtraUsuario.value_id = filasDatoExtraProyOpcion[0].OpcionID;
                        pListaJsonDatosExtra.Add(datosExtraUsuario);
                    }
                }
            }

            foreach (AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad filaDatoExtraProyVirtuosoIdent in pIdentidad.GestorIdentidades.DataWrapperIdentidad.ListaDatoExtraProyectoVirtuosoIdentidad)
            {
                if (pDataWrapperProyecto.ListaDatoExtraProyectoVirtuoso != null && pDataWrapperProyecto.ListaDatoExtraProyectoVirtuoso.Count > 0)
                {
                    AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoVirtuoso filaDatoExtraVirtuosoProy = pDataWrapperProyecto.ListaDatoExtraProyectoVirtuoso.FirstOrDefault(dato => dato.ProyectoID.Equals(pIdentidad.FilaIdentidad.ProyectoID) && dato.DatoExtraID.Equals(filaDatoExtraProyVirtuosoIdent.DatoExtraID));

                    if (filaDatoExtraVirtuosoProy != null)
                    {
                        ExtraUserData datosExtraUsuario = new ExtraUserData();
                        datosExtraUsuario.name = filaDatoExtraVirtuosoProy.Titulo;
                        datosExtraUsuario.name_id = filaDatoExtraVirtuosoProy.DatoExtraID;
                        datosExtraUsuario.value = filaDatoExtraProyVirtuosoIdent.Opcion;
                        pListaJsonDatosExtra.Add(datosExtraUsuario);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pUsuarioID">Identificador del usuario que participa en el proyecto con alguna identidad</param>
        /// <param name="pProyectoID">Identificador del proyecto en el que participa el usuario</param>
        /// <param name="pListaJsonDatosExtra">Lista de DatosExtra con las clausulas del registro</param>
        /// <param name="listadoEventosusuarioProyecto">Eventos del proyecto activos durante el registro del usuario</param>
        [NonAction]
        private void ObtenerClausulasYEventosUsuarioEnProyecto(Guid pUsuarioID, Guid pProyectoID, ref List<ExtraUserData> pListaJsonDatosExtra, out List<UserEvent> pListadoEventosUsuarioProyecto)
        {
            if (pListaJsonDatosExtra == null)
            {
                pListaJsonDatosExtra = new List<ExtraUserData>();
            }
            pListadoEventosUsuarioProyecto = new List<UserEvent>();

            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            DataWrapperUsuario usuClauProyDS = proyCL.ObtenerClausulasRegitroProyecto(pProyectoID);
            proyCL.Dispose();

            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            usuClauProyDS.Merge(usuCN.ObtenerProyClausulasUsuPorUsuarioID(pUsuarioID));
            usuCN.Dispose();

            foreach (ClausulaRegistro filaClausula in usuClauProyDS.ListaClausulaRegistro.Where(item => item.Tipo.Equals((short)TipoClausulaAdicional.Opcional)))
            {
                List<ProyRolUsuClausulaReg> filasProyRolClau = usuClauProyDS.ListaProyRolUsuClausulaReg.Where(item => item.UsuarioID.Equals(pUsuarioID) && item.ProyectoID.Equals(pProyectoID) && item.ClausulaID.Equals(filaClausula.ClausulaID)).ToList();

                if (filasProyRolClau.Count > 0)
                {
                    ExtraUserData jsonDatosExtra = new ExtraUserData();
                    jsonDatosExtra.name = filaClausula.Texto;
                    jsonDatosExtra.name_id = filaClausula.ClausulaID;
                    jsonDatosExtra.value = filasProyRolClau[0].Valor.ToString();
                    pListaJsonDatosExtra.Add(jsonDatosExtra);
                }
            }

            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Guid identidadID = identCN.ObtenerIdentidadUsuarioEnProyecto(pUsuarioID, pProyectoID);
            identCN.Dispose();

            if (!identidadID.Equals(Guid.Empty))
            {
                ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataSet ds = proyCN.ObtenerEventoProyectoIdentidadID(pProyectoID, identidadID);
                proyCN.Dispose();

                if (ds.Tables["EventosProyectoIdentidad"] != null && ds.Tables["EventosProyectoIdentidad"].Rows.Count > 0)
                {
                    foreach (DataRow fila in ds.Tables["EventosProyectoIdentidad"].Rows)
                    {
                        UserEvent jsonEventoUsuario = new UserEvent();
                        jsonEventoUsuario.event_id = (Guid)fila["EventoID"];
                        jsonEventoUsuario.name = fila["Nombre"].ToString();
                        jsonEventoUsuario.Date = (DateTime)fila["Fecha"];
                        pListadoEventosUsuarioProyecto.Add(jsonEventoUsuario);
                    }
                }
            }
        }
        [NonAction]
        private void RellenarDatosPersona(Identidad pIdentidad, User pUsuarioJSON, string pIdioma)
        {
            bool CambiadoNombre = (pIdentidad.Persona.FilaPersona.Nombre != pUsuarioJSON.name);
            bool CambiadoApellidos = (pIdentidad.Persona.FilaPersona.Apellidos != pUsuarioJSON.last_name);

            if (!string.IsNullOrEmpty(pUsuarioJSON.name))
            {
                pIdentidad.Persona.Nombre = pUsuarioJSON.name;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.last_name))
            {
                pIdentidad.Persona.Apellidos = pUsuarioJSON.last_name;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.email))
            {
                pIdentidad.Persona.Mail = pUsuarioJSON.email;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.sex))
            {
                pIdentidad.Persona.Sexo = pUsuarioJSON.sex;
            }
            if (pUsuarioJSON.born_date != null && !pUsuarioJSON.born_date.Equals(DateTime.MinValue))
            {
                pIdentidad.Persona.Fecha = pUsuarioJSON.born_date;
            }
            if (pUsuarioJSON.country_id != null && !pUsuarioJSON.country_id.Equals(Guid.Empty))
            {
                pIdentidad.Persona.PaisID = pUsuarioJSON.country_id;
            }
            if (pUsuarioJSON.province_id != null && !pUsuarioJSON.province_id.Equals(Guid.Empty))
            {
                pIdentidad.Persona.ProvinciaID = pUsuarioJSON.province_id;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.provice))
            {
                pIdentidad.Persona.Provincia = pUsuarioJSON.provice;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.city))
            {
                pIdentidad.Persona.Localidad = pUsuarioJSON.city;
            }
            if (!string.IsNullOrEmpty(pUsuarioJSON.postal_code))
            {
                pIdentidad.Persona.CodPostal = pUsuarioJSON.postal_code;
            }

            // Modificar los tags de nombre de persona
            if (CambiadoNombre || CambiadoApellidos)
            {
                UtilIdiomas utilIdiomas = new UtilIdiomas(pIdioma, mLoggingService, mEntityContext, mConfigService);
                string profesorSexo = "";
                if (pIdentidad.Persona.Sexo.Equals("H"))
                {
                    profesorSexo = utilIdiomas.GetText("SOLICITUDESNUEVOSPROFESORES", "PROFESOR") + " · ";
                }
                else
                {
                    profesorSexo = utilIdiomas.GetText("SOLICITUDESNUEVOSPROFESORES", "PROFESORA") + " · ";
                }

                foreach (AD.EntityModel.Models.IdentidadDS.Perfil filaPerfil in pIdentidad.GestorIdentidades.DataWrapperIdentidad.ListaPerfil.Where(perfil => perfil.PersonaID.Equals(pIdentidad.PersonaID)).ToList())
                {
                    string profesor = "";
                    if (pIdentidad.GestorIdentidades.DataWrapperIdentidad.ListaProfesor.Where(item => item.PerfilID.Equals(filaPerfil.PerfilID)).Count() > 0)
                    {
                        profesor = profesorSexo;
                    }
                    filaPerfil.NombrePerfil = profesor + pIdentidad.Persona.NombreConApellidos;

                    if (CambiadoNombre)
                    {
                        foreach (AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad in filaPerfil.Identidad)
                        {
                            filaIdentidad.NombreCortoIdentidad = profesor + pIdentidad.Persona.Nombre;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Agregamos los campos extra de la solicitud a la identidadDS.
        /// </summary>
        /// <param name="pIidentidadDS">DS de identidades donde agregamos los nuevos campos.</param>
        /// <param name="pIdSolicitud">GUID de la solicitud de usuario</param>
        /// <param name="pSolicitudDW">DS con los datos de la solicitud</param>
        [NonAction]
        private void GuardarDatosExtraDeSolicitudEnIdentidad(DataWrapperIdentidad pDataWrapperIdentidad, Guid pPerfilID, Guid pIdSolicitud, DataWrapperSolicitud pSolicitudDW)
        {
            //Recorrer las tablas datoextrasolicitudproyectovirtuoso, datoextrasolicitudecosistemavirtuoso, datoextrasolicitudproyecto, datoextrasolicitudecosistema y para la solicitudID agregar los campos para el perfilID.
            foreach (DatoExtraProyectoOpcionSolicitud deevs in pSolicitudDW.ListaDatoExtraProyectoOpcionSolicitud.Where(item => item.SolicitudID.Equals(pIdSolicitud)))
            {
                AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad = pDataWrapperIdentidad.ListaIdentidad.FirstOrDefault(identidad => identidad.PerfilID.Equals(pPerfilID) && identidad.ProyectoID.Equals(deevs.ProyectoID));
                AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad datoExtraProyectoOpcionIdentidad = new AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad();
                datoExtraProyectoOpcionIdentidad.OrganizacionID = deevs.OrganizacionID;
                datoExtraProyectoOpcionIdentidad.ProyectoID = deevs.ProyectoID;
                datoExtraProyectoOpcionIdentidad.DatoExtraID = deevs.DatoExtraID;
                datoExtraProyectoOpcionIdentidad.OpcionID = deevs.OpcionID;
                datoExtraProyectoOpcionIdentidad.IdentidadID = filaIdentidad.IdentidadID;
                pDataWrapperIdentidad.ListaDatoExtraProyectoOpcionIdentidad.Add(datoExtraProyectoOpcionIdentidad);
                mEntityContext.DatoExtraProyectoOpcionIdentidad.Add(datoExtraProyectoOpcionIdentidad);
            }

            foreach (DatoExtraEcosistemaOpcionSolicitud deevs in pSolicitudDW.ListaDatoExtraEcosistemaOpcionSolicitud.Where(item => item.SolicitudID.Equals(pIdSolicitud)))
            {
                AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaOpcionPerfil datoExtraEcosistemaOpcionPerfil = new AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaOpcionPerfil();
                datoExtraEcosistemaOpcionPerfil.DatoExtraID = deevs.DatoExtraID;
                datoExtraEcosistemaOpcionPerfil.OpcionID = deevs.OpcionID;
                datoExtraEcosistemaOpcionPerfil.PerfilID = pPerfilID;
                pDataWrapperIdentidad.ListaDatoExtraEcosistemaOpcionPerfil.Add(datoExtraEcosistemaOpcionPerfil);
                mEntityContext.DatoExtraEcosistemaOpcionPerfil.Add(datoExtraEcosistemaOpcionPerfil);
            }

            foreach (DatoExtraProyectoVirtuosoSolicitud deevs in pSolicitudDW.ListaDatoExtraProyectoVirtuosoSolicitud.Where(item => item.SolicitudID.Equals(pIdSolicitud)))
            {
                AD.EntityModel.Models.IdentidadDS.Identidad filaIdentidad = pDataWrapperIdentidad.ListaIdentidad.FirstOrDefault(identidad => identidad.PerfilID.Equals(pPerfilID) && identidad.ProyectoID.Equals(deevs.ProyectoID));
                AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad datoExtraProyectoVirtuosoIdentidad = new AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad();
                datoExtraProyectoVirtuosoIdentidad.OrganizacionID = deevs.OrganizacionID;
                datoExtraProyectoVirtuosoIdentidad.ProyectoID = deevs.ProyectoID;
                datoExtraProyectoVirtuosoIdentidad.DatoExtraID = deevs.DatoExtraID;
                datoExtraProyectoVirtuosoIdentidad.Opcion = deevs.Opcion;
                datoExtraProyectoVirtuosoIdentidad.IdentidadID = filaIdentidad.IdentidadID;
                pDataWrapperIdentidad.ListaDatoExtraProyectoVirtuosoIdentidad.Add(datoExtraProyectoVirtuosoIdentidad);
                mEntityContext.DatoExtraProyectoVirtuosoIdentidad.Add(datoExtraProyectoVirtuosoIdentidad);
            }

            foreach (DatoExtraEcosistemaVirtuosoSolicitud deevs in pSolicitudDW.ListaDatoExtraEcosistemaVirtuosoSolicitud.Where(item => item.SolicitudID.Equals(pIdSolicitud)))
            {
                AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaVirtuosoPerfil datoExtraEcosistemaVirtuosoPerfil = new AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaVirtuosoPerfil();
                datoExtraEcosistemaVirtuosoPerfil.DatoExtraID = deevs.DatoExtraID;
                datoExtraEcosistemaVirtuosoPerfil.PerfilID = pPerfilID;
                datoExtraEcosistemaVirtuosoPerfil.Opcion = deevs.Opcion;
                pDataWrapperIdentidad.ListaDatoExtraEcosistemaVirtuosoPerfil.Add(datoExtraEcosistemaVirtuosoPerfil);
                mEntityContext.DatoExtraEcosistemaVirtuosoPerfil.Add(datoExtraEcosistemaVirtuosoPerfil);
            }


        }
        [NonAction]
        private void GuardarDatosExtraSolicitud(DataWrapperSolicitud pSolicitudDW, Solicitud pSolicitud, List<ExtraUserData> pDatosExtraUsuario, Guid pOrganizacionID, Guid pProyectoID, bool pEsEcosistema)
        {
            //cargar la configfuraion
            //DatoExtraEcosistemaOpcion
            //DatoExtraEcosistemaVirtuoso

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperProyecto dataWrapperProyecto = proyCN.ObtenerDatosExtraProyectoPorID(pProyectoID);
            proyCN.Dispose();


            foreach (ExtraUserData datoExtra in pDatosExtraUsuario)
            {
                if (datoExtra.name_id != null && !datoExtra.name_id.Equals(Guid.Empty))
                {
                    if (pEsEcosistema)
                    {
                        if (datoExtra.value_id != null && !datoExtra.value_id.Equals(Guid.Empty) && dataWrapperProyecto.ListaDatoExtraEcosistemaOpcion.Count(dato => dato.DatoExtraID.Equals(datoExtra.name_id) && dato.OpcionID.Equals(datoExtra.value_id)) > 0)
                        {
                            pSolicitudDW.ListaDatoExtraEcosistemaOpcionSolicitud.Add(new DatoExtraEcosistemaOpcionSolicitud { DatoExtraID = datoExtra.name_id, OpcionID = datoExtra.value_id, SolicitudID = pSolicitud.SolicitudID });
                        }
                        else if (!string.IsNullOrEmpty(datoExtra.value) && dataWrapperProyecto.ListaDatoExtraEcosistemaVirtuoso.Count(dato => dato.DatoExtraID.Equals(datoExtra.name_id)) > 0)
                        {
                            pSolicitudDW.ListaDatoExtraEcosistemaVirtuosoSolicitud.Add(new DatoExtraEcosistemaVirtuosoSolicitud { DatoExtraID = datoExtra.name_id, SolicitudID = pSolicitud.SolicitudID, Opcion = datoExtra.value });
                        }
                    }
                    else
                    {
                        if (datoExtra.value_id != null && !datoExtra.value_id.Equals(Guid.Empty) && dataWrapperProyecto.ListaDatoExtraEcosistemaOpcion.Count(dato => dato.DatoExtraID.Equals(datoExtra.name_id) && dato.OpcionID.Equals(datoExtra.value_id)) > 0)
                        {
                            pSolicitudDW.ListaDatoExtraProyectoOpcionSolicitud.Add(new DatoExtraProyectoOpcionSolicitud { OrganizacionID = pOrganizacionID, ProyectoID = pProyectoID, DatoExtraID = datoExtra.name_id, OpcionID = datoExtra.value_id, SolicitudID = pSolicitud.SolicitudID });
                        }
                        else if (!string.IsNullOrEmpty(datoExtra.value) && dataWrapperProyecto.ListaDatoExtraEcosistemaVirtuoso.Count(dato => dato.DatoExtraID.Equals(datoExtra.name_id)) > 0)
                        {
                            pSolicitudDW.ListaDatoExtraProyectoVirtuosoSolicitud.Add(new DatoExtraProyectoVirtuosoSolicitud { OrganizacionID = pOrganizacionID, ProyectoID = pProyectoID, DatoExtraID = datoExtra.name_id, SolicitudID = pSolicitud.SolicitudID, Opcion = datoExtra.value });
                        }
                    }
                }
            }
        }
        [NonAction]
        private void GuardarDatosExtra(List<ExtraUserData> pDatosExtraUsuario, Identidad pIdentidad, Dictionary<int, string> pDicDatosExtraProyectoVirtuoso, Dictionary<int, string> pDicDatosExtraEcosistemaVirtuoso, bool pEsEcosistema)
        {
            DataWrapperIdentidad dataWrapperIdentidad = pIdentidad.GestorIdentidades.DataWrapperIdentidad;
            ProyectoCN proyectoCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperProyecto datosExtraProyectoDWP = proyectoCN.ObtenerDatosExtraProyectoPorID(pIdentidad.FilaIdentidad.ProyectoID);
            proyectoCN.Dispose();

            if (pEsEcosistema)
            {
                foreach (AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaOpcionPerfil filaDatoExtraEcosistema in dataWrapperIdentidad.ListaDatoExtraEcosistemaOpcionPerfil)
                {
                    if (filaDatoExtraEcosistema.PerfilID == pIdentidad.PerfilID)
                    {
                        mEntityContext.Entry(filaDatoExtraEcosistema).State = EntityState.Deleted;
                    }
                }

                foreach (AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaVirtuosoPerfil filaDatoExtraEcosistemaVirtuoso in dataWrapperIdentidad.ListaDatoExtraEcosistemaVirtuosoPerfil)
                {
                    if (filaDatoExtraEcosistemaVirtuoso.PerfilID == pIdentidad.PerfilID)
                    {
                        mEntityContext.Entry(filaDatoExtraEcosistemaVirtuoso).State = EntityState.Deleted;
                    }
                }
            }
            else
            {
                foreach (AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad filaDatoExtra in dataWrapperIdentidad.ListaDatoExtraProyectoOpcionIdentidad)
                {
                    if (filaDatoExtra.IdentidadID == pIdentidad.Clave)
                    {
                        mEntityContext.Entry(filaDatoExtra).State = EntityState.Deleted;
                    }
                }

                foreach (AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad filaDatoExtraVirtuoso in dataWrapperIdentidad.ListaDatoExtraProyectoVirtuosoIdentidad)
                {
                    if (filaDatoExtraVirtuoso.IdentidadID == pIdentidad.Clave)
                    {
                        mEntityContext.Entry(filaDatoExtraVirtuoso).State = EntityState.Deleted;
                    }
                }
            }

            Dictionary<Guid, Guid> dicDatosExtraProyecto = new Dictionary<Guid, Guid>();
            Dictionary<Guid, Guid> dicDatosExtraEcosistema = new Dictionary<Guid, Guid>();

            if (pDatosExtraUsuario != null)
            {
                foreach (ExtraUserData campoExtra in pDatosExtraUsuario)
                {
                    string nombreCampo = campoExtra.name;
                    Guid nombreID = campoExtra.name_id;
                    string valorCampo = campoExtra.value;
                    Guid valorID = campoExtra.value_id;

                    if (nombreID != null && !nombreID.Equals(Guid.Empty))
                    {
                        AD.EntityModel.Models.ProyectoDS.DatoExtraEcosistema filaDatoExtraEcosistema = datosExtraProyectoDWP.ListaDatoExtraEcosistema.FirstOrDefault(dato => dato.DatoExtraID.Equals(nombreID));
                        if (pEsEcosistema && filaDatoExtraEcosistema != null)
                        {
                            if (valorID != null && !valorID.Equals(Guid.Empty))
                            {
                                dicDatosExtraEcosistema.Add(filaDatoExtraEcosistema.DatoExtraID, valorID);
                            }
                        }
                        else
                        {
                            AD.EntityModel.Models.ProyectoDS.DatoExtraProyecto filaDatoExtraProyecto = datosExtraProyectoDWP.ListaDatoExtraProyecto.FirstOrDefault(dato => dato.OrganizacionID.Equals(pIdentidad.FilaIdentidad.OrganizacionID) && dato.ProyectoID.Equals(pIdentidad.FilaIdentidad.ProyectoID) && dato.DatoExtraID.Equals(nombreID));
                            if (filaDatoExtraProyecto != null && valorID != null && !valorID.Equals(Guid.Empty))
                            {
                                dicDatosExtraProyecto.Add(filaDatoExtraProyecto.DatoExtraID, valorID);
                            }
                        }

                        AD.EntityModel.Models.ProyectoDS.DatoExtraEcosistemaVirtuoso filasDatoExtraEcosistemaVirtuoso = datosExtraProyectoDWP.ListaDatoExtraEcosistemaVirtuoso.FirstOrDefault(dato => dato.DatoExtraID.Equals(nombreID));
                        if (pEsEcosistema && filasDatoExtraEcosistemaVirtuoso != null)
                        {
                            pDicDatosExtraEcosistemaVirtuoso.Add(filasDatoExtraEcosistemaVirtuoso.Orden, valorCampo);
                        }
                        else
                        {
                            AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoVirtuoso filasDatoExtraProyectoVirtuoso = datosExtraProyectoDWP.ListaDatoExtraProyectoVirtuoso.FirstOrDefault(dato => dato.OrganizacionID.Equals(pIdentidad.FilaIdentidad.OrganizacionID) && dato.ProyectoID.Equals(pIdentidad.FilaIdentidad.ProyectoID) && dato.DatoExtraID.Equals(nombreID));
                            if (filasDatoExtraProyectoVirtuoso != null)
                            {
                                pDicDatosExtraProyectoVirtuoso.Add(filasDatoExtraProyectoVirtuoso.Orden, valorCampo);
                            }
                        }
                    }
                }
            }

            if (pEsEcosistema)
            {
                foreach (Guid datoExtra in dicDatosExtraEcosistema.Keys)
                {
                    AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaOpcionPerfil datoExtraEcosistemaOpcionPerfil = new AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaOpcionPerfil();
                    datoExtraEcosistemaOpcionPerfil.DatoExtraID = datoExtra;
                    datoExtraEcosistemaOpcionPerfil.DatoExtraID = dicDatosExtraEcosistema[datoExtra];
                    datoExtraEcosistemaOpcionPerfil.PerfilID = pIdentidad.PerfilUsuario.FilaPerfil.PerfilID;
                    dataWrapperIdentidad.ListaDatoExtraEcosistemaOpcionPerfil.Add(datoExtraEcosistemaOpcionPerfil);
                    mEntityContext.DatoExtraEcosistemaOpcionPerfil.Add(datoExtraEcosistemaOpcionPerfil);
                }

                foreach (int orden in pDicDatosExtraEcosistemaVirtuoso.Keys)
                {
                    if (!string.IsNullOrEmpty(pDicDatosExtraEcosistemaVirtuoso[orden].Trim()) && pDicDatosExtraEcosistemaVirtuoso[orden].Trim() != "|")
                    {
                        string valor = pDicDatosExtraEcosistemaVirtuoso[orden].Trim();
                        if (valor.EndsWith("|"))
                        {
                            valor = valor.Substring(0, valor.Length - 1);
                        }

                        valor = IntentoObtenerElPais(valor);

                        AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaVirtuosoPerfil datoExtraEcosistemaVirtuosoPerfil = new AD.EntityModel.Models.IdentidadDS.DatoExtraEcosistemaVirtuosoPerfil();
                        datoExtraEcosistemaVirtuosoPerfil.DatoExtraID = datosExtraProyectoDWP.ListaDatoExtraEcosistemaVirtuoso.FirstOrDefault(dato => dato.Orden.Equals(orden)).DatoExtraID;
                        datoExtraEcosistemaVirtuosoPerfil.PerfilID = pIdentidad.PerfilUsuario.FilaPerfil.PerfilID;
                        datoExtraEcosistemaVirtuosoPerfil.Opcion = valor;
                        dataWrapperIdentidad.ListaDatoExtraEcosistemaVirtuosoPerfil.Add(datoExtraEcosistemaVirtuosoPerfil);
                        mEntityContext.DatoExtraEcosistemaVirtuosoPerfil.Add(datoExtraEcosistemaVirtuosoPerfil);
                    }
                }
            }
            else
            {
                foreach (Guid datoExtra in dicDatosExtraProyecto.Keys)
                {
                    AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad datoExtraProyectoOpcionIdentidad = new AD.EntityModel.Models.ProyectoDS.DatoExtraProyectoOpcionIdentidad();
                    datoExtraProyectoOpcionIdentidad.OrganizacionID = pIdentidad.FilaIdentidad.OrganizacionID;
                    datoExtraProyectoOpcionIdentidad.ProyectoID = pIdentidad.FilaIdentidad.ProyectoID;
                    datoExtraProyectoOpcionIdentidad.DatoExtraID = datoExtra;
                    datoExtraProyectoOpcionIdentidad.OpcionID = dicDatosExtraProyecto[datoExtra];
                    datoExtraProyectoOpcionIdentidad.IdentidadID = pIdentidad.FilaIdentidad.IdentidadID;
                    mEntityContext.DatoExtraProyectoOpcionIdentidad.Add(datoExtraProyectoOpcionIdentidad);
                    dataWrapperIdentidad.ListaDatoExtraProyectoOpcionIdentidad.Add(datoExtraProyectoOpcionIdentidad);
                }

                foreach (int orden in pDicDatosExtraProyectoVirtuoso.Keys)
                {
                    if (!string.IsNullOrEmpty(pDicDatosExtraProyectoVirtuoso[orden].Trim()) && pDicDatosExtraProyectoVirtuoso[orden].Trim() != "|")
                    {
                        string valor = pDicDatosExtraProyectoVirtuoso[orden].Trim();
                        if (valor.EndsWith("|"))
                        {
                            valor = valor.Substring(0, valor.Length - 1);
                        }

                        valor = IntentoObtenerElPais(valor);

                        AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad datoExtraProyectoVirtuosoIdentidad = new AD.EntityModel.Models.IdentidadDS.DatoExtraProyectoVirtuosoIdentidad();
                        datoExtraProyectoVirtuosoIdentidad.OrganizacionID = pIdentidad.FilaIdentidad.OrganizacionID;
                        datoExtraProyectoVirtuosoIdentidad.ProyectoID = pIdentidad.FilaIdentidad.ProyectoID;
                        datoExtraProyectoVirtuosoIdentidad.DatoExtraID = datosExtraProyectoDWP.ListaDatoExtraProyectoVirtuoso.FirstOrDefault(dato => dato.Orden == orden).DatoExtraID;
                        datoExtraProyectoVirtuosoIdentidad.Opcion = valor;
                        datoExtraProyectoVirtuosoIdentidad.IdentidadID = pIdentidad.FilaIdentidad.IdentidadID;
                        mEntityContext.DatoExtraProyectoVirtuosoIdentidad.Add(datoExtraProyectoVirtuosoIdentidad);
                        dataWrapperIdentidad.ListaDatoExtraProyectoVirtuosoIdentidad.Add(datoExtraProyectoVirtuosoIdentidad);
                    }
                }
            }
        }
        [NonAction]
        private void EditarSuscripciones(List<Guid> pListacategorias, Identidad pIdentidad)
        {
            SuscripcionCN suscCN = new SuscripcionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperSuscripcion suscDS = suscCN.ObtenerSuscripcionesDePerfil(pIdentidad.FilaIdentidad.PerfilID, false);
            GestionSuscripcion gestorSuscripciones = new GestionSuscripcion(suscDS, mLoggingService, mEntityContext);
            Suscripcion suscripcion = gestorSuscripciones.ObtenerSuscripcionAProyecto(pIdentidad.FilaIdentidad.ProyectoID);

            TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            GestionTesauro gestorTesauro = new GestionTesauro(tesauroCL.ObtenerTesauroDeProyecto(pIdentidad.FilaIdentidad.ProyectoID), mLoggingService, mEntityContext);
            gestorTesauro.CargarCategorias();
            tesauroCL.Dispose();

            if (suscripcion == null)
            {
                Guid suscripcionid = gestorSuscripciones.AgregarNuevaSuscripcion(pIdentidad, 1);
                gestorSuscripciones.CrearSuscripcionTesauroProyecto(suscripcionid, pIdentidad.FilaIdentidad.OrganizacionID, pIdentidad.FilaIdentidad.ProyectoID, gestorTesauro.TesauroActualID);

                suscripcion = gestorSuscripciones.ObtenerSuscripcionAProyecto(pIdentidad.FilaIdentidad.ProyectoID);
            }

            if (suscripcion != null)
            {
                if (suscripcion.FilasCategoriasVinculadas != null)
                {
                    foreach (AD.EntityModel.Models.Suscripcion.CategoriaTesVinSuscrip filaCat in suscripcion.FilasCategoriasVinculadas.ToList())
                    {
                        if (!pListacategorias.Contains(filaCat.CategoriaTesauroID))
                        {
                            //Si tengo una categoría que no está en el selector la quito del DataWrapper
                            suscripcion.FilasCategoriasVinculadas.Remove(filaCat);
                            mEntityContext.CategoriaTesVinSuscrip.Remove(filaCat);
                        }
                        else
                        {
                            if (!pListacategorias.Contains(filaCat.CategoriaTesauroID))
                            {
                                //Si la categoría está en el selector, en el DataWrapper y no está seleccionada, la elimino de la lista para dejar sólo las añadidas
                                pListacategorias.Remove(filaCat.CategoriaTesauroID);
                            }
                        }
                    }
                }
                foreach (Guid catID in pListacategorias)
                {
                    //Ya sólo quedan categorías añadidas, así que añado las filas
                    suscripcion.GestorSuscripcion.VincularCategoria(suscripcion, gestorTesauro.ListaCategoriasTesauro[catID]);
                }

                suscripcion.FilaSuscripcion.Periodicidad = (short)PeriodicidadSuscripcion.NoEnviar;
                suscCN.ActualizarSuscripcion();
            }
            suscCN.Dispose();
        }
        [NonAction]
        private string IntentoObtenerElPais(string pValor)
        {
            PaisCL paisCL = new PaisCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperPais paisDW = paisCL.ObtenerPaisesProvincias();

            foreach (Pais fila in paisDW.ListaPais)
            {
                if (fila.PaisID.ToString() == pValor)
                {
                    pValor = fila.Nombre;
                }
            }

            paisCL.Dispose();

            return pValor;
        }
        [NonAction]
        private void EliminarCaches(Identidad pIdentidad)
        {
            //TODO : Si se cambia la foto de una organización, hay que pedirle al servicio de refresco de cache que borre las caches de los miembros de la organización
            //TODO : Si se cambia algun dato de la organizacion, hay que pedirle al servicio de refresco de cache que borre las caches de los miembros de la organización

            List<string> listaClavesInvalidar = new List<string>();

            IdentidadCL identidadCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);

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

            foreach (Guid perfilID in pIdentidad.GestorIdentidades.ListaPerfiles.Keys)
            {
                string rawKey = string.Concat("IdentidadActual_", pIdentidad.PersonaID, "_", perfilID);
                string rawKey2 = "PerfilMVC_" + perfilID;
                listaClavesInvalidar.Add(prefijoClave + rawKey.ToLower());
                listaClavesInvalidar.Add(prefijoClave + rawKey2.ToLower());
            }

            foreach (Guid identidadID in pIdentidad.GestorIdentidades.ListaIdentidades.Keys)
            {
                string rawKey3 = "FichaIdentidadMVC_" + identidadID;
                listaClavesInvalidar.Add(prefijoClave + rawKey3.ToLower());
            }

            identidadCL.InvalidarCachesMultiples(listaClavesInvalidar);
            identidadCL.Dispose();
        }
        [NonAction]
        private string GenerarLoginUsuario(string pNombre, string pApellidos, ref int pHashNumUsu)
        {
            string loginUsuario = UtilCadenas.LimpiarCaracteresNombreCortoRegistro(pNombre) + '-' + UtilCadenas.LimpiarCaracteresNombreCortoRegistro(pApellidos);
            if (loginUsuario.Length > 12)
            {
                loginUsuario = loginUsuario.Substring(0, 12);
            }
            UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            if (usuarioCN.ExisteUsuarioEnBD(loginUsuario))
            {
                loginUsuario = usuarioCN.ObtenerLoginLibre(loginUsuario);
            }

            //Antes de la migracion del V2 Incidencia LRE-145
            //while (usuarioCN.ExisteUsuarioEnBD(loginUsuario))
            //{
            //    loginUsuario = loginUsuario.Substring(0, loginUsuario.Length - pHashNumUsu.ToString().Length) + pHashNumUsu.ToString();
            //    pHashNumUsu++;
            //}
            usuarioCN.Dispose();

            return loginUsuario;
        }

        //Antes de la migracion del V2 Incidencia LRE-145
        //private string GenerarNombreCortoUsuario(string pLoginUsuario, ref int hashNumUsu)
        //{
        //    UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService);
        //    string nombreCortoUsuario = pLoginUsuario;
        //    while (usuarioCN.ExisteNombreCortoEnBD(nombreCortoUsuario))
        //    {
        //        nombreCortoUsuario = nombreCortoUsuario.Substring(0, nombreCortoUsuario.Length - hashNumUsu.ToString().Length) + hashNumUsu.ToString();
        //        hashNumUsu++;
        //    }
        //    usuarioCN.Dispose();
        //    return nombreCortoUsuario;
        //}
        [NonAction]
        public string GenerarNombreCortoUsuario(ref string pLoginUsuario, string pNombre, string pApellidos, DataWrapperUsuario pDataWrapperUsuario, int pIntentos = 0)
        {
            UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            string nombreCortoUsuario = pLoginUsuario;
            int hashNumUsu = 1;
            if (usuarioCN.ExisteNombreCortoEnBD(nombreCortoUsuario))
            {
                // El login está siendo usado como nombrecorto, le busco uno a partir del nombre y apellidos 
                string loginUsuario = UtilCadenas.LimpiarCaracteresNombreCortoRegistro(pNombre) + '-' + UtilCadenas.LimpiarCaracteresNombreCortoRegistro(pApellidos);
                if (loginUsuario.Length > 12)
                {
                    loginUsuario = loginUsuario.Substring(0, 12);
                }

                nombreCortoUsuario = usuarioCN.ObtenerNombreCortoLibre(loginUsuario);
                // Establezco el mismo login que nombre corto. Se ya existía el nombre corto, lo más probable es que también exista el login
                pLoginUsuario = nombreCortoUsuario;
            }
            Usuario filaUsuario = new Usuario();
            filaUsuario.UsuarioID = Guid.NewGuid();
            filaUsuario.Login = pLoginUsuario;
            filaUsuario.Password = "";
            filaUsuario.EstaBloqueado = true;
            filaUsuario.NombreCorto = nombreCortoUsuario;
            filaUsuario.Version = 1;
            filaUsuario.FechaCambioPassword = DateTime.Now;
            filaUsuario.Validado = (short)ValidacionUsuario.Verificado; ;
            mEntityContext.Usuario.Add(filaUsuario);
            try
            {
                //Reservo el nombrecorto del usuario
                usuarioCN.GuardarActualizaciones(pDataWrapperUsuario);

                // Marco la fila como eliminada para que al crear el usuario de verdad, primero elimine la fila de reserva del nombrecorto
                mEntityContext.Entry(filaUsuario).State = EntityState.Deleted;
            }
            catch (Exception ex)
            {
                if (pIntentos < 10)
                {
                    if (pIntentos > 2)
                    {
                        // Hay más de un proceso intentando registrar al mismo usuario, espero un número aleatorio de segundos para desbloquear la situación
                        Random rnd = new Random();
                        int sleepTime = rnd.Next(1, 10);
                        Thread.Sleep(sleepTime * 1000);
                    }
                    mLoggingService.GuardarLogError(ex, $"Ha fallado la creación del usuario {pLoginUsuario} con nombre corto {nombreCortoUsuario}. Le buscamos otro nombre");

                    pLoginUsuario = GenerarLoginUsuario(pNombre, pApellidos, ref hashNumUsu);
                    mEntityContext.Entry(filaUsuario).State = EntityState.Deleted;
                    nombreCortoUsuario = GenerarNombreCortoUsuario(ref pLoginUsuario, pNombre, pApellidos, pDataWrapperUsuario, pIntentos + 1);
                }
                else
                {
                    // Si en 10 intentos no hemos sido capaces de crear el usuario, lo dejamos por imposible y devolvemos error.
                    throw;
                }
            }
            usuarioCN.Dispose();
            return nombreCortoUsuario;
        }
        [NonAction]
        private string ObtenerClausulasAdicionales(Guid pProyectoID, List<ExtraUserData> pDatosExtraUsuario)
        {
            string calusulasAdicionales = "";
            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            DataWrapperUsuario usuClauProyDW = proyCL.ObtenerClausulasRegitroProyecto(pProyectoID);
            proyCL.Dispose();

            List<ProyRolUsuClausulaReg> filasProyRolClau = usuClauProyDW.ListaProyRolUsuClausulaReg.Where(item => item.ProyectoID.Equals(pProyectoID)).ToList();
            foreach (ExtraUserData datoextra in pDatosExtraUsuario)
            {
                foreach (ClausulaRegistro filaClausula in usuClauProyDW.ListaClausulaRegistro.Where(item => item.Tipo.Equals((short)TipoClausulaAdicional.Opcional)))
                {
                    if (datoextra.name_id.Equals(filaClausula.ClausulaID))
                    {
                        calusulasAdicionales += filaClausula.ClausulaID + ", ";
                        break;
                    }
                }
            }
            return calusulasAdicionales;
        }
        [NonAction]
        private void ValidarNombresProyectosYOrganizacion(Guid pOrganizacionID, List<string> pNombresCortosProyectos, out List<Guid> pListaProyectosID)
        {
            pListaProyectosID = new List<Guid>();
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            OrganizacionCN orgCN = new OrganizacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            foreach (string proyecto in pNombresCortosProyectos)
            {
                Guid proyectoID = proyCN.ObtenerProyectoIDPorNombre(proyecto);
                if (!proyectoID.Equals(Guid.Empty))
                {
                    bool participacion = orgCN.ParticipaOrganizacionEnProyecto(proyectoID, pOrganizacionID);
                    if (participacion && !pListaProyectosID.Contains(proyectoID))
                    {
                        if (!pListaProyectosID.Contains(proyectoID))
                        {
                            pListaProyectosID.Add(proyectoID);
                        }
                    }
                    else
                    {
                        throw new GnossException("La organización no participa en el proyecto", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    throw new GnossException("La comunidad no existe", HttpStatusCode.BadRequest);
                }
            }
        }
        [NonAction]
        private string ValidacionNombresGruposYOrganizacion(Guid pOrganizacionID, List<string> pNombresCortosGrupos, Dictionary<string, Guid> dicGrupos)
        {
            string error = string.Empty;
            IdentidadCN identCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            foreach (string grupo in pNombresCortosGrupos)
            {
                List<string> listaGrupos = new List<string>();
                listaGrupos.Add(grupo);
                List<Guid> gruposID = identCN.ObtenerGruposIDPorNombreCortoYOrganizacion(listaGrupos, pOrganizacionID);

                if (gruposID.Count > 0)
                {
                    if (!dicGrupos.ContainsKey(grupo))
                    {
                        dicGrupos.Add(grupo, gruposID[0]);
                    }
                }
                else
                {
                    error += "\r\n ERROR: El grupo " + grupo + " pasado como parámetro en NombresCortosGrupos no pertenece a la organización.";
                }
            }

            return error;
        }
        [NonAction]
        private void AgregarParticipantesGrupoOrganizacion(Guid pOrganizacionID, List<Guid> pListaParticipantes, GrupoIdentidades pGrupo)
        {
            IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            Dictionary<Guid, string> listaNombresParticipantes = identidadCN.ObtenerNombresDeIdentidades(pListaParticipantes);

            List<Guid> listaNuevasIdentidades = new List<Guid>();

            StringBuilder sb = new StringBuilder();

            foreach (Guid identidad in pListaParticipantes)
            {
                if (!pGrupo.Participantes.ContainsKey(identidad))
                {
                    AD.EntityModel.Models.IdentidadDS.GrupoIdentidadesParticipacion filaGrupoIdentidadesParticipacion = new AD.EntityModel.Models.IdentidadDS.GrupoIdentidadesParticipacion();

                    filaGrupoIdentidadesParticipacion.GrupoID = pGrupo.Clave;
                    filaGrupoIdentidadesParticipacion.IdentidadID = identidad;
                    filaGrupoIdentidadesParticipacion.FechaAlta = DateTime.Now;

                    pGrupo.GestorIdentidades.DataWrapperIdentidad.ListaGrupoIdentidadesParticipacion.Add(filaGrupoIdentidadesParticipacion);
                    mEntityContext.GrupoIdentidadesParticipacion.Add(filaGrupoIdentidadesParticipacion);

                    sb.Append(FacetadoAD.GenerarTripleta("<http://gnoss/" + pGrupo.Clave.ToString().ToUpper() + ">", "<http://gnoss/hasparticipanteID>", "<http://gnoss/" + identidad.ToString().ToUpper() + ">"));

                    listaNuevasIdentidades.Add(identidad);
                }
            }

            identidadCN.ActualizaIdentidades();

            FacetadoCN facetadoCN = new FacetadoCN(UrlIntragnoss, mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            facetadoCN.InsertaTripletas(ProyectoAD.MyGnoss.ToString(), sb.ToString(), 0, false);
            facetadoCN.Dispose();

            ////Notificamos a los usuarios de que han sido agregados al grupo.
            //EnviarMensajeMiembros(listaNuevasIdentidades, Grupo.Nombre, Grupo.NombreCorto, false);

            //ControladorGrupos.ActualizarBase(ProyectoAD.MetaProyecto, pGrupo.Clave);

            List<Guid> perfilesDeIdentidadesNuevas = identidadCN.ObtenerPerfilesDeIdentidades(listaNuevasIdentidades);

            FacetadoCL facetadoCL = new FacetadoCL(UrlIntragnoss, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            foreach (Guid perfilID in perfilesDeIdentidadesNuevas)
            {
                facetadoCL.InvalidarCacheQueContengaCadena(NombresCL.PRIMEROSRECURSOS + "_" + ProyectoAD.MyGnoss.ToString() + "_" + perfilID);
            }

            facetadoCL.Dispose();
            identidadCN.Dispose();

            IdentidadCL identidadCL = new IdentidadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            identidadCL.InvalidarCacheMiembrosOrganizacionParaFiltroGrupos(pOrganizacionID);
            identidadCL.InvalidarCacheGrupoPorNombreCortoYOrganizacion(pGrupo.NombreCorto, pOrganizacionID);
            identidadCL.Dispose();
        }
        [NonAction]
        private void AgregarParticipanteComunidadesParticipaGrupo(Guid pGrupoID, Guid pUsuarioID, GestionIdentidades pGestorIdentidades, Identidad pIdentidad, string pNombreCortoOrg)
        {
            //damos de alta al usuario en las comunidades en las que participan los grupos de organización en los que se va a añadir
            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            List<GrupoOrgParticipaProy> listaProysParticipaGr = proyCN.ObtenerProyectosParticipaGrupoOrganizacion(pGrupoID);

            if (listaProysParticipaGr != null && listaProysParticipaGr.Count > 0)
            {
                Dictionary<Guid, bool> recibirNewsletterDefectoProyectos = proyCN.ObtenerProyectosConConfiguracionNewsletterPorDefecto();
                List<Guid> idsProyectosGrupo = listaProysParticipaGr.Select(fila => fila.ProyectoID).ToList();
                GestionProyecto gestorProyecto = new GestionProyecto(proyCN.ObtenerProyectosPorID(idsProyectosGrupo), mLoggingService, mEntityContext);
                gestorProyecto.CargarGestor();

                foreach (GrupoOrgParticipaProy fila in listaProysParticipaGr)
                {
                    if (!usuCN.EstaUsuarioEnProyecto(pUsuarioID, fila.ProyectoID))
                    {
                        Proyecto proyecto = gestorProyecto.ListaProyectos[fila.ProyectoID];
                        RegistrarUsuarioEnComunidad(UsuarioOAuth, pGestorIdentidades, pIdentidad, proyecto, pUsuarioID, pNombreCortoOrg, (TiposIdentidad)fila.TipoPerfil, false, recibirNewsletterDefectoProyectos);

                        try
                        {
                            DataWrapperProyecto dataWrapperProyecto = proyCN.ObtenerAccionesExternasProyectoPorProyectoID(fila.ProyectoID);
                            ControladorIdentidades.AccionEnServicioExternoProyecto(TipoAccionExterna.Registro, pGestorIdentidades.GestorPersonas.ListaPersonas[pIdentidad.PersonaID.Value], fila.ProyectoID, pIdentidad.Clave, "", "", pIdentidad.FilaIdentidad.FechaAlta, dataWrapperProyecto);
                        }
                        catch (Exception ex)
                        {
                            mLoggingService.GuardarLogError(ex, "\r\n ERROR: AltaUsuarioGrupoOrganizacion. Error en llamada a acciones externas");
                        }
                    }
                }
            }

            usuCN.Dispose();
            proyCN.Dispose();
        }

        /// <summary>
        /// Change email a user
        /// </summary>
        /// <param name="user_id">User's identificator</param>
        /// <param name="email">Email to change</param>
        /// <example>POST community/block-member</example>
        private void CambiarEmailUser(Guid user_id, string email)
        {
            try
            {
                if (!user_id.Equals(Guid.Empty))
                {
                    //es administrador quien realiza la petición
                    if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                    {
                        DataWrapperPersona dataWrapperPersona = new DataWrapperPersona();
                        PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        dataWrapperPersona = personaCN.ObtenerPersonaPorUsuario(user_id);


                        AD.EntityModel.Models.PersonaDS.Persona filaPersona = dataWrapperPersona?.ListaPersona.Find(persona => persona.UsuarioID.Equals(user_id));

                        if (filaPersona != null)
                        {
                            if (UtilCadenas.ValidarEmail(email))
                            {
                                filaPersona.Email = email;
                                personaCN.ActualizarPersonas(dataWrapperPersona);
                            }
                            else
                            {
                                throw new GnossException("The email doesn't valid", HttpStatusCode.BadRequest);
                            }
                        }
                        else
                        {
                            throw new GnossException("The user doesn't exists", HttpStatusCode.BadRequest);
                        }

                        personaCN.Dispose();
                    }
                    else
                    {
                        throw new GnossException("The oauth user does not have permission in the community", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw new GnossException("The user ID can not be empty", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw new GnossException("Unexpected error. Try it again later. ", HttpStatusCode.InternalServerError);
            }
        }

        [NonAction]
        private PermisosPaginasUsuarios CrearFilaPermisosPaginasUsuarios(TipoPaginaAdministracion pPagina, Guid pUsuarioID, Guid pOrganizacionID, Guid pProyectoID)
        {
            PermisosPaginasUsuarios filaUsuario = new PermisosPaginasUsuarios();
            filaUsuario.Pagina = (short)pPagina;
            filaUsuario.UsuarioID = pUsuarioID;
            filaUsuario.OrganizacionID = pOrganizacionID;
            filaUsuario.ProyectoID = pProyectoID;
            return filaUsuario;
        }
        [NonAction]
        private bool CambiarRolUsuarioEnProyecto(Guid pUsuarioId, Guid pProyectoId, TipoRolUsuario pRol)
        {
            bool cambiadoRol = false;
            IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            Guid identidadID = identidadCN.ObtenerIdentidadUsuarioEnProyecto(pUsuarioId, pProyectoId);
            List<Guid> listaIdentidades = new List<Guid>();
            listaIdentidades.Add(identidadID);
            GestionIdentidades gestorIdentidades = new GestionIdentidades(identidadCN.ObtenerIdentidadPorID(identidadID, true), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);

            if (gestorIdentidades.ListaIdentidades.ContainsKey(identidadID))
            {
                Identidad identidad = gestorIdentidades.ListaIdentidades[identidadID];
                gestorIdentidades.GestorPersonas = new GestionPersonas(personaCN.ObtenerPersonaPorID(identidad.PersonaID.Value), mLoggingService, mEntityContext);
                DataWrapperUsuario dataWrapperUsuario = new DataWrapperUsuario();
                dataWrapperUsuario.ListaUsuario = usuarioCN.ObtenerUsuariosPorIdentidadesCargaLigera(listaIdentidades);
                gestorIdentidades.GestorUsuarios = new GestionUsuarios(dataWrapperUsuario, mLoggingService, mEntityContext, mConfigService);
                gestorIdentidades.GestorPersonas.CargarGestor();
                gestorIdentidades.GestorUsuarios.CargarGestor();

                cambiadoRol = ControladorIdentidades.CambiarRolUsuarioEnProyecto(identidad, (short)pRol);
            }
            else
            {
                mLoggingService.GuardarLogError("The user isn't a community member");
            }

            usuarioCN.Dispose();
            personaCN.Dispose();
            identidadCN.Dispose();
            return cambiadoRol;
        }
        [NonAction]
        private string ObtenerUsuarioLoginRedSocial(Guid pUsuarioId, string pTipoRedSocial)
        {
            UsuarioCN usuCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

            TipoRedSocialLogin tipoRedSocial;
            if (!Enum.TryParse(pTipoRedSocial, true, out tipoRedSocial))
            {
                tipoRedSocial = TipoRedSocialLogin.Otros;
            }

            return usuCN.ObtenerLoginEnRedSocialPorUsuarioId(tipoRedSocial, pUsuarioId);
        }

        #endregion

    }
}
