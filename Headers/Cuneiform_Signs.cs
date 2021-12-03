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
        public List<float> Average_Occurrences = new List<float>();

        public CSO_Table DeepCopy()
        {
            CSO_Table CSO_Table_Temp = new CSO_Table();
            CSO_Table_Temp.FileName = FileName;
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

            foreach (float Occ in Average_Occurrences)
            {
                CSO_Table_Temp.Average_Occurrences.Add(Occ);
            }

            return CSO_Table_Temp;
        }
    }

    public class Tablet
    {
        public string Tablet_Name;
        public double TotalNumberOfSigns = new double();
        public List<float> Occurrences = new List<float>();
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
            foreach (float Occ in Occurrences)
            {
                Tablet_Temp.Occurrences.Add(Occ);
            }

            return Tablet_Temp;
        }
    }

    public class MeanAndSD
    {
        public double Mean { get; set; } = new double();
        public double SD { get; set; } = new double();

    }

    public class Cuneiform_Signs
    {
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

                    // Check if the table contains the total number of diagnostic signs
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
                            if(string.IsNullOrEmpty(values[i]))
                            {
                                values[i] = "0";
                            }
                            Current_Tablet.Occurrences.Add(float.Parse(values[i]));
                        }
                        Current_Cuneiform_Table.Cuneiform_Tablet.Add(Current_Tablet);
                    }
                }
                All_Cuneiform_Tables.Add(Current_Cuneiform_Table);
                reader.Close();
            }

            return All_Cuneiform_Tables;
        }

        public Mat Extract_Features_CSO_Table(CSO_Table Cuneiform_Table)
        {
            Mat Cuneiform_Features = new Mat();


            foreach (Tablet Tab in Cuneiform_Table.Cuneiform_Tablet)
            {
                List<float> Current_Occurrences = new List<float>();
                Current_Occurrences.AddRange(Tab.Occurrences);
                Mat Current_Cuneiform_Features = new Mat();
                foreach (int i in Current_Occurrences)
                {
                    double Normalised_Occurrences = new double();
                    if (Tab.TotalNumberOfSigns > 0)
                    {
                        Normalised_Occurrences = i / Tab.TotalNumberOfSigns;
                        Current_Cuneiform_Features.PushBack(Normalised_Occurrences);
                    }
                    else
                    {
                        Current_Cuneiform_Features.PushBack(i);
                    }
                }
                Current_Cuneiform_Features = Current_Cuneiform_Features.T();
                Cuneiform_Features.PushBack(Current_Cuneiform_Features);
            }
            Cuneiform_Features.ConvertTo(Cuneiform_Features, MatType.CV_32F);

            return Cuneiform_Features;
        }

        public Mat Extract_Features_CSO_Tablet(Tablet Cuneiform_Tablet)
        {
            Mat Tablet_Features = new Mat();

            List<float> Current_Occurrences = new List<float>();
            Current_Occurrences.AddRange(Cuneiform_Tablet.Occurrences);
            foreach (int i in Current_Occurrences)
            {
                double Normalised_Occurrences = new double();
                if (Cuneiform_Tablet.TotalNumberOfSigns > 0)
                {
                    Normalised_Occurrences = i / Cuneiform_Tablet.TotalNumberOfSigns;
                    Tablet_Features.PushBack(Normalised_Occurrences);
                }
                else
                {
                    Tablet_Features.PushBack(i);
                }
            }
            Tablet_Features = Tablet_Features.T();
            Tablet_Features.ConvertTo(Tablet_Features, MatType.CV_32F);
            return Tablet_Features;
        }

        public void Average_Occurrences(List<CSO_Table> Tables)
        {
            foreach (CSO_Table table in Tables)
            {
                table.Average_Occurrences = Enumerable.Repeat(0.0F, table.Signs.Count()).ToList();
                foreach (Tablet tab in table.Cuneiform_Tablet)
                {
                    for (int Occ = 0; Occ < tab.Occurrences.Count(); Occ++)
                    {
                        double Normalised_Occurrences = new double();
                        if (tab.TotalNumberOfSigns > 0)
                        {
                            Normalised_Occurrences = tab.Occurrences[Occ] / tab.TotalNumberOfSigns;
                            table.Average_Occurrences[Occ] += Convert.ToSingle(Normalised_Occurrences);
                        }
                        else
                        {
                            table.Average_Occurrences[Occ] += Convert.ToSingle(tab.Occurrences[Occ]);
                        }
                    }
                }
                for (int Occ = 0; Occ < table.Average_Occurrences.Count(); Occ++)
                {
                    table.Average_Occurrences[Occ] /= table.Cuneiform_Tablet.Count();
                }
            }
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

                    tab_Outer.Dist.PushBack(Cv2.Norm(tab_Outer.CSO_Features, tab_Inner.CSO_Features, NormTypes.L2));
                }

                Mat Mean_Mat = new Mat();
                Mat SD_Mat = new Mat();
                Cv2.MeanStdDev(tab_Outer.Dist, Mean_Mat, SD_Mat);
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
    }
}
