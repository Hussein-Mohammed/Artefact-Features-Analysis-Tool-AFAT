using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Cuneiform_Style_Analyser.Headers;
using OpenCvSharp.Flann;
using OpenCvSharp;
using Microsoft.AspNetCore.Http;

namespace Cuneiform_Style_Analyser.Pages
{
    public class IndexModel : PageModel
    {
        public IEnumerable<string> ValidFiles { get; set; }
        private readonly ILogger<IndexModel> _logger;

        // needed to get the wwwroot directory
        private IWebHostEnvironment _hostingEnvironment;
        private readonly Uploaded_CSO _Uploaded_CSO;

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment hostingEnvironment, Uploaded_CSO Uploaded_CSO)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _Uploaded_CSO = Uploaded_CSO;
        }

        public void OnGet()
        {
            /// Empty All existing folders and create non-existing ones 
            string Tables_Folder = "CSO_Tables";
            string Statistics_Individual_Folder = "CSO_Statistics_Individual";
            string Statistics_General_Folder = "CSO_Statistics_General";
            string Temp_Folder = "Temp";
            string Similarities_Tables_Folder = "Similarities_Tables";

            string webRootPath = _hostingEnvironment.WebRootPath;
            string Tables_Path = Path.Combine(webRootPath, Tables_Folder);
            string Statistics_Individual_Path = Path.Combine(webRootPath, Statistics_Individual_Folder);
            string Statistics_General_Path = Path.Combine(webRootPath, Statistics_General_Folder);
            string TempPath = Path.Combine(webRootPath, Temp_Folder);
            string Similarities_Tables_Path = Path.Combine(webRootPath, Similarities_Tables_Folder);

            if (!Directory.Exists(Statistics_Individual_Path))
            {
                Directory.CreateDirectory(Statistics_Individual_Path);
            }
            if (!Directory.Exists(Statistics_General_Path))
            {
                Directory.CreateDirectory(Statistics_General_Path);
            }
            if (!Directory.Exists(Tables_Path))
            {
                Directory.CreateDirectory(Tables_Path);
            }
            if (!Directory.Exists(Similarities_Tables_Path))
            {
                Directory.CreateDirectory(Similarities_Tables_Path);
            }
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
            Directory.EnumerateFiles(Statistics_Individual_Path).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(Tables_Path).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(Similarities_Tables_Path).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(Statistics_General_Path).ToList().ForEach(f => System.IO.File.Delete(f));

            ///Clear all Singleton variables and re-assign default values.
            _Uploaded_CSO.All_CSO_Tables.Clear();
        }

        /// Called when clicking Upload and Validate
        public IActionResult OnPostUpload(IFormFile[] files)
        {
            string Tables_Folder = "CSO_Tables";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Tables_Path = Path.Combine(webRootPath, Tables_Folder);
            if (!Directory.Exists(Tables_Path))
            {
                Directory.CreateDirectory(Tables_Path);
            }
            Directory.EnumerateFiles(Tables_Path).ToList().ForEach(f => System.IO.File.Delete(f));

            if (files != null && files.Count() > 0)
            {
                // Iterate through uploaded files array
                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file.FileName);
                    if (ext != ".csv")
                        continue;
                    if (file.Length > 0)
                    {
                        // Extract file name from whatever was posted by browser
                        var fileName = System.IO.Path.GetFileName(file.FileName);

                        // If file with same name exists delete it
                        if (System.IO.File.Exists(fileName))
                        {
                            System.IO.File.Delete(fileName);
                        }

                        // Create new local file and copy contents of uploaded file
                        using (var localFile = System.IO.File.OpenWrite(Tables_Path + "/" + fileName))
                        using (var uploadedFile = file.OpenReadStream())
                        {
                            uploadedFile.CopyTo(localFile);
                        }
                    }
                }

                // Check whether there is at least one file with a valid extension
                ValidFiles = Directory.EnumerateFiles(Tables_Path, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".csv"));
                ViewData["ValidFiles"] = ValidFiles.Count();
                if (ValidFiles.Count() <= 0)
                {
                    ViewData["Message"] = "Uploaded files are not valid!";
                    return Page();
                }

                /// Read and save the tables
                Cuneiform_Signs CSO_Data = new Cuneiform_Signs();
                List<CSO_Table> ListOfTables = new List<CSO_Table>();
                ListOfTables = CSO_Data.Read_CSO_Tables(Tables_Path);

                for (int tabel_Index = 0; tabel_Index < ListOfTables.Count(); tabel_Index++)
                {
                    if (ListOfTables[tabel_Index].Cuneiform_Tablet.Count() > 0)
                    {
                        _Uploaded_CSO.All_CSO_Tables.Add(ListOfTables[tabel_Index]);
                    }
                }

                /// Check if there is at least one valid table
                if (_Uploaded_CSO.All_CSO_Tables.Count() < 1)
                {
                    ViewData["Message"] = "Uploaded files does not contain any valid table!";
                    return Page();
                }

                /// Calculate average occurrences for each table
               

                /// Check if all tables have equal number of signs
                List<int> Signs_Number = new List<int>();
                foreach (var table in _Uploaded_CSO.All_CSO_Tables)
                {
                    Signs_Number.Add(table.Signs.Count());
                }
                if (Signs_Number.Distinct().Skip(1).Any())
                {
                    ViewData["Message"] = "All tables need to have equal number of signs!";
                    return Page();
                }

                //Delete all uploaded files afer reading and storing the contents
                Directory.EnumerateFiles(Tables_Path).ToList().ForEach(f => System.IO.File.Delete(f));

                return RedirectToPage("View_Lists");
            }
            ViewData["Message"] = "Uploaded files are not valid";
            return Page();
        }
    }
}
