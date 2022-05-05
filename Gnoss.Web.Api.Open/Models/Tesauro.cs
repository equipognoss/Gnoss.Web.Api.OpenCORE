using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{
    /// <summary>
    /// Parameters to move a node
    /// </summary>
    public class ParamsMoveNode
    {
        /// <summary>
        /// Ontology URL of the thesaurus
        /// </summary>
        [Required]
        public string thesaurus_ontology_url { get; set; }

        /// <summary>
        /// Ontology URL of the resources that references this thesaurus
        /// </summary>
        [Required]
        public string resources_ontology_url { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Identificator of the category
        /// </summary>
        [Required]
        public string category_id { get; set; }

        /// <summary>
        /// Path from root to the new parent category
        /// </summary>
        [Required]
        public string[] path { get; set; }
    }

    /// <summary>
    /// Parameters to change the category name
    /// </summary>
    public class ParamsChangeCategoryName
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

       /// <summary>
       /// Category id
       /// </summary>
        [Required]
        public Guid category_id { get; set; }

        /// <summary>
        /// New category name
        /// </summary>
        [Required]
        public string new_category_name { get; set; }
    }

    /// <summary>
    /// Parameters to create a new category
    /// </summary>
    public class ParamsCreateCategory
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Category name
        /// </summary>
        [Required]
        public string category_name { get; set; }

        /// <summary>
        /// Identificator of the parent category
        /// </summary>
        public Guid? parent_catergory_id { get; set; }
    }

    /// <summary>
    /// Parameters to create a new category
    /// </summary>
    public class ParamsDeleteCategory
    {
        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Category id
        /// </summary>
        [Required]
        public Guid category_id { get; set; }
    }

    /// <summary>
    /// Parameters to delete a node
    /// </summary>
    public class ParamsDeleteNode
    {
        /// <summary>
        /// URL of the thesaurus ontology
        /// </summary>
        [Required]
        public string thesaurus_ontology_url { get; set; }

        /// <summary>
        /// Ontology URL of the resources that references this thesaurus
        /// </summary>
        [Required]
        public string resources_ontology_url { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Identificator of the category
        /// </summary>
        [Required]
        public string category_id { get; set; }

        /// <summary>
        /// Path from root to her last child to which will move the resources that are in the deleted category
        /// </summary>
        [Required]
        public string[] path { get; set; }
    }

    /// <summary>
    /// Parameters to change the parent node of a node
    /// </summary>
    public class ParamsParentNode
    {
        /// <summary>
        /// URL of the thesaurus ontology
        /// </summary>
        [Required]
        public string thesaurus_ontology_url { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Identificator of the parent category
        /// </summary>
        [Required]
        public string parent_catergory_id { get; set; }

        /// <summary>
        /// Identificator of the child category
        /// </summary>
        [Required]
        public string child_category_id { get; set; }
    }

    /// <summary>
    /// Parameters to chage the name of a node
    /// </summary>
    public class ParamsChangeName
    {
        /// <summary>
        /// URL of the thesaurus ontology
        /// </summary>
        [Required]
        public string thesaurus_ontology_url { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Identificator of the category
        /// </summary>
        [Required]
        public string category_id { get; set; }

        /// <summary>
        /// Name of the category
        /// </summary>
        [Required]
        public string category_name { get; set; }
    }

    /// <summary>
    /// Parameters to insert a node
    /// </summary>
    public class ParamsInsertNode
    {
        /// <summary>
        /// URL of the thesaurus ontology
        /// </summary>
        [Required]
        public string thesaurus_ontology_url { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// RDF of the category
        /// </summary>
        [Required]
        public byte[] rdf_category { get; set; }
    }

    /// <summary>
    /// Represents a thesaurus category
    /// </summary>
    public class ThesaurusCategory
    {
        /// <summary>
        /// Category identificator
        /// </summary>
        [Required]
        public Guid category_id { get; set; }

        /// <summary>
        /// Category name
        /// </summary>
        [Required]
        public string category_name { get; set; }

        /// <summary>
        /// Parent category identificator
        /// </summary>
        public Guid parent_category_id { get; set; }
    }
}