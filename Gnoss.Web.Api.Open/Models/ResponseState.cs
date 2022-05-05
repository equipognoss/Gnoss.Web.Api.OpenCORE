namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{
    /// <summary>
    /// Shows the response state and additional info about it
    /// </summary>
    public class ResponseState
    {
        /// <summary>
        /// It indicates whether the action was successful
        /// </summary>
        public bool Correct { get; set; }
        /// <summary>
        /// Show additional info about the action
        /// </summary>
        public string ExtraInfo { get; set; }
    }
}