using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.EntityModelBASE.Models;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.BASE_BD;
using Es.Riam.Gnoss.Logica.Notificacion;
using Es.Riam.Gnoss.Logica.Parametro;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models;
using Es.Riam.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{

    /// <summary>
    /// Use it to send notification to your users
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControlApiGnossBase
    {
        public NotificationController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {

        }

        /// <summary>
        /// Send an e-mail notification
        /// </summary>
        /// <param name="parameters">Notification parameters</param>
        /// <example>POST notification/send-email</example>
        [HttpPost, Route("send-email")]
        public void EnviarNotificacionAUsuariosPorEmail(NotificationModel parameters)
        {
            try
            {
                if (!string.IsNullOrEmpty(parameters.subject) && !string.IsNullOrEmpty(parameters.message))
                {
                    if (EsAdministradorProyectoMyGnoss(UsuarioOAuth))
                    {
                        List<string> listaEmails = new List<string>();
                        List<string> noEncontrados = new List<string>();

                        foreach (string destinatario in parameters.receivers)
                        {
                            Guid destinatarioID = Guid.Empty;
                            if (Guid.TryParse(destinatario, out destinatarioID))
                            {
                                UsuarioCN usuarioCN = new UsuarioCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                if (usuarioCN.ExisteUsuarioEnBD(destinatarioID))
                                {
                                    PersonaCN personaCN = new PersonaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                                    string email = personaCN.ObtenerEmailPersonalPorUsuario(destinatarioID);
                                    if (!string.IsNullOrEmpty(email))
                                    {
                                        if (!listaEmails.Contains(email))
                                        {
                                            listaEmails.Add(email);
                                        }
                                    }
                                    else if (!noEncontrados.Contains(destinatario))
                                    {
                                        noEncontrados.Add(destinatario);
                                    }
                                    personaCN.Dispose();
                                }
                                else if (!noEncontrados.Contains(destinatario))
                                {
                                    noEncontrados.Add(destinatario);
                                }
                                usuarioCN.Dispose();
                            }
                            else if (UtilCadenas.ValidarEmail(destinatario))
                            {
                                if (!string.IsNullOrEmpty((string)destinatario) && !listaEmails.Contains((string)destinatario))
                                {
                                    listaEmails.Add((string)destinatario);
                                }
                            }
                            else if (!noEncontrados.Contains(destinatario))
                            {
                                noEncontrados.Add(destinatario);
                            }
                        }

                        Guid proyectoID = ProyectoAD.MyGnoss;

                        if (!string.IsNullOrEmpty(parameters.community_short_name))
                        {
                            ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                            proyectoID = proyCL.ObtenerProyectoIDPorNombreCorto(parameters.community_short_name);

                            if (proyectoID.Equals(Guid.Empty))
                            {
                                proyectoID = ProyectoAD.MyGnoss;
                            }
                        }

                        if (parameters.transmitter_mail_configuration == null)
                        {
                            EnviarNotificacion(listaEmails, parameters.subject, parameters.message, parameters.is_html, parameters.sender_mask, proyectoID);
                        }
                        else
                        {
                            EnviarNotificacionSMTPDefinido(listaEmails, parameters.subject, parameters.message, parameters.is_html, parameters.sender_mask, proyectoID, parameters.transmitter_mail_configuration);
                        }

                    }
                    else
                    {
                        throw new GnossException("The oauth user does not have permission in the community", HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw new GnossException("The requested parameters can not be empty", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw;
            }
        }

        //Creacion metodo
        [HttpGet, ActionName("mail-state")]
        public MailStateModel ComprobarEstadoCorreo(int mail_id)
        {
            try
            {
                NotificacionCN notificacionCN = new NotificacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                MailStateModel mailStateModel = new MailStateModel();
                List<string> listaEmailsPendientes = new List<string>();
                List<string> listaEmailsErroneos = new List<string>();
                List<ColaCorreoDestinatario> listaResultado = notificacionCN.ObtenerColaCorreoDestinatariosPorCorreoID(mail_id);

                if (listaResultado.Count == 0)
                {
                    return mailStateModel;
                }

                listaEmailsPendientes = listaResultado.Where(item => item.Estado == 0).Select(item => item.Email).ToList();
                if (listaEmailsPendientes.Count > 0)
                {
                    mailStateModel.pending_mails.AddRange(listaEmailsPendientes);
                }

                listaEmailsErroneos = listaResultado.Where(item => item.Estado == 2).Select(item => item.Email).ToList();
                if (listaEmailsErroneos.Count > 0)
                {
                    mailStateModel.error_mails.AddRange(listaEmailsErroneos);
                }
                return mailStateModel;
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw;
            }
        }

        #region Métodos Privados

        private void EnviarNotificacion(List<string> pDestinatarios, string pAsunto, string pCuerpo, bool pEsHtml, string pMascaraRemitente, Guid proyectoID)
        {
            //obtener configuracion correo
            ParametroCN paramCN = new ParametroCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            var parametrosCorreo = paramCN.ObtenerConfiguracionEnvioCorreo(proyectoID);

            if (parametrosCorreo == null)
            {
                parametrosCorreo = paramCN.ObtenerConfiguracionEnvioCorreo(ProyectoAD.MyGnoss);
            }

            if (parametrosCorreo != null)
            {
                BaseComunidadCN baseComCN = new BaseComunidadCN("base", mEntityContext, mLoggingService, mEntityContextBASE, mConfigService, mServicesUtilVirtuosoAndReplication);
                baseComCN.InsertarCorreo(parametrosCorreo, pDestinatarios, pAsunto, pCuerpo, pEsHtml, pMascaraRemitente);
                baseComCN.Dispose();
            }
            else
            {
                throw new GnossException("The mail service is not configured. Contact with your administrator", HttpStatusCode.InternalServerError);
            }
        }

        private void EnviarNotificacionSMTPDefinido(List<string> pDestinatarios, string pAsunto, string pCuerpo, bool pEsHtml, string pMascaraRemitente, Guid proyectoID, MailConfigurationModel pConfiguracionEmisor)
        {
            if (pConfiguracionEmisor != null)
            {
                BaseComunidadCN baseComCN = new BaseComunidadCN("base", mEntityContext, mLoggingService, mEntityContextBASE, mConfigService, mServicesUtilVirtuosoAndReplication);

                ConfiguracionEnvioCorreo configuracionEnvioCorreo = new ConfiguracionEnvioCorreo();
                configuracionEnvioCorreo.clave = pConfiguracionEmisor.clave;
                configuracionEnvioCorreo.email = pConfiguracionEmisor.email;
                configuracionEnvioCorreo.emailsugerencias = "";
                configuracionEnvioCorreo.ProyectoID = proyectoID;
                configuracionEnvioCorreo.puerto = pConfiguracionEmisor.puerto;
                configuracionEnvioCorreo.smtp = pConfiguracionEmisor.smtp;
                configuracionEnvioCorreo.SSL = true;
                configuracionEnvioCorreo.tipo = pConfiguracionEmisor.tipo;
                configuracionEnvioCorreo.usuario = pConfiguracionEmisor.email;

                baseComCN.InsertarCorreo(configuracionEnvioCorreo, pDestinatarios, pAsunto, pCuerpo, pEsHtml, pMascaraRemitente);

                baseComCN.Dispose();
            }
            else
            {
                throw new GnossException("The mail service is not configured. Send mail configuration", HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}
