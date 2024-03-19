using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EligibleAutoRefernces
{
    class REFSFillLiveData
    {
        public void process(ref StringBuilder sberrorlog, SortedList<string, string> sortedListSeqFlag)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
                string fileReferenceANAll = ConfigurationManager.AppSettings["ReportANAll"].ToString();
             
                StringBuilder sbtmp = new StringBuilder();
                if (File.Exists(fileReferenceANAll))
                {
                    string content = File.ReadAllText(fileReferenceANAll);
                    // Get the current date and time
                    DateTime currentDate = DateTime.Now;
                    string PrevAN = string.Empty;
                    string PrevPageno = string.Empty;
                    // Format the date to "yyyy-MM-dd"
                    string TodayDate = currentDate.ToString("yyyy-MM-dd");
                    using (StringReader reader = new StringReader(content))
                    {
                        string line;
                        int lineNumber = 0;
                        int count;
                        string ShipmentDate = string.Empty;
                        string strMID = string.Empty;
                        string AN = string.Empty;
                        //string querytmp = "insert into toc_uat.dbo.REFANDetails_CompareInfo (MID,[IssueDate],[CountMatched],[ANID],[RefID],[LiveReference],[ExtractedReference],[PercentageMatched],[ShipmentDate],[Entrydate]) ";
                        string querytmp = "insert into REFSIRP.dbo.AutoREFANDetails_CompareInfo (MID,[IssueDate],[CountMatched],[ANID],[AN],[RefID],[PageNo],[LiveReference],[ExtractedReference],[PercentageMatched],[ShipmentDate],[Entrydate]) ";

                        SqlConnection connection = new SqlConnection(connectionString);
                        connection.Open();
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Skip the first line.
                            if (lineNumber > 0 && line.Length >0)
                            {
                                string[] parts = line.Split('\t');
                                //1 MID-2 IssueDate -3 CM-4 ANID -5 AN-6 refid -7 pageno -8 live -9 extracted-10 PM-11 entrydate-
                                string ANID = parts[4];
                                if (ANID == "792683")
                                    ;
                                 AN = parts[5];
                                string LiveRef = string.Empty;
                                string ExtRef = string.Empty;


                                // Check if dateString1 is a valid date format
                                DateTime parsedDate1;
                                bool isDate1 = DateTime.TryParseExact(parts[11], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out parsedDate1);

                                if (parts[1] == "JGW")
                                    ;
                                if (isDate1)
                                {
                                    // Format the parsed date to "yyyy-MM-dd"
                                    ShipmentDate = parsedDate1.ToString("yyyy-MM-dd");

                                }
                                LiveRef = parts[8].Replace("'","\"");
                                ExtRef = parts[9].Replace("'", "\"");
                                strMID = parts[1];
                                if (AN ==PrevAN )
                                {
                                    if(parts[7].Length ==0)
                                    {
                                         parts[7]= PrevPageno;
                                    }
                                    sbtmp.AppendLine(querytmp + "values ('" + parts[1] + "','" +
                                                                               parts[2] + "','" +
                                                                               parts[3] + "','" +
                                                                               parts[4] + "','" +
                                                                               parts[5] + "','" +
                                                                                parts[6] + "','" +
                                                                                 parts[7] + "','" +
                                                                              LiveRef + "','" +
                                                                              ExtRef + "','" +
                                                                               parts[10] + "','" +
                                                                             ShipmentDate + "','" +
                                                                               TodayDate + "')");
                                    PrevPageno = parts[7];
                                }
                                else
                                {
                                    if(int.TryParse(ANID, out int result))
                                    {
                                        if (parts[7].Length == 0)
                                        {
                                            parts[7] = PrevPageno;
                                        }
                                        string query = "delete FROM REFSIRP.dbo.AutoREFANDetails_CompareInfo where ANID='" + ANID + "'";

                                        ExecuteQueryExists(connection, query);
                                        ExecuteInsertQuery(connection, sbtmp, PrevAN, strMID, sortedListSeqFlag);
                                        sbtmp.Clear();
                                        
                                        sbtmp.AppendLine(querytmp + "values ('" + parts[1] + "','" +
                                               parts[2] + "','" +
                                               parts[3] + "','" +
                                               parts[4] + "','" +
                                               parts[5] + "','" +
                                               parts[6] + "','" +
                                               parts[7] + "','" +
                                               LiveRef + "','" +
                                               ExtRef + "','" +
                                               parts[10] + "','" +
                                               ShipmentDate + "','" +
                                               TodayDate + "')");
                                        //query = "delete FROM REFSIRP.dbo.AutoREFANDetails_CompareInfo where ANID='" + ANID + "'";

                                       // ExecuteQueryExists(connection, query);
                                        //ExecuteInsertQuery(connection, sbtmp, AN, strMID);
                                        PrevPageno = parts[7];
                                    }
                                   
                                }

                                //string query = $"SELECT * FROM [TOC_UAT].[dbo].[REFANDetails_CompareInfo] where ANID='" + AN + "'";
                               




                                //// Display each part.
                                //foreach (string part in parts)
                                //{

                                // Console.WriteLine(sbtmp.ToString ());
                                //}

                                //using (SqlConnection connection = new SqlConnection(connectionString))
                                //{
                                //connection.Open();


                                //}

                                PrevAN = AN;

                            }
                            else
                            {
                               
                            }
                            lineNumber++;

                        }

                        if (sbtmp.Length > 0)
                        {
                           
                            ExecuteInsertQuery(connection, sbtmp, PrevAN, strMID, sortedListSeqFlag);
                            sbtmp.Clear();
                           // updateSeqFlag(sortedListSeqFlag,AN);
                            
                        }



                    }

                }

            }
            

             catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine(err);
                Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();
               
                // Handle the exception as needed
            }


        }

        private void updateSeqFlag(SortedList<string, string> sortedListSeqFlag, string AN)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;

            string key = AN;
            key = key.Replace(".xml", "");
            if (sortedListSeqFlag.ContainsKey(key))
            {
                // If the key exists, retrieve its corresponding value
                string value = sortedListSeqFlag[key];
                string updateQuery = "UPDATE [REFSIRP].[dbo].[AutoREFANDetails_CompareInfo] SET SeqFlag = @NewValue WHERE AN="+"'" + AN + "'";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Create a SqlCommand with the update query and connection
                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        // Add parameters to the SqlCommand
                        command.Parameters.AddWithValue("@NewValue", value);

                        // Execute the update query
                        int rowsAffected = command.ExecuteNonQuery();

                        // Output the number of rows affected by the update operation
                        Console.WriteLine($"Rows Seq Flag Updated: {rowsAffected}");
                    }
                }



            }
        }

        internal void MIDWiseADDtoDB(ref StringBuilder sberrorlog, string fileReportMIDWise)
        {
            try
            {
                string[] lines = File.ReadAllLines(fileReportMIDWise);
                string Mid = string.Empty;
                string IssuseDate = string.Empty;
                string ShipmentDate = string.Empty;
                string Status = string.Empty;
                int CntValidref = 0;
                int CntInvalidRef = 0;
                int CntTotal = 0;
                double perMatch=0.0;
                double perMatch1 = 0.0;
                string query = string.Empty;
                string publisher = string.Empty;
                string sentdate = string.Empty;
                string entrydate = string.Empty;
                string IsEligible = string.Empty;
                string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
                string IsEligibleMID = ConfigurationManager.AppSettings["IsEligibleMID"].ToString();


                // Check if the file has any content
                if (lines.Length > 0)
                {
                    // Split the first line to get column names
                    string[] columnNames = lines[0].Split('\t');
                    for (int i = 1; i < lines.Length; i++)
                    {
                        columnNames = lines[i].Split('\t');
                        if (columnNames.Length > 1)
                        {
                            string[] value1= columnNames[0].Split('-');
                            Mid = value1[0];
                            ShipmentDate = value1[1];
                            IssuseDate = value1[2];
                            Status = value1[3];
                            string[] value2 = columnNames[1].Split('|');
                            CntValidref = int.Parse(value2[1]);
                            CntInvalidRef = int.Parse(value2[2]);
                            CntTotal = int.Parse(value2[3]);

                            if (int.TryParse(value2[0].ToString(), out int firstDigit))
                            {
                                 perMatch = firstDigit / 100.0; // Use 100.0 to ensure double division
                                perMatch1 = perMatch;


                            }
                            query = "select a.Publisher,c.sentDate from MIDIssue c,[ShipmentProcessDetails] d,ManifestInfo a"+
                             " where c.MId = '"+ Mid + "' and c.IssueDate = '"+IssuseDate+"'and d.ShipmentDate = '"+ ShipmentDate + "' and c.ShipmentId = d.ShipmentID  and c.MId = a.MId";
                            DataTable resultTable = ExecuteQuery(connectionString, query);
                            for (int j = 0; j < resultTable.Rows.Count; j++)
                            {
                                publisher= resultTable.Rows[j]["Publisher"].ToString();
                                sentdate = resultTable.Rows[j]["sentDate"].ToString();
                                if (!string.IsNullOrEmpty(sentdate) && DateTime.TryParse(sentdate, out DateTime parsedDate))
                                {
                                    DateTime Date = DateTime.Parse(sentdate);
                                    sentdate = Date.ToString("yyyy-MM-dd");
                                   
                                }
                                else
                                {
                                    sentdate = "1900-01-01";
                                }

                                entrydate = DateTime.Now.ToString("yyyy-MM-dd");
                            }

                            perMatch = perMatch * 100;
                            if (perMatch > 90)
                            {
                                if(IsEligibleMID != "0")
                                {
                                    query = "insert into [REFSIRP].[dbo].[AutoMIDEligible]([MID],[Publisher],[ISActive],[ISManual],[Entrydate]) " +
                                                               "values('" + Mid + "','" + publisher + "',1,0,'" + entrydate + "')";

                                    SqlConnection connection1 = new SqlConnection(connectionString);
                                    connection1.Open();
                                    string query1 = "select * from [REFSIRP].[dbo].[AutoMIDEligible] where MID='" + Mid + "'";
                                    if (!ExecuteQueryExists(connection1, query1))
                                    {
                                        ExecuteInsertMIDQuery(connectionString, query, Mid, IssuseDate, ShipmentDate);
                                    }

                                }
                            }
                            IsEligible = GetIsEligibleMIDStatus(Mid, publisher, connectionString);

                            query = "delete FROM [REFSIRP].[dbo].[AutoRefLog] where MId='" + Mid + "' and IssueDate='" + IssuseDate + "' and ShipmentDate='" + ShipmentDate + "'";
                            SqlConnection connection = new SqlConnection(connectionString);
                            connection.Open();
                            ExecuteQueryExists(connection, query);

                            query = "insert into [REFSIRP].[dbo].[AutoRefLog]([MId],[IssueDate],[ShipmentDate],[Status]"+
                                ",[CntValidRefs],[CntInValidRefs],[CntTotal],[PercentageMatched],[Publisher],[sentDate],[Entrydate],[IsEligible]) " +
                                "values('" + Mid + "','" + IssuseDate + "','" + ShipmentDate + "','" + Status + "',"+
                                "'"+CntValidref+ "','" + CntInvalidRef + "','" + CntTotal + "','" + perMatch1 + "',"+
                                "'" + publisher + "','" + sentdate + "','" + entrydate + "','"+ IsEligible + "')";
                       

                            ExecuteInsertMIDQuery(connectionString, query, Mid, IssuseDate, ShipmentDate);


                            if (IsEligibleAutoMID(connectionString, Mid, publisher))
                            {

                                query = "insert into [REFSIRP].[dbo].[AutomatedMIDREFSReport]([MId],[IssueDate],[ShipmentDate],[Status]" +
                                ",[CntValidRefs],[CntInValidRefs],[CntTotal],[PercentageMatched],[Publisher],[sentDate],[Entrydate],[IsEligible]) " +
                                "values('" + Mid + "','" + IssuseDate + "','" + ShipmentDate + "','" + Status + "'," +
                                "'" + CntValidref + "','" + CntInvalidRef + "','" + CntTotal + "','" + perMatch1 + "'," +
                                "'" + publisher + "','" + sentdate + "','" + entrydate + "','" + IsEligible + "')";

                                ExecuteInsertMIDQuery(connectionString, query, Mid, IssuseDate, ShipmentDate);

                            }





                            //IsEligible(Eligible / probable / NotAttemted)

                            // perMatch = int.Parse(value2[0]) / 100;


                        }



                    }

                }




            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine(err);
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


           
        }

        private bool IsEligibleAutoMID(string connectionString, string mid, string publisher)
        {
            string query = "SELECT 1 " +
                            "FROM [REFSIRP].[dbo].[AutoMIDEligible] " +
                            "WHERE [MID] = @midValue AND [Publisher] = @publisherValue";
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@midValue", mid);
                command.Parameters.AddWithValue("@publisherValue", publisher);

                return command.ExecuteScalar() != null;
            }
        }

        private string GetIsEligibleMIDStatus(string mid, string publisher,string connectionString)
        {
            string StrIsEligible = string.Empty;
            string query = string.Empty;
            // Check if both MID and Publisher values are available (eligible)

            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            if (IsEligible(connection, mid, publisher))
            {
                StrIsEligible = "Eligible";
            }
            // Check if MID is not in the table but Publisher is there (probable)
            else if (IsProbable(connection, mid, publisher))
            {
                StrIsEligible = " Probable";
            }
            // Check if both MID and Publisher are not in the table (not attempted)
            else
            {
                StrIsEligible = "NotAttempted";
            }


            return StrIsEligible;
        }



        static bool IsEligible(SqlConnection connection, string midValue, string publisherValue)
        {
            string query = "SELECT 1 " +
                           "FROM [REFSIRP].[dbo].[AutoMIDEligible] " +
                           "WHERE [MID] = @midValue AND [Publisher] = @publisherValue";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@midValue", midValue);
                command.Parameters.AddWithValue("@publisherValue", publisherValue);

                return command.ExecuteScalar() != null;
            }
        }

        static bool IsProbable(SqlConnection connection, string midValue, string publisherValue)
        {
            string query = "SELECT 1 " +
                           "FROM [REFSIRP].[dbo].[AutoMIDEligible] " +
                           "WHERE [Publisher] = @publisherValue";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@publisherValue", publisherValue);

                return command.ExecuteScalar() != null;
            }
        }

        private void ExecuteInsertQuery(SqlConnection connection, StringBuilder sbtmp,string AN,string strMID, SortedList<string, string> sortedListSeqFlag)
        {
           
            try
            {
                if (sbtmp.Length > 0)
                {
                    string insertBatchQuery = sbtmp.ToString();

                    using (SqlCommand command = new SqlCommand(insertBatchQuery, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        updateSeqFlag(sortedListSeqFlag, AN);
                        Console.WriteLine($"Rows inserted: {rowsAffected}"+"   AN : "+ AN + "   MID: " + strMID);
                    }
                }

            }
             catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();

                // Handle the exception as needed
            }
        }

        private void ExecuteInsertMIDQuery(string connectionString, string Query, string MID, string Issusedate,string ShipmentDate)
        {

            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                using (SqlCommand command = new SqlCommand(Query, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Rows inserted: {rowsAffected}" + "   MID : " + MID + "   IssuseDate: " + Issusedate + "   ShipmentDate: " + ShipmentDate);
                    }
       
            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();

                // Handle the exception as needed
            }
        }
        public string GetShipmentdateDate(string mid, string issusedate)
        {
            string ShipmentDate = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ToString();
            // Replace with your actual connection string

            string query = " select d.ShipmentDate from MIDIssue c,[ShipmentProcessDetails] d" +
           " where c.MId = '" + mid + "' and c.IssueDate = '" + issusedate + "' and c.ShipmentId = d.ShipmentID";
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
        public void AutoFindMIDEligible(ref StringBuilder sberrorlog,string fileReferenceANAll,string InputHTMLPath)
        {
            string FileReportMIDWise = string.Empty;
            //string FileReportMIDWise = ConfigurationManager.AppSettings["OutputFolderPath"].ToString();
            // Read all lines from the current text file
            //string[] lines = File.ReadAllLines(@"E:\EbscoResearch\REFS\Report20240215\allrefs.txt");

            // string content = File.ReadAllText(fileReferenceANAll);
            FileReportMIDWise = Path.Combine(InputHTMLPath, "ReportMIDWise.txt");

            string[] lines = File.ReadAllLines(fileReferenceANAll);

            


            SortedList<string, string> slMIDs = new SortedList<string, string>();

            // Check if the file has any content
            if (lines.Length > 0)
            {
                // Split the first line to get column names
                string[] columnNames = lines[0].Split('\t');


                int ColAN = 5;
                int ColMID = 1;
                int ColIssuseDate = 2;
                int CntMatched = 3;
                int PerCentMatched = 10;
                string MID;
                int ColShipmentDate = 11;
                string ShipmentDate;
                string IssuseDate;

                // Iterate through each line (excluding the header)
                for (int i = 1; i <lines.Length; i++)
                {
                    string[] values = lines[i].Split('\t');

                    if (values.Length > 10)
                    {
                        //if (values[CntMatched] == "Count Match")
                        {
                            MID = values[ColMID];
                            if (MID == "KJC")
                                MID = MID;
                           // ShipmentDate = values[ColShipmentDate];

                            IssuseDate= values[ColIssuseDate];
                            ShipmentDate = GetShipmentdateDate(MID, IssuseDate);
                            DateTime Date = DateTime.Parse(ShipmentDate);
                            ShipmentDate = Date.ToString("yyyyMMdd");
                            string keystring = MID + "-" + ShipmentDate + "-"+ IssuseDate + "-" + values[CntMatched].Replace(" ", "");

                            if (slMIDs.ContainsKey(keystring))
                            {
                                slMIDs[keystring] = slMIDs[keystring] + "|" + values[PerCentMatched];
                            }
                            else
                                slMIDs.Add(keystring, values[PerCentMatched]);
                        }
                    }
                }

            }

            SortedList<string, string> slPerDetails = new SortedList<string, string>();
            slPerDetails.Add("000MIDISSUE", "PercentAbove90|Above90|Below90|Total");
            for (int i2 = 0; i2 < slMIDs.Count; i2++)
            {

                string MIDISSUE = slMIDs.Keys[i2];
                string percentages = slMIDs.Values[i2];
                bool IsFailedMid = false;

                if (MIDISSUE.Contains("0CL-20240201-20240213-Mismatch"))
                    i2 = i2;

                //if (slMIDs.Keys[i2].ToString().Contains("Mismatch") || slMIDs.Keys[i2].ToString().Contains("MID-ShipmentDate-IssuseDate"))
                //    IsFailedMid = true;

                if (i2 + 1 < slMIDs.Count)
                {
                    if (slMIDs.Keys[i2 + 1] == MIDISSUE.Replace("CountMatch", "Mismatch"))
                        IsFailedMid = true;

                }

                if (IsFailedMid == false)
                {
                    string[] perstrings = percentages.Split('|');

                    int cntAbv90 = 0;
                    int cntBlw90 = 0;


                    for (int i = 0; i < perstrings.Length; i++)
                    {
                        if (Convert.ToInt16(perstrings[i]) >= 90)
                            cntAbv90++;
                        else
                            cntBlw90++;
                    }

                    int Percentage = Convert.ToInt32(Convert.ToDouble(cntAbv90) * 100 / Convert.ToDouble(perstrings.Length));

                    slPerDetails.Add(MIDISSUE, Percentage + "|" + cntAbv90 + "|" + cntBlw90 + "|" + perstrings.Length);
                }
            }

            //string filePath = @"FileReportMIDWise";

            // Create a StreamWriter to write to the file
            using (StreamWriter writer = new StreamWriter(FileReportMIDWise))
            {
                // Iterate through the sorted list and write each value to the file
                foreach (KeyValuePair<string, string> pair in slPerDetails)
                {
                    writer.WriteLine(pair.Key + "\t" + pair.Value);
                }
            }

            


        }
        static SortedList<string, string> SortByValues(SortedList<string, string> sortedList)
        {
            // Create a new SortedList with string keys and string values
            SortedList<string, string> sortedListSortedByValues = new SortedList<string, string>();

            // Swap keys and values in the original sorted list
            foreach (KeyValuePair<string, string> pair in sortedList)
            {
                sortedListSortedByValues.Add(pair.Value, pair.Key);
            }

            return sortedListSortedByValues;
        }


        public void GetNOLiveData(ref StringBuilder sberrorlog)
        {
            try
            {
                string query = string.Empty;
                string MId = string.Empty;
                string IssueDate = string.Empty;
                string ShipmentDate = string.Empty;
                string inputPath = ConfigurationManager.AppSettings["InputFolderPath"].ToString();
                string outputPath = ConfigurationManager.AppSettings["OutputFolderPath"].ToString();
                string inputFilePath = string.Empty;
                string ouputFilePath = string.Empty;

                // Delete all subdirectories in the folder
                if (Directory.Exists(outputPath))  
                {
                    foreach (string subdirectory in Directory.GetDirectories(outputPath))
                    {
                        Directory.Delete(subdirectory, true); // The second parameter specifies whether to delete subdirectories and files
                    }
                }
               

                string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
                //query = "SELECT * FROM [REFSIRP].[dbo].[AutoRefLog] WHERE Status = 'NoLiveData' and sentDate!='1900-01-01' ";

                query = "SELECT *,b.sentDate FROM[REFSIRP].[dbo].[AutoRefLog] a,[REFSIRP].[dbo].[MIDIssue] b " +
                    "where a.Status = 'NoLiveData' and a.MId = b.MId and a.IssueDate = b.IssueDate " +
                    "and a.sentDate != b.sentDate";
                DataTable resultTable = ExecuteQuery(connectionString, query);
                for (int i = 0; i < resultTable.Rows.Count; i++)
                {
                    MId = resultTable.Rows[i]["MId"].ToString();
                    IssueDate = resultTable.Rows[i]["IssueDate"].ToString();
                    ShipmentDate = resultTable.Rows[i]["ShipmentDate"].ToString();

                    DateTime Date = DateTime.Parse(ShipmentDate);
                    ShipmentDate = Date.ToString("yyyyMMdd");

                    ShipmentDate = ShipmentDate.Replace("-", "");
                    inputFilePath = Path.Combine(inputPath, ShipmentDate, MId + "_" + IssueDate);
                    ouputFilePath = Path.Combine(outputPath, MId + "_" + IssueDate);
                    if (Directory.Exists(inputFilePath))
                    {
                        CopyFolder(inputFilePath, ouputFilePath);
                        Console.WriteLine($"Folder Copied Succesfully:   + MID : " + MId + "   IssuseDate: " + IssueDate + "   ShipmentDate: " + ShipmentDate);
                    }

                }


            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");

            }

        }

        static void CopyFolder(string sourceFolder, string destinationFolder)
        {
            // Create the destination folder if it doesn't exist
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            // Get all files and subdirectories in the source folder
            string[] files = Directory.GetFileSystemEntries(sourceFolder);

            foreach (string file in files)
            {
                // Create the destination file/directory path by combining the destination folder with the file/directory name
                string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file));

                // If it's a file, copy it
                if (File.Exists(file))
                {
                    File.Copy(file, destinationFile, true); // The third parameter specifies whether to overwrite the file if it already exists
                }
                // If it's a directory, recursively copy it
                else if (Directory.Exists(file))
                {
                    CopyFolder(file, destinationFile);
                }
            }
        }


        public void AutoMIDEligible(ref StringBuilder sberrorlog)
        {
            Dictionary<string, string> dicEMID = new Dictionary<string, string>();
            DataTable ANTable = new DataTable();
            StringBuilder sbtemp = new StringBuilder();
            string ANID = string.Empty;

            string strQuery = string.Empty;

            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;

            try
            {
                dicEMID = GetEligibleMIDRefsdetails(ref sberrorlog);
                foreach (var Entry in dicEMID)
                {
                    string MID = Entry.Key.ToString();

                    //MID = "3CPN";
                    ANTable = GetExtractedReferences(MID);
                    SqlConnection connection = new SqlConnection(connectionString);
                    connection.Open();

                    strQuery = "insert into [REFSIRP].[dbo].[ANCopiedRefs]([ANID],[RefsText],[PageNo]) ";
                    //sbtemp.AppendLine("values(");

                    for (int i = 0; i < ANTable.Rows.Count; i++)
                    {
                        // dicEMID.Add(ANTable.Rows[i]["MID"].ToString(), resultTable.Rows[i]["Publisher"].ToString());

                        ANID = ANTable.Rows[i]["ANID"].ToString();

                        string query = "delete FROM [REFSIRP].[dbo].[ANCopiedRefs] where ANID='" + ANID + "'";
                        ExecuteQueryExists(connection, query);

                        sbtemp.AppendLine(strQuery + "values ('" + ANTable.Rows[i]["ANID"].ToString() + "','" +
                                              ANTable.Rows[i]["CombinedExtractedReferences"].ToString() + "','" +
                                                ANTable.Rows[i]["PageNo"].ToString() + "')");


                        if (sbtemp.Length > 0)
                        {
                            ExecuteInsertANCopiedRefs(connection, sbtemp, ANID, MID);
                            sbtemp.Clear();
                        }

                    }





                
                }




            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");

            }
        }




        private void ExecuteInsertANCopiedRefs(SqlConnection connection, StringBuilder sbtemp, string ANID, string MID)
        {
            try
            {
                if (sbtemp.Length > 0)
                {
                    string insertBatchQuery = sbtemp.ToString();

                    using (SqlCommand command = new SqlCommand(insertBatchQuery, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Rows inserted: {rowsAffected}" + "   ANID : " + ANID + "   MID: " + MID);
                    }
                }

            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();

                // Handle the exception as needed
            }
        }
        private DataTable GetExtractedReferences(string MID)
        {

            string stran = string.Empty;
            DateTime currentDate = DateTime.Now;
            string TodayDate = currentDate.ToString("yyyy-MM-dd");
            //DataTable ExtractRefTable = new DataTable();
            // string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;
            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;


            //string query = "SELECT * FROM [REFSIRP].[dbo].[AutoREFANDetails_CompareInfo] where  MID='" + MID + "' and Entrydate='"+ TodayDate + "'";
            //string query = "SELECT * FROM [TOC_UAT].[dbo].[REFANDetails_CompareInfo] where  MID='" + MID + "' ";
            //and Entrydate='" + TodayDate + "'";


            //string query = "SELECT ANID,[PageNo],STRING_AGG([ExtractedReference] + CHAR(13) + CHAR(10), '') AS CombinedExtractedReferences " +
            //    "FROM [TOC_UAT].[dbo].[REFANDetails_CompareInfo] WHERE MID = '" + MID + "' GROUP BY PageNo, ANID ORDER BY ANID";


            string query = "SELECT ANID,[PageNo],STRING_AGG([ExtractedReference] + CHAR(13) + CHAR(10), '') AS CombinedExtractedReferences " +
                "FROM  [REFSIRP].[dbo].[AutoREFANDetails_CompareInfo] WHERE MID = '" + MID + "' and Entrydate='" + TodayDate + "' " +
                "GROUP BY PageNo, ANID ORDER BY ANID";

            //string query = "SELECT ANID,[PageNo],STRING_AGG([ExtractedReference] + CHAR(13) + CHAR(10), '') AS CombinedExtractedReferences " +
            //  "FROM  [REFSIRP].[dbo].[AutoREFANDetails_CompareInfo] WHERE MID = '" + MID + "' and Entrydate='20240226' " +
            //  "GROUP BY PageNo, ANID ORDER BY ANID";



            DataTable resultTable = ExecuteQuery(connectionStringREF, query);

            return resultTable;
        }

        private Dictionary<string, string> GetEligibleMIDRefsdetails(ref StringBuilder sberrorlog)
        {
            string stran = string.Empty;
            Dictionary<string, string> dicEMID = new Dictionary<string, string>();
            string connectionStringREF = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;

            string query = "SELECT * FROM [REFSIRP].[dbo].[AutoMIDEligible] where ISActive = 1 and ISManual=0";

            DataTable resultTable = ExecuteQuery(connectionStringREF, query);
            for (int i = 0; i < resultTable.Rows.Count; i++)
            {
                dicEMID.Add(resultTable.Rows[i]["MID"].ToString(), resultTable.Rows[i]["Publisher"].ToString());

            }

            return dicEMID;

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
        private bool ExecuteQueryExists(SqlConnection connection,string query)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                   
                    object result = command.ExecuteScalar();
                    return (result != null && result != DBNull.Value);
                }
            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                Console.WriteLine($"An error occurred: {ex.Message}");
                //Console.ReadLine();
                return false;
                             // Handle the exception as needed
            }

        }


        public void GetReportLogAbovePer(string connectionString,string FileProbableReportlog, ref StringBuilder sberrorlog)
        {
            try
            {
                string query = string.Empty;
                string MId = string.Empty;
                string IssueDate = string.Empty;
                string ShipmentDate = string.Empty;
                string sentDate = string.Empty;
                string Entrydate = string.Empty;
                string TotalRefs = string.Empty;
                string PM = string.Empty;
                int id = 0;
                StringBuilder sb = new StringBuilder();
                string output = string.Empty;
                //          SELECT TOP(1000) [ID]
                //,[MId]
                //,[IssueDate]
                //,[ShipmentDate]
                //,[Status]
                //,[CntValidRefs]
                //,[CntInValidRefs]
                //,[CntTotal]
                //,[PercentageMatched]
                //,[Publisher]
                //,[sentDate]
                //,[Entrydate]
                //,[IsEligible]
                //          FROM[REFSIRP].[dbo].[AutoRefLog]

                sb.AppendLine("ID\tPublisher\tMID\tIssueDate\tShipmentDate\tTotalReferences\tPercentageMatched\tsentDate\tEntrydate");
                query = "SELECT DISTINCT a.* FROM[REFSIRP].[dbo].[AutoRefLog] a "+
                        "LEFT JOIN[AutoMIDEligible] b ON a.MId = b.MID "+
                        "WHERE convert(numeric(10,2), [PercentageMatched]) > 0.80 AND b.MID IS NULL ";
                DataTable resultTable = ExecuteQuery(connectionString, query);
                for (int i = 0; i < resultTable.Rows.Count; i++)
                {
                    MId = resultTable.Rows[i]["MId"].ToString();
                    IssueDate = resultTable.Rows[i]["IssueDate"].ToString();
                    ShipmentDate = resultTable.Rows[i]["ShipmentDate"].ToString();
                    sentDate = resultTable.Rows[i]["sentDate"].ToString();
                    Entrydate = resultTable.Rows[i]["Entrydate"].ToString();
                    TotalRefs = resultTable.Rows[i]["CntTotal"].ToString();
                    PM = resultTable.Rows[i]["PercentageMatched"].ToString();

                    DateTime Date = DateTime.Parse(ShipmentDate);
                    ShipmentDate = Date.ToString("yyyyMMdd");

                    ShipmentDate = ShipmentDate.Replace("-", "");
                    output = ++id+"\t"+resultTable.Rows[i]["Publisher"].ToString()+"\t"+ MId+"\t"+ IssueDate+"\t"+ ShipmentDate+"\t"+ TotalRefs +"\t"+ PM+"\t" + sentDate+ "\t" +Entrydate;
                    sb.AppendLine(output);

                }
                File.WriteAllText(FileProbableReportlog, sb.ToString());


            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");

            }

        }
    }
}
