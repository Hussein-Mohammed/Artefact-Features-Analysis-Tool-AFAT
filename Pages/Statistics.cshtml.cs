using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cuneiform_Style_Analyser.Pages
{
    public class StatisticsModel : PageModel
    {
        private IWebHostEnvironment _hostingEnvironment;

        public StatisticsModel(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void OnGet()
        {

        }

        public ActionResult OnPostDownloadSimilarities()
        {
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Temp_Folder = "Temp";
            string Similarities_Tables_Folder = "Similarities_Tables";
            string Similarities_Tables_Path = Path.Combine(webRootPath, Similarities_Tables_Folder);
            string TempPath = Path.Combine(webRootPath, Temp_Folder);
            if (!Directory.Exists(Similarities_Tables_Path))
            {
                Directory.CreateDirectory(Similarities_Tables_Path);
            }
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            // create a new archive          
            string archive = TempPath + "\\Distances_Tables.zip";
            ZipFile.CreateFromDirectory(Similarities_Tables_Path, archive);
            return File("/Temp/Distances_Tables.zip", "application/zip", "Distances_Tables.zip");
        }

        public ActionResult OnPostDownloadStatistics()
        {
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Temp_Folder = "Temp";
            string CSO_Statistics_Folder = "CSO_Statistics_Individual";
            string TempPath = Path.Combine(webRootPath, Temp_Folder);
            string CSO_Statistics_Path = Path.Combine(webRootPath, CSO_Statistics_Folder);
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
            if (!Directory.Exists(CSO_Statistics_Path))
            {
                Directory.CreateDirectory(CSO_Statistics_Path);
            }

            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            
            // create a new archive          
            string archive = TempPath + "\\Statistics_Tables.zip";
            ZipFile.CreateFromDirectory(CSO_Statistics_Path, archive);
            return File("/Temp/Statistics_Tables.zip", "application/zip", "Statistics_Tables.zip");
        }
    }
}