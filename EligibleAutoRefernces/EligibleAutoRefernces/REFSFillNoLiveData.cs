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
    class REFSFillNoLiveData
    {

        //Created by Jaya on 20240307
        public void GetNOLiveData(ref StringBuilder sberrorlog)
        {
            try
            {

                string InputHTMLPath = string.Empty;
                string fileReferenceAll= string.Empty;
                string fileReferenceANAll = string.Empty;
                string FileReportMIDWise = string.Empty;
                string ErrorlogReport = string.Empty;

                string inputPath = ConfigurationManager.AppSettings["InputFolderPath"].ToString();
                string outputPath = ConfigurationManager.AppSettings["OutputFolderPath"].ToString();
              
                string IsNeedToGenerateReport = ConfigurationManager.AppSettings["IsNeedToGenerateReport"].ToString();
                string IsNeedDBREFInsertion = ConfigurationManager.AppSettings["IsNeedDBREFInsertion"].ToString();
                string Constr = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ToString();
                string ConstrTOC = ConfigurationManager.ConnectionStrings["MyConnectionString"].ToString();
                string fileReportLog = ConfigurationManager.AppSettings["ReportLog"].ToString();

                string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ConnectionString;
                REFSIdentification objRefs = new REFSIdentification(Constr, ConstrTOC, InputHTMLPath, fileReferenceAll, fileReportLog);
                SortedList<string, string> sortedListSeqFlag = new SortedList<string, string>();


                //query = "SELECT * FROM [REFSIRP].[dbo].[AutoRefLog] WHERE Status = 'NoLiveData' and sentDate!='1900-01-01' ";

                // Delete all subdirectories in the folder
                if (Directory.Exists(outputPath))
                {
                    foreach (string subdirectory in Directory.GetDirectories(outputPath))
                    {
                        Directory.Delete(subdirectory, true); // The second parameter specifies whether to delete subdirectories and files
                    }
                    // Delete all files in the folder
                    foreach (string file in Directory.GetFiles(outputPath))
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(outputPath);
                }
                                
               GetNoLiveDataFromDB(connectionString, inputPath, outputPath);//Copying data from Source to Destination

               
                string[] folders = Directory.GetDirectories(outputPath);
                if (folders.Length > 0)
                {

               

                    InputHTMLPath = ConfigurationManager.AppSettings["OutputFolderPath"].ToString();


                fileReferenceAll = Path.Combine(InputHTMLPath, "ReportAll.txt");

                fileReferenceANAll = Path.Combine(InputHTMLPath, "ReportRefMatchAll.txt");
                FileReportMIDWise = Path.Combine(InputHTMLPath, "ReportMIDWise.txt");
                ErrorlogReport = Path.Combine(InputHTMLPath, "Errorlog.txt");

                //Python code for convert PDF to HTML / CSS files
                 if (IsNeedToGenerateReport != "0")//Generate Report,ReportAll files
                {
                    string Refout = objRefs.GenerateReferencesFromHTML(InputHTMLPath, ref sberrorlog);
                    fileReferenceAll = Path.Combine(InputHTMLPath, "ReportAll.txt");
                    File.WriteAllText(fileReferenceAll, Refout);
                    Console.WriteLine("Report Generation Completed......");
                    // Console.ReadLine();
                }


                REFSFillLiveData REFSFillLiveData = new REFSFillLiveData();
                //Generating PM live DB reference with Extracted reference - ReportRefMatchAll file
               
                if (File.Exists(fileReferenceAll))  //Percentage Matching
                {

                    string RefANout = objRefs.GenerateReferencesPM(fileReferenceAll, ref sberrorlog, sortedListSeqFlag);
                    File.WriteAllText(fileReferenceANAll, RefANout);
                    Console.WriteLine("Percentage Matching LIVE Reference with Extracted Refernce is Completed......");
                    //Console.ReadLine();
                }


                //Generating PM live DB reference with Extracted reference file exist or not
                if (File.Exists(fileReferenceANAll))  //Generate ReportMIDWise File and inserted into DB([REFSIRP].[dbo].[AutoRefLog]) 
                {

                    Console.WriteLine("Finding the Eligible MID for above 90 % and count above 90 proces is started......");
                    //Generating text file of ReportMIDWise
                    REFSFillLiveData.AutoFindMIDEligible(ref sberrorlog, fileReferenceANAll, InputHTMLPath);//Finding the MID for above 90 % and count above 90

                    Console.WriteLine("Finding the Eligible MID for above 90 % and count above 90 process is Completed......");
                    
                    if (File.Exists(FileReportMIDWise))//MID wise details adding to DB [REFSIRP].[dbo].[AutoRefLog]
                    {
                        REFSFillLiveData.MIDWiseADDtoDB(ref sberrorlog, FileReportMIDWise);

                    }
                }

                ////inserting extracted references to DB
                if (IsNeedDBREFInsertion != "0")  //DB insertion
                {
                    // REFSFillLiveData REFSFillLiveData = new REFSFillLiveData();

                    REFSFillLiveData.process(ref sberrorlog, sortedListSeqFlag);  //Adding extracted refs to AutoREFANDetails_CompareInfo table
                    Console.WriteLine("Insertion of Extracted Reference in DB Completed......");
                    Console.WriteLine("Eligible MID Insertion of Extracted Reference in REF DB Started......");
                    REFSFillLiveData.AutoMIDEligible(ref sberrorlog);  //Adding extracted refs to ANCopiedRefs table
                    Console.WriteLine("Eligible MID Insertion of Extracted Reference in REF DB Completed......");


                }
                //Error 
                string Errortxt = string.Empty;
                Errortxt = sberrorlog.ToString();
               
                File.WriteAllText(ErrorlogReport, Errortxt);
                    // Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                string err = $"An error occurred: {ex.Message}";
                sberrorlog.AppendLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");

            }

        }

        private void GetNoLiveDataFromDB(string connectionString, string inputPath, string outputPath)
        {
            string MId = string.Empty;
            string IssueDate = string.Empty;
            string ShipmentDate = string.Empty;
            string inputFilePath = string.Empty;
            string ouputFilePath = string.Empty;
            string query = string.Empty;

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

    }
}
