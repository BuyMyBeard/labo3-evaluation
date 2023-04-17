using Antlr.Runtime.Misc;
using FileKeyReference;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace MDB.Models
{
    [MetadataType(typeof(ActorView))]
    public partial class Actor
    {
        #region Avatar handling
        [Display(Name = "Avatar")]
        public string AvatarImageData { get; set; }
        private static ImageFileKeyReference PostersRepository = new ImageFileKeyReference(@"/Images_Data/Actor_Avatars/", @"no_avatar.png");
        public string GetAvatarURL(bool thumbailFormat = false)
        {
            return PostersRepository.GetURL(AvatarImageKey, thumbailFormat);
        }
        public void SaveAvatar()
        {
            AvatarImageKey = PostersRepository.Save(AvatarImageData, AvatarImageKey);
        }
        public void RemoveAvatar()
        {
            PostersRepository.Remove(AvatarImageKey);
        }
        #endregion

        public List<Movie> Movies
        {
            get
            {
                List<Movie> movies = new List<Movie>();
                foreach (Casting casting in Castings)
                {
                    movies.Add(casting.Movie);
                }
                return movies.OrderBy(f => f.Title).ToList();
            }
        }
    }
    public class ActorView
    {
        [Display(Name = "Nom"), Required(ErrorMessage = "Le nom est requis")]
        public string Name { get; set; }

        [Display(Name = "Date de naissance"), Required(ErrorMessage = "La date de naissance est requise")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "Nationalité")]
        public string CountryCode { get; set; }
    }
}