using Microsoft.AspNetCore.Hosting;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cuneiform_Style_Analyser.Headers
{
    public class CSO_Table
    {
        public string FileName;
        public List<string> Signs = new List<string>();
        public List<Tablet> Cuneiform_Tablet = new List<Tablet>();
        public double Avg_Mean_Dist = new double();
        public double Avg_SD_Dist = new double();
        public List<string> Outliers_Tablets = new List<string>();
        public List<Tuple<string, double>> style_Distances = new List<Tuple<string, double>>();
        public Mat Dist = new Mat();

        public CSO_Table DeepCopy()
        {
            CSO_Table CSO_Table_Temp = new CSO_Table();
            CSO_Table_Temp.FileName = FileName;
            CSO_Table_Temp.Dist = Dist.Clone();
            foreach (string sin in Signs)
            {
                CSO_Table_Temp.Signs.Add(sin);
            }

            foreach (Tablet Cuni in Cuneiform_Tablet)
            {
                Tablet Tablet_Temp = new Tablet();
                Tablet_Temp = Cuni.DeepCopy();
                CSO_Table_Temp.Cuneiform_Tablet.Add(Tablet_Temp);
            }

            return CSO_Table_Temp;
        }
    }

    public class Tablet
    {
        public string Tablet_Name;
        public double TotalNumberOfSigns = new double();
        public List<string> Occurrences = new List<string>();
        public List<VariantAndVariation> SignVersions = new List<VariantAndVariation>();
        public Mat Dist = new Mat();
        public double Mean_Dist = new double();
        public double SD_Dist = new double();
        public Mat CSO_Features = new Mat();

        public Tablet DeepCopy()
        {
            Tablet Tablet_Temp = new Tablet();
            Tablet_Temp.Tablet_Name = Tablet_Name;
            Tablet_Temp.TotalNumberOfSigns = TotalNumberOfSigns;
            Tablet_Temp.Mean_Dist = Mean_Dist;
            Tablet_Temp.SD_Dist = SD_Dist;
            Tablet_Temp.CSO_Features = CSO_Features.Clone();
            Tablet_Temp.Dist = Dist.Clone();

            foreach (string Occ in Occurrences)
            {
                Tablet_Temp.Occurrences.Add(Occ);
            }

            foreach (VariantAndVariation SV in SignVersions)
            {
                Tablet_Temp.SignVersions.Add(SV);
            }

            return Tablet_Temp;
        }
    }

    public class MeanAndSD
    {
        public double Mean { get; set; } = new double();
        public double SD { get; set; } = new double();

    }

    public class VariantAndVariation
    {
        public string Variant { get; set; } = "";
        public string Variation { get; set; } = "";
        public int count { get; set; } = 0;
    }

    public class Cuneiform_Signs
    {
        // private IWebHostEnvironment _hostingEnvironment;
        private readonly Uploaded_CSO _uploaded_CSO;

        public Cuneiform_Signs(/*IWebHostEnvironment hostingEnvironment,*/ Uploaded_CSO Uploaded_CSO)
        {
            // _hostingEnvironment = hostingEnvironment;
            _uploaded_CSO = Uploaded_CSO;
        }

        public List<CSO_Table> DeepCopy(List<CSO_Table> Tables)
        {
            List<CSO_Table> Tables_Results = new List<CSO_Table>();

            foreach (CSO_Table table in Tables)
            {
                CSO_Table temp = table.DeepCopy();
                Tables_Results.Add(temp);
            }

            return Tables_Results;
        }

        /// Read the signs occurrences from all tables in Path
        public List<CSO_Table> Read_CSO_Tables(string FilePath)
        {
            List<CSO_Table> All_Cuneiform_Tables = new List<CSO_Table>();

            // loop over all valid files with ".csv" extension
            IEnumerable<string> files_CSV;
            files_CSV = Directory.EnumerateFiles(FilePath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".csv"));

            foreach (string csv_table in files_CSV)
            {
                CSO_Table Current_Cuneiform_Table = new CSO_Table();
                string OnlyFileName = System.IO.Path.GetFileNameWithoutExtension(csv_table);
                Current_Cuneiform_Table.FileName = OnlyFileName;

                string Current_Path = System.IO.Path.Combine(FilePath, csv_table);
                StreamReader reader = new StreamReader(File.OpenRead(@Current_Path));
                int FirstSign = 1;
                List<string> All_Signs = new List<string>();

                // Read the first line in the table
                string First_Line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(First_Line))
                {
                    if (First_Line.Contains(';'))
                    {
                        First_Line = First_Line.Replace(',', '.');
                    }

                    string[] values = First_Line.Split(',', ';');

                    // Check if the table contains the total number of diagnostic signs // Not needed anymore
                    if (values[1].ToLower() == "total")
                    {
                        FirstSign = 2;
                    }
                    for (int i = FirstSign; i < values.Length; i++)
                    {
                        All_Signs.Add(values[i]);
                    }
                }
                Current_Cuneiform_Table.Signs.AddRange(All_Signs);

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        if (line.Contains(';'))
                        {
                            line = line.Replace(',', '.');
                        }

                        Tablet Current_Tablet = new Tablet();
                        string[] values = line.Split(',', ';');
                        Current_Tablet.Tablet_Name = values[0];

                        // If there is total number, save it
                        if (FirstSign == 2)
                        {
                            Current_Tablet.TotalNumberOfSigns = Convert.ToDouble(values[1]);
                        }
                        else
                        {
                            Current_Tablet.TotalNumberOfSigns = 0;
                        }

                        for (int i = FirstSign; i < values.Length; i++)
                        {
                            if (string.IsNullOrEmpty(values[i]))
                            {
                                values[i] = "0";
                            }
                            Current_Tablet.Occurrences.Add(values[i]);
                        }

                        Current_Tablet = ParseOccurrences(Current_Tablet);
                        Current_Cuneiform_Table.Cuneiform_Tablet.Add(Current_Tablet);
                    }
                }
                
                All_Cuneiform_Tables.Add(Current_Cuneiform_Table);
                reader.Close();
            }


            CalculateNumberOfVariations(All_Cuneiform_Tables);


            return All_Cuneiform_Tables;
        }

        /// <summary>
        /// Parse the occurrences of a tablet and store them as variants and variations
        /// </summary>
        /// <param name="Tab"></param>
        public Tablet ParseOccurrences(Tablet Tab)
        {
            for (int i = 0; i < Tab.Occurrences.Count(); i++)
            {
                string CurOccTab = Tab.Occurrences[i];
                VariantAndVariation CurVersion_Tab = new VariantAndVariation();

                if (CurOccTab.Count() == 1)
                {
                    CurVersion_Tab.count = 1;
                    CurVersion_Tab.Variant = CurOccTab;
                    Tab.SignVersions.Add(CurVersion_Tab);
                }

                if (CurOccTab.Count() == 2)
                {
                    CurVersion_Tab.count = 2;
                    CurVersion_Tab.Variant = CurOccTab.Substring(0, 1);
                    CurVersion_Tab.Variation = CurOccTab.Substring(1, 1);
                    Tab.SignVersions.Add(CurVersion_Tab);
                }

                if (CurOccTab.Count() != 1 && CurOccTab.Count() != 2)
                {
                    CurVersion_Tab.count = 1;
                    CurVersion_Tab.Variant = "0";
                    Tab.SignVersions.Add(CurVersion_Tab);
                }
            }
            return Tab.DeepCopy();
        }

        /// <summary>
        /// Calculate the distance between different versions of signs, based of the variant and variation of each sign
        /// </summary>
        /// <param name="SignVersions1"></param>
        /// <param name="SignVersions2"></param>
        /// <returns></returns>
        public List<double> SignsDistance(List<VariantAndVariation> SignVersions1, List<VariantAndVariation> SignVersions2)
        {
            List<double> Distances = new List<double>();

            for (int i = 0; i < SignVersions1.Count(); i++)
            {
                double CurDist = 0;

                if (SignVersions1[i].Variant == "0" && SignVersions2[i].Variant == "0")
                {
                    //CurDist = 0;
                    continue;
                }
                else if (SignVersions1[i].Variant == "0" || SignVersions2[i].Variant == "0")
                {
                    //CurDist = 0.5;
                    continue;
                }
                else
                {
                    if (!SignVersions1[i].Variant.Equals(SignVersions2[i].Variant, StringComparison.OrdinalIgnoreCase))
                    {
                        CurDist = 1;
                    }
                    else
                    {
                        if (SignVersions1[i].Variation.Equals(SignVersions2[i].Variation, StringComparison.OrdinalIgnoreCase))
                        {
                            CurDist = 0;
                        }
                        else
                        {

                            if (_uploaded_CSO.VariationsNumber[i] > 0)
                            {
                                CurDist = 1.0 / _uploaded_CSO.VariationsNumber[i];
                            }
                            else
                                CurDist = 1.0;
                        }
                    }
                }
                Distances.Add(CurDist);
            }

            return Distances;
        }

        /// <summary>
        /// Calcuates the number of variations for all the signs in all the tables
        /// </summary>
        /// <param name="Tables"></param>
        /// <returns></returns>
        public void CalculateNumberOfVariations(List<CSO_Table> Tables)
        {      
            // Create an alphabet list
            char[] az = Enumerable.Range('a', 'z' - 'a' + 1).Select(i => (Char)i).ToArray();
            List<string> Alphabet = new List<string>();
            foreach (var c in az)
            {
                Alphabet.Add(c.ToString());
            }
            
            if(Tables.Count() > 0)
            {
                foreach(var sign in Tables[0].Signs)
                {
                    _uploaded_CSO.VariationsNumber.Add(0);
                }
            }    

            foreach (CSO_Table table in Tables)
            {
                foreach (Tablet tab in table.Cuneiform_Tablet)
                {
                    int index = 0;
                    foreach (VariantAndVariation version in tab.SignVersions)
                    {
                        string CurVariation = version.Variation;
                        if (String.IsNullOrWhiteSpace(CurVariation))
                        {
                            index++;
                            continue;
                        }

                        int CurIndex = Alphabet.FindIndex(x => x == CurVariation);
                        int CurVariationNumber = CurIndex + 1;
                        if(CurVariationNumber > _uploaded_CSO.VariationsNumber[index])
                        {
                            _uploaded_CSO.VariationsNumber[index] = CurVariationNumber;
                        }

                        index++;
                    }
                    
                }
            }
        }

        /// <summary>
        /// Calculate statistical information about the distances between list of tablets. The mean and standard deviation are calculated
        /// </summary>
        /// <param name="Tablets"></param>
        /// <returns></returns>
        public MeanAndSD MeanAndSD_ForL2(List<Tablet> Tablets)
        {

            MeanAndSD Results = new MeanAndSD();
            double Sum_Means = new double();
            double Sum_SDs = new double();
            double Counter = 0;
            foreach (Tablet tab_Outer in Tablets)
            {
                foreach (Tablet tab_Inner in Tablets)
                {
                    if (tab_Outer.Tablet_Name == tab_Inner.Tablet_Name)
                    {
                        continue;
                    }

                    List<double> distances = new List<double>();
                    distances = SignsDistance(tab_Outer.SignVersions, tab_Inner.SignVersions);
                    Mat distances_Mat = new Mat();
                    if (distances.Count() > 0)
                    {
                        distances_Mat.PushBack(distances.Average());
                    }
                    else
                    {
                        distances_Mat.PushBack(1);
                    }

                    tab_Outer.Dist.PushBack(distances_Mat);
                }

                Mat Mean_Mat = new Mat();
                Mat SD_Mat = new Mat();
                Cv2.MeanStdDev(tab_Outer.Dist, Mean_Mat, SD_Mat);
                tab_Outer.Dist = new Mat();

                double Mean = Mean_Mat.At<double>(0, 0);
                double SD = SD_Mat.At<double>(0, 0);
                tab_Outer.Mean_Dist = Mean;
                tab_Outer.SD_Dist = SD;
                Sum_Means += Mean;
                Sum_SDs += SD;
                Counter++;
            }
            Results.Mean = Sum_Means / Counter;
            Results.SD = Sum_SDs / Counter;

            return Results;
        }

        public double DistanceBetweenTables(CSO_Table Table1, CSO_Table Table2)
        {
            double Final_distance = 0;
            double Sum_Means = 0;
            double Counter = 0;

            foreach (Tablet tablet_table1 in Table1.Cuneiform_Tablet)
            {
                foreach (Tablet tablet_table2 in Table2.Cuneiform_Tablet)
                {
                    List<double> CurDistances = new List<double>();
                    CurDistances = SignsDistance(tablet_table1.SignVersions, tablet_table2.SignVersions);
                    Mat distances_Mat = new Mat();
                    
                    if (CurDistances.Count() > 0)
                    {
                        distances_Mat.PushBack(CurDistances.Average());
                    }
                    else
                    {
                        distances_Mat.PushBack(1);
                    }

                    Table1.Dist.PushBack(distances_Mat);
                }

                Mat Mean_Mat = new Mat();
                Mat SD_Mat = new Mat();
                Cv2.MeanStdDev(Table1.Dist, Mean_Mat, SD_Mat);
                Table1.Dist = new Mat();

                double Mean = Mean_Mat.At<double>(0, 0);
                Sum_Means += Mean;
            
                Counter++;
            }

            if (Sum_Means > 0)
            { 
                Final_distance = Sum_Means / Counter; 
            }
            else
            {
                Final_distance = 0;
            }

            return Final_distance;
        }
    }
}
