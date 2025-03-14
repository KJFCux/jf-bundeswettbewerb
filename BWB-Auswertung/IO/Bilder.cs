using System;

namespace BWB_Auswertung.IO
{
    static class Bilder
    {
        public static string readBase64(string path)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(path);
            return Convert.ToBase64String(imageArray);
        }
    }
}
