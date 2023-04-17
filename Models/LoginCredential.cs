using System.ComponentModel.DataAnnotations;

namespace MDB.Models
{
    public class LoginCredential
    {
        [Display(Name = "Courriel"), EmailAddress(ErrorMessage = "Invalide"), Required(ErrorMessage = "Obligatoire")]
        [System.Web.Mvc.Remote("EmailExist", "Accounts", HttpMethod = "POST", ErrorMessage = "Ce courriel est introuvable.")]
        public string Email { get; set; }

        [Display(Name = "Mot de passe"), Required(ErrorMessage = "Obligatoire")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}