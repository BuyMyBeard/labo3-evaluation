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
    [MetadataType(typeof(DistributorView))]
    public partial class Distributor
    {
        #region Logo handling
        [Display(Name = "Logo")]
        public string LogoImageData { get; set; }
        private static ImageFileKeyReference LogosRepository = new ImageFileKeyReference(@"/Images_Data/Distributor_Logos/", @"No_Logo.png");
        public string GetLogoURL(bool thumbailFormat = false)
        {
            return LogosRepository.GetURL(LogoImageKey, thumbailFormat);
        }
        public void SaveLogo()
        {
            LogoImageKey = LogosRepository.Save(LogoImageData, LogoImageKey);
        }
        public void RemoveLogo()
        {
            LogosRepository.Remove(LogoImageKey);
        }
        #endregion

        public List<Movie> Movies
        {
            get
            {
                List<Movie> movies = new List<Movie>();
                foreach (Distribution distribution in Distributions)
                {
                    movies.Add(distribution.Movie);
                }
                return movies.OrderBy(f => f.Title).ToList();
            }
        }
    }
    public class DistributorView
    {
        [Display(Name = "Distributeur"), Required(ErrorMessage = "Le nom est requis")]
        public string Name { get; set; }
       
        [Display(Name = "Pays")]
        public string CountryCode { get; set; }
    }
}