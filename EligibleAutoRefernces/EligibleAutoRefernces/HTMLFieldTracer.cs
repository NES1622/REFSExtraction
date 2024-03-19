using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace EligibleAutoRefernces
{
    internal class HTMLFieldTracer
    {
        private string folderInPath;
        private string outFile;
        private string logFile;

        private string MID;
        private string connectionString;
        bool IsUpdateinDB = false;
        private int id = 0;
        private Dictionary<string, string> dicFieldsStyles = new Dictionary<string, string>();
        private Dictionary<string, string> dicANOutput = new Dictionary<string, string>();

        public HTMLFieldTracer(string conStr,string folderInPath, string outFile, string logFile, string MID, bool isDBUpdate)
        {
            this.folderInPath = folderInPath;
            this.outFile = outFile;
            this.logFile = logFile;
            this.MID = MID;
            connectionString = conStr;// 
            IsUpdateinDB = isDBUpdate;
            string strIsDBUpdate = ConfigurationManager.AppSettings["IsNeedDBInsertion"].ToString();
           
            if (strIsDBUpdate != "0")
                IsUpdateinDB = true;

        }

        internal string AutoFontFinder(out string refflag)
        {
            string fontSizeCode = string.Empty;
            string fontSize = string.Empty;
            string fontColor = string.Empty;
            string fontStroke = string.Empty;

            StringBuilder sbqry = new StringBuilder();
            string pattern = @"<[^>]+>";
            refflag = string.Empty;

            string[] HTMLfiles = Directory.GetFiles(folderInPath, "*.html", SearchOption.TopDirectoryOnly);
            //  for (int rc = 0; rc < HTMLfiles.Length; rc++)
            for (int rc = HTMLfiles.Length - 1; rc > 0; rc--)
            {
                string[] lines = File.ReadAllLines(HTMLfiles[rc]);

                if (HTMLfiles[rc].Contains("173385880"))
                    rc = rc;
                bool isQueryDone = false;

                if (lines.Length > 0)
                {
                    for (int rl = 0; rl < lines.Length; rl++)
                    {
                        bool IsRefFound = false; 

                        if ((Regex.Replace(lines[rl], pattern, string.Empty).ToUpper().Trim() == "REFERENCES" || lines[rl].Contains(">Références</div>") ||
                                  lines[rl].Contains(">REFERENCES</div>") || lines[rl].Contains(">References</div>") || lines[rl].Contains(">REFERENCE</div>") || lines[rl].Contains(">Notes</div>") || lines[rl].Contains(">Reference</div>"))
                                  && lines[rl].Contains("<div"))
                        {
                            string divcontent = lines[rl].Substring(lines[rl].IndexOf("<div"), lines[rl].IndexOf("</div>"));

                            if (divcontent.Length > 0 && divcontent.Contains(" "))
                            {
                                string[] divwords = divcontent.Split(new char[] { ' ' });

                                fontSizeCode = divwords[7];
                                fontColor = divwords[8];
                                fontStroke = divwords[9];


                                fontSize = GetFontSizeFromCode(HTMLfiles[rc], fontSizeCode);


                                if (fontSize.StartsWith("font-size") == false)
                                {
                                    ;
                                }
                                else
                                {

                                    string qrytmp = " insert into MIDREFDetails(MID, FieldName, FontSize, FieldFonts, Isworking, DataType, FontFamily, FontColor, FontStroke, FontHeight) " +
                                                 " select '" + MID + "', 'RH', '" + fontSize + "', '" + fontColor + " " + fontStroke + "', 1, 'Data', 'ff', '" + fontColor + "', '" + fontStroke + "', 'h' ";

                                    qrytmp += " WHERE NOT EXISTS( SELECT 1 FROM MIDREFDetails WHERE MID = '" + MID + "' AND FieldName = 'RH' AND FontSize = '" + fontSize + "' AND FieldFonts = '" + fontColor + " " + fontStroke + "')";

                                    sbqry.AppendLine(qrytmp);
                                    IsRefFound = true;
                                    rl++;
                                }
                            }

                            if (lines[rl].Contains("<div") && lines[rl].Contains("</div>"))
                                divcontent = lines[rl].Substring(lines[rl].IndexOf("<div"), lines[rl].IndexOf("</div>"));


                            if (IsRefFound && !isQueryDone && divcontent.Length > 0 && divcontent.Contains(" "))
                            {
                                string[] divwords = divcontent.Split(new char[] { ' ' });

                                fontSizeCode = divwords[7];
                                fontColor = divwords[8];
                                fontStroke = divwords[9];

                                string plaincontent = Regex.Replace(divcontent, pattern, string.Empty).Trim();
                                if (plaincontent.Length > 0 && char.IsDigit(plaincontent[0]))
                                    refflag += HTMLfiles[rc] + " Numbered Reference Found\n";

                                fontSize = GetFontSizeFromCode(HTMLfiles[rc], fontSizeCode);

                                string qrytmp = " insert into MIDREFDetails(MID, FieldName, FontSize, FieldFonts, Isworking, DataType, FontFamily, FontColor, FontStroke, FontHeight) " +
                                             " select '" + MID + "', 'RF', '" + fontSize + "', '" + fontColor + " " + fontStroke + "', 1, 'Data', 'ff', '" + fontColor + "', '" + fontStroke + "', 'h' ";


                                qrytmp += " WHERE NOT EXISTS( SELECT 1 FROM MIDREFDetails WHERE MID = '" + MID + "' AND FieldName = 'RF' AND FontSize = '" + fontSize + "' AND FieldFonts = '" + fontColor + " " + fontStroke + "')";


                                sbqry.AppendLine(qrytmp);


                                string addlQuery = GetAdditionalFontStyles(HTMLfiles[rc], rl + 1, fontSize, fontSizeCode, fontColor, fontStroke, MID);
                                sbqry.AppendLine(addlQuery);


                                isQueryDone = true;
                            }
                        }

                        if (IsRefFound)
                        {
                            if (refflag.Contains(HTMLfiles[rc]) == false)
                                refflag += HTMLfiles[rc] + " Non numbered Reference Found\n";
                            break;
                        }
                    }
                }

                if (refflag.Contains(HTMLfiles[rc]) == false)
                    refflag += HTMLfiles[rc] + " NO Reference found\n";

                if (sbqry.Length > 0 && IsUpdateinDB)
                {
                    string[] splitstr = { "\n" };
                    string[] insertQueries = sbqry.ToString().Split(splitstr, StringSplitOptions.RemoveEmptyEntries);

                    ExecuteMultipleInserts(insertQueries);
                    sbqry.Length = 0;
                }
            }

            return sbqry.ToString();
        }

        public void ExecuteMultipleInserts(string[] insertQueries)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    try
                    {
                        foreach (string query in insertQueries)
                        {
                            command.CommandText = query;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Console.WriteLine("Inserts executed successfully.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Error executing inserts: " + ex.Message);
                    }
                }
            }
        }
        private string GetAdditionalFontStyles(string hFile, int lineNo, string fontSize, string fontSizeCode, string fontColor, string fontStroke, string mID)
        {
            string[] lines = File.ReadAllLines(hFile);
            string prevline = string.Empty;
            string nextline = string.Empty;
            string curLine = string.Empty;

            string divcontent = string.Empty;
            StringBuilder sbtmp = new StringBuilder();
            try
            {

                for (int rl = lineNo; rl < lines.Length; rl++)
                {
                    if (lines[rl].Contains(">mine D</div>"))
                        rl = rl;


                    if (hFile.Contains("173036143"))
                        hFile = hFile;

                    if (rl == 360)
                        rl = rl;

                    if (lines[rl].Contains("<div"))
                    {
                        curLine = lines[rl];
                        divcontent = lines[rl].Substring(lines[rl].IndexOf("<div"), lines[rl].IndexOf(">"));
                        nextline = lines[rl + 1];
                        if (nextline.Contains("<div") == false)
                        {
                            for (; rl + 1 < lines.Length; rl++)
                            {
                                if (lines[rl + 1].Contains("<div"))
                                { nextline = lines[rl + 1]; break; }
                            }
                        }

                        if (rl + 1 < lines.Length)
                        {
                            nextline = lines[rl + 1].Substring(lines[rl + 1].IndexOf("<div"), lines[rl + 1].IndexOf(">"));
                        }


                        if (divcontent.Contains(" " + fontSizeCode + " ") == false && nextline.Length > 0 && prevline.Length > 0 &&
                              nextline.Contains(" " + fontSizeCode + " ") && prevline.Contains(" " + fontSizeCode + " ") && divcontent.Contains(fontStroke))
                        {
                            string[] fontdetails = divcontent.Split(' ');
                            string fontSizeNew = GetFontSizeFromCode(hFile, fontdetails[7]);
                            string qrytmp = " insert into MIDREFDetails(MID, FieldName, FontSize, FieldFonts, Isworking, DataType, FontFamily, FontColor, FontStroke, FontHeight) " +
                                                " select '" + MID + "', 'RF', '" + fontSizeNew + "', '" + fontdetails[8] + " " + fontdetails[9] + "', 1, 'Data', 'ff', '" + fontdetails[8] + "', '" + fontdetails[9] + "', 'h' ";

                            qrytmp += " WHERE NOT EXISTS( SELECT 1 FROM MIDREFDetails WHERE MID = '" + MID + "' AND FieldName = 'RF' AND FontSize = '" + fontSizeNew + "' AND FieldFonts = '" + fontdetails[8] + " " + fontdetails[9] + "')";

                            if (sbtmp.ToString().Contains(qrytmp) == false)
                            {
                                sbtmp.AppendLine(qrytmp);
                            }
                        }
                        else if (divcontent.Contains(" " + fontColor + " ") == false && divcontent.Contains(" " + fontSizeCode + " "))
                        {
                            string[] fontdetails2 = divcontent.Split(' ');
                            string qrytmp = " insert into MIDREFDetails(MID, FieldName, FontSize, FieldFonts, Isworking, DataType, FontFamily, FontColor, FontStroke, FontHeight) " +
                                                " select '" + MID + "', 'RF', '" + fontSize + "', '" + fontdetails2[8] + " " + fontStroke + "', 1, 'Data', 'ff', '" + fontdetails2[8] + "', '" + fontStroke + "', 'h' ";
                            qrytmp += " WHERE NOT EXISTS( SELECT 1 FROM MIDREFDetails WHERE MID = '" + MID + "' AND FieldName = 'RF' AND FontSize = '" + fontSize + "' AND FieldFonts = '" + fontdetails2[8] + " " + fontdetails2[9] + "')";

                            if (sbtmp.ToString().Contains(qrytmp) == false)
                            {
                                sbtmp.AppendLine(qrytmp);
                            }
                        }
                    }

                    prevline = divcontent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            return sbtmp.ToString();
        }

        private string GetFontSizeFromCode(string hFile, string fontSizeCode)
        {
            string cssfile = hFile.Replace(".html", ".css");

            string cssContent = File.ReadAllText(cssfile);
            string[] cssLines = cssContent.Split('\n');
            for (int ri = 0; ri < cssLines.Length; ri++)
            {
                if (cssLines[ri].Length > 0)
                {
                    if (cssLines[ri].StartsWith(".fs") &&
                               cssLines[ri].Contains("font-size:") && cssLines[ri].Contains("px;"))
                    {
                        if (cssLines[ri].Contains(fontSizeCode + "{"))
                        {
                            string fontline = cssLines[ri];
                            fontline = fontline.Substring(fontline.IndexOf("{") + 1);
                            fontline = fontline.Trim('}');
                            return fontline;
                        }
                    }
                }
            }
            return "";
        }

        internal void GetFontDetails()
        {
            DataTable dtFonts = GetFontDetailsFromDatabase();

            string FieldName = "";
            string FontHeight = "";
            string FontFamily = "";
            string FontSize = "";
            string FontColor = "";
            string FontStroke = "";

            #region font details in css files
            /*

           //find each field below font details and match through out the PDF pages
           //ff{d} - font family - fixed
           //fs{d} - font size - dynamic
           //fc{d} - font color - fixed
           //fc{d} - font color - fixed
           //sc{d} - text stroke - fixed

           //Geographical align - no use
           //x{d} - x position of line
           //y{d} - y position of line

           //font height
           //h{d} - height (font/line height)
           //l{d} - line space
           //w{d} - word space

           //other details
           //t - text (might be)
           //m{d} - matrix (no clude at this moment, might be no use)
           //d - design (might be)
           */

            #endregion

            for (int i = 0; i < dtFonts.Rows.Count; i++)
            {
                FieldName = dtFonts.Rows[i]["FieldName"].ToString();
                FontHeight = dtFonts.Rows[i]["FontHeight"].ToString();
                FontFamily = dtFonts.Rows[i]["FontFamily"].ToString();
                FontSize = dtFonts.Rows[i]["FontSize"].ToString();
                FontColor = dtFonts.Rows[i]["FontColor"].ToString();
                FontStroke = dtFonts.Rows[i]["FontStroke"].ToString();

                if (dicFieldsStyles.ContainsKey(FieldName) == false)
                {
                    dicFieldsStyles.Add(FieldName, FontHeight + " " + FontFamily + " " + FontSize + " " + FontColor + " " + FontStroke);
                }
                else if (dicFieldsStyles.ContainsKey(FieldName))
                {
                    dicFieldsStyles[FieldName] = dicFieldsStyles[FieldName] + "|" + FontHeight + " " + FontFamily + " " + FontSize + " " + FontColor + " " + FontStroke;
                }

            }

        }

        private DataTable GetFontDetailsFromDatabase()
        {
            // Create a DataTable to hold the data
            DataTable dataTable = new DataTable();

            // Use a SqlConnection to connect to the database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Define the SQL query you want to execute
                string sqlQuery = "select * from MIDREFDetails where MID='" + MID + "'";

                // Use a SqlDataAdapter to retrieve data from the database and fill the DataTable
                using (SqlDataAdapter adapter = new SqlDataAdapter(sqlQuery, connection))
                {
                    // Fill the DataTable with the data
                    adapter.Fill(dataTable);
                }

                // Close the connection
                connection.Close();
            }

            return dataTable;
        }

        //internal List<Tuple<int, string, string>> IdentifyREFs(ref StringBuilder sbANPCM,string MID,string IssueDate)
        //{
        //    StringBuilder sbRefs = new StringBuilder();
        //    // Creating a list to store two variables
        //    List<Tuple<int, string, string>> dataList = new List<Tuple<int, string, string>>();

        //    try
        //    {

        //        // Get an array of file paths in the specified folder
        //        // Get all the files in the folder with the .css extension
        //        string[] cssfiles = Directory.GetFiles(folderInPath, "*.css", SearchOption.TopDirectoryOnly);
        //        string pattern = @"<[^>]+>";
        //        string strData = "";
               
        //        StringBuilder sbANtemp = new StringBuilder();
               
               
        //        string strAN = string.Empty;
        //        int refAN = 0;

        //        for (int rc = 0; rc < cssfiles.Length; rc++)
        //        {
        //            if (cssfiles[rc].Contains("160868421"))
        //                rc = rc;
        //            if (cssfiles[rc].Contains("_"))
        //            {
        //                Dictionary<string, string> dicFontStylesHeading = new Dictionary<string, string>();
        //                dicFontStylesHeading = GetFontStylesFromCss(cssfiles, rc, "RH");

        //                Dictionary<string, string> dicFontStylesData = new Dictionary<string, string>();
        //                dicFontStylesData = GetFontStylesFromCss(cssfiles, rc, "RF");

        //                string HTMLFileName = cssfiles[rc].Replace(".css", ".html");
        //                if (File.Exists(HTMLFileName))
        //                {
        //                    string htmlData = File.ReadAllText(HTMLFileName);
        //                    strAN = HTMLFileName.Split('\\').Last();
        //                    strAN = strAN.Replace(".html", ".xml");

        //                    htmlData = htmlData.Replace("</div>", "</div>\n");
        //                    htmlData = htmlData.Replace("\r", "");
        //                    htmlData = htmlData.Replace("\n\n", "\n");

        //                    string[] htmlLines = htmlData.Split('\n');
        //                    bool IsFoundAndContinue = false;
        //                    StringBuilder sbtemp = new StringBuilder();
                                                


        //                    int LineHaveReferenceHeading = 0;
        //                    for (int rp = htmlLines.Length-1; rp > 0; rp--)
        //                    {
        //                        if (htmlLines[rp].Length > 0)
        //                        {
        //                            if (rp > 1340)
        //                                rp = rp;
        //                            if (htmlLines[rp].StartsWith("<div class"))
        //                            {
        //                                string divdata = htmlLines[rp].Substring(0, htmlLines[rp].IndexOf(">"));
        //                                string linedata = htmlLines[rp];
        //                                bool IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesHeading);

        //                                if (IsFontCodesFound && (Regex.Replace(htmlLines[rp], pattern, string.Empty).ToUpper().Trim() == "REFERENCES"
        //                                    || linedata.Contains(">REFERENCES</div>") ||
        //                                    linedata.Contains(">References</div>") ||
        //                                    linedata.Contains(">Reference</div>")))
        //                                {
        //                                    LineHaveReferenceHeading = rp;
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }

        //                    if (LineHaveReferenceHeading > 0)
        //                    {
        //                        for (int rp = LineHaveReferenceHeading; rp < htmlLines.Length; rp++)
        //                        {
        //                            if (htmlLines[rp].StartsWith("<div class"))
        //                            {
        //                                string divdata = htmlLines[rp].Substring(0, htmlLines[rp].IndexOf(">"));
        //                                string linedata = htmlLines[rp];
        //                                bool IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesHeading);

        //                                if (IsFoundAndContinue)
        //                                    IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesData);

        //                                if (linedata.Contains("<div class=\"") && (IsFoundAndContinue && !IsFontCodesFound))
        //                                {
        //                                    bool IsNextPagefound = false;
        //                                    bool IsNextFontFound = false;
        //                                    for (int i = rp; i < htmlLines.Length; i++)
        //                                    {
        //                                        string nextLine = htmlLines[i];
        //                                        if (nextLine.Contains("\" data-page-no=\"") == true)
        //                                            IsNextPagefound = true;

        //                                        if (IsNextPagefound)
        //                                        {
        //                                            for (; i < htmlLines.Length; i++)
        //                                            {
        //                                                nextLine = htmlLines[i];
        //                                                if (nextLine.Length > 0)
        //                                                {
        //                                                    divdata = nextLine.Substring(0, nextLine.IndexOf(">"));
        //                                                    IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesData);
        //                                                    if (IsFontCodesFound)
        //                                                    {
        //                                                        linedata = htmlLines[i];
        //                                                        rp = i;
        //                                                        IsNextFontFound = true;
        //                                                        break;
        //                                                    }
        //                                                }
        //                                            }
        //                                        }

        //                                        if (IsNextFontFound)
        //                                            break;
        //                                    }
        //                                }
        //                                if (IsFontCodesFound && (Regex.Replace(htmlLines[rp], pattern, string.Empty).ToUpper().Trim() == "REFERENCES"
        //                                    || linedata.Contains(">REFERENCES</div>") ||
        //                                    linedata.Contains(">References</div>") ||
        //                                    linedata.Contains(">Reference</div>")))
        //                                {
        //                                    strData = htmlLines[rp];
        //                                    sbRefs.AppendLine(strData);
        //                                    dataList.Add(Tuple.Create(1, cssfiles[rc], strData));
        //                                    strData = Regex.Replace(strData, pattern, string.Empty);
        //                                    sbtemp.AppendLine(strData);
        //                                    sbANtemp.AppendLine(strData);
        //                                    IsFoundAndContinue = true;
        //                                }
        //                                else if (IsFontCodesFound && IsFoundAndContinue)
        //                                {
        //                                    //strData = htmlLines[rp].Replace("<span class=\"ff", " | <span class=\"ff");
        //                                    strData = htmlLines[rp];
        //                                    dataList.Add(Tuple.Create(1, cssfiles[rc], strData));
        //                                    strData = Regex.Replace(strData, pattern, string.Empty);
        //                                    string xcode = FindXCode(htmlLines[rp], cssfiles[rc]).Trim();
                                          
        //                                    if (rp + 1 < htmlLines.Length)
        //                                    {
        //                                        string xcodeNxt = FindXCode(htmlLines[rp + 1], cssfiles[rc]).Trim();
        //                                        if (Convert.ToDouble(xcodeNxt) >= Convert.ToDouble(xcode))
        //                                        {
        //                                            //Console.WriteLine(sbANtemp.ToString());
        //                                            if (sbANtemp.ToString().Trim() != "References")
        //                                            {
        //                                                refAN++;
        //                                                //sbANlog.AppendLine(strAN + "\t" + refAN + "\t" + sbANtemp.ToString());
        //                                                dicANOutput.Add(strAN + "_" + refAN, sbANtemp.ToString());
        //                                            }


        //                                            sbtemp.AppendLine("-------");
        //                                            sbANtemp.Clear();



        //                                        }
                                              


        //                                    }
        //                                    sbtemp.AppendLine(strData);
        //                                    sbANtemp.Append(strData);


        //                                }
        //                                else if (IsFoundAndContinue && !IsFontCodesFound)
        //                                {
        //                                   // dicANOutput.Add(strAN + "_" + refAN, sbANtemp.ToString());
        //                                   // sbANtemp.Clear();
        //                                    break;
        //                                }
                                           
        //                            }
        //                        }
        //                    }
                           
        //                    string pcm = PCM(dicANOutput,MID, IssueDate);
        //                    if(pcm.Length>0)
        //                    sbANPCM.AppendLine(pcm);
        //                    dicANOutput.Clear();
        //                    refAN = 0;


        //                }
        //            }
        //        }
              
               
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }


        //    return dataList;
        //}

        internal List<Tuple<int, string, string>> IdentifyREFs()
        {
            StringBuilder sbRefs = new StringBuilder();
            // Creating a list to store two variables
            List<Tuple<int, string, string>> dataList = new List<Tuple<int, string, string>>();

            try
            {

                // Get an array of file paths in the specified folder
                // Get all the files in the folder with the .css extension
                string[] cssfiles = Directory.GetFiles(folderInPath, "*.css", SearchOption.TopDirectoryOnly);
                string pattern = @"<[^>]+>";
                string strData = "";

                StringBuilder sbANtemp = new StringBuilder();


                string strAN = string.Empty;
                int refAN = 0;

                for (int rc = 0; rc < cssfiles.Length; rc++)
                {
                    if (cssfiles[rc].Contains("174512143"))
                        rc = rc;
                    if (cssfiles[rc].Contains("_"))
                    {
                        Dictionary<string, string> dicFontStylesHeading = new Dictionary<string, string>();
                        dicFontStylesHeading = GetFontStylesFromCss(cssfiles, rc, "RH");

                        Dictionary<string, string> dicFontStylesData = new Dictionary<string, string>();
                        dicFontStylesData = GetFontStylesFromCss(cssfiles, rc, "RF");

                        string HTMLFileName = cssfiles[rc].Replace(".css", ".html");
                        if (File.Exists(HTMLFileName))
                        {
                            string htmlData = File.ReadAllText(HTMLFileName);
                            strAN = HTMLFileName.Split('\\').Last();
                            strAN = strAN.Replace(".html", ".xml");

                            htmlData = htmlData.Replace("</div>", "</div>\n");
                            htmlData = htmlData.Replace("\r", "");
                            htmlData = htmlData.Replace("\n\n", "\n");

                            string[] htmlLines = htmlData.Split('\n');
                            bool IsFoundAndContinue = false;
                            StringBuilder sbtemp = new StringBuilder();



                            int LineHaveReferenceHeading = 0;
                            for (int rp = htmlLines.Length - 1; rp > 0; rp--)
                            {
                                if (htmlLines[rp].Length > 0)
                                {
                                    if (rp > 1340)
                                        rp = rp;
                                    if (htmlLines[rp].StartsWith("<div class"))
                                    {
                                        string divdata = htmlLines[rp].Substring(0, htmlLines[rp].IndexOf(">"));
                                        string linedata = htmlLines[rp];
                                        bool IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesHeading);

                                        if (IsFontCodesFound && (Regex.Replace(htmlLines[rp], pattern, string.Empty).ToUpper().Trim() == "REFERENCES"
                                            || linedata.Contains(">REFERENCES</div>") ||
                                            linedata.Contains(">References</div>") ||
                                             linedata.Contains(">Notes</div>") ||
                                             linedata.Contains(">REFERENCE</div>") ||
                                            linedata.Contains(">Reference</div>")))
                                        {
                                            LineHaveReferenceHeading = rp;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (LineHaveReferenceHeading > 0)
                            {
                                for (int rp = LineHaveReferenceHeading; rp < htmlLines.Length; rp++)
                                {
                                    if (htmlLines[rp].StartsWith("<div class"))
                                    {
                                        string divdata = htmlLines[rp].Substring(0, htmlLines[rp].IndexOf(">"));
                                        string linedata = htmlLines[rp];
                                        bool IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesHeading);

                                        if (IsFoundAndContinue)
                                            IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesData);

                                        if (linedata.Contains("<div class=\"") && (IsFoundAndContinue && !IsFontCodesFound))
                                        {
                                            bool IsNextPagefound = false;
                                            bool IsNextFontFound = false;
                                            for (int i = rp; i < htmlLines.Length; i++)
                                            {
                                                string nextLine = htmlLines[i];
                                                if (nextLine.Contains("\" data-page-no=\"") == true)
                                                    IsNextPagefound = true;

                                                if (IsNextPagefound)
                                                {
                                                    for (; i < htmlLines.Length; i++)
                                                    {
                                                        nextLine = htmlLines[i];
                                                        if (nextLine.Length > 0)
                                                        {
                                                            divdata = nextLine.Substring(0, nextLine.IndexOf(">"));
                                                            IsFontCodesFound = IsFontCodesFoundInDiv(divdata, dicFontStylesData);
                                                            if (IsFontCodesFound)
                                                            {
                                                                linedata = htmlLines[i];
                                                                rp = i;
                                                                IsNextFontFound = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                                if (IsNextFontFound)
                                                    break;
                                            }
                                        }
                                        if (IsFontCodesFound && (Regex.Replace(htmlLines[rp], pattern, string.Empty).ToUpper().Trim() == "REFERENCES"
                                            || linedata.Contains(">REFERENCES</div>") ||
                                            linedata.Contains(">References</div>") ||
                                             linedata.Contains(">Notes</div>") ||
                                             linedata.Contains(">REFERENCE</div>") ||
                                            linedata.Contains(">Reference</div>")))
                                        {
                                            strData = htmlLines[rp];
                                            sbRefs.AppendLine(strData);
                                            dataList.Add(Tuple.Create(1, cssfiles[rc], strData));
                                            strData = Regex.Replace(strData, pattern, string.Empty);
                                            sbtemp.AppendLine(strData);
                                            sbANtemp.AppendLine(strData);
                                            IsFoundAndContinue = true;
                                        }
                                        else if (IsFontCodesFound && IsFoundAndContinue)
                                        {
                                            //strData = htmlLines[rp].Replace("<span class=\"ff", " | <span class=\"ff");
                                            strData = htmlLines[rp];
                                            dataList.Add(Tuple.Create(1, cssfiles[rc], strData));
                                            strData = Regex.Replace(strData, pattern, string.Empty);
                                            //string xcode = FindXCode(htmlLines[rp], cssfiles[rc]).Trim();

                                            //if (rp + 1 < htmlLines.Length)
                                            //{
                                            //    string xcodeNxt = FindXCode(htmlLines[rp + 1], cssfiles[rc]).Trim();
                                            //    if (Convert.ToDouble(xcodeNxt) >= Convert.ToDouble(xcode))
                                            //    {
                                            //        //Console.WriteLine(sbANtemp.ToString());
                                            //        if (sbANtemp.ToString().Trim() != "References")
                                            //        {
                                            //            refAN++;
                                            //            //sbANlog.AppendLine(strAN + "\t" + refAN + "\t" + sbANtemp.ToString());
                                            //            dicANOutput.Add(strAN + "_" + refAN, sbANtemp.ToString());
                                            //        }


                                            //        sbtemp.AppendLine("-------");
                                            //        sbANtemp.Clear();



                                            //    }



                                            //}
                                            //sbtemp.AppendLine(strData);
                                            //sbANtemp.Append(strData);


                                        }
                                        else if (IsFoundAndContinue && !IsFontCodesFound)
                                        {
                                            // dicANOutput.Add(strAN + "_" + refAN, sbANtemp.ToString());
                                            // sbANtemp.Clear();
                                            break;
                                        }

                                    }
                                }
                            }

                            //string pcm = PCM(dicANOutput, MID, IssueDate);
                            //if (pcm.Length > 0)
                            //    sbANPCM.AppendLine(pcm);
                            //dicANOutput.Clear();
                            //refAN = 0;


                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            return dataList;
        }
        private string PCM(Dictionary<string, string> dicANOutput,string MID,string IssueDate)
        {
            double matchPer = 0.0;
            Dictionary<string, string> dicANLiveOutput = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            string output = string.Empty;
            //ID(Auto), MID, IssueDate, ANID,RefID,LiveReference,ExtractedReference, PercentageMatched,IsNumbered,EntryDate

            foreach (var dicOut in dicANOutput)
            {
                string strKey = dicOut.Key.ToString();
                string strValue = dicOut.Value.ToString();
                dicANLiveOutput = GetLiveRefsdetails(strKey);

                break;
            }
            foreach (var dicOut in dicANOutput)
            {
                
                string strKey = dicOut.Key.ToString();
                string strValue = dicOut.Value.ToString();
                string strLiveValue = string.Empty;
                if (dicANLiveOutput.ContainsKey(strKey))
                {
                    strLiveValue = dicANLiveOutput[strKey].ToString();
                }
                string[] str = strKey.Split('_');
                strValue = strValue.Replace("  ", " ");
                //strValue = strLiveValue;
                float disOfName = Distance(strValue, strLiveValue);
                matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
                output = id++ + "\t" + MID + "\t" + IssueDate + "\t" + str[0] + "\t" + str[1] + "\t" + strLiveValue+ "\t"+strValue + "\t" + matchPer;
                sb.AppendLine(output);

            }

            output = sb.ToString();
            return output;
            // Compare sets and calculate percentage matching
            //var matchingPercentages = CompareSets(dicANOutput, dicANLiveOutput);

            //throw new NotImplementedException();
        }

        public static float Distance(string s1, string s2)
        {
            int maxOffset = 5;
            if (string.IsNullOrEmpty(s1))
            {
                return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            }

            if (string.IsNullOrEmpty(s2))
            {
                return s1.Length;
            }

            int c = 0;
            int offset1 = 0;
            int offset2 = 0;
            int dist = 0;
            while ((c + offset1 < s1.Length) && (c + offset2 < s2.Length))
            {
                if (s1[c + offset1] != s2[c + offset2])
                {
                    offset1 = 0;
                    offset2 = 0;
                    for (int i = 0; i < maxOffset; i++)
                    {
                        if ((c + i < s1.Length) && (s1[c + i] == s2[c]))
                        {
                            if (i > 0)
                            {
                                dist++;
                                offset1 = i;
                            }

                            goto ender;
                        }

                        if ((c + i < s2.Length) && (s1[c] == s2[c + i]))
                        {
                            if (i > 0)
                            {
                                dist++;
                                offset2 = i;
                            }

                            goto ender;
                        }
                    }

                    dist++;
                }

            ender:
                c++;
            }

            return dist + ((s1.Length - offset1 + s2.Length - offset2) / 2) - c;
        }

        private Dictionary<string, string> GetLiveRefsdetails(string strKey)
        {
            Dictionary<string, string> dicANLiveOutput = new Dictionary<string, string>();
            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
            //select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN = '174698482.xml' and a.ANId =b.ANID 
            strKey = strKey.Replace("_1", "");
            string query = "select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN ='" + strKey + "' and a.ANId =b.ANID"; // Replace with your SELECT query
            DataTable resultTable = ExecuteQuery(connectionStringREF, query);
            for (int i = 0; i < resultTable.Rows.Count; i++)
            {
                dicANLiveOutput.Add(resultTable.Rows[i]["AN"].ToString() + "_" + resultTable.Rows[i]["RefNo"].ToString(), resultTable.Rows[i]["ER"].ToString());
            }

            return dicANLiveOutput;

        }
        public DataTable ExecuteQuery(string connectionString, string query)
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }
        private string FindXCode(string lineData, string csfile)
        {
            string xcode = string.Empty;
            if (lineData.Contains(" x"))
            {
                xcode = lineData.Substring(lineData.IndexOf(" x"));
                xcode = xcode.Trim();
                xcode = xcode.Substring(0, xcode.IndexOf(" ")).Trim();
            }

            //return xcode;

            string xlocation = GetXPositionFromCSS(xcode, csfile);

            return xlocation;
        }
        private string GetXPositionFromCSS(string xcode, string csfile)
        {
            string xLoc = string.Empty;
            string[] lines = File.ReadAllLines(csfile);
            for (int rl = 0; rl < lines.Length; rl++)
            {
                if (lines[rl].StartsWith("." + xcode) && lines[rl].Contains("px"))
                {
                    xLoc = lines[rl].Substring(lines[rl].IndexOf(":") + 1);
                    xLoc = xLoc.Substring(0, xLoc.IndexOf("px"));
                    break;
                }
            }
            return xLoc;
        }

        private bool IsFontCodesFoundInDiv(string divdata, Dictionary<string, string> dicFontStyles)
        {
            bool isFontCodesFound = true;

            if (dicFieldsStyles.Count == 0)
                isFontCodesFound = false;
            string FontFoundStatus = "";
            foreach (var dicFont in dicFontStyles)
            {
                string strkey = dicFont.Key.ToString();
                string strval = dicFont.Value.ToString();

                if (strval.Length > 0)
                {
                    string[] strFontStyles = strval.Split('|');


                    for (int i = 0; i < strFontStyles.Length; i++)
                    {
                        bool isOneStyleFontCodeFound = true;
                        string[] fontcodes = strFontStyles[i].Split(' ');

                        for (int c = 0; c < fontcodes.Length; c++)
                        {
                            //if (c != 0)
                            {
                                if (divdata.Contains(fontcodes[c]) == false)
                                    isOneStyleFontCodeFound = false;
                            }
                        }

                        if (isOneStyleFontCodeFound)
                            FontFoundStatus += "1";
                        else
                            FontFoundStatus += "0";
                    }
                }
            }

            if (FontFoundStatus.Contains("1"))
                isFontCodesFound = true;
            else
                isFontCodesFound = false;

            return isFontCodesFound;
        }


        private Dictionary<string, string> GetFontStylesFromCss(string[] cssfiles, int rc, string Field)
        {
            Dictionary<string, string> dicTemp = new Dictionary<string, string>();

            // Read the content of the .css file
            string cssContent = File.ReadAllText(cssfiles[rc]);
            string[] cssLines = cssContent.Split('\n');

            string[] FieldDetails = GetFieldStyleRaw(Field).Split('|');

            if (FieldDetails.Length > 0)
            {
                for (int rf = 0; rf < FieldDetails.Length; rf++)
                {
                    string[] EachField = FieldDetails[rf].Split(' ');//*************//

                    for (int ri = 0; ri < cssLines.Length; ri++)
                    {
                        if (cssLines[ri].Length > 0)
                        {
                            ////Get font family code from css using fontsize in pixel
                            #region
                            //if (cssLines[ri].StartsWith("@font-face") &&
                            //    cssLines[ri].Contains("font-family:"))

                            //{
                            //    if (EachField[1].ToString().Length > 0 &&
                            //     cssLines[ri].Contains(EachField[1]))
                            //    {
                            //        string tmp = cssLines[ri].Substring(cssLines[ri].IndexOf(":") + 1);
                            //        tmp = tmp.Substring(0, tmp.IndexOf(";"));

                            //        FieldDetails[rf] = FieldDetails[rf].Replace(EachField[1], tmp);
                            //    }
                            //}
                            #endregion
                            //Get font size code from css using fontsize in pixel
                            if (cssLines[ri].StartsWith(".fs") &&
                                cssLines[ri].Contains("font-size:") && cssLines[ri].Contains("px;")
                                && EachField.Length > 1)

                            {
                                if (EachField[2].ToString().Length > 0 &&
                                 cssLines[ri].Contains(EachField[2]))
                                {
                                    string tmp = cssLines[ri].Substring(1, cssLines[ri].IndexOf("{") - 1);

                                    FieldDetails[rf] = FieldDetails[rf].Replace(EachField[2], tmp);
                                }
                            }

                            ////Get font height code from css using fontsize in pixel
                            #region
                            //if (cssLines[ri].StartsWith(".h") &&
                            //    cssLines[ri].Contains("height:") && cssLines[ri].Contains("px;"))

                            //{
                            //    if (EachField[0].ToString().Length > 0 &&
                            //     cssLines[ri].Contains(EachField[0]))
                            //    {
                            //        string tmp = cssLines[ri].Substring(1, cssLines[ri].IndexOf("{") - 1);

                            //        FieldDetails[rf] = FieldDetails[rf].Replace(EachField[0], tmp);
                            //    }
                            //}
                            #endregion
                        }
                    }

                    if (dicTemp.ContainsKey(Field) == false)
                        dicTemp.Add(Field, FieldDetails[rf]);
                    else
                        dicTemp[Field] = dicTemp[Field] + "|" + FieldDetails[rf];
                }
            }
            return dicTemp;
        }

        private string GetFieldStyleRaw(string field)
        {
            string outstr = "";
            foreach (var dicOne in dicFieldsStyles)
            {
                string strkey = dicOne.Key.ToString();
                string strval = dicOne.Value.ToString();

                if (strkey.Length > 0 && strkey == field)
                {
                    outstr = strval;
                    break;
                }
            }
            return outstr;
        }
    }
}