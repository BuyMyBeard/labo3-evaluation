using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MDB.Models
{
    public class EmailView
    {
        [Display(Name = "Courriel"), EmailAddress(ErrorMessage = "Invalide"), Required(ErrorMessage = "Obligatoire")]
        [Remote("EmailExist", "Accounts", HttpMethod = "POST", ErrorMessage = "Ce courriel est introuvable.")]
        public string Email { get; set; }
    }
}