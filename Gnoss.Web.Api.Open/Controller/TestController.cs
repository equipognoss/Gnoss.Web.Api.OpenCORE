using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControlApiGnossBase
    {

        public TestController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, IHttpContextAccessor httpContextAccessor, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, EntityContextBASE entityContextBASE, GnossCache gnossCache, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(entityContext, loggingService, configService, httpContextAccessor, redisCacheWrapper, virtuosoAD, entityContextBASE, gnossCache, servicesUtilVirtuosoAndReplication)
        {
        }

        #region Test Llamadas servicios externos GNOSS

        public class JsonRespuesta
        {
            public short Status { get; set; }
            public string Message { get; set; }
            public JsonAction Action { get; set; }
        }

        public class JsonAction
        {
            public bool RedirectCommunityHome { get; set; }
            public bool RedirectResource { get; set; }
            public string RedirectUrl { get; set; }
        }
        [HttpGet, HttpPost]
        [Route("TestExternalServiceOk")]
        public JsonRespuesta TestExternalServiceOk()
        {
            try
            {
                string rdf = null;

                if (mHttpContextAccessor.HttpContext.Request.Query.ContainsKey("RDFBytes"))
                {
                    byte[] buffer = Convert.FromBase64String(mHttpContextAccessor.HttpContext.Request.Query["RDFBytes"]);

                    StreamReader stream = new StreamReader(new MemoryStream(buffer));
                    rdf = stream.ReadToEnd();
                    stream.Close();
                    stream.Dispose();
                }

                string rdfAnt = null;

                if (mHttpContextAccessor.HttpContext.Request.Query.ContainsKey("OldRDFBytes"))
                {
                    byte[] buffer = Convert.FromBase64String(mHttpContextAccessor.HttpContext.Request.Query["OldRDFBytes"]);

                    StreamReader stream = new StreamReader(new MemoryStream(buffer));
                    rdfAnt = stream.ReadToEnd();
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
            }

            JsonRespuesta resp = new JsonRespuesta();

            resp.Status = 1;
            resp.Message = "Prueba con resultado satisfactorio.";

            return resp;
        }

        [Route("TestExternalServiceKo")]
        [HttpGet]
        public JsonRespuesta TestExternalServiceKo()
        {
            JsonRespuesta resp = new JsonRespuesta();
            resp.Status = 0;
            resp.Message = "Prueba con un error en el proceso.";

            return resp;
        }
        [HttpGet]
        [Route("TestExternalServiceHomeRedirect")]
        public JsonRespuesta TestExternalServiceHomeRedirect()
        {
            JsonRespuesta resp = new JsonRespuesta();

            resp.Status = 1;
            resp.Action = new JsonAction();
            resp.Action.RedirectCommunityHome = true;

            return resp;
        }

        #endregion
    }
}
