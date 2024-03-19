using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace EligibleAutoRefernces
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Get Input PDF files
            string IsNeedToCopyPDFFromSource = ConfigurationManager.AppSettings["IsNeedToCopyPDFFromSource"].ToString();
            string IsNeedToGenerateReport = ConfigurationManager.AppSettings["IsNeedToGenerateReport"].ToString();
            string IsNeedDBREFInsertion = ConfigurationManager.AppSettings["IsNeedDBREFInsertion"].ToString();

            string InputREFPathFromSource = ConfigurationManager.AppSettings["InputREFPathFromSource"].ToString();
            string CurFolder = ConfigurationManager.AppSettings["InputFolder"].ToString();
            string inputPDFPath = ConfigurationManager.AppSettings["InputPDFPath"].ToString();
            string inputPublisher = ConfigurationManager.AppSettings["InputPublisher"].ToString();
            string fileReportLog = ConfigurationManager.AppSettings["ReportLog"].ToString();
            string fileReferenceAll = ConfigurationManager.AppSettings["ReferenceAll"].ToString();
            string fileReferenceANAll = ConfigurationManager.AppSettings["ReportANAll"].ToString();
            string InputHTMLPath = ConfigurationManager.AppSettings["InputHTMLPath"].ToString();
            string ErrorlogReport = ConfigurationManager.AppSettings["Errorlog"].ToString();
            string FileReportMIDWise = ConfigurationManager.AppSettings["ReportMIDWise"].ToString();
            string FileProbableReportlog = string.Empty;
            StringBuilder sberrorlog = new StringBuilder();

            #endregion

            string Constr = ConfigurationManager.ConnectionStrings["MyConnectionStringREFS"].ToString();
            string ConstrTOC = ConfigurationManager.ConnectionStrings["MyConnectionString"].ToString();
            SortedList<string, string> sortedListSeqFlag = new SortedList<string, string>();


            string InputREFPathFromSourceFiltered = Path.Combine(InputREFPathFromSource, CurFolder);
            REFSIdentification objRefs = new REFSIdentification(Constr, ConstrTOC, InputHTMLPath, fileReferenceAll, fileReportLog);

            //objRefs.GetPublisherMIDsFromDB(inputPublisher);
            //if (IsNeedToCopyPDFFromSource != "0")
            //    objRefs.CopyPDFfilesFromSource(InputREFPathFromSourceFiltered, inputPDFPath);


            ////Get NO_live Data from DB
            REFSFillNoLiveData REFSFillNOLiveData = new REFSFillNoLiveData();
         // REFSFillNOLiveData.GetNOLiveData(ref sberrorlog);



            ////InputHTMLPath = ConfigurationManager.AppSettings["OutputFolderPath"].ToString();

            REFSFillLiveData REFSFillLiveData = new REFSFillLiveData();
            fileReferenceAll = Path.Combine(InputHTMLPath, "ReportAll.txt");
            fileReferenceANAll = Path.Combine(InputHTMLPath, "ReportRefMatchAll.txt");
            FileReportMIDWise = Path.Combine(InputHTMLPath, "ReportMIDWise.txt");
            ErrorlogReport = Path.Combine(InputHTMLPath, "Errorlog.txt");
            FileProbableReportlog = Path.Combine(InputHTMLPath, "ProbableReportlog.txt");


            //REFSFillLiveData.GetReportLogAbovePer(Constr, FileProbableReportlog, ref sberrorlog);
            //Python code for convert PDF to HTML / CSS files
            if (IsNeedToGenerateReport != "0")//Generate Report,ReportAll files
            {
                string Refout = objRefs.GenerateReferencesFromHTML(InputHTMLPath, ref sberrorlog);

                File.WriteAllText(fileReferenceAll, Refout);
                Console.WriteLine("Report Generation Completed......");

            }



            ////Generating PM live DB reference with Extracted reference - ReportRefMatchAll file

            if (File.Exists(fileReferenceAll))  //Percentage Matching
            {
                string RefANout = objRefs.GenerateReferencesPM(fileReferenceAll, ref sberrorlog, sortedListSeqFlag);
                File.WriteAllText(fileReferenceANAll, RefANout);
                Console.WriteLine("Percentage Matching LIVE Reference with Extracted Refernce is Completed......");
                //Console.ReadLine();
            }


            ////Generating PM live DB reference with Extracted reference file exist or not
            if (File.Exists(fileReferenceANAll))  //Generate ReportMIDWise File and inserted into DB([REFSIRP].[dbo].[AutoRefLog]) 
            {

                Console.WriteLine("Finding the Eligible MID for above 90 % and count above 90 proces is started......");
                // REFSFillLiveData REFSFillLiveData = new REFSFillLiveData();
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

            //Log that having above 0.8 percentage matching.
            REFSFillLiveData.GetReportLogAbovePer(Constr, FileProbableReportlog,ref sberrorlog);
            


            //Error 
            string Errortxt = string.Empty;
            Errortxt = sberrorlog.ToString();
          
            File.WriteAllText(ErrorlogReport, Errortxt);
            Console.WriteLine("REFS Auto Extraction Completed......");
           // Console.ReadLine();





        }
    }
}
