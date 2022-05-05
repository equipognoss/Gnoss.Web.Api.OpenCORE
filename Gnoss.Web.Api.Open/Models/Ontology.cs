using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{
    /// <summary>
    /// Represents an Ontology file
    /// </summary>
    //[ModelName("FileOntology")]
    [ModelBinder(Name = "FileOntology")]
    public class FileOntology
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Ontology name
        /// </summary>
        [Required]
        public string ontology_name { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        [Required]
        public string file_name { get; set; }

        /// <summary>
        /// The file bytes
        /// </summary>
        [Required]
        public byte[] file { get; set; }

    }

    /// <summary>
    /// Represents a list of triples to modify
    /// </summary>
    public class ModifyTripleListModel
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// URL of the secondary ontology
        /// </summary>
        [Required]
        public string secondary_ontology_url { get; set; }

        /// <summary>
        /// Identificator of the secondary entity
        /// </summary>
        [Required]
        public string secondary_entity { get; set; }

        /// <summary>
        /// Triple list to modify. It's a array of string arrays with three items: Old object, Predicate, New object. To delete a triple, send the New object empty
        /// </summary>
        [Required]
        public string[][] triple_list { get; set; }
    }

    /// <summary>
    /// Represents a secondary entity
    /// </summary>
    public class SecondaryEntityModel
    {
        /// <summary>
        /// Ontology url
        /// </summary>
        [Required]
        public string ontology_url { get; set; }

        /// <summary>
        /// Communtiy short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// RDF of the secondary entity
        /// </summary>
        [Required]
        public byte[] rdf { get; set; }
    }

    /// <summary>
    /// Represents a secondary entity
    /// </summary>
    public class DeleteSecondaryEntityModel
    {
        /// <summary>
        /// Ontology url
        /// </summary>
        [Required]
        public string ontology_url { get; set; }

        /// <summary>
        /// Communtiy short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// The secondary entity identificator
        /// </summary>
        [Required]
        public string entity_id { get; set; }
    }
}