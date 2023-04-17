using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

namespace FileKeyReference
{
    ////////////////////////////////////////////////////////////////////////////////////
    // Author : Nicolas Chourot
    // 
    ////////////////////////////////////////////////////////////////////////////////////
    public class ImageFileKeyReference
    {
        #region private members
        private int MaxSize { get; set; }
        private bool HasThumbnail { get; set; }
        private int ThumbnailSize { get; set; }
        private string DefaultImage { get; set; }
        private string BasePath { get; set; }
        private ImageFormat imageFormat { get; set; }
        #endregion
        #region public methods
        public ImageFileKeyReference(string basePath, string defautImage, bool hasThumbnail = true)
        {
            BasePath = basePath;
            DefaultImage = defautImage;
            MaxSize = 4096;
            ThumbnailSize = 512;
            imageFormat = ImageFormat.Jpeg;
            HasThumbnail = hasThumbnail;
        }
        // Get server image file url
        public string GetURL(string key, bool thumbnail = false)
        {
            string url = MakeUrl(key, thumbnail);

            if (thumbnail)
            {
                string imagePath = HttpContext.Current.Server.MapPath(url);
                if (!File.Exists(imagePath))
                {
                    url = MakeUrl(key);
                }
            }
            return url;
        }
        // Save image data in server file then return its key
        public string Save(string ImageData, string Previouskey = "")
        {
            if (!string.IsNullOrEmpty(ImageData))
            {
                string imagePath;
                string key;
                if (!string.IsNullOrEmpty(Previouskey))
                {
                    File.Delete(HttpContext.Current.Server.MapPath(MakeUrl(Previouskey)));
                    if (HasThumbnail)
                        File.Delete(HttpContext.Current.Server.MapPath(MakeUrl(Previouskey, true /*thumbnail*/)));
                }
                do
                {
                    key = Guid.NewGuid().ToString();
                    imagePath = HttpContext.Current.Server.MapPath(GetURL(key));
                    // make sure new GUID does not already exists 
                } while (File.Exists(imagePath));

                SaveImageFile(key, ImageData);
                if (HasThumbnail)
                    SaveImageFile(key, ImageData, true /*thumbnail*/);

                return key;
            }
            return Previouskey;
        }
        // remove server image file refered by the parameter key
        public void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                System.IO.File.Delete(HttpContext.Current.Server.MapPath(MakeUrl(key)));
                if (HasThumbnail)
                    System.IO.File.Delete(HttpContext.Current.Server.MapPath(MakeUrl(key, true /* thumbnail */)));
            }
        }
        #endregion
        #region private methods
        private string MakeUrl(string key, bool thumbnail = false)
        {
            string url;

            if (string.IsNullOrEmpty(key))
                url = "~" + BasePath + DefaultImage;
            else
                url = "~" + BasePath + (thumbnail ? @"Thumbnails/" : "") + key + "." + imageFormat.ToString();

            return url;
        }
        private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            try
            {
                if ((image.Width > maxWidth) || (image.Height > maxHeight))
                {
                    var ratioX = (double)maxWidth / image.Width;
                    var ratioY = (double)maxHeight / image.Height;
                    var ratio = Math.Min(ratioX, ratioY);

                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);

                    var newImage = new Bitmap(newWidth, newHeight);

                    using (var graphics = Graphics.FromImage(newImage))
                        graphics.DrawImage(image, 0, 0, newWidth, newHeight);

                    return newImage;
                }
                return new Bitmap(image);
            }
            catch
            {
                return image;
            }
        }
        private void SaveImageFile(string key, string ImageData, bool thumbnail = false)
        {
            // Extract image data <MIME,DATA>
            string mime = ImageData.Split(',')[0];
            string data = ImageData.Split(',')[1];
            if ((mime.IndexOf("webp") != -1) || (mime.IndexOf("avif") != -1))
            {
                // La classe Image ne supporte pas le format webp. Du coup pas possible de manipuler l'échelle pour créer un miniature.
                if (!thumbnail)
                {
                    var stream = new MemoryStream(Convert.FromBase64String(data));
                    FileStream file = new FileStream(HttpContext.Current.Server.MapPath(MakeUrl(key)), FileMode.Create, FileAccess.Write);
                    stream.WriteTo(file);
                    file.Close();
                    stream.Close();
                }
            }
            else
            {
                ImageFormat overrideFormat = (mime.IndexOf("png") != -1 ? ImageFormat.Png : imageFormat);
                var stream = new MemoryStream(Convert.FromBase64String(data));

                int maxSize = thumbnail ? ThumbnailSize : MaxSize;
                Image original = null;
                bool wrongFormat = false;   
                try
                {
                    original = Image.FromStream(stream);
                }
                catch (Exception)
                {
                    wrongFormat = true;
                }
                if (!wrongFormat)
                {
                    // Limit size of image
                    if ((original.Size.Width > maxSize) || (original.Size.Height > maxSize))
                    {
                        try
                        {
                            original = ScaleImage(original, maxSize, maxSize);
                        }
                        catch (Exception)
                        {

                        }
                    }
                    original.Save(HttpContext.Current.Server.MapPath(MakeUrl(key, thumbnail)), overrideFormat);
                }
            }
        }
        #endregion
    }
}