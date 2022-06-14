using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{

    public class SPARQLObject
    {
        /// <summary>
        /// Head of the sparql query
        /// </summary>
        public Head head { get; set; }
        /// <summary>
        /// Results of the sparql query
        /// </summary>
        public Results results { get; set; }
        public class Head
        {
            public List<object> link { get; set; }
            public List<string> vars { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string value { get; set; }
            public string datatype { get; set; }
        }

        public class Results
        {
            public bool distinct { get; set; }
            public bool ordered { get; set; }
            public List<Dictionary<string, Data>> bindings { get; set; }
        }
    }

    /// <summary>
    /// Represents a sparql query
    /// </summary>
    public partial class SparqlQuery
    {
        /// <summary>
        /// Ontology name or community identificator to query. It will be used in the form clause
        /// </summary>
        [Required]
        public string ontology { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Select clause of the sparql query
        /// </summary>
        [Required]
        public string query_select { get; set; }

        /// <summary>
        /// Where clause of the sparql query
        /// </summary>
        [Required]
        public string query_where { get; set; }
    }

    /// <summary>
    /// Represents a sparql query
    /// </summary>
    public partial class SparqlQueryMultipleGraph
    {
        /// <summary>
        /// Ontology name or community identificator to query. It will be used in the form clause
        /// </summary>
        [Required]
        public List<string> ontology_list { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Select clause of the sparql query
        /// </summary>
        [Required]
        public string query_select { get; set; }

        /// <summary>
        /// Where clause of the sparql query
        /// </summary>
        [Required]
        public string query_where { get; set; }
    }
}