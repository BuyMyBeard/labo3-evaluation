using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;

namespace MDB.Models
{

    public class Country
    {
        const string FlagCDN = @"https://flagcdn.com/";
        public string Code { get; set; }
        public string Name { get; set; }
        public string Flag { get { return FlagCDN + Code + ".svg"; }}
        public string SmallFlag { get { return FlagCDN + @"w40/" + Code + ".png"; } }
    }

    public sealed class Countries
    {
        #region private members and methods
        private static readonly Countries instance = new Countries();
        public static Countries Instance
        {
            get { return instance; }
        }
        private static string Iso3166File = @"~/App_Data/iso-3166.txt";
        private static List<Country> _countries = new List<Country>();
        private static void LoadCountries()
        {
            var httpServerUtility = new HttpServerUtilityWrapper(HttpContext.Current.Server);
            string iso3166Path = httpServerUtility.MapPath(Iso3166File);
            try
            {
                // Create a StreamReader  
                using (StreamReader reader = new StreamReader(iso3166Path))
                {
                    string line;
                    // Read line by line  
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] token = line.Split(new char[] { ',' });
                        _countries.Add(new Country() { Code = token[0].ToLower().Trim(), Name = token[1].Trim() });
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region public method
        public static List<Country> List
        {
            get
            {
                if (_countries.Count == 0) 
                    LoadCountries();
                return _countries.OrderBy(c => c.Name).ToList();
            }
        }
        public static Country Get(string code)
        {
            var country = List.FirstOrDefault(c => c.Code == code);
            return country;
        }
        public static string FlagUrl(string code)
        {
            string url = string.Empty;
            var country = Get(code);
            if (country != null) 
                url = country.Flag;
            return url;
        }
        #endregion
    }
}