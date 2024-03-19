using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using CsvHelper;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Net;
using System.Web;

namespace EligibleAutoRefernces
{
    internal class REFSIdentification
    {
        List<string> lstInputMIDs;
        public string connectionStringREFs;
        public string connectionStringTOC;
        public string fileRefAll;
        public string fileRepLog;
        public string inputHTMLfolder;
        StringBuilder sblog = new StringBuilder();
        StringBuilder sbErrorReportlog = new StringBuilder();
        private int id = 0;
        private Dictionary<string, string> dicANOutput = new Dictionary<string, string>();
        private Dictionary<string, string> dicPageNo = new Dictionary<string, string>();

        public REFSIdentification(string conStr, string constrTOC, string inHTMLfolder, string fleRefAll, string fleRepLog) {
            lstInputMIDs = new List<string>();
            connectionStringREFs = conStr;//
            connectionStringTOC = constrTOC;
            fileRefAll = fleRefAll;
            fileRepLog = fleRepLog;
            inputHTMLfolder = inHTMLfolder;
        }

        public void GetPublisherMIDsFromDB(string InputPublisherName)
        {
            string query = "select distinct MID  from ManifestInfo where Publisher like '%" + InputPublisherName + "%'";
           // string query = "select distinct MID  from ManifestInfo";
            using (SqlConnection connection = new SqlConnection(connectionStringREFs))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Create a DataTable to store the results
                    DataTable dataTable = new DataTable();

                    // Create a SqlDataAdapter to fill the DataTable
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    // Process the rows in the DataTable
                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Access columns using row["ColumnName"]
                        lstInputMIDs.Add(row["MID"].ToString());// Console.WriteLine($"Column1: {row["Column1"]}, Column2: {row["Column2"]}");
                    }
                }
            }
        }

        internal string GenerateReferencesPM(string fileReferenceAll, ref StringBuilder sberrorlog, SortedList<string, string> sortedListSeqFlag)
        {
            try
            {
                string content = File.ReadAllText(fileReferenceAll);
                SortedList<string, string> sortedListSFlag = new SortedList<string, string>();

                StringBuilder sbANPCM = new StringBuilder();
                sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tCountMatched\tANID\tAN\tRefID\tPageNo\tLiveReference\tExtractedReference\tMatchPercentageWithLiveDB\tShipmentDate");

                string MID = string.Empty;
                string IssueDate = string.Empty;
                string strAN = string.Empty;
                string shipmentDate = string.Empty;
                string direcory = string.Empty;
                int refAN = 0;
                int refPage = 0;
                dicANOutput.Clear();
                Boolean flag = false;
                // Split the string using a line of hyphens as the delimiter
                string[] strRefs = content.Split(new string[] { "------------------------------" }, StringSplitOptions.None);

                foreach (string strRef in strRefs)
                {
                    // Split the string using double newline characters as the delimiter
                    string[] paragraphs = strRef.Split(new string[] { "\n\n" }, StringSplitOptions.None);
                    foreach (string paragraph in paragraphs)
                    {                       
                        if (MID.Trim() ==string.Empty )
                        {
                            if(paragraph.Length >4)
                            {
                                // Split the string using newline character as the delimiter
                                string filePath = string.Empty;
                                string s = paragraph.Trim();
                                string[] lines = s.Split('\n');
                                string[] htmlLines = lines.Where(line1 => line1.EndsWith(".html")).ToArray();
                                foreach (string li in htmlLines)
                                {

                                    filePath = li.Trim();

                                }

                                // Get the first line
                               // filePath = lines.Length > 0 ? lines[0] : string.Empty;
                                
                                
                                //string direcory = Path.GetDirectoryName(filePath.Trim());
                                string fileName = Path.GetFileName(filePath.Trim());
                                strAN = fileName.Replace(".html", "");
                                if (strAN == "REFERENCES")
                                    ;

                                if(File.Exists(filePath))
                                {
                                    direcory = Path.GetFileName(Path.GetDirectoryName(filePath.Trim()));
                                    shipmentDate = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath.Trim())));
                                }
                                else 
                                {
                                   direcory = string.Empty;
                                  shipmentDate = string.Empty;
                                }
                               

                                string[] line = direcory.Split('_');
                                // Get an array of line lengths
                                int[] lineLengths = line.Select(line1 => line1.Length).ToArray();


                                if (lineLengths.Length  > 1)
                                {
                                    MID = line[0];
                                    IssueDate = line[1];
                                    shipmentDate = GetShipmentdateDate(MID, IssueDate);
                                    DateTime Date = DateTime.Parse(shipmentDate);
                                    shipmentDate = Date.ToString("yyyyMMdd");

                                    if (MID.Length < 3 && MID.Length > 4)
                                    {
                                        sberrorlog.AppendLine("MID is Not in Format" + MID);
                                       // Console.WriteLine
                                       // Console.ReadLine();
                                    }
                                    if(IssueDate.Length !=8)
                                    {
                                        sberrorlog.AppendLine("IssuseDate length in correct(EX: yyyymmdd)" + IssueDate);
                                       // Console.WriteLine("IssuseDate length in correct(EX: yyyymmdd)");
                                      //  Console.ReadLine();
                                    }


                                }
                              
                                                            
                                flag = true;
                               

                                
                            }
                            else
                            {
                                flag = true;
                            }
                            
                        }
                        else
                        {
                            flag = false;
                        }
                        if (flag == false)
                        {
                           
                            string[] lines = paragraph.Split(new string[] { "PageNumber: " }, StringSplitOptions.RemoveEmptyEntries);
                            string remainingPart = string.Empty;
                            string pageno = string.Empty;
                            // Check if there is at least one line
                            if (lines.Length > 0)
                            {
                                string firstLine = lines[0];
                                // Now 'firstLine' contains the first line of the paragraph
                                //Console.WriteLine(firstLine);

                                // Check if there are more lines
                                if (lines.Length > 1)
                                {
                                    // Join the remaining lines back together
                                     remainingPart = string.Join("\\r\\n", lines, 1, lines.Length - 1);
                                    pageno = remainingPart[0].ToString ();
                                    remainingPart=Regex.Replace(remainingPart, @"^\d+", "");
                                   // remainingPart = remainingPart.Replace(remainingPart[0].ToString(), " ").Trim();
                                    // Now 'remainingPart' contains the rest of the paragraph
                                    //Console.WriteLine(remainingPart);
                                }

                                //for (int i = 1; i < lines.Length; i++)
                                //{
                                 // Console.WriteLine("\nPageNumber: " + remainingPart[0]);
                                //}
                            }
                            string outp = remainingPart.Replace("\r\n", " ").Trim();
                            if(outp!= "REFERENCES" && outp != "References" && outp != "Reference" && outp != "REFERENCE" && outp != "Notes")
                            {

                                if(!(int.TryParse(strAN, out _)))
                                {
                                    sberrorlog.AppendLine("AN is wrong" + strAN);
                                    //Console.WriteLine("AN is wrong");
                                   // Console.ReadLine();
                                }
                                string pattern = "^[0-9A-Z\\s\\.]+$";
                                //@"^[0-9A-Z\s\.]+$";
                                Regex regex = new Regex(pattern);
                                if (!(regex.IsMatch(outp)))
                                {
                                    if(outp.Length >0)
                                    {
                                        dicANOutput.Add(strAN + "_" + ++refAN, outp);
                                        dicPageNo.Add(strAN + "_" + ++refPage, pageno);
                                    }
                                    else
                                    {
                                        ;
                                    }
                                   
                                }
                                       

                            }
                              
                        }
                            
                    }

                    string pcm = string.Empty;
                    //AN wise checking percentaging Matching
                    if (dicANOutput.Count >0)
                    {
                        if (strAN == "175049450")
                            ;
                        Console.WriteLine(" MID   : "+ MID+ "  IssueDate  : "+ IssueDate + "  AN: "+ strAN + "  Count: "+ dicANOutput.Count);
                        sortedListSFlag = GetFormatReferences(dicANOutput, strAN, ref sberrorlog);
                        AppendSortedLists(sortedListSFlag, sortedListSeqFlag);

                        pcm = PCM(dicANOutput, MID, IssueDate, shipmentDate, dicPageNo, ref sberrorlog);


                    }
                   

                    if (pcm.Length > 0)
                        sbANPCM.AppendLine(pcm);

                    dicANOutput.Clear();
                    dicPageNo.Clear();
                   // pageno = string.Empty;
                    refAN = 0;
                    refPage = 0;
                    MID = string.Empty;
                    IssueDate = string.Empty;
                    strAN = string.Empty;


                }
                return sbANPCM.ToString();
            }
            catch (Exception ex)
            {
                sberrorlog.AppendLine("Error Message: " + ex.Message);
                string err = $"An error occurred: {ex.Message}";
                //Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();
                return err;
               // Console.WriteLine($"An error occurred: {ex.Message}");
                // Handle the exception as needed
            }
        }
        static void AppendSortedLists<T, U>(SortedList<T, U> source, SortedList<T, U> destination) where T : IComparable<T>
        {
            foreach (var kvp in source)
            {
                // Check if the key exists in the destination list
                if (!destination.ContainsKey(kvp.Key))
                {
                    // Add the key-value pair to the destination list
                    destination.Add(kvp.Key, kvp.Value);
                }
            }
        }
        private SortedList<string, string> GetFormatReferences(Dictionary<string, string> dicANOutput, string strAN, ref StringBuilder sberrorlog)
        {
            SortedList<string, string> sortedList1 = new SortedList<string, string>();
            try
            {
                bool IsNumberedRef = false;
                bool IsSeqFollowed = true;
                //bool IsSeqFollowed = false;
                string NumberDetails = string.Empty;
                int RefNumber = 1;
                string outstr = string.Empty;
                //refType = string.Empty;
                string charactersInNumber = string.Empty;
                string firstValue = string.Empty;

                foreach (var kvp in dicANOutput)
                {
                    firstValue = kvp.Value;
                    if (char.IsDigit(firstValue[0]))
                        IsNumberedRef = true;
                    break; // Exit the loop after accessing the first key-value pair
                }

                if (IsNumberedRef)
                {

                    foreach (var dicOut in dicANOutput)
                    {

                        string strKey = dicOut.Key.ToString();
                        string strValue = dicOut.Value.ToString();

                        string input = strValue.Trim();

                        string numberPart = string.Empty;
                        string startNumber = string.Empty;

                        string restOfLine;

                        // Split the input by space
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 1)
                        {
                            numberPart = parts[0];
                            restOfLine = string.Join(" ", parts, 1, parts.Length - 1);
                            numberPart = numberPart.Replace("[", "").Replace("]", "").Replace(".", "").Replace("(", "").Replace(")", "");

                            bool IsStringAsNumber = IsNumeric(numberPart);

                            if (IsStringAsNumber && char.IsDigit(numberPart[0]) && charactersInNumber.Length < 1)
                            {
                                // Define a regular expression pattern to match the starting number
                                string pattern = @"^\d+"; // This pattern matches one or more digits at the beginning of the string

                                // Use Regex to find and extract the starting number
                                Match match = Regex.Match(numberPart, pattern);

                                if (match.Success)
                                {
                                    startNumber = match.Value;
                                    charactersInNumber = numberPart.Substring(match.Length); // Get the rest of the string after the number
                                    NumberDetails += startNumber + charactersInNumber + " ";
                                }

                                if (RefNumber == Convert.ToInt64(startNumber))
                                    RefNumber++;
                                else
                                    IsSeqFollowed = false;



                            }
                            else if (IsStringAsNumber && char.IsDigit(numberPart[0]) && charactersInNumber.Length >= 1 && numberPart.EndsWith(charactersInNumber))
                            {
                                // Define a regular expression pattern to match the starting number
                                string pattern = @"^\d+"; // This pattern matches one or more digits at the beginning of the string

                                // Use Regex to find and extract the starting number
                                Match match = Regex.Match(numberPart, pattern);

                                if (match.Success)
                                {
                                    startNumber = match.Value;
                                    charactersInNumber = numberPart.Substring(match.Length); // Get the rest of the string after the number
                                    NumberDetails += startNumber + charactersInNumber + " ";
                                }

                                if (RefNumber == Convert.ToInt64(startNumber))
                                    RefNumber++;
                                else
                                    IsSeqFollowed = false;

                                restOfLine = string.Join(" ", parts, 1, parts.Length - 1);
                            }
                            else
                                ;

                        }


                    }


                }
                else
                {
                    IsSeqFollowed = false;

                }
                if (IsNumberedRef && !IsSeqFollowed && NumberDetails.Length > 0)
                    ReCheckSequence(ref IsSeqFollowed, ref NumberDetails, charactersInNumber);
                if (sortedList1.ContainsKey(strAN))
                {

                }
                else
                {
                    if (IsNumberedRef)
                        sortedList1.Add(strAN, "Y");
                    else
                        sortedList1.Add(strAN, "N");
                }
                   

                


                    return sortedList1;

            }
            catch (Exception ex)
            {
                sberrorlog.AppendLine("Error Message: " + ex.Message);
                string err = $"An error occurred: {ex.Message}";

                return sortedList1;
            }
                           
        }

        private bool IsNumeric(string str)
        {
            // Try to parse the string as an integer
            if (int.TryParse(str, out _))
            {
                return true;
            }

            // Try to parse the string as a double (handles floating-point numbers)
            if (double.TryParse(str, out _))
            {
                return true;
            }

            // If parsing as both int and double fails, it's not a number
            return false;
        }

        private void ReCheckSequence(ref bool isSeqFollowed, ref string numberDetails, string charactersInNumber)
        {
            string[] nums = numberDetails.Split(new char[] { ' ' });
            string curNum = string.Empty;
            string nextNum = string.Empty;
            string nextNextNumer = string.Empty;
            bool IsReCheckSequence = true;
            StringBuilder sbNewNumberDetails = new StringBuilder();

            try
            {
                for (int rc = 0; rc < nums.Length; rc++)
                {
                    if (charactersInNumber.Length > 0)
                        curNum = nums[rc].Replace(charactersInNumber, "").Trim();
                    else
                        curNum = nums[rc].Trim();

                    nextNextNumer = nextNum = "";

                    if (rc + 1 < nums.Length)
                    {
                        if (charactersInNumber.Length > 0)
                            nextNum = nums[rc + 1].Replace(charactersInNumber, "").Trim();
                        else
                            nextNum = nums[rc + 1].Trim();

                    }

                    if (rc + 2 < nums.Length)
                    {
                        if (charactersInNumber.Length > 0)
                            nextNextNumer = nums[rc + 2].Replace(charactersInNumber, "").Trim();
                        else
                            nextNextNumer = nums[rc + 2].Trim();
                    }
                    if (!IsReCheckSequence)
                    {
                        sbNewNumberDetails.Append(Convert.ToString(curNum) + charactersInNumber + " ");
                    }
                    else if (nextNum.Length > 0 && nextNextNumer.Length > 0)
                    {
                        if (Convert.ToInt32(curNum) + 1 == Convert.ToInt32(nextNum))
                        {
                            sbNewNumberDetails.Append(Convert.ToString(curNum) + charactersInNumber + " ");
                        }
                        else if (rc + 2 < nums.Length && Convert.ToInt32(curNum) + 1 != Convert.ToInt32(nextNum) && Convert.ToInt32(curNum) + 1 == Convert.ToInt32(nextNextNumer))
                        {
                            IsReCheckSequence = true;
                            sbNewNumberDetails.Append(curNum.ToString() + charactersInNumber + " " + "[" + nextNum.ToString() + "]" + charactersInNumber + " " + nextNextNumer.ToString() + charactersInNumber + " ");
                            rc++;
                            rc++;

                        }
                        else if (rc + 2 < nums.Length && Convert.ToInt32(curNum) + 1 != Convert.ToInt32(nextNum) && Convert.ToInt32(curNum) + 1 != Convert.ToInt32(nextNextNumer))
                        {
                            IsReCheckSequence = false;
                            sbNewNumberDetails.Append("*" + curNum.ToString() + "" + charactersInNumber + " ");
                        }
                        else
                        {
                            sbNewNumberDetails.Append(Convert.ToString(curNum) + charactersInNumber + " ");
                        }
                    }
                }
                isSeqFollowed = IsReCheckSequence;
                numberDetails = sbNewNumberDetails.ToString();
            }
            catch (Exception e)
            {
                isSeqFollowed = false;
                Console.WriteLine(e.Message);
            }
        }
        public string GetShipmentdateDate(string mid,string issusedate)
        {
            string ShipmentDate = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ToString();
            // Replace with your actual connection string

            string query = " select d.ShipmentDate from MIDIssue c,[ShipmentProcessDetails] d" +
           " where c.MId = '"+ mid + "' and c.IssueDate = '"+ issusedate + "' and c.ShipmentId = d.ShipmentID";
            // "and d.ShipmentDate = '" + shipmentDate + "'";


          

            

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    object obj = command.ExecuteScalar();

                    if (obj != null)
                    {
                        ShipmentDate = obj.ToString();
                     }
                    else
                    {
                        Console.WriteLine("No matching records found.");
                    }
                }
            }

            return ShipmentDate;
        }


        static List<string> ReadFileContent(string filePath)
        {
            List<string> contentList = new List<string>();

            try
            {
                // Read all lines from the file and add them to the list
                string[] lines = File.ReadAllLines(filePath);
                contentList.AddRange(lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Handle the exception as needed
            }

            return contentList;
        }

        internal void CopyPDFfilesFromSource(string inputREFPathFromSource, string inputPDFPath)
        {
            string[] pdfFiles = GetPdfFilesInSubfolders(inputREFPathFromSource);

            foreach (var pdfFile in pdfFiles)
            {
                if (pdfFile.Substring(pdfFile.LastIndexOf("\\")).Contains("_") == false)
                {
                    string Folderpath = Path.GetDirectoryName(pdfFile);
                   

                    string[] substrings = Folderpath.Split('\\');// SplitString(Folderpath, delimiter);
                    string IssueDate = substrings[substrings.Length - 1];
                    string MID = substrings[substrings.Length - 2];
                    if (MID.Length == 3 || MID.Length == 4)
                    {
                        if (MID=="5E7" || (lstInputMIDs.Count > 0 && lstInputMIDs.Contains(MID)) || lstInputMIDs.Count == 0)
                        {
                            string OutFilePath = Path.Combine(inputPDFPath, MID + "_" + IssueDate);
                            if (Directory.Exists(OutFilePath) == false)
                                Directory.CreateDirectory(OutFilePath);

                            string PDFoutfile = Path.Combine(OutFilePath, Path.GetFileName(pdfFile));
                            if (File.Exists(PDFoutfile) == false)
                                File.Copy(pdfFile, PDFoutfile, true);
                        }
                    }
                }
            }
        }

        private string[] GetPdfFilesInSubfolders(string directory)
        {
            if (Directory.Exists(directory))
            {
                var pdfFiles = Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories);
                return pdfFiles;
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
                return new string[0];
            }
        }

        internal string GenerateReferencesFromHTML(string inputHTMLPath,ref StringBuilder sberrorlog)
        {
            string MID = string.Empty;
            string IssueDate = string.Empty;
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbANPCM = new StringBuilder();
            //sbANPCM.AppendLine("AN\tRefNo\tRefName\tMatchPercentage with LiveDB");
            //ID(Auto), MID, IssueDate, ANID,RefID,LiveReference,ExtractedReference, PercentageMatched,IsNumbered,EntryDate
            //output = id++ + "\t" + MID + "\t" + IssueDate + "\t" + str[0] + "\t" + str[1] + "\t" + strLiveValue + "\t" + strValue + "\t" + matchPer;
            //sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tANID\tRefID\tLiveReference\tExtractedReference\tMatchPercentage with LiveDB");
            string ReportANAll = ConfigurationManager.AppSettings["ReportANAll"].ToString();
            //string[] folderNames = Directory.GetDirectories(inputHTMLfolder);
            string[] folderNames = Directory.GetDirectories(inputHTMLPath);

            //Console.WriteLine("Folder names in " + inputFolderPath + ":");
            int rc = 0;
            foreach (string folderName in folderNames)
            {
                if (folderName.Contains("_"))
                {
                    try
                    {
                        if (folderName.Contains("57O_20231201"))
                            rc = rc;
                        string[] names = folderName.Split(new char[] { '_' });
                        MID = names[0].Substring(names[0].LastIndexOf("\\") + 1);
                        IssueDate = names[1];
                        string REFsoutFile = Path.Combine(folderName, "RefsFound.txt");
                        string REFslogFile = Path.Combine(folderName, "RefsFound.log");
                        bool isDBupdate = false;

                        HTMLFieldTracer objFieldTracer = new HTMLFieldTracer(connectionStringTOC, folderName, REFsoutFile, REFslogFile, MID, isDBupdate); 
                        string REFflag = string.Empty;
                        string Qry = objFieldTracer.AutoFontFinder(out REFflag);
                        sblog.AppendLine(REFflag);
                        sblog.AppendLine("-------------------------------------------");
                        
                        objFieldTracer.GetFontDetails();
                        List<Tuple<int, string, string>> dataList = new List<Tuple<int, string, string>>();
                        //dataList = objFieldTracer.IdentifyAndExtractREFs();
                       // dataList = objFieldTracer.IdentifyREFs(ref sbANPCM,MID, IssueDate);
                        dataList = objFieldTracer.IdentifyREFs();
                        RefsDataMatric[] dataMatric = GetFormattedRefs(dataList); //Publisher or MID specific
                        //Console.WriteLine("References: " + dataMatric.Length);
                        // Write data to CSV file
                        string csvFilePath = Path.Combine(folderName, MID + "_" + IssueDate + ".csv");
                        if(File.Exists(csvFilePath))
                            File.Delete(csvFilePath);
                        using (var writer = new StreamWriter(csvFilePath))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(dataMatric);
                        }

                        Console.WriteLine(csvFilePath  + " CSV file has been created.");
                        Console.WriteLine("References Lines: " + dataMatric.Length);
                        Console.WriteLine();
                        SortedList<string, double> slRefPoints = new SortedList<string, double>();
                        slRefPoints = FindRefBoundaries(dataMatric);
                        sbOut.AppendLine(GetReferencesFromMatrix(dataMatric, slRefPoints, ref sbANPCM, MID, IssueDate));
                    }
                    catch (Exception ex)
                    {
                        sberrorlog.AppendLine(ex.ToString());
                        //Console.WriteLine(ex.ToString());
                    }
                }
            }

            fileRepLog = Path.Combine(inputHTMLPath, "Report.log");
            ReportANAll = Path.Combine(inputHTMLPath, "ReportRefMatchAll.txt");

            File.WriteAllText(ReportANAll, sbANPCM.ToString());
            File.WriteAllText(fileRepLog, sblog.ToString());

            return sbOut.ToString();

       }

        private string GetReferencesFromMatrix(RefsDataMatric[] dataMatric, SortedList<string, double> slRefPoints, ref StringBuilder sbANPCM, string MID, string IssueDate)
        {
            StringBuilder sBuilder = new StringBuilder();
           
            string ID = string.Empty;
            int rc = 0;
            string an = string.Empty;
            string pgno = string.Empty;
            Boolean flag = false;
           // int refAN = 0;
           if(MID=="5BV")
            {
                foreach (var item in dataMatric)
                {

                    string Key = item.ID + "|" + item.PageNumber + "|" + item.ColumnNumber;
                    //if(flag==false)
                    //{
                    //    pgno = item.PageNumber.ToString();
                    //}
                    if (item.ID == "175375817")
                        ;


                    if (item.ID.Trim() != an.Trim())
                    {
                        an = item.ID;
                        sBuilder.AppendLine("------------------------------");
                    }

                    double dVal = item.XValue;

                    if (slRefPoints.ContainsKey(Key))
                    {
                        if (slRefPoints[Key] == dVal)
                        {
                            //Key = "MID="+ MID + "IssueDate="+ IssueDate +"ID=" + item.ID + " | Page: " + item.PageNumber + " | Column: " + item.ColumnNumber + " | Ref No: " + rc++;
                            //sBuilder.AppendLine("\n");
                            if (item.ID != ID || ID.Length == 0)
                            {
                                rc = 1;
                                //sBuilder.AppendLine("\n");
                                sBuilder.AppendLine();
                                sBuilder.AppendLine(item.fID.Replace(".css", ".html"));
                                sBuilder.AppendLine("\n");
                                //sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                //sBuilder.AppendLine();
                                //sBuilder.AppendLine();
                                ID = item.ID;
                            }
                            else
                            {
                                string data1 = item.Data.TrimStart('>');
                                if (data1.Length > 0)
                                {
                                    data1 = Regex.Replace(data1, "<.*?>", String.Empty);
                                    if (data1 != "Appendices" && MID == "5BV")
                                    {
                                        //data = data.Replace("\n". " ");
                                        string pattern = "^[0-9A-Z\\s\\.]+$";
                                        Regex regex = new Regex(pattern);
                                        if (!(regex.IsMatch(data1)) && flag == false)
                                        {
                                            sBuilder.AppendLine("\n");
                                            sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                        }
                                        //sBuilder.AppendLine(data);
                                    }
                                    else
                                    {
                                        //data = data.Replace("\n". " ");
                                        string pattern = "^[0-9A-Z\\s\\.]+$";
                                        Regex regex = new Regex(pattern);
                                        if (!(regex.IsMatch(data1)))
                                        {
                                            sBuilder.AppendLine("\n");
                                            sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                        }

                                    }



                                }

                                //sBuilder.AppendLine("\n");
                                //sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                            }
                            if (rc != 1)
                                sBuilder.AppendLine(Key);

                        }
                    }
                    string data = item.Data.TrimStart('>');
                    if (data.Length > 0)
                    {
                        data = Regex.Replace(data, "<.*?>", String.Empty);
                        if (data.Trim() != "Appendices" && MID == "5BV")
                        {
                            //data = data.Replace("\n". " ");

                            string pattern = "^[0-9A-Z\\s\\.]+$";
                            Regex regex = new Regex(pattern);
                            if (!(regex.IsMatch(data)) && flag == false)
                            {
                                sBuilder.AppendLine(data);
                            }
                            if (data == "REFERENCES" || data == "REFERENCE" && MID == "5BV")
                                flag = false;
                            //sBuilder.AppendLine(data);
                        }
                        else
                        {
                            //data = data.Replace("\n". " ");

                            string pattern = "^[0-9A-Z\\s\\.]+$";
                            Regex regex = new Regex(pattern);
                            if (!(regex.IsMatch(data)) && flag == false)
                            {
                                sBuilder.AppendLine(data);
                            }
                            if (data.Trim() == "Appendices" && MID == "5BV")
                            {
                                flag = true;
                            }

                            //sBuilder.AppendLine(data);
                        }



                    }





                }
            }
           else
            {
                foreach (var item in dataMatric)
                {

                    string Key = item.ID + "|" + item.PageNumber + "|" + item.ColumnNumber;
                    //if(flag==false)
                    //{
                    //    pgno = item.PageNumber.ToString();
                    //}



                    if (item.ID.Trim() != an.Trim())
                    {
                        an = item.ID;
                        sBuilder.AppendLine("------------------------------");
                    }

                    double dVal = item.XValue;

                    if (slRefPoints.ContainsKey(Key))
                    {
                        if (slRefPoints[Key] == dVal)
                        {
                            //Key = "MID="+ MID + "IssueDate="+ IssueDate +"ID=" + item.ID + " | Page: " + item.PageNumber + " | Column: " + item.ColumnNumber + " | Ref No: " + rc++;
                            //sBuilder.AppendLine("\n");
                            if (item.ID != ID || ID.Length == 0)
                            {
                                rc = 1;
                                //sBuilder.AppendLine("\n");
                                sBuilder.AppendLine();
                                sBuilder.AppendLine(item.fID.Replace(".css", ".html"));
                                sBuilder.AppendLine("\n");
                                sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                //sBuilder.AppendLine();
                                //sBuilder.AppendLine();
                                ID = item.ID;
                            }
                            else
                            {
                                string data1 = item.Data.TrimStart('>');
                                if (data1.Length > 0)
                                {
                                    data1 = Regex.Replace(data1, "<.*?>", String.Empty);
                                    if (data1 != "SUGGESTED CITATION" && MID == "3CPN")
                                    {
                                        //data = data.Replace("\n". " ");
                                        string pattern = "^[0-9A-Z\\s\\.]+$";
                                        Regex regex = new Regex(pattern);
                                        if (!(regex.IsMatch(data1)) && flag == false)
                                        {
                                            sBuilder.AppendLine("\n");
                                            sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                        }
                                        //sBuilder.AppendLine(data);
                                    }
                                    else
                                    {
                                        //data = data.Replace("\n". " ");
                                        string pattern = "^[0-9A-Z\\s\\.]+$";
                                        Regex regex = new Regex(pattern);
                                        if (!(regex.IsMatch(data1)))
                                        {
                                            sBuilder.AppendLine("\n");
                                            sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                                        }

                                    }



                                }

                                //sBuilder.AppendLine("\n");
                                //sBuilder.AppendLine("PageNumber: " + item.PageNumber);
                            }
                            if (rc != 1)
                                sBuilder.AppendLine(Key);

                        }
                    }
                    string data = item.Data.TrimStart('>');
                    if (data.Length > 0)
                    {
                        data = Regex.Replace(data, "<.*?>", String.Empty);
                        if (data != "SUGGESTED CITATION" && MID == "3CPN")
                        {
                            //data = data.Replace("\n". " ");

                            string pattern = "^[0-9A-Z\\s\\.]+$";
                            Regex regex = new Regex(pattern);
                            if (!(regex.IsMatch(data)) && flag == false)
                            {
                                sBuilder.AppendLine(data);
                            }
                            if (data == "REFERENCES" || data == "REFERENCE" && MID == "3CPN")
                                flag = false;
                            //sBuilder.AppendLine(data);
                        }
                        else
                        {
                            //data = data.Replace("\n". " ");

                            string pattern = "^[0-9A-Z\\s\\.]+$";
                            Regex regex = new Regex(pattern);
                            if (!(regex.IsMatch(data)) && flag == false)
                            {
                                sBuilder.AppendLine(data);
                            }
                            if (data == "SUGGESTED CITATION" && MID == "3CPN")
                            {
                                flag = true;
                            }

                            //sBuilder.AppendLine(data);
                        }



                    }





                }
            }

           


            return sBuilder.ToString();
        }

        //private string PCM(Dictionary<string, string> dicANOutput, string MID, string IssueDate,string shipmentDate, Dictionary<string, string> dicPageNo,ref StringBuilder sberrorlog)
        //{
        //    double matchPer = 0.0;
        //    Dictionary<string, string> dicANLiveOutput = new Dictionary<string, string>();
        //    StringBuilder sb = new StringBuilder();
        //    string output = string.Empty;
        //    string CM = string.Empty;
        //    string ANID = string.Empty;
        //    //string shipmentdate = string.Empty;
        //    //ID(Auto), MID, IssueDate, ANID,RefID,LiveReference,ExtractedReference, PercentageMatched,IsNumbered,EntryDate

        //    foreach (var dicOut in dicANOutput)
        //    {
        //        string strKey = dicOut.Key.ToString();
        //        string strValue = dicOut.Value.ToString();
        //        dicANLiveOutput = GetLiveRefsdetails(strKey, shipmentDate,ref ANID);
        //       // shipmentdate = GetLiveRefShipmentDate(strKey, shipmentDate);

        //        break;
        //    }

        //    if (dicANOutput.Count >= dicANLiveOutput.Count)
        //    {
        //        CM = "Count Match";
        //    }
        //    else
        //    {
        //        CM = "Mismatch";
        //    }
        //    foreach (var dicOut in dicANLiveOutput)
        //    {
        //        // dicANOutput
        //        //dicANLiveOutput
        //        string strKey = dicOut.Key.ToString();
        //        string strValue = dicOut.Value.ToString();
        //        string strLiveValue = string.Empty;
        //        string strPageNo = string.Empty;
        //        if (dicANOutput.ContainsKey(strKey))
        //        {
        //            strValue = dicANOutput[strKey].ToString();

        //        }
        //        else
        //        {
        //            strValue = string.Empty;
        //        }
        //        if (dicPageNo.ContainsKey(strKey))
        //        {
        //            strPageNo = dicPageNo[strKey].ToString();
        //        }
        //        else
        //        {
        //            strPageNo= string.Empty;
        //        }
        //        strLiveValue = dicOut.Value.ToString();

        //        string[] str = strKey.Split('_');
        //        strValue = strValue.Replace("  ", " ");
        //        strValue = strValue.Replace("&amp;", "&");
        //        string encodedString = EncodeNonAsciiCharacters(strValue);
        //        strValue = encodedString.Trim();

        //        //strValue = strLiveValue;
        //        string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");
        //        float disOfName = Distance(strValue1.Trim(), strLiveValue);
        //        matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
        //        int intValue = (int)matchPer;
        //        if (intValue < 50)
        //        {
        //            string strValue2 = string.Empty;
        //            string strLiveValue1 = string.Empty;
        //            strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
        //            strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

        //            float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
        //            matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
        //            intValue = (int)matchPer;
        //        }

        //        //int intValue = (int)Math.Round(matchPer);
        //        if (id == 393)
        //            ;
        //        if(intValue!=0)
        //        {
        //            output = ++id + "\t" + MID + "\t" + IssueDate + "\t" + CM + "\t" + ANID + "\t" + str[0] + ".xml" + "\t" + str[1] + "\t" + strPageNo + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
        //            //sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tCountMatched\tANID\tAN\tRefID\tPageNo\tLiveReference\tExtractedReference\tMatchPercentageWithLiveDB\tShipmentDate");

        //            sb.AppendLine(output);
        //        }


        //    }




        //    //foreach (var dicOut in dicANOutput)
        //    //foreach (var dicOut in dicANLiveOutput)
        //    //foreach (var dicOut in dicANOutput)
        //    //     {

        //    //     string strKey = dicOut.Key.ToString();
        //    //     string strValue = dicOut.Value.ToString();
        //    //     string strLiveValue = string.Empty;
        //    //    if (dicANLiveOutput.ContainsKey(strKey))
        //    //     {
        //    //         strLiveValue = dicANLiveOutput[strKey].ToString();
        //    //     }
        //    //     string[] str = strKey.Split('_');
        //    //     strValue = strValue.Replace("  ", " ");
        //    //     strValue= strValue.Replace("&amp;", "&");
        //    //     string encodedString = EncodeNonAsciiCharacters(strValue);
        //    //     strValue = encodedString.Trim();

        //    //     //strValue = strLiveValue;
        //    //     string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");
        //    //     float disOfName = Distance(strValue1.Trim (), strLiveValue);
        //    //     matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
        //    //     int intValue = (int)matchPer;
        //    //     if(intValue < 50)
        //    //     {
        //    //         string strValue2 = string.Empty;
        //    //         string strLiveValue1 = string.Empty;
        //    //         strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
        //    //         strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

        //    //         float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
        //    //         matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
        //    //          intValue = (int)matchPer;
        //    //     }

        //    //     //int intValue = (int)Math.Round(matchPer);

        //    //     output = ++id + "\t" + MID + "\t" + IssueDate +"\t"+CM + "\t" + str[0] + "\t" + str[1] + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
        //    //     sb.AppendLine(output);

        //    // }

        //    output = sb.ToString();
        //    return output;
        //    // Compare sets and calculate percentage matching
        //    //var matchingPercentages = CompareSets(dicANOutput, dicANLiveOutput);

        //    //throw new NotImplementedException();
        //}


        private string PCM(Dictionary<string, string> dicANOutput, string MID, string IssueDate, string shipmentDate, Dictionary<string, string> dicPageNo, ref StringBuilder sberrorlog)
        {
            double matchPer = 0.0;
            Dictionary<string, string> dicANLiveOutput = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            string output = string.Empty;
            string CM = string.Empty;
            string ANID = string.Empty;
            string strorgValue = string.Empty;
            string strorgLiveValue = string.Empty;
            string LiveDBFlag = ConfigurationManager.AppSettings["IsCompareWithLiveDB"].ToString();
            //string shipmentdate = string.Empty;
            //ID(Auto), MID, IssueDate, ANID,RefID,LiveReference,ExtractedReference, PercentageMatched,IsNumbered,EntryDate

            foreach (var dicOut in dicANOutput)
            {
                string strKey = dicOut.Key.ToString();
                string strValue = dicOut.Value.ToString();
                dicANLiveOutput = GetLiveRefsdetails(strKey, shipmentDate, ref ANID);
                ANID = GetLiveRefANID(strKey, shipmentDate);

                break;
            }

            if (ANID == "791306")
             ;


            if (dicANOutput.Count >= dicANLiveOutput.Count)
            {
                if (dicANLiveOutput.Count == 0)
                    CM = "No LiveData";
                else
                    CM = "Count Match";
            }
            else
            {
                CM = "Mismatch";
            }
            ///// Compare with Live data compari
            if (LiveDBFlag != "0") //Reports are generated based on live REFS data comparision
            {
                if (dicANLiveOutput.Count > 0) //Live REFS available in DB
                {
                    foreach (var dicOut in dicANLiveOutput)
                    {
                        // dicANOutput
                        //dicANLiveOutput
                        string strKey = dicOut.Key.ToString();
                        string strValue = dicOut.Value.ToString();
                        string strLiveValue = string.Empty;
                        string strPageNo = string.Empty;
                        if (dicANOutput.ContainsKey(strKey))
                        {
                            strValue = dicANOutput[strKey].ToString();

                        }
                        else
                        {
                            strValue = string.Empty;
                        }
                        if (dicPageNo.ContainsKey(strKey))
                        {
                            strPageNo = dicPageNo[strKey].ToString();
                        }
                        else
                        {
                            strPageNo = string.Empty;
                        }
                        strLiveValue = dicOut.Value.ToString();

                        string[] str = strKey.Split('_');
                        strValue = strValue.Replace("  ", " ");
                        strValue = strValue.Replace("&amp;", "&");
                        string encodedString = EncodeNonAsciiCharacters(strValue);
                        strValue = encodedString.Trim();
                        
                        strValue = ReplaceEntityReplaces(strValue);
                        strValue = DecodeHtmlEntities(strValue);
                        //strValue = strLiveValue;
                        // string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");

                        string strValue1 = Regex.Replace(strValue, @"^\d+", "");




                        float disOfName = Distance(strValue1.Trim(), strLiveValue);
                        matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
                        int intValue = (int)matchPer;
                        if (id == 307)
                            ;
                        
                        if (intValue < 90)
                        {
                            string strValue2 = string.Empty;
                            string strLiveValue1 = string.Empty;
                            strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
                            strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

                            float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
                            matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
                            intValue = (int)matchPer;
                        }

                        //int intValue = (int)Math.Round(matchPer);
                       
                        if (intValue != 0 && ANID.Length > 0)
                        {
                            strLiveValue= strLiveValue.Replace("\t", "");
                            output = ++id + "\t" + MID + "\t" + IssueDate + "\t" + CM + "\t" + ANID + "\t" + str[0] + ".xml" + "\t" + str[1] + "\t" + strPageNo + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
                            //sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tCountMatched\tANID\tAN\tRefID\tPageNo\tLiveReference\tExtractedReference\tMatchPercentageWithLiveDB\tShipmentDate");

                            sb.AppendLine(output);
                        }
                        else
                        {
                            sberrorlog.AppendLine("ANID is Not Generated for this AN: " + str[0] + ".xml");
                        }


                    }
                }
                else //Live REFS not available in DB
                {

                    foreach (var dicOut in dicANOutput)
                    {
                        // dicANOutput
                        //dicANLiveOutput
                        string strKey = dicOut.Key.ToString();
                        string strValue = dicOut.Value.ToString();
                        string strLiveValue = string.Empty;
                        string strPageNo = string.Empty;
                        if (dicANOutput.ContainsKey(strKey))
                        {
                            strValue = dicANOutput[strKey].ToString();

                        }
                        else
                        {
                            strValue = string.Empty;
                        }
                        if (dicPageNo.ContainsKey(strKey))
                        {
                            strPageNo = dicPageNo[strKey].ToString();
                        }
                        else
                        {
                            strPageNo = string.Empty;
                        }
                        // strLiveValue = dicOut.Value.ToString();

                        string[] str = strKey.Split('_');
                        strValue = strValue.Replace("  ", " ");
                        strValue = strValue.Replace("&amp;", "&");
                        string encodedString = EncodeNonAsciiCharacters(strValue);
                        strValue = encodedString.Trim();

                        //strValue = strLiveValue;
                        //string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");

                        strValue = DecodeHtmlEntities(strValue);
                        strValue = ReplaceEntityReplaces(strValue);
                        string strValue1 = strValue;
                        int intValue = 0;
                        //float disOfName = Distance(strValue1.Trim(), strLiveValue);
                        //matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
                        //int intValue = (int)matchPer;
                        //if (intValue < 50)
                        //{
                        //    string strValue2 = string.Empty;
                        //    string strLiveValue1 = string.Empty;
                        //    strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
                        //    strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

                        //    float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
                        //    matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
                        //    intValue = (int)matchPer;
                        //}

                        //int intValue = (int)Math.Round(matchPer);
                        if (id == 393)
                            ;
                        if (strValue1.Length != 0 && ANID.Length > 0)
                        {
                            output = ++id + "\t" + MID + "\t" + IssueDate + "\t" + CM + "\t" + ANID + "\t" + str[0] + ".xml" + "\t" + str[1] + "\t" + strPageNo + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
                            //sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tCountMatched\tANID\tAN\tRefID\tPageNo\tLiveReference\tExtractedReference\tMatchPercentageWithLiveDB\tShipmentDate");

                            sb.AppendLine(output);
                        }
                        else
                        {
                            sberrorlog.AppendLine("ANID is Not Generated for this AN: " + str[0] + ".xml");
                        }


                    }

                }
            }
            else   //Reports are generated based on Extarcted References from PDF
            {
                CM = "No LiveData";

                foreach (var dicOut in dicANOutput)
                {
                    // dicANOutput
                    //dicANLiveOutput
                    string strKey = dicOut.Key.ToString();
                    string strValue = dicOut.Value.ToString();
                    string strLiveValue = string.Empty;
                    string strPageNo = string.Empty;
                    if (dicANOutput.ContainsKey(strKey))
                    {
                        strValue = dicANOutput[strKey].ToString();

                    }
                    else
                    {
                        strValue = string.Empty;
                    }
                    if (dicPageNo.ContainsKey(strKey))
                    {
                        strPageNo = dicPageNo[strKey].ToString();
                    }
                    else
                    {
                        strPageNo = string.Empty;
                    }
                    // strLiveValue = dicOut.Value.ToString();

                    string[] str = strKey.Split('_');
                    strValue = strValue.Replace("  ", " ");
                    strValue = strValue.Replace("&amp;", "&");
                    string encodedString = EncodeNonAsciiCharacters(strValue);
                    strValue = encodedString.Trim();

                    //strValue = strLiveValue;
                    // string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");
                    strValue = DecodeHtmlEntities(strValue);
                    strValue = ReplaceEntityReplaces(strValue);
                    string strValue1 = strValue;
                    int intValue = 0;
                    //float disOfName = Distance(strValue1.Trim(), strLiveValue);
                    //matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
                    //int intValue = (int)matchPer;
                    //if (intValue < 50)
                    //{
                    //    string strValue2 = string.Empty;
                    //    string strLiveValue1 = string.Empty;
                    //    strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
                    //    strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

                    //    float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
                    //    matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
                    //    intValue = (int)matchPer;
                    //}

                    //int intValue = (int)Math.Round(matchPer);
                    if (id == 393)
                        ;
                    if (strValue1.Length != 0 && ANID.Length > 0)
                    {
                        output = ++id + "\t" + MID + "\t" + IssueDate + "\t" + CM + "\t" + ANID + "\t" + str[0] + ".xml" + "\t" + str[1] + "\t" + strPageNo + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
                        //sbANPCM.AppendLine("ID(Auto)\tMID\tIssueDate\tCountMatched\tANID\tAN\tRefID\tPageNo\tLiveReference\tExtractedReference\tMatchPercentageWithLiveDB\tShipmentDate");

                        sb.AppendLine(output);
                    }
                    else
                    {
                        sberrorlog.AppendLine("ANID is Not Generated for this AN: " + str[0] + ".xml");
                    }


                }
            }







            //foreach (var dicOut in dicANOutput)
            //foreach (var dicOut in dicANLiveOutput)
            //foreach (var dicOut in dicANOutput)
            //     {

            //     string strKey = dicOut.Key.ToString();
            //     string strValue = dicOut.Value.ToString();
            //     string strLiveValue = string.Empty;
            //    if (dicANLiveOutput.ContainsKey(strKey))
            //     {
            //         strLiveValue = dicANLiveOutput[strKey].ToString();
            //     }
            //     string[] str = strKey.Split('_');
            //     strValue = strValue.Replace("  ", " ");
            //     strValue= strValue.Replace("&amp;", "&");
            //     string encodedString = EncodeNonAsciiCharacters(strValue);
            //     strValue = encodedString.Trim();

            //     //strValue = strLiveValue;
            //     string strValue1 = Regex.Replace(strValue, @"\[\d+\]", " ");
            //     float disOfName = Distance(strValue1.Trim (), strLiveValue);
            //     matchPer = (strLiveValue.Length - disOfName) * 100 / strLiveValue.Length;
            //     int intValue = (int)matchPer;
            //     if(intValue < 50)
            //     {
            //         string strValue2 = string.Empty;
            //         string strLiveValue1 = string.Empty;
            //         strValue2 = GetSmashedER(strValue1.Trim(), ref sberrorlog);
            //         strLiveValue1 = GetSmashedER(strLiveValue.Trim(), ref sberrorlog);

            //         float disOfName1 = Distance(strValue2.Trim(), strLiveValue1);
            //         matchPer = (strLiveValue1.Length - disOfName1) * 100 / strLiveValue1.Length;
            //          intValue = (int)matchPer;
            //     }

            //     //int intValue = (int)Math.Round(matchPer);

            //     output = ++id + "\t" + MID + "\t" + IssueDate +"\t"+CM + "\t" + str[0] + "\t" + str[1] + "\t" + strLiveValue + "\t" + strValue1 + "\t" + intValue + "\t" + shipmentDate;
            //     sb.AppendLine(output);

            // }

            output = sb.ToString();
            return output;
            // Compare sets and calculate percentage matching
            //var matchingPercentages = CompareSets(dicANOutput, dicANLiveOutput);

            //throw new NotImplementedException();
        }


        private string ReplaceEntityReplaces(string filedata)
        {
            string result = filedata;
            result = result.Replace("&nbsp;", " ").Replace("&ndash;", "-").Replace("&Eacute;", "É").Replace("&eacute;", "é").Replace("&ldquo;", "\"").Replace("&rdquo;", "\"");
            result = result.Replace("&egrave;", "è").Replace("&agrave;", "à").Replace("&ast;", "*").Replace("&amacr;", "ā").Replace("&ouml;", "ö");//.Replace("&","");
            result = result.Replace("&acirc;", "â").Replace("&ecirc;", "ê").Replace("&euml;", "ë").Replace("&uuml;", "ü").Replace("&uacute;", "ù");
            result = result.Replace("&aacute;", "á").Replace("&iacute;", "í").Replace("&rsquo;", "\"").Replace("&lsquo;", "\"").Replace("&quest;", "?");
            result = result.Replace("&equals;", "=").Replace("&aring;", "å").Replace("&szlig;", "ß").Replace("&inodot;", "ı").Replace("&plus;", "+");
            result = result.Replace("&beta;", "β").Replace("&gamma;", "ɣ").Replace("&atilde;", "ã").Replace("&ntilde;", "ñ").Replace("&ccedil;", "ç");
            result = result.Replace("&acute;", "'").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó").Replace("&dash;", "-");
            result = result.Replace("&nacute;", "ń").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó");
            result = result.Replace("&mdash;", "—").Replace("&igrave;", "ì").Replace("&Igrave;", "Ì").Replace("&auml;", "ä").Replace("&Auml;", "Ä").Replace("&dash;", "-").Replace("&Yacute;", "Ý").Replace("&yacute;", "ý");
            result = result.Replace("&Oslash;", "Ø").Replace("&oslash;", "ø").Replace("&deg;", "°").Replace("&Ugrave;", "Ù").Replace("&ugrave;", "ù");
            result = result.Replace("&ocirc;", "ô").Replace("&Ocirc;", "Ô").Replace("&rsquo;", "’").Replace("&Agrave;", "À").Replace("&#xB0;", "&#176;").Replace("&#xb0;", "&#176;");
            result = result.Replace("&hyphen;", "‐").Replace("&apos;", "'").Replace("&#201c;", "\"").Replace("&#201d;", "\"").Replace("&#2003;", " ");
            result = result.Replace("&#231;", "ç").Replace("&apos;", "'").Replace("&#201c;", "\"").Replace("&#201d;", "\"").Replace("&#2003;", " ");


            return result;
        }

        static string DecodeHtmlEntities(string input)
        {
            // Define regular expression pattern to match HTML entities like &#201c;
            string pattern = "&#(x?[0-9A-Fa-f]+);";

            // Replace HTML entities with their corresponding Unicode characters
            string decodedString = Regex.Replace(input, pattern, match =>
            {
                string hex = match.Groups[1].Value.TrimStart('x');
                int charCode;
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out charCode))
                {
                    return char.ConvertFromUtf32(charCode);
                }
                return match.Value; // If parsing fails, return the original match
            });

            return decodedString;
        }

        //ReplaceEntityReplaces
        //private string ReplaceEntityReplaces(string filedata)
        //{


        //    string result = filedata;
        //    result = result.Replace("&nbsp;", " ").Replace("&ndash;", "-").Replace("&Eacute;", "É").Replace("&eacute;", "é").Replace("&ldquo;", "\"").Replace("&rdquo;", "\"");
        //    result = result.Replace("&egrave;", "è").Replace("&agrave;", "à").Replace("&ast;", "*").Replace("&amacr;", "ā").Replace("&ouml;", "ö");//.Replace("&","");
        //    result = result.Replace("&acirc;", "â").Replace("&ecirc;", "ê").Replace("&euml;", "ë").Replace("&uuml;", "ü").Replace("&uacute;", "ù");
        //    result = result.Replace("&aacute;", "á").Replace("&iacute;", "í").Replace("&rsquo;", "\"").Replace("&lsquo;", "\"").Replace("&quest;", "?");
        //    result = result.Replace("&equals;", "=").Replace("&aring;", "å").Replace("&szlig;", "ß").Replace("&inodot;", "ı").Replace("&plus;", "+");
        //    result = result.Replace("&beta;", "β").Replace("&gamma;", "ɣ").Replace("&atilde;", "ã").Replace("&ntilde;", "ñ").Replace("&ccedil;", "ç");
        //    result = result.Replace("&acute;", "'").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó").Replace("&dash;", "-");
        //    result = result.Replace("&nacute;", "ń").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó");
        //    result = result.Replace("&mdash;", "—").Replace("&igrave;", "ì").Replace("&Igrave;", "Ì").Replace("&auml;", "ä").Replace("&Auml;", "Ä").Replace("&dash;", "-").Replace("&Yacute;", "Ý").Replace("&yacute;", "ý");
        //    result = result.Replace("&Oslash;", "Ø").Replace("&oslash;", "ø").Replace("&deg;", "°").Replace("&Ugrave;", "Ù").Replace("&ugrave;", "ù");
        //    result = result.Replace("&ocirc;", "ô").Replace("&Ocirc;", "Ô").Replace("&rsquo;", "’").Replace("&Agrave;", "À").Replace("&#xB0;", "&#176;").Replace("&#xb0;", "&#176;");
        //    result = result.Replace("&hyphen;", "‐").Replace("&apos;", "'").Replace("&#201c;", "\"").Replace("&#201d;", "\"").Replace("&#2003;", " ");
        //    result = result.Replace("&#231;", "ç").Replace("&apos;", "'").Replace("&#201c;", "\"").Replace("&#201d;", "\"").Replace("&#2003;", " ");


        //    return result;

        //    //string result = filedata;
        //    //result = result.Replace("&nbsp;", " ").Replace("&ndash;", "-").Replace("&Eacute;", "É").Replace("&eacute;", "é").Replace("&ldquo;", "\"").Replace("&rdquo;", "\"");
        //    //result = result.Replace("&egrave;", "è").Replace("&agrave;", "à").Replace("&ast;", "*").Replace("&amacr;", "ā").Replace("&ouml;", "ö");//.Replace("&","");
        //    //result = result.Replace("&acirc;", "â").Replace("&ecirc;", "ê").Replace("&euml;", "ë").Replace("&uuml;", "ü").Replace("&uacute;", "ù");
        //    //result = result.Replace("&aacute;", "á").Replace("&iacute;", "í").Replace("&rsquo;", "\"").Replace("&lsquo;", "\"").Replace("&quest;", "?");
        //    //result = result.Replace("&equals;", "=").Replace("&aring;", "å").Replace("&szlig;", "ß").Replace("&inodot;", "ı").Replace("&plus;", "+");
        //    //result = result.Replace("&beta;", "β").Replace("&gamma;", "ɣ").Replace("&atilde;", "ã").Replace("&ntilde;", "ñ").Replace("&ccedil;", "ç");
        //    //result = result.Replace("&acute;", "'").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó").Replace("&dash;", "-");
        //    //result = result.Replace("&nacute;", "ń").Replace("&ccaron;", "č").Replace("&scaron;", "š").Replace("&oacute;", "ó");
        //    //result = result.Replace("&mdash;", "—").Replace("&igrave;", "ì").Replace("&Igrave;", "Ì").Replace("&auml;", "ä").Replace("&Auml;", "Ä").Replace("&dash;", "-").Replace("&Yacute;", "Ý").Replace("&yacute;", "ý");
        //    //result = result.Replace("&Oslash;", "Ø").Replace("&oslash;", "ø").Replace("&deg;", "°").Replace("&Ugrave;", "Ù").Replace("&ugrave;", "ù");
        //    //result = result.Replace("&ocirc;", "ô").Replace("&Ocirc;", "Ô").Replace("&rsquo;", "’").Replace("&Agrave;", "À").Replace("&#xB0;", "&#176;").Replace("&#xb0;", "&#176;");
        //    //result = result.Replace("&hyphen;", "‐").Replace("&apos;", "'").Replace("&#201c;", "\"").Replace("&#201d;", "\"");


        //    //return result;
        //}

        //static string DecodeHtmlEntities(string input)
        //{
        //    // Define regular expression pattern to match HTML entities like &#201c;
        //    string pattern = "&#(x?[0-9A-Fa-f]+);";

        //    // Replace HTML entities with their corresponding Unicode characters
        //    string decodedString = Regex.Replace(input, pattern, match =>
        //    {
        //        string hex = match.Groups[1].Value.TrimStart('x');
        //        int charCode;
        //        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out charCode))
        //        {
        //            return char.ConvertFromUtf32(charCode);
        //        }
        //        return match.Value; // If parsing fails, return the original match
        //    });

        //    return decodedString;
        //}


        static string DecodeNumericCharacterReferences(string input)
        {
            string pattern = "&#(?:x([0-9A-Fa-f]+)|([0-9]+));";
            string decodedString = Regex.Replace(input, pattern, match =>
            {
                int charCode;
                if (match.Groups[1].Success)
                {
                    // Hexadecimal reference
                    if (int.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out charCode))
                    {
                        return char.ConvertFromUtf32(charCode);
                    }
                }
                else
                {
                    // Decimal reference
                    if (int.TryParse(match.Groups[2].Value, out charCode))
                    {
                        return char.ConvertFromUtf32(charCode);
                    }
                }
                return match.Value;
            });
            return decodedString;
        }

        private string GetLiveRefANID(string strKey, string shipmentDate)
        {
            string strANID = string.Empty;
            string stran = string.Empty;


            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
            //select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN = '174698482.xml' and a.ANId =b.ANID 
            stran = strKey.Replace("_1", "");
            strKey = strKey.Replace("_1", "") + ".xml";

            //174569082_1
            //string query = "select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN ='" + strKey + "' and a.ANId =b.ANID"; // Replace with your SELECT query

            //string query = "select * FROM [REFSIRP].[dbo].[ANInfo]" +
            //" where AN = '" + strKey + "' and  ";

            string query = "select a.ANId from ANInfo a,MIDIssue c,ShipmentProcessDetails d" +
            " where a.AN = '" + strKey + "' and a.MIDIssueId = c.MidIssueId and c.ShipmentId = d.ShipmentID " +
           "and d.ShipmentDate = '" + shipmentDate + "'  ";



            DataTable resultTable = ExecuteQuery(connectionStringREF, query);
            for (int i = 0; i < resultTable.Rows.Count; i++)
            {
                //dicANLiveOutput.Add(resultTable.Rows[i]["AN"].ToString() + "_" + resultTable.Rows[i]["RefNo"].ToString(), resultTable.Rows[i]["ER"].ToString());
                strANID = resultTable.Rows[i]["ANId"].ToString();
            }

            return strANID;
        }



        public string GetSmashedER(string er, ref StringBuilder sberrorlog)
        {
            try
            {
                string[] stopWords = { "a", "about", "abs", "accordingly", "affected", "affecting", "after", "again", "against", "all", "almost", "already", "also", "although", "always", "among", "an", "and", "any", "anyone", "apparently", "are", "arise", "as", "aside", "at", "away", "be", "became", "because", "become", "becomes", "been", "before", "being", "between", "both", "briefly", "but", "by", "call", "called", "came", "can", "cannot", "certain", "certainly", "come", "comes", "coming", "could", "do", "does", "doesn't", "doing", "don't", "done", "during", "each", "early", "either", "else", "end", "etc", "even", "ever", "every", "far", "following", "for", "found", "from", "further", "gave", "get", "gets", "give", "given", "gives", "giving", "go", "goes", "going", "gone", "got", "had", "hadn't", "handle", "hardly", "has", "have", "having", "he", "help", "her", "here", "him", "himself", "his", "how", "however", "i", "i'm", "if", "in", "into", "is", "isn't", "it", "it's", "itself", "joined", "just", "keep", "kept", "kg", "kind", "know", "knowing", "knowledge", "knows", "largely", "late", "later", "let", "like", "longtime", "look", "looked", "looking", "looks", "loses", "lost", "lot", "made", "main", "mainly", "make", "makes", "making", "many", "may", "mean", "means", "meant", "meet", "men", "met", "mg", "middle", "might", "ml", "more", "most", "mostly", "move", "much", "must", "my", "name", "nearly", "necessarily", "necessary", "need", "needed", "needs", "neither", "never", "new", "next", "no", "none", "nor", "normally", "not", "noted", "nothing", "now", "nowadays", "nowhere", "obtain", "obtained", "occurred", "of", "off", "offering", "often", "on", "once", "only", "or", "other", "others", "others'", "our", "ourselves", "out", "outside", "over", "overcome", "owing", "part", "particularly", "past", "perhaps", "please", "poorly", "possible", "possibly", "potentially", "predominantly", "present", "previously", "primarily", "probably", "proceeds", "process", "prompt", "promptly", "provide", "provides", "push", "put", "puts", "quickly", "quite", "rarely", "rather", "readily", "really", "recently", "refs", "regarding", "regardless", "relatively", "respectively", "resulted", "resulting", "results", "said", "same", "saw", "say", "saying", "says", "see", "seem", "seemed", "seems", "seen", "separates", "set", "several", "shall", "should", "show", "showed", "shown", "shows", "significantly", "similar", "similarly", "since", "slightly", "so", "some", "something", "sometime", "sometimes", "somewhat", "soon", "specifically", "start", "state", "states", "strongly", "substantially", "successfully", "such", "sufficiently", "sure", "take", "takes", "taking", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there", "therefore", "these", "they", "this", "those", "though", "through", "throughout", "to", "told", "too", "took", "toward", "unable", "under", "unless", "until", "up", "upon", "us", "use", "used", "useful", "usefully", "usefulness", "uses", "using", "usually", "various", "very", "want", "wanted", "wants", "was", "wasn't", "we", "went", "were", "what", "what's", "when", "whenever", "where", "whether", "which", "while", "who", "whole", "whose", "why", "widely", "will", "with", "within", "without", "would", "yes", "yet", "you", "you're", "you've", "your", "yourself" };
                if (!string.IsNullOrEmpty(er))
                {

                    er = Regex.Replace(er, "(?i)&#([0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");

                    er = Regex.Replace(er, "(?i)&#([0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z][0-9a-z]);", "");


                    string[] spStr = er.Split(' ');

                    string finalStr = "";

                    for (int i = 0; i < spStr.Length; i++)

                    {

                        var found = 0;

                        string temp = spStr[i].Trim();

                        for (int j = 0; j < stopWords.Length; j++)

                        {

                            if (temp.ToLower().Equals(stopWords[j]))

                            {

                                found = 1;

                                break;

                            }

                        }

                        if (found == 0)

                        {

                            finalStr += " " + spStr[i];

                        }


                    }

                    er = finalStr;

                    er = Regex.Replace(er, "[^a-zA-Z0-9]", "");

                    er = er.Replace(" ", "");

                }

                else

                {

                    return "";

                }

                return er;

            }

            catch (Exception ex)

            {

                if (er != null)
                    sberrorlog.AppendLine("Error while generating smashedcode ER is: " + ex.ToString());
                Console.WriteLine(ex.ToString());
                // log.Error("Error while generating smashedcode ER is: " + er.Replace("'", "''") + " in DRP . Exception:", ex);

                return "";

            }

        }

        static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // Append Unicode escape sequence
                    sb.Append("&#").Append(((int)c).ToString("x")).Append(";");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        private string GetLiveRefShipmentDate(string strKey,string shipmentDate)
        {
            string strShipmentDate = string.Empty;
            string stran = string.Empty;

            
            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
            //select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN = '174698482.xml' and a.ANId =b.ANID 
            stran = strKey.Replace("_1", "");
            strKey = strKey.Replace("_1", "") + ".xml";

            //174569082_1
            //string query = "select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN ='" + strKey + "' and a.ANId =b.ANID"; // Replace with your SELECT query

            string query = "select a.AN,b.RefNo ,b.ER,a.MIDIssueId, c.ShipmentId ,d.ShipmentDate from ANInfo a,[ANReferences] b,MIDIssue c,[ShipmentProcessDetails] d" +
            " where a.AN = '" + strKey + "' and a.ANId = b.ANID and a.MIDIssueId = c.MidIssueId and c.ShipmentId = d.ShipmentID " +
            "and d.ShipmentDate = '" + shipmentDate + "'";

            DataTable resultTable = ExecuteQuery(connectionStringREF, query);
            for (int i = 0; i < resultTable.Rows.Count; i++)
            {
                //dicANLiveOutput.Add(resultTable.Rows[i]["AN"].ToString() + "_" + resultTable.Rows[i]["RefNo"].ToString(), resultTable.Rows[i]["ER"].ToString());
                strShipmentDate= resultTable.Rows[i]["ShipmentDate"].ToString();
            }

            return strShipmentDate;
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
        private Dictionary<string, string> GetLiveRefsdetails(string strKey,string shipmentDate,ref string ANID)
        {
            string stran = string.Empty;
            Dictionary<string, string> dicANLiveOutput = new Dictionary<string, string>();
            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
            //select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN = '174698482.xml' and a.ANId =b.ANID 
            stran = strKey.Replace("_1", "");
            strKey = strKey.Replace("_1", "") + ".xml";

            //174569082_1
            // string query = "select a.AN,b.RefNo ,b.ER  from ANInfo a,[ANReferences] b where a.AN ='" + strKey + "' and a.ANId =b.ANID"; // Replace with your SELECT query
            string query = "select a.ANId,a.AN,b.RefNo ,b.ER,a.MIDIssueId, c.ShipmentId ,d.ShipmentDate from ANInfo a,[ANReferences] b,MIDIssue c,[ShipmentProcessDetails] d" +
             " where a.AN = '" + strKey + "' and a.ANId = b.ANID and a.MIDIssueId = c.MidIssueId and c.ShipmentId = d.ShipmentID "+
            "and d.ShipmentDate = '" + shipmentDate + "' order by b.RefNo ";


  //          select a.ANId,a.AN,b.RefNo ,b.ER,a.MIDIssueId, c.ShipmentId ,d.ShipmentDate from ANInfo a,[ANReferences] b,MIDIssue c,[ShipmentProcessDetails] d
  //where c.MId = 'JCW' and c.IssueDate = '20240101' and c.MidIssueId = a.MIDIssueId and c.ShipmentId = d.ShipmentID
  //and a.ANId = b.ANID

            //           select a.AN,b.RefNo ,b.ER,a.MIDIssueId, c.ShipmentId ,d.ShipmentDate from ANInfo a,[ANReferences] b,MIDIssue c,[ShipmentProcessDetails] d
            //where a.AN = '" + strKey + "' and a.ANId = b.ANID and a.MIDIssueId = c.MidIssueId and c.ShipmentId = d.ShipmentID

            DataTable resultTable = ExecuteQuery(connectionStringREF, query);
            for (int i = 0; i < resultTable.Rows.Count; i++)
            {
               //dicANLiveOutput.Add(resultTable.Rows[i]["AN"].ToString() + "_" + resultTable.Rows[i]["RefNo"].ToString(), resultTable.Rows[i]["ER"].ToString());
                dicANLiveOutput.Add(stran+ "_" + resultTable.Rows[i]["RefNo"].ToString(), resultTable.Rows[i]["ER"].ToString());
                ANID = resultTable.Rows[i]["ANId"].ToString();
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

        private SortedList<string, double> FindRefBoundaries(RefsDataMatric[] dataMatric)
        {
            SortedList<string, double> slRefPoints = new SortedList<string, double>();

            foreach (var item in dataMatric)
            {
                if (item.ID == "174552204")
                    ;
                string Key = item.ID + "|" + item.PageNumber + "|" + item.ColumnNumber;
                double dVal = item.XValue;
                if(item.DataType!="Header")
                {
                    if (slRefPoints.ContainsKey(Key) == false)
                        slRefPoints.Add(Key, dVal);
                    else
                    {
                        if (dVal < slRefPoints[Key])
                            slRefPoints[Key] = dVal;
                    }
                }
               
            }
            return slRefPoints;
        }

        

        private void FindRefBoundaries(RefsDataMatric[] dataMatric, ref List<REFsGeoPoints> lstRefPoints)
        {
            int PageNo = 1;
            int ColNo = 1;
            string ID = string.Empty;
            double XBegin = 0;
            // Access elements in the dynamic array
            foreach (var item in dataMatric)
            {
               if(item.Data=="Header")
                {
                    PageNo = item.PageNumber;
                    ColNo = item.ColumnNumber;
                    ID = item.ID;
                    XBegin = item.XValue;
                }
                else
                {
                    if(ID==item.ID && PageNo==item.PageNumber && ColNo==item.ColumnNumber && XBegin==item.XValue)
                    {
                        if(lstRefPoints.Count>0)
                        {

                        }
                        else
                        {
                             lstRefPoints.Add(new REFsGeoPoints
                            {
                                Key=item.ID+"|"+item.PageNumber+"|"+item.ColumnNumber,
                                ID = item.ID,
                                PageNumber = item.PageNumber,
                                ColumnNumber = item.ColumnNumber,
                                XBeginPoint = item.XValue
                            });
                        }
                    }
                }
            }
        }

        private RefsDataMatric[] GetFormattedRefs(List<Tuple<int, string, string>> dataList)
        {
            string strFormattedData = string.Empty;
            RefsDataMatric[] refsData = new RefsDataMatric[dataList.Count];
            double yPrev = 0, yCur = 0;
            int PNumber = 0;
            string IDPrev = string.Empty;
            int lineNumber = 0;
            double XDistToHead = 0;
            double YDistToHead = 0;
            double XDistToPrev = 0;
            double YDistToPrev = 0;
            int columnNumber = 1;
            string Datatype = "";

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i % 500 == 0)
                    Console.WriteLine("Ref Lines:" + i + "/" + dataList.Count);
                var entry = dataList[i];
                string xPos = FindXCode(entry.Item3, entry.Item2);
                string yPos = FindYCode(entry.Item3, entry.Item2);
                //Console.WriteLine($"Item {entry.Item1}: Value = {entry.Item2}");

                string fileID = entry.Item2.ToString();
                if (fileID.Contains("\\"))
                    fileID = fileID.Substring(fileID.LastIndexOf("\\") + 1);

                if (fileID.Contains("/"))
                    fileID = fileID.Substring(fileID.LastIndexOf("/") + 1);

                if (fileID.Contains("."))
                    fileID = fileID.Substring(0, fileID.IndexOf("."));

                string xcode = string.Empty;
                if (entry.Item3.Contains(" x"))
                {
                    xcode = entry.Item3.Substring(entry.Item3.IndexOf(" x"));
                    xcode = xcode.Trim();
                    xcode = xcode.Substring(0, xcode.IndexOf(" ")).Trim();
                }

                string ycode = string.Empty;
                if (entry.Item3.Contains(" y"))
                {
                    ycode = entry.Item3.Substring(entry.Item3.IndexOf(" y"));
                    ycode = ycode.Trim();
                    ycode = ycode.Substring(0, ycode.IndexOf(" ")).Trim();
                }

                string fSize = FindFontCode(entry.Item3, entry.Item2);

                string data = entry.Item3.Substring(entry.Item3.IndexOf(">"));

                if (i == 0)
                {
                    PNumber = 1;
                    yPrev = Convert.ToDouble(yPos);
                    IDPrev = fileID;
                    lineNumber = 1;
                    XDistToHead = Convert.ToDouble(xPos);
                    YDistToHead = Convert.ToDouble(yPos);
                    XDistToPrev = 0;
                    YDistToPrev = 0;
                    Datatype = "Header";
                }
                else
                {
                    XDistToPrev = Convert.ToDouble(xPos) - refsData[i - 1].XValue;
                    YDistToPrev = Convert.ToDouble(yPos) - refsData[i - 1].YValue;
                    yCur = Convert.ToDouble(yPos);
                    Datatype = "Content";

                    if (yCur > yPrev + 5)
                    {
                        // PNumber++;
                        lineNumber = 1;
                    }
                    else if (yCur == yPrev || Convert.ToInt16(yCur) == Convert.ToInt16(yPrev))
                    {
                        XDistToPrev = refsData[i - 1].XDistPrev;
                        YDistToPrev = refsData[i - 1].YDistPrev;
                    }
                    else
                        lineNumber++;

                    yPrev = yCur;

                    if (XDistToPrev > 100 && YDistToPrev > 20)
                    {
                        columnNumber++;
                    }
                    else if (XDistToPrev <= 0 && YDistToPrev > 100)
                    {
                        PNumber++;
                        columnNumber = 1;
                    }
                   

                    if (fileID != IDPrev)
                    {
                        PNumber = 1;
                        columnNumber = 1;
                       
                        yPrev = Convert.ToDouble(yPos);
                        IDPrev = fileID;
                        lineNumber = 1;
                        XDistToHead = Convert.ToDouble(xPos);
                        YDistToHead = Convert.ToDouble(yPos);
                        XDistToPrev = 0;
                        YDistToPrev = 0;
                        Datatype = "Header";
                    }
                }



                refsData[i] = new RefsDataMatric
                {
                    PageNumber = PNumber,
                    LineNumber = lineNumber,
                    XCode = xcode,
                    XValue = Convert.ToDouble(xPos),
                    YCode = ycode,
                    YValue = Convert.ToDouble(yPos),
                    fontSize = Convert.ToDouble(fSize),
                    Data = data,
                    DataType = Datatype,
                    XDistHead = Convert.ToDouble(xPos) - XDistToHead,
                    YDistHead = YDistToHead - Convert.ToDouble(yPos),
                    XDistPrev = XDistToPrev, YDistPrev = YDistToPrev,
                    ColumnNumber = columnNumber,
                    fID = entry.Item2.ToString(),
                    ID = fileID
                };
            }

            return refsData;
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

            string xlocation = GetPositionFromCSS(xcode, csfile);

            return xlocation;
        }

        private string GetPositionFromCSS(string xycode, string csfile)
        {
            string xLoc = string.Empty;
            string[] lines = File.ReadAllLines(csfile);
            for (int rl = 0; rl < lines.Length; rl++)
            {
                if (lines[rl].StartsWith("." + xycode + "{") && lines[rl].Contains("px"))
                {
                    xLoc = lines[rl].Substring(lines[rl].IndexOf(":") + 1);
                    xLoc = xLoc.Substring(0, xLoc.IndexOf("px"));
                    break;
                }
            }
            return xLoc;
        }


        private string FindYCode(string lineData, string csfile)
        {
            string ycode = string.Empty;
            if (lineData.Contains(" y"))
            {
                ycode = lineData.Substring(lineData.IndexOf(" y"));
                ycode = ycode.Trim();
                ycode = ycode.Substring(0, ycode.IndexOf(" ")).Trim();
            }

            //return xcode;

            string ylocation = GetPositionFromCSS(ycode, csfile);

            return ylocation;
        }

        private string FindFontCode(string lineData, string csfile)
        {
            string fCode = string.Empty;
            if (lineData.Contains(" fs"))
            {
                fCode = lineData.Substring(lineData.IndexOf(" fs"));
                fCode = fCode.Trim();
                fCode = fCode.Substring(0, fCode.IndexOf(" ")).Trim();
            }

            //return xcode;

            string fontSize = GetPositionFromCSS(fCode, csfile);

            return fontSize;
        }
    }
}
