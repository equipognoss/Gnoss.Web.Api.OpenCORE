using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{
    /// <summary>
    /// Represents a community
    /// </summary>
    public class CommunityInfoModel
    {
        /// <summary>
        /// Name
        /// </summary>
        [Required]
        public string name { get; set; }

        /// <summary>
        /// Short name
        /// </summary>
        [Required]
        public string short_name { get; set; }

        /// <summary>
        /// Brief Description
        /// </summary>
        [Required]
        public string description { get; set; }

        /// <summary>
        /// Tags (comma separated)
        /// </summary>
        [Required]
        public string tags { get; set; }

        /// <summary>
        /// Community's type
        /// </summary>
        [Required]
        public string type { get; set; }

        /// <summary>
        /// Community's access type
        /// </summary>
        public short access_type { get; set; }

        /// <summary>
        /// Community's categories
        /// </summary>
        public List<Guid> categories { get; set; }

        /// <summary>
        /// Community's users
        /// </summary>
        public List<Guid> users { get; set; }
        /// <summary>
        /// Community's state
        /// </summary>
        public short state { get; set; }
    }

    /// <summary>
    /// Parameters to obtein a text in other language
    /// </summary>
    public class GetTextByLanguageModel
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Language of the text
        /// </summary>
        [Required]
        public string language { get; set; }

        /// <summary>
        /// ID of the text
        /// </summary>
        [Required]
        public string texto_id { get; set; }
    }
}
