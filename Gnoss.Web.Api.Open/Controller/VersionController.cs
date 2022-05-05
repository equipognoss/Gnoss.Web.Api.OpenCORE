using Es.Riam.Gnoss.AD;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Es.Riam.Gnoss.Web.MVC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(VersionEnsambladoAD);
        }

        /// <summary>
        /// Obtiene la versión del ensamblado Es.Riam.Gnoss.AD
        /// </summary>
        public static Version VersionEnsambladoAD
        {
            get
            {
                return typeof(BaseAD).Assembly.GetName().Version;
            }
        }
    }
}