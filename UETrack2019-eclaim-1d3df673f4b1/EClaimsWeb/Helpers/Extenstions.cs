using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Helpers
{
    public static class Extenstions
    {
        public static string SaveFileToSpecifiedFolder(this IFormFile formfile, IFormFile file, string folder)
        {
            string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(folder, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return filePath;
        }
    }
}
