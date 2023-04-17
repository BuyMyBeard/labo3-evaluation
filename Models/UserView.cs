using FileKeyReference;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace MDB.Models
{
    [MetadataType(typeof(UserView))]
    public partial class User
    {
        #region Avatar handling
        [NotMapped]
        [Display(Name = "Avatar")]
        public string AvatarImageData { get; set; }
        [NotMapped]
        private static ImageFileKeyReference AvatarReference =
            new ImageFileKeyReference(@"/Images_Data/User_Avatars/", @"no_avatar.png", false);
        public String GetAvatarURL()
        {
            return AvatarReference.GetURL(Avatar, false);
        }
        public void SaveAvatar()
        {
            Avatar = AvatarReference.Save(AvatarImageData, Avatar);
        }
        public void RemoveAvatar()
        {
            AvatarReference.Remove(Avatar);
        }
        #endregion

        [NotMapped]
        public string ConfirmEmail { get; set; }
        [NotMapped]
        public string ConfirmPassword { get; set; }

        public bool IsPowerUser
        {
            get { return UserTypeId <= 2 /* Admin = 1 , PowerUser = 2 */; }
        }
        public bool IsAdmin
        {
            get { return UserTypeId == 1 /* Admin */; }
        }
        public bool CRUD_Access
        { 
            get { return IsPowerUser; } 
        }
        public string GetFullName(bool showGender = false)
        {
            if (showGender)
            {
                if (Gender.Name != "Neutre")
                    return Gender.Name + " " + LastName;
            }
            return FirstName + " " + LastName;
        }
    }

    public class UserView
    {
        [Display(Name = "Prenom"), Required(ErrorMessage = "Obligatoire")]
        public string FirstName { get; set; }

        [Display(Name = "Nom"), Required(ErrorMessage = "Obligatoire")]
        public string LastName { get; set; }

        [Display(Name = "Genre")]
        public int GenderId { get; set; }

        [Display(Name = "Courriel"), EmailAddress(ErrorMessage = "Invalide"), Required(ErrorMessage = "Obligatoire")]
        [System.Web.Mvc.Remote("EmailAvailable", "Accounts", HttpMethod = "POST", AdditionalFields = "Id", ErrorMessage = "Ce courriel n'est pas disponible.")]
        public string Email { get; set; }

        [Display(Name = "Confirmation")]
        [Compare("Email", ErrorMessage = "Le courriel et celui de confirmation ne correspondent pas.")]
        public string ConfirmEmail { get; set; }

        [Display(Name = "Mot de passe"), Required(ErrorMessage = "Obligatoire")]
        [StringLength(50, ErrorMessage = "Le mot de passe doit comporter au moins {2} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public String Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmation")]
        [Compare("Password", ErrorMessage = "Le mot de passe et celui de confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Genre")]
        public virtual Gender Gender { get; set; }

        [Display(Name = "Date de création")]
        [DataType(DataType.Date)]
        public DateTime CreationDate { get; set; }
    }
}