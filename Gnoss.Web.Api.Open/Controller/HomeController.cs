using Microsoft.AspNetCore.Mvc;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Controllers
{
    /// <summary>
    /// Home controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        /// <summary>
        /// Index method
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("/Help");
        }
    }
}
