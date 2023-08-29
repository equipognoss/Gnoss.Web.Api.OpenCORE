using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Es.Riam.Gnoss.Web.ServicioApiRecursosMVC.Models
{
    /// <summary>
    /// Parameters for a community user
    /// </summary>
    public class ParamsUserCommunity
    {
        /// <summary>
        /// User short name
        /// </summary>
        [Required]
        public string user_short_name { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }
    }

    /// <summary>
    /// Parameters for login and password
    /// </summary>
    public class ParamsLoginPassword
    {
        /// <summary>
        /// Login o email of the user
        /// </summary>
        /// <example>fer123</example>
        [Required]
        public string login { get; set; }
        /// <summary>
        /// Password of the user
        /// </summary>
        [Required]
        public string password { get; set; }
    }

    /// <summary>
    /// Parameters for add a user in a organization
    /// </summary>
    public class ParamsAddUserOrg
    {
        /// <summary>
        /// User identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// Organization short name
        /// </summary>
        [Required]
        public string organization_short_name { get; set; }

        /// <summary>
        /// User position in the organization
        /// </summary>
        [Required]
        public string position { get; set; }

        /// <summary>
        /// Communities short names where the user is going to be added (The organization must be member of all of them)
        /// </summary>
        public List<string> communities_short_names { get; set; }
    }

    /// <summary>
    /// Parameters for add a user in a organization group
    /// </summary>
    public class ParamsAddUserOrgGroups
    {
        /// <summary>
        /// User identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// Organization short name
        /// </summary>
        [Required]
        public string organization_short_name { get; set; }

        /// <summary>
        /// Groups where the user is going to be added
        /// </summary>
        [Required]
        public List<string> groups_short_names { get; set; }
    }

    /// <summary>
    /// Represents a user
    /// </summary>
    public class UserCommunity
    {
        /// <summary>
        /// Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Last name or Family name
        /// </summary>
        public string last_name { get; set; }

        /// <summary>
        /// User short name
        /// </summary>
        public string user_short_name { get; set; }

        /// <summary>
        /// User identificator
        /// </summary>
        public Guid user_id { get; set; }

        /// <summary>
        /// Number of resources
        /// </summary>
        public string num_resources{ get; set; }

        /// <summary>
        /// Number of comments
        /// </summary>
        public string num_comments { get; set; }

        /// <summary>
        /// Groups
        /// </summary>
        public List<string> groups { get; set; }
    }

    /// <summary>
    /// Represents a user
    /// </summary>
    public class Userlite
    {
        /// <summary>
        /// Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Last name or Family name
        /// </summary>
        public string last_name { get; set; }

        /// <summary>
        /// User short name
        /// </summary>
        public string user_short_name { get; set; }

        /// <summary>
        /// Imagen
        /// </summary>
        public string image { get; set; }

        /// <summary>
        /// User born date
        /// </summary>
        public DateTime? born_date { get; set; }
    }


    /// <summary>
    /// Represents a user
    /// </summary>
    public class User
    {
        /// <summary>
        /// Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Last name or Family name
        /// </summary>
        public string last_name { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Password (Only for update, not for query)
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// User extra data
        /// </summary>
        public List<ExtraUserData> extra_data { get; set; }

        /// <summary>
        /// User events
        /// </summary>
        public List<UserEvent> user_events { get; set; }

        /// <summary>
        /// Auxiliary data
        /// </summary>
        public string aux_data { get; set; }

        /// <summary>
        /// Community identificator
        /// </summary>
        public Guid community_id { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        public string community_short_name { get; set; }

        /// <summary>
        /// User short name
        /// </summary>
        public string user_short_name { get; set; }

        /// <summary>
        /// User identificator
        /// </summary>
        public Guid user_id { get; set; }

        /// <summary>
        /// User identification (passport number, dni...)
        /// </summary>
        public string id_card { get; set; }

        /// <summary>
        /// User born date
        /// </summary>
        public DateTime born_date { get; set; }

        /// <summary>
        /// Country identificator
        /// </summary>
        public Guid country_id { get; set; }

        /// <summary>
        /// User country
        /// </summary>
        public string country { get; set; }

        /// <summary>
        /// Province identificator
        /// </summary>
        public Guid province_id { get; set; }

        /// <summary>
        /// User province
        /// </summary>
        public string provice { get; set; }

        /// <summary>
        /// User city
        /// </summary>
        public string city { get; set; }

        /// <summary>
        /// User address
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// User postal code
        /// </summary>
        public string postal_code { get; set; }

        /// <summary>
        /// Date when the member has joined to this community
        /// </summary>
        public DateTime join_community_date { get; set; }

        /// <summary>
        /// H for Male or M to Female
        /// </summary>
        public string sex { get; set; }

        /// <summary>
        /// User preferences
        /// </summary>
        public List<ThesaurusCategory> preferences { get; set; }

        /// <summary>
        /// True if this user must recive the community newsletter
        /// </summary>
        public bool receive_newsletter { get; set; }

        /// <summary>
        /// User prefered language
        /// </summary>
        public string languaje { get; set; }

        /// <summary>
        /// Photo of personal profile
        /// </summary>
        public string photo { get; set; }

        /// <summary>
        /// Number of access user
        /// </summary>
        public int num_access { get; set; }

        /// <summary>
        /// Date of last login
        /// </summary>
        public DateTime? last_login { get; set; }
    }

    /// <summary>
    /// User extra data
    /// </summary>
    public class ExtraUserData
    {
        /// <summary>
        /// Identificator of the extra data
        /// </summary>
        public Guid name_id { get; set; }

        /// <summary>
        /// Extra data name. 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Extra data value identificator. Only if the list of values has been defined
        /// </summary>
        public Guid value_id { get; set; }

        /// <summary>
        /// Extra data value
        /// </summary>
        public string value { get; set; }
    }

    /// <summary>
    /// User event
    /// </summary>
    public class UserEvent
    {
        /// <summary>
        /// Event identificator
        /// </summary>
        public Guid event_id { get; set; }

        /// <summary>
        /// Event name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Event date
        /// </summary>
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Properties of a user
    /// </summary>
    public class UserNoveltiesModel
    {
        /// <summary>
        /// User identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// User subscriptions to community categories. <see cref="CommunitySubscriptionModel"/>
        /// </summary>
        [Required]
        public CommunitySubscriptionModel community_subscriptions { get; set; }

        /// <summary>
        /// User subscriptions to another user. <see cref="UserSubscriptionModel"/>
        /// </summary>
        [Required]
        public List<UserSubscriptionModel> user_subscriptions { get; set; }

        /// <summary>
        /// User info about community membership. <see cref="UserCommunityMembership"/>
        /// </summary>
        public UserCommunityMembership user_community_membership { get; set; }

    }

    /// <summary>
    /// Represents the subscription to a community
    /// </summary>
    public class CommunitySubscriptionModel
    {
        /// <summary>
        /// Subscriptor user identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// Categories wich user is subscribed to
        /// </summary>
        [Required]
        public List<ThesaurusCategory> category_list { get; set; }
    }

    /// <summary>
    /// Represents the subscription from user to another user
    /// </summary>
    public class UserSubscriptionModel
    {
        /// <summary>
        /// Subscriptor user identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// User identificator who user is subscribed to
        /// </summary>
        [Required]
        public Guid user_followed_id { get; set; }

        /// <summary>
        /// Community short name. Null if user is a follower
        /// </summary>
        public string community_short_name { get; set; }

        /// <summary>
        /// Categories wich user is subscribed to
        /// </summary>
        public DateTime? subscription_date { get; set; }
    }

    /// <summary>
    /// Represents the user community membership information 
    /// </summary>
    public class UserCommunityMembership
    {
        /// <summary>
        /// User identificator
        /// </summary>
        [Required]
        public Guid user_id { get; set; }

        /// <summary>
        /// Community short name
        /// </summary>
        [Required]
        public string community_short_name { get; set; }

        /// <summary>
        /// User community registration date in ISO 8601 format
        /// </summary>
        public DateTime? registration_date { get; set; }

        /// <summary>
        /// Indicates if th user manages the community
        /// </summary>
        [Required]
        public bool administrator_rol { get; set; }
    }

}