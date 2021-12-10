using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cuneiform_Style_Analyser.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using OpenCvSharp.Flann;

namespace Cuneiform_Style_Analyser.Pages
{
    public class View_ListsModel : PageModel
    {
        private IWebHostEnvironment _hostingEnvironment;
        private readonly Uploaded_CSO _uploaded_CSO;
        public void OnGet()
        {

        }

        public View_ListsModel(IWebHostEnvironment hostingEnvironment, Uploaded_CSO Uploaded_CSO)
        {
            _hostingEnvironment = hostingEnvironment;
            _uploaded_CSO = Uploaded_CSO;
        }

        /// <summary>
        /// Delete specific table from the XRF_Table structures
        /// </summary>
        /// <param name="FN">The Folio_Source of the selected table to be deleted, the folio_source serves as an ID in this context </param>
        /// <returns></returns>
        public ActionResult OnPostDeleteTable(string FN)
        {
            for (int SameFN = 0; SameFN < _uploaded_CSO.All_CSO_Tables.Count(); SameFN++)
            {
                if (FN == _uploaded_CSO.All_CSO_Tables[SameFN].FileName)
                {
                    _uploaded_CSO.All_CSO_Tables.Remove(_uploaded_CSO.All_CSO_Tables[SameFN]);
                    break;
                }
            }

            return RedirectToPage("View_Lists");
        }

        public ActionResult OnPostCalculate()
        {
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Similarities_Tables_Folder = "Similarities_Tables";
            string CSO_Statistics_Folder = "CSO_Statistics_Individual";
            string Similarities_Tables_Path = Path.Combine(webRootPath, Similarities_Tables_Folder);
            string CSO_Statistics_Path = Path.Combine(webRootPath, CSO_Statistics_Folder);
        
            if (!Directory.Exists(Similarities_Tables_Path))
            {
                Directory.CreateDirectory(Similarities_Tables_Path);
            }
            if (!Directory.Exists(CSO_Statistics_Path))
            {
                Directory.CreateDirectory(CSO_Statistics_Path);
            }
          
            Directory.EnumerateFiles(Similarities_Tables_Path).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(CSO_Statistics_Path).ToList().ForEach(f => System.IO.File.Delete(f));
       
            // Statistics Calculation
            Cuneiform_Signs CSO = new Cuneiform_Signs(_uploaded_CSO);

            foreach (CSO_Table table in _uploaded_CSO.All_CSO_Tables)
            {
                var Results_Statistics_Style = new System.Text.StringBuilder();
                var Results_Statistics_General = new System.Text.StringBuilder();
                Results_Statistics_Style.AppendLine("Results for " + table.FileName);
                Results_Statistics_General.AppendLine("Results for " + table.FileName);
                Results_Statistics_Style.AppendLine("Tablet,Mean,SD");
                Results_Statistics_General.AppendLine("Average Mean,Average SD");

                MeanAndSD AvgMeanAndSD = CSO.MeanAndSD_ForL2(table.Cuneiform_Tablet);
                foreach (Tablet tab_Outer in table.Cuneiform_Tablet)
                {
                    Results_Statistics_Style.AppendLine(tab_Outer.Tablet_Name + "," + Convert.ToString(Math.Round(tab_Outer.Mean_Dist, 2)) + "," + Convert.ToString(Math.Round(tab_Outer.SD_Dist, 2)));
                }

                Results_Statistics_Style.AppendLine("");
                System.IO.File.WriteAllText(CSO_Statistics_Path + "/" + "Statistics_Individual_" + table.FileName + ".csv", Results_Statistics_Style.ToString());
                Results_Statistics_Style.Clear();

                table.Avg_Mean_Dist = AvgMeanAndSD.Mean;
                table.Avg_SD_Dist = AvgMeanAndSD.SD;

                Results_Statistics_General.AppendLine(table.Avg_Mean_Dist + "," + table.Avg_SD_Dist);

                /// Detect outliers
                Results_Statistics_General.AppendLine("");
                Results_Statistics_General.AppendLine("Outliers");

                foreach (Tablet tab_outer in table.Cuneiform_Tablet)
                {
                    List<Tablet> Current_Tab_List = new List<Tablet>();
                    foreach (Tablet tab_Inner in table.Cuneiform_Tablet)
                    {
                        if (tab_outer.Tablet_Name == tab_Inner.Tablet_Name)
                        {
                            continue;
                        }

                        Current_Tab_List.Add(tab_Inner.DeepCopy());
                    }
                    MeanAndSD Current_AvgMeanAndSD = CSO.MeanAndSD_ForL2(Current_Tab_List);

                    if (tab_outer.Mean_Dist > (Current_AvgMeanAndSD.Mean + Current_AvgMeanAndSD.SD))
                    {
                        table.Outliers_Tablets.Add(tab_outer.Tablet_Name);
                        Results_Statistics_General.AppendLine(tab_outer.Tablet_Name);
                    }
                }

                Results_Statistics_General.AppendLine("");
                System.IO.File.WriteAllText(CSO_Statistics_Path + "/" + "Statistics_General_" + table.FileName + ".csv", Results_Statistics_General.ToString());
                Results_Statistics_General.Clear();
            }

            // Calculate distances between Styles
            /*
            foreach (CSO_Table Outer_table in _uploaded_CSO.All_CSO_Tables)
            {
                List<double> Current_Distances = new List<double>();
                List<string> Tables_Names = new List<string>();
                Mat Outer_table_Feature = new Mat();
                foreach (float Occ in Outer_table.Average_Occurrences)
                {
                    Outer_table_Feature.Add(Occ);
                }
                Outer_table_Feature = Outer_table_Feature.T();

                foreach (CSO_Table Inner_table in _uploaded_CSO.All_CSO_Tables)
                {
                    if (Inner_table.FileName == Outer_table.FileName)
                        continue;

                    Tables_Names.Add(Inner_table.FileName);
                    Mat Inner_table_Feature = new Mat();
                    foreach (float Occ in Inner_table.Average_Occurrences)
                    {
                        Inner_table_Feature.Add(Occ);
                    }
                    Inner_table_Feature = Inner_table_Feature.T();

                    Current_Distances.Add(Cv2.Norm(Outer_table_Feature, Inner_table_Feature, NormTypes.L2));
                }

                var Results_csv = new System.Text.StringBuilder();
                Results_csv.AppendLine("Results for " + Outer_table.FileName);
                Results_csv.AppendLine("FileName,L2_Distances");

                int RankCounter = 0;
                foreach (double Dist in Current_Distances)
                {
                    Outer_table.style_Distances.Add(new Tuple<string, double>(Tables_Names[RankCounter], Math.Round(Dist, 2)));
                    RankCounter++;
                }

                Outer_table.style_Distances = Outer_table.style_Distances.OrderBy(t => t.Item2).ToList();

                foreach (var Dist in Outer_table.style_Distances)
                {   
                    Results_csv.AppendLine(Dist.Item1 + "," + Convert.ToString(Dist.Item2));                   
                }                

                Results_csv.AppendLine("");

                System.IO.File.WriteAllText(Similarities_Tables_Path + "/" + Outer_table.FileName + ".csv", Results_csv.ToString());

                Results_csv.Clear();
            }
           */
            return RedirectToPage("Statistics");
        }
    }
}