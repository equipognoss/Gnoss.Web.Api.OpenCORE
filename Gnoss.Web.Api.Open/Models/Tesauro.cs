using Microsoft.Azure.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using static Es.Riam.Gnoss.Web.MVC.Models.Tesauro.TesauroModels;

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

        /// <summary>
        /// List of category image in different sizes 120px, 240px, 420px, 2400px 
        /// </summary>
        public Dictionary<int, string> category_images { get; set; }
    }

    

    /// <summary>
    /// Model with the parameters to add a new Concept to a Thesaurus
    /// </summary>
    public class ConceptToAddModel
    {
		/// <summary>
		/// Represents a Concept according to the Ontology
		/// </summary>
		[Required]
        public Concept Concept { get; set; }

		/// <summary>
		/// Source of the Thesaurus to load
		/// </summary>
		[Required]
        public string Source {get;set;}

		/// <summary>
		/// Name of the ontology
		/// </summary>
		[Required]
        public string Ontology { get; set; }

        /// <summary>
        /// Subject parent of the category to load
        /// </summary>
        public string ParentCategorySubject { get; set; }

        /// <summary>
        /// Short name of the community
        /// </summary>
        public string CommunityShortName { get; set; }
	}

	/// <summary>
	/// Model with the parameters to add a new Concept to a Thesaurus
	/// </summary>
	public class ConceptToDeleteModel
	{
        /// <summary>
        /// Subject of the concept to delete
        /// </summary>
        public string ConceptSubject { get; set; }        

		/// <summary>
		/// Name of the ontology
		/// </summary>
		[Required]
		public string Ontology { get; set; }

		/// <summary>
		/// Short name of the community
		/// </summary>
		public string CommunityShortName { get; set; }
	}

	/// <summary>
	/// Model with the parameters to modify a Concept for the Thesaurus
	/// </summary>
	public class ConceptToModifyModel
	{
		/// <summary>
		/// Represents a Concept according to the Ontology
		/// </summary>
		[Required]
		public Concept Concept { get; set; }

		/// <summary>
		/// Source of the Thesaurus to load
		/// </summary>
		[Required]
		public string Source { get; set; }

		/// <summary>
		/// Name of the ontology
		/// </summary>
		[Required]
		public string Ontology { get; set; }

		/// <summary>
		/// Short name of the community
		/// </summary>
		public string CommunityShortName { get; set; }

        /// <summary>
        /// Indicates if the method has to modify the narrowers
        /// </summary>
        public bool ModifyNarrower { get; set; }

		/// <summary>
		/// Subject parent of the category to load
		/// </summary>
		public string ParentCategorySubject { get; set; }
	}

	/// <summary>
	/// Model with the params to delete the indicated thesaurus
	/// </summary>
	public class ThesaurusToDeleteModel
	{
		/// <summary>
		/// Short name of the community when the thesaurus will be deleted
		/// </summary>
		[Required]
		public string CommunityShortName { get; set; }

		/// <summary>
		/// Name of the ontology
		/// </summary>
		[Required]
		public string Ontology { get; set; }

		/// <summary>
		/// Source of the Thesaurus to delete
		/// </summary>
		[Required]
		public string Source { get; set; }
	}
}