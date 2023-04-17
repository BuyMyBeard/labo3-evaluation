using FileKeyReference;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MDB.Models
{
    [MetadataType(typeof(MovieView))]
    public partial class Movie
    {
        #region Poster handling
        [Display(Name = "Affiche")]
        public string PosterImageData { get; set; }
        private static ImageFileKeyReference PostersRepository = new ImageFileKeyReference(@"/Images_Data/Movie_Posters/", @"no_poster.png");
        public string GetPosterURL(bool thumbailFormat = false)
        {
            return PostersRepository.GetURL(PosterImageKey, thumbailFormat);
        }
        public void SavePoster()
        {
            PosterImageKey = PostersRepository.Save(PosterImageData, PosterImageKey);
        }
        public void RemovePoster()
        {
            PostersRepository.Remove(PosterImageKey);
        }
        #endregion

        public List<Actor> Actors
        {
            get
            {
                List<Actor> ators = new List<Actor>();
                foreach (Casting casting in Castings)
                {
                    ators.Add(casting.Actor);
                }
                return ators.OrderBy(f => f.Name).ToList();
            }
        }
        public List<Distributor> Distributors
        {
            get
            {
                List<Distributor> distributors = new List<Distributor>();
                foreach (Distribution distribution in Distributions)
                {
                    distributors.Add(distribution.Distributor);
                }
                return distributors.OrderBy(d => d.Name).ToList();
            }
        }
        
    }
    public class MovieView
    {
        [Display(Name = "Titre"), Required(ErrorMessage = "Le titre est requis")]
        public string Title { get; set; }
        [Display(Name = "Année de sortie")]
        [Range(1930, 2099,  ErrorMessage = "Valeur pour {0} doit être entre {1} et {2}.")]
        public int ReleaseYear { get; set; }
        [Display(Name = "Pays")]
        public string CountryCode { get; set; }
    }
}