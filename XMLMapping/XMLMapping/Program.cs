using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using XMLMapping;
using System.Threading;
using XMLStorageTypes;

namespace Project1
{
    class Class1
    {
        public static DateTime startTime;
        public static int totalFiles = 0;
        public static HashSet<string> BrokenIMANRel = new HashSet<string>();
        public static HashSet<string> TotalIMAN = new HashSet<string>();
        public static int fileCount = 0;

        public static void Main(string[] args)
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            ShowTitle();

            startTime = DateTime.Now;

            if (!args.Select(x => x.ToUpper()).Contains("-CLEAN"))
            {
                string _inputPath = args[0].Remove(0, 11);
                Config _config = new Config(_inputPath);

                if (_config.IsMade(Config.FilesEnum.ResetCache))
                {
                    string[] files = Directory.GetFiles(@".\Cache");

                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                }
            }


            #region Clean/Validate
            if (!isValidate(args) && isRemoveBadChars(args))
            {
                Clean(args);
            }
            else if (isValidate(args) && !isRemoveBadChars(args))
            {
                try
                {
                    ConsoleColor oldColor = Console.ForegroundColor;
                    Console.Title = "XML Validator";

                    if (IsFileLocked(new FileInfo(@"./FailedValidation.csv")))
                    {

                        foreach (Process p in Process.GetProcessesByName("EXCEL"))
                        {
                            if (p.MainWindowTitle == "Microsoft Excel - FailedValidation.csv")
                            {
                                p.Kill();
                                p.WaitForExit();
                            }
                        }

                    }


                    if (File.Exists(@"./FailedValidation.csv"))
                        File.Delete(@"./FailedValidation.csv");
                    if (getFileType(getValidateFile(args)) == 1)//Dir
                    {
                        String[] files = Directory.GetFiles(getValidateFile(args));

                        if (files.Length == 0)
                        {
                            Console.WriteLine("The 'Source XML Files' folder is empty.");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("");
                            Console.WriteLine("***XML File Validator***");
                            Console.WriteLine("");
                            Console.WriteLine("Files Loaded...Validating XML file(s)");
                            foreach (string file in files)
                            {
                                Console.WriteLine("-------------------------------------------------------------------");
                                Console.WriteLine("Loading File " + file);
                                Console.WriteLine("");
                                Console.Write("Validating...");
                                ValidateData(file);
                                WriteLineComplete("Complete");
                                Console.WriteLine("");
                            }
                        }
                    }
                    else if (getFileType(getValidateFile(args)) == 2)//File
                    {
                        ValidateData(getValidateFile(args));
                    }
                    else
                    {
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Path not Found.");
                        Console.ForegroundColor = oldColor;

                        return;
                    }


                    string currentContent = String.Empty;
                    if (File.Exists(@"./FailedValidation.csv"))
                    {
                        currentContent = File.ReadAllText(@"./FailedValidation.csv");
                        File.WriteAllText(@"./FailedValidation.csv", "PUID,Object Type,Attribute/Bad Value, Line,Char Position,Char,File\n" + currentContent);
                    }
                    Console.WriteLine("");
                    TimeSpan duration = DateTime.Now - startTime;

                    WriteLineComplete("Validation complete. Duration - " + duration.Hours + ":" + duration.Minutes + ":" + duration.Seconds + "." + duration.Milliseconds);

                    if (File.Exists(@"./FailedValidation.csv"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Log file generated at " + Path.GetFullPath(@"./FailedValidation.csv"));
                    }
                    else
                    {
                        ConsoleColor old = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" - 0 errors found.");
                        Console.ForegroundColor = old;
                    }

                    Console.WriteLine("");
                    Console.ForegroundColor = oldColor;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            #endregion
            else if (args.Length == 1 && args[0].ToUpper().StartsWith("-INPUTFILE="))
            {
                string inputPath = args[0].Remove(0, 11);

                Console.Title = "Denso Mapping Utility";

                //load config file
                Config config = new Config(inputPath);

                string RSIPS = Path.Combine(config.OutputPath, "ReleaseStatus_IPS_Files");
                string PCIPS = Path.Combine(config.OutputPath, "ParamCode_IPS_Files");
                string DSRNIPS = Path.Combine(config.OutputPath, "DatasetRename_IPS_Files");
                string DSRNSQL = Path.Combine(config.OutputPath, "DatasetRename_SQL_Files");

                #region Folder Create/Delete


                if (!Directory.Exists(DSRNSQL))
                {
                    Directory.CreateDirectory(DSRNSQL);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(DSRNSQL), File.Delete);
                }


                if (!Directory.Exists(config.OutputPath))
                {
                    Directory.CreateDirectory(config.OutputPath);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(config.OutputPath), File.Delete);
                }

                if (!Directory.Exists(DSRNIPS))
                {
                    Directory.CreateDirectory(DSRNIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(DSRNIPS), File.Delete);
                }

                if (!Directory.Exists(RSIPS))
                {
                    Directory.CreateDirectory(RSIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(RSIPS), File.Delete);
                }

                if (!Directory.Exists(PCIPS))
                {
                    Directory.CreateDirectory(PCIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(PCIPS), File.Delete);
                }

                if (!Directory.Exists(config.TargetPath))
                {
                    Directory.CreateDirectory(config.TargetPath);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(config.TargetPath), File.Delete);
                }
                #endregion


                //Get List of XML Files
                String[] files = Directory.GetFiles(config.SourcePath);

                if (files.Length == 0)
                {
                    Console.WriteLine("The '" + Path.GetDirectoryName(config.SourcePath) + "' folder is empty.");
                }
                else
                {


                    dynamic o = HelperUtility.LoadSourceData(config.SourcePath);

                    IEnumerable<Classes.Item> MasterItems = (IEnumerable<Classes.Item>)o.Items;
                    IEnumerable<Classes.Revision> MasterRevisions = (IEnumerable<Classes.Revision>)o.Revisions;
                    IEnumerable<string[]> UsedDatasets = (IEnumerable<string[]>)o.UsedDatasets;
                    int TotalDatasets = (int)o.DatasetCount;
                    //int OrphanDatasets = MasterDatasets.Except(MasterRevisions.SelectMany(x => x.GetDatasets())).Count();
                    //int RecursiveDatasets = MasterDatasets.AsEnumerable().Where(x => x.ParentUID == x.PUID).Count();

                    IEnumerable<string> RefItem = ((HashSet<string>)(o.RefCadItems)).ToList();
                    IEnumerable<Classes.Revision> RefRevs = (IEnumerable<Classes.Revision>)(o.RefCadRevisions);
                    Console.WriteLine("Starting Mapping....");




                    foreach (string file in files)
                    {
                        HelperUtility util = new HelperUtility();
                        util.MasterItems = MasterItems;
                        util.MasterRevisions = MasterRevisions;
                        util.UsedDatasets = UsedDatasets;
                        util.RefCadItems = RefItem;
                        util.RefCadRevs = RefRevs;
                        util.config = config;
                        totalFiles = files.Count();

                        util.LoadFile(file);
                        string newFile = Path.Combine(config.TargetPath, Path.GetFileNameWithoutExtension(file) + "_Mapped.xml");

                        //Method for Translate Instructions
                        Translate(ref util, file, newFile);


                        //Saves the XML file
                        util.SaveFile(newFile);

                    }


                    #region PartRenumberList

                    Console.Write("Generating Part-Renumbering File");

                    Processing();
                    HelperUtility.GeneratePartRenumFile(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.ItemRenum));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    #region ReleaseStatus File

                    Console.Write("Generating ReleaseStatus File");

                    Processing();
                    HelperUtility.GenerateIPSReleaseStatus(config.MaxSplitRsIPS, RSIPS, MasterRevisions, config.IsMade(Config.FilesEnum.ReleaseStatusIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Dataset Rename IPS Code File

                    Console.Write("Generating DatasetRename IPS File");

                    Processing();
                    HelperUtility.GenerateIPSDatasetRename(config.MaxSplitPcIPS, DSRNIPS, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetRenameIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Dataset Rename SQL Code File

                    Console.Write("Generating DatasetRename SQL File");
                    Processing();
                    HelperUtility.GenerateSQLDatasetRename(config.MaxSplitDsRenameSQL, DSRNSQL, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetRenameSQL));

                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Parameter Code File

                    Console.Write("Generating ReleaseStatus File");

                    Processing();
                    HelperUtility.GenerateIPSParameterCode(config.MaxSplitPcIPS, PCIPS, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetParamCodeIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Ref->CAD List
                    Console.Write("Generating Reference To CAD File");

                    Processing();
                    HelperUtility.GenerateRef2CADFile(config.OutputPath, RefRevs, config.IsMade(Config.FilesEnum.ReferenceToCAD));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    #region Rev Errors

                    Console.Write("Generating Revision Error File");
                    Processing();
                    HelperUtility.GenerateErrorRevs(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.MissingItems));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region DatasetFailures
                    Console.Write("Generating Revision Error File");
                    Processing();
                    HelperUtility.GenerateDatasetFailures(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetFailures));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    //#region OrphanDatasets
                    //Console.Write("Generating Revision Error File");
                    //Processing();
                    //HelperUtility.GenerateOrphanDatasets(config.OutputPath, MasterDatasets, config.IsMade(Config.FilesEnum.OrphanDatasets));
                    //WriteLineComplete("Complete");
                    //Console.WriteLine("");
                    //#endregion

                    //#region RecursiveDatasets
                    //Console.Write("Generating Revision Error File");
                    //Processing();
                    //HelperUtility.GenerateRecursiveDatasets(config.OutputPath, MasterDatasets, config.IsMade(Config.FilesEnum.RecursiveDatasets));
                    //WriteLineComplete("Complete");
                    //Console.WriteLine("");
                    //#endregion

                    #region RevisionImport
                    Console.Write("Generating Revision Import File");
                    Processing();
                    HelperUtility.GenerateRevisionImport(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.RevisionImport));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    if (config.Reports.Contains(Config.FilesEnum.Log))
                    {
                        #region Log File

                        Console.Write("Generating Log File");
                        Processing();

                        HelperUtility.GenerateLog(config.OutputPath, MasterItems, MasterRevisions, RefItem.Count(), TotalDatasets, RefRevs.Count(), BrokenIMANRel.Count(), TotalIMAN.Count(), DateTime.Now - startTime, false);
                        WriteLineComplete("Complete");
                        Console.WriteLine("");

                        #endregion
                    }
                }

                Console.WriteLine("");
                TimeSpan duration = DateTime.Now - startTime;

                WriteLineComplete("Mapping complete. Duration - " + duration.Hours + ":" + duration.Minutes + ":" + duration.Seconds + "." + duration.Milliseconds);
                Console.WriteLine("");
            }
            //**********************************************************************************************
            //************************************** R E P O R T *******************************************
            //**********************************************************************************************
            else if (args.Select(x => x.ToUpper()).Where(x => x.Contains("-INPUTFILE") || x.Contains("-REPORT")).Count() == 2)
            {
                string inputPath = args.Select(x => x.ToUpper()).Where(x => x.Contains("-INPUTFILE")).Single().Remove(0, 11);

                Console.Title = "Denso Mapping Reporting";

                //load config file
                Config config = new Config(inputPath);

                string RSIPS = Path.Combine(config.OutputPath, "ReleaseStatus_IPS_Files");
                string PCIPS = Path.Combine(config.OutputPath, "ParamCode_IPS_Files");
                string DSRNIPS = Path.Combine(config.OutputPath, "DatasetRename_IPS_Files");
                string DSRNSQL = Path.Combine(config.OutputPath, "DatasetRename_SQL_Files");

                #region Folder Create/Delete
                if (!Directory.Exists(config.OutputPath))
                {
                    Directory.CreateDirectory(config.OutputPath);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(config.OutputPath), File.Delete);
                }

                if (!Directory.Exists(DSRNSQL))
                {
                    Directory.CreateDirectory(DSRNSQL);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(DSRNSQL), File.Delete);
                }

                if (!Directory.Exists(DSRNIPS))
                {
                    Directory.CreateDirectory(DSRNIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(DSRNIPS), File.Delete);
                }

                if (!Directory.Exists(RSIPS))
                {
                    Directory.CreateDirectory(RSIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(RSIPS), File.Delete);
                }

                if (!Directory.Exists(PCIPS))
                {
                    Directory.CreateDirectory(PCIPS);
                }
                else
                {
                    Array.ForEach(Directory.GetFiles(PCIPS), File.Delete);
                }

                #endregion


                //Get List of XML Files
                String[] files = Directory.GetFiles(config.SourcePath);

                if (files.Length == 0)
                {
                    Console.WriteLine("The '" + Path.GetDirectoryName(config.SourcePath) + "' folder is empty.");
                }
                else
                {


                    dynamic o = HelperUtility.LoadSourceData(config.SourcePath);

                    IEnumerable<Classes.Item> MasterItems = (IEnumerable<Classes.Item>)o.Items;
                    IEnumerable<Classes.Revision> MasterRevisions = (IEnumerable<Classes.Revision>)o.Revisions;
                    //IEnumerable<Classes.Dataset> UsedDatasets = (IEnumerable<Classes.Dataset>)o.UsedDatasets;
                    int TotalDatasets = (int)o.DatasetCount;
                    //int OrphanDatasets = MasterDatasets.AsEnumerable().Except(MasterRevisions.SelectMany(x => x.GetDatasets())).Count();
                    //int RecursiveDatasets = MasterDatasets.AsEnumerable().Where(x => x.ParentUID == x.PUID).Count();

                    //int OrhpanDatasets = MasterDatasets.AsEnumerable().Where(x => x.ParentUID == "").Count();
                    //int RecursiveDatasets = MasterDatasets.AsEnumerable().Where(x => x.ParentUID == x.PUID).Count();

                    IEnumerable<string> RefItem = ((HashSet<string>)(o.RefCadItems)).ToList();
                    IEnumerable<Classes.Revision> RefRevs = (IEnumerable<Classes.Revision>)(o.RefCadRevisions);
                    Console.WriteLine("Starting Reporting....");




                    #region PartRenumberList

                    Console.Write("Generating Part-Renumbering File");

                    Processing();
                    HelperUtility.GeneratePartRenumFile(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.ItemRenum));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    #region Ref->CAD List
                    Console.Write("Generating Reference To CAD File");

                    Processing();
                    HelperUtility.GenerateRef2CADFile(config.OutputPath, RefRevs, config.IsMade(Config.FilesEnum.ReferenceToCAD));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    #region ReleaseStatus File

                    Console.Write("Generating ReleaseStatus File");

                    Processing();
                    HelperUtility.GenerateIPSReleaseStatus(config.MaxSplitRsIPS, RSIPS, MasterRevisions, config.IsMade(Config.FilesEnum.ReleaseStatusIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Dataset Rename IPS Code File

                    Console.Write("Generating DatasetRename IPS File");

                    Processing();
                    HelperUtility.GenerateIPSDatasetRename(config.MaxSplitPcIPS, DSRNIPS, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetRenameIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Dataset Rename SQL Code File

                    Console.Write("Generating DatasetRename SQL File");

                    Processing();
                    //HelperUtility.GenerateIPSDatasetRename(config.MaxSplitPcIPS, DSRNIPS, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetRenameIPS));
                    HelperUtility.GenerateSQLDatasetRename(config.MaxSplitDsRenameSQL, DSRNSQL, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetRenameSQL));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Parameter Code File

                    Console.Write("Generating ReleaseStatus File");

                    Processing();
                    HelperUtility.GenerateIPSParameterCode(config.MaxSplitPcIPS, PCIPS, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetParamCodeIPS));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region Rev Errors

                    Console.Write("Generating Revision Error File");
                    Processing();
                    HelperUtility.GenerateErrorRevs(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.MissingItems));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");

                    #endregion

                    #region DatasetFailures
                    Console.Write("Generating Revision Error File");
                    Processing();
                    HelperUtility.GenerateDatasetFailures(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.DatasetFailures));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    //#region OrphanDatasets
                    //Console.Write("Generating Revision Error File");
                    //Processing();
                    //HelperUtility.GenerateOrphanDatasets(config.OutputPath, MasterDatasets, config.IsMade(Config.FilesEnum.OrphanDatasets));
                    //WriteLineComplete("Complete");
                    //Console.WriteLine("");
                    //#endregion

                    //#region RecursiveDatasets
                    //Console.Write("Generating Revision Error File");
                    //Processing();
                    //HelperUtility.GenerateRecursiveDatasets(config.OutputPath, MasterDatasets, config.IsMade(Config.FilesEnum.RecursiveDatasets));
                    //WriteLineComplete("Complete");
                    //Console.WriteLine("");
                    //#endregion

                    #region RevisionImport
                    Console.Write("Generating Revision Import File");
                    Processing();
                    HelperUtility.GenerateRevisionImport(config.OutputPath, MasterRevisions, config.IsMade(Config.FilesEnum.RevisionImport));
                    WriteLineComplete("Complete");
                    Console.WriteLine("");
                    #endregion

                    if (config.Reports.Contains(Config.FilesEnum.Log))
                    {
                        #region Log File

                        Console.Write("Generating Log File");
                        Processing();

                        //HelperUtility.GenerateLog(config.OutputPath, MasterItems, MasterRevisions, RefItem.Count(), TotalDatasets, RecursiveDatasets, RefRevs.Count(), BrokenIMANRel.Count(), TotalIMAN.Count(), DateTime.Now - startTime, true);
                        WriteLineComplete("Complete");
                        Console.WriteLine("");

                        #endregion
                    }


                }

                Console.WriteLine("");
                TimeSpan duration = DateTime.Now - startTime;

                WriteLineComplete("Reporting complete. Duration - " + duration.Hours + ":" + duration.Minutes + ":" + duration.Seconds + "." + duration.Milliseconds);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Invalid Arugments - Did not process");
            }

            GC.Collect();
        }

        public static void Translate(ref HelperUtility util, String file, string mNewFile)
        {
            XNamespace ns = HelperUtility.xmlFile.GetDefaultNamespace();
            //IEnumerable<XElement> releaseStatus;

            util.SetFormatting(SaveOptions.None);

            Console.WriteLine("");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Loading File " + Path.GetFileNameWithoutExtension(file));
            fileCount++;
            Console.WriteLine("File " + fileCount.ToString() + "/" + totalFiles.ToString());
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.White;


            #region Reference to GNM8
            Console.Write("Apply Logic for Reference to GNM8");
            Processing();

            //Change Items

            var assemList = from el in HelperUtility.xmlFile.Elements(ns + "ItemRevision")
                            join assemRev in util.RefCadRevs on el.Attribute("puid").Value equals assemRev.PUID
                            select el;

            foreach (XElement el in assemList)
            {
                el.SetAttributeValue("object_type", "Production Revision");
            }

            assemList = from el in HelperUtility.xmlFile.Elements(ns + "Item")
                        join assemItem in util.RefCadItems on el.Attribute("puid").Value equals assemItem
                        select el;

            foreach (XElement el in assemList)
            {
                el.SetAttributeValue("object_type", "Production");
            }

            assemList = from el in HelperUtility.xmlFile.Elements(ns + "POM_stub")
                        join assemRev in util.RefCadRevs on el.Attribute("object_uid").Value equals assemRev.PUID
                        select el;

            foreach (XElement el in assemList)
            {
                el.SetAttributeValue("object_type", "Production Revision");
            }

            assemList = from el in HelperUtility.xmlFile.Elements(ns + "POM_stub")
                        join assemItem in util.RefCadItems on el.Attribute("object_uid").Value equals assemItem
                        select el;

            foreach (XElement el in assemList)
            {
                el.SetAttributeValue("object_type", "Production");
            }

            //change ref form
            var assemItemList = util.RefCadItems;

            var refForms = from form in util.GetElementsBy("Form", "object_type", "Reference Master").SearchList
                           join item in HelperUtility.xmlFile.Elements(ns + "Item") on form.Attribute("parent_uid").Value equals item.Attribute("puid").Value
                           where assemItemList.Contains(item.Attribute("puid").Value)
                           select form;

            foreach (var f in refForms)
            {
                f.SetAttributeValue("object_type", "Production Master");
            }

            refForms = from form in util.GetElementsBy("Form", "object_type", "Reference Revision Master").SearchList
                       join item in HelperUtility.xmlFile.Elements(ns + "Item") on form.Attribute("parent_uid").Value equals item.Attribute("puid").Value
                       where assemItemList.Contains(item.Attribute("puid").Value)
                       select form;

            foreach (var f in refForms)
            {
                f.SetAttributeValue("object_type", "Production Revision Master");
            }

            WriteLineComplete("Complete");
            Console.WriteLine("");

            #endregion

            #region Status Changes
            Console.Write("Status Change");
            Processing();

            #region everthing

            IEnumerable<XElement> list = from el in HelperUtility.xmlFile.Elements()
                                         where el.Attribute("release_status_list") != null && el.Attribute("release_status_list").Value != ""
                                         select el;


            foreach (var el in list)
            {
                el.SetAttributeValue("release_status_list", "");
            }

            //change rest of statuses to frozen
            list = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                   select status;


            foreach (var el in list)
            {
                el.SetAttributeValue("name", "GNM8_Frozen");
            }

            #endregion


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove JP & Extra IMAN Rel
            Console.Write("Remove JP");
            Processing();


            IEnumerable<XElement> listSd = from item in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "Item")
                                           where item.Attribute("item_id").Value.Count() > 2 &&
                                           item.Attribute("item_id").Value.Substring(0, 2).ToUpper() == "JP"
                                           select item;

            foreach (XElement el in listSd)
            {
                el.Attribute("item_id").Value = el.Attribute("item_id").Value.Remove(0, 2);
            }

            listSd = from dataset in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "Dataset")
                     where dataset.Attribute("object_name").Value.Count() > 2 &&
                     dataset.Attribute("object_name").Value.Substring(0, 2).ToUpper() == "JP"
                     select dataset;

            foreach (XElement el in listSd)
            {
                el.Attribute("object_name").Value = el.Attribute("object_name").Value.Remove(0, 2);
            }

            listSd = from form in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "Form")
                     where form.Attribute("object_name").Value.Count() > 2 &&
                     form.Attribute("object_name").Value.Substring(0, 2).ToUpper() == "JP"
                     select form;

            foreach (XElement el in listSd)
            {
                el.Attribute("object_name").Value = el.Attribute("object_name").Value.Remove(0, 2);
            }


            listSd = from form in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "PSBOMViewRevision")
                     where form.Attribute("object_name").Value.Count() > 2 &&
                     form.Attribute("object_name").Value.Substring(0, 2).ToUpper() == "JP"
                     select form;

            foreach (XElement el in listSd)
            {
                el.Attribute("object_name").Value = el.Attribute("object_name").Value.Remove(0, 2);
            }

            listSd = from form in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "PSBOMView")
                     where form.Attribute("object_name").Value.Count() > 2 &&
                     form.Attribute("object_name").Value.Substring(0, 2).ToUpper() == "JP"
                     select form;

            foreach (XElement el in listSd)
            {
                el.Attribute("object_name").Value = el.Attribute("object_name").Value.Remove(0, 2);
            }
            WriteLineComplete("Complete");
            Console.WriteLine("");

            #region IMAN
            #endregion


            #endregion

            #region Change Attribute Names
            Console.Write("Attribute Change");
            Processing();

            util.GetElementsBy("Item").ToUpperValue("item_id");
            util.GetElementsBy("Item", "object_type", "Production").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "Production Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "Prototype").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "Prototype Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "PartialProcMatl").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "PartialProcMatl Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "StandardPart").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "StandardPart Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            listSd = from el in HelperUtility.xmlFile.Elements(ns + "ItemRevision")
                     where
                     el.Attribute("object_type").Value == "Production Revision" ||
                      el.Attribute("object_type").Value == "Prototype Revision" ||
                       el.Attribute("object_type").Value == "PartialProcMatl Revision" ||
                        el.Attribute("object_type").Value == "StandardPart Revision"
                     select el;

            foreach (XElement el in listSd)
            {
                el.SetAttributeValue("gnm8_part_name", el.Attribute("object_name").Value.ToUpper());
            }

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Uppercase
            Console.Write("Uppercase");
            Processing();

            util.GetElementsBy("Form").ToUpperValue("object_name");
            util.GetElementsBy("ItemRevision").ToUpperValue("item_revision_id");
            util.GetElementsBy("ItemRevision").ToUpperValue("object_name");
            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove Attributes
            Console.Write("Remove Attributes");
            Processing();

            util.GetElementsBy("Form").RemoveAttribute("data_file");

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Change Object Types and Node Names
            Console.Write("Change Object Types and Node Names");
            Processing();

            #region PomStubs
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Production").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Production Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "PartialProcMatl").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "PartialProcMatl Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Prototype").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Prototype Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "StandardPart").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "StandardPart Revision").SetAttribute("object_type", "GNM8_CADItemRevision");


            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Reference").SetAttribute("object_type", "GNM8_Reference");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Reference Revision").SetAttribute("object_type", "GNM8_ReferenceRevision");

            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "StandardPart Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "StandardPart Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");

            #endregion

            #region Object Types
            //Production & Revision
            util.GetElementsBy("Item", "object_type", "Production").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "Production Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            //Prototype
            util.GetElementsBy("Item", "object_type", "Prototype").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "Prototype Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            //StandardPart
            util.GetElementsBy("Item", "object_type", "StandardPart").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "StandardPart Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            //PartialProcMatl
            util.GetElementsBy("Item", "object_type", "PartialProcMatl").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "PartialProcMatl Revision").SetAttribute("object_type", "GNM8_CADItemRevision");

            //Reference
            util.GetElementsBy("Item", "object_type", "Reference").SetAttribute("object_type", "GNM8_Reference");
            util.GetElementsBy("ItemRevision", "object_type", "Reference Revision").SetAttribute("object_type", "GNM8_ReferenceRevision");

            util.GetElementsBy("Form", "object_type", "Reference Master").SetAttribute("object_type", "GNM8_ReferenceMaster");
            util.GetElementsBy("Form", "object_type", "Reference Revision Master").SetAttribute("object_type", "GNM8_ReferenceRevisionMaster");

            util.GetElementsBy("Form", "object_type", "Production Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("Form", "object_type", "Production Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");

            util.GetElementsBy("Form", "object_type", "Prototype Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("Form", "object_type", "Prototype Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");

            util.GetElementsBy("Form", "object_type", "StandardPart Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("Form", "object_type", "StandardPart Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");

            util.GetElementsBy("Form", "object_type", "PartialProcMatl Master").SetAttribute("object_type", "GNM8_CADItemMaster");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Revision Master").SetAttribute("object_type", "GNM8_CADItemRevisionMaster");

            //util.GetElementsBy("Form", "object_type", "GNM8_CADItemMaster").RenameNodes("GNM8_CADItemMaster");
            //util.GetElementsBy("Form", "object_type", "GNM8_CADItemRevisionMaster").RenameNodes("GNM8_CADItemRevisionMaster");

            //util.GetElementsBy("Form", "object_type", "GNM8_ReferenceMaster").RenameNodes("GNM8_ReferenceMaster");
            //util.GetElementsBy("Form", "object_type", "GNM8_ReferenceRevisionMaster").RenameNodes("GNM8_ReferenceRevisionMaster");
            #endregion

            #region Node Names
            //Production
            util.GetElementsBy("Item", "object_type", "GNM8_CADItem").RenameNodes("GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "GNM8_CADItemRevision").RenameNodes("GNM8_CADItemRevision");

            //Reference
            util.GetElementsBy("Item", "object_type", "GNM8_Reference").RenameNodes("GNM8_Reference");
            util.GetElementsBy("ItemRevision", "object_type", "GNM8_ReferenceRevision").RenameNodes("GNM8_ReferenceRevision");

            util.GetElementsBy("DIAMReferenceMaster000").RenameNodes("GNM8_ReferenceMasterS");
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameNodes("GNM8_ReferenceRevMasterS");

            #endregion

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Move Attribute
            Console.Write("Move Attributes");
            Processing();

            //change attributes on DIAMMaster and GNM8Rev
            util.GetElementsBy("GNM8_CADItemRevision").RenameAttribute("dia3_NDI_ECI_number", "gnm8_issue_no");
            util.GetElementsBy("GNM8_CADItemRevision").RenameAttribute("dia3_Split_Number", "gnm8_Issue_split_no");
            util.GetElementsBy("GNM8_CADItemRevision").RemoveAttribute("dia3_partNumber");

            util.GetElementsBy("GNM8_ReferenceRevision").RemoveAttribute("dia3_NDI_ECI_number");
            util.GetElementsBy("GNM8_ReferenceRevision").RemoveAttribute("dia3_Split_Number");
            util.GetElementsBy("GNM8_ReferenceRevision").RemoveAttribute("dia3_partNumber");



            //change attributes on DIAMRefMaster
            util.GetElementsBy("GNM8_ReferenceMasterS").RenameAttribute("Customer", "gnm8_Customer");
            util.GetElementsBy("GNM8_ReferenceMasterS").RenameAttribute("Description", "gnm8_Description");
            util.GetElementsBy("GNM8_ReferenceMasterS").RenameAttribute("Lead_Program", "gnm8_Lead_Program");
            util.GetElementsBy("GNM8_ReferenceRevMasterS").RenameAttribute("ECI_Number", "gnm8_ECI_Number");
            util.GetElementsBy("GNM8_ReferenceRevMasterS").RenameAttribute("Description", "gnm8_Description");


            util.CopyAttributeByRel("gnm8_dn_part_number", "GNM8_CADItem", "GNM8_CADItemRevision", "puid", "parent_uid");
            util.GetElementsBy("GNM8_CADItem").RemoveAttribute("gnm8_dn_part_number");

            #region transfer to master form
            util.GetElementsBy("DIAMProductionMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMProductionMaster000", "GNM8_CADItem", "parent_uid", "puid");

            util.GetElementsBy("DIAMProductionMaster000").RenameAttribute("Lead_Program", "gnm8_car_model");
            util.CopyAttributeByRel("gnm8_car_model", "DIAMProductionMaster000", "GNM8_CADItemRevision", "parent_uid", "parent_uid");

            //Cad Item Master REV

            util.GetElementsBy("DIAMProductionRevMaster000").RenameAttribute("ECI_Number", "gnm8_issue_no");

            util.GetElementsBy("DIAMProductionRevMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMProductionRevMaster000", "GNM8_CADItemRevision", "parent_uid", "parent_uid");

            #endregion



            //If only dia3_Split_Number IR Attribute is filled in, map to gnm8_issue_no
            //If both dia3_Split_Number & ECI_Number are filled in, map dia3_Split_Number to gnm8_issue_no
            list =
            from rev in util.GetElementsBy("GNM8_CADItemRevision").SearchList
            join diamMaster in util.GetElementsBy("DIAMProductionRevMaster000").SearchList on (string)rev.Attribute("parent_uid") equals (string)diamMaster.Attribute("parent_uid").Value
            where rev.Attribute("gnm8_Issue_split_no").Value != "" &&
            rev.Attribute("gnm8_issue_no").Value == ""
            //&& diamMaster.Attribute(" gnm8_issue_no").Value == ""
            select rev;

            if (list.Count() > 0)
            {
                foreach (XElement rev in list)
                {
                    rev.SetAttributeValue("gnm8_issue_no", rev.Attribute("gnm8_Issue_split_no").Value);

                    util.GetSingleElementByAttrID("DIAMProductionRevMaster000", "parent_uid", rev.Attribute("parent_uid").Value).SetAttributeValue("gnm8_issue_no", rev.Attribute("gnm8_issue_no").Value);
                }
            }


            //If only ECI_Number in Master form is filled in, map it to gnm8_issue_no
            IEnumerable<XElement[]> list2 = from diamMaster in HelperUtility.xmlFile.Elements(ns + "DIAMProductionRevMaster000")
                                            join rev in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision") on diamMaster.Attribute("parent_uid").Value equals rev.Attribute("parent_uid").Value
                                            where rev.Attribute("gnm8_issue_no").Value == "" &&
                                            diamMaster.Attribute("gnm8_issue_no").Value != ""
                                            select new XElement[2] { diamMaster, rev };


            if (list2.Count() > 0)
            {
                foreach (XElement[] el in list2)
                {
                    string eci = el[0].Attribute("gnm8_issue_no").Value;
                    el[1].Attribute("gnm8_issue_no").SetValue(eci);
                }
            }

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Custom Forms
            Console.Write("Change to Custom Forms");
            Processing();


            var forms = from form in HelperUtility.xmlFile.Elements(ns + "Form")
                        where form.Attribute("object_type").Value == "UGPartAttr"
                        select form;

            foreach (var el in forms)
            {
                el.Attribute("object_type").Value = "GNM8_PartAttr";

                //add param code
                //el.SetAttributeValue("gnm8_parameter_code", "d");
            }

            forms = from form in HelperUtility.xmlFile.Elements(ns + "Form")
                    where form.Attribute("object_type").Value == "catia_doc_attributes"
                    select form;

            foreach (var el in forms)
            {
                el.Attribute("object_type").Value = "Gnm8_catia_doc_attr";

                //add param code
                //el.SetAttributeValue("gnm8_parameter_code", "d");
            }








            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion


            #region param code

            list = from rev in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                   join imanRel in HelperUtility.xmlFile.Elements(ns + "ImanRelation") on rev.Attribute("puid").Value equals imanRel.Attribute("primary_object").Value
                   join ds in HelperUtility.xmlFile.Elements(ns + "Dataset") on imanRel.Attribute("secondary_object").Value equals ds.Attribute("puid").Value
                   where ds.Attribute("object_type").Value.ToUpper() == "UGMASTER" || ds.Attribute("object_type").Value.ToUpper() == "CATPART" || ds.Attribute("object_type").Value.ToUpper() == "CATPRODUCT"
                   select rev;

            foreach (var el in list)
            {
                el.SetAttributeValue("gnm8_parameter_code", "c");
            }

            #endregion

            #region Remove Nodes
            Console.Write("Remove Nodes & Baselines and fix temp. 'R' revisions");
            Processing();




           

            list = from el in HelperUtility.xmlFile.Elements()
                   where el.Name.LocalName == "DIAMProductionMaster000" ||
                   el.Name.LocalName == "DIAMProductionRevMaster000" ||
                   el.Name.LocalName == "DIAMReferenceMaster000" ||
                   el.Name.LocalName == "DIAMReferenceRevMaster000" ||
                   el.Name.LocalName == "DIAMTemplateMaster000" ||
                   el.Name.LocalName == "DIAMTemplateRevMaster000"
                   select el;

            foreach (XElement el in list.ToArray())
            {
                el.Remove();
            }

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Relationship Swap
            Console.Write("Relationship Swap");
            Processing();
            util.IMANRelSwap();
            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Tool Type Change
            Console.Write("Tool Type Change");
            Processing();

            XElement WordPadNode = util.GetSingleElementByAttrID("Tool", "object_name", "WordPad.exe");
            if (WordPadNode != null)
            {
                string WordPadElemId = "#" + WordPadNode.Attribute("elemId").Value;
                string NotepadElemId = "#" + util.GetSingleElementByAttrID("Tool", "object_name", "Notepad").Attribute("elemId").Value;

                list =
                  from DatasetType in util.GetElementsBy("DatasetType").SearchList
                  where DatasetType.Attribute("list_of_tools").Value.Contains(WordPadElemId) || DatasetType.Attribute("list_of_tools_view").Value.Contains(WordPadElemId)
                  select DatasetType;

                foreach (XElement el in list)
                {
                    el.Attribute("list_of_tools").Value.Replace(WordPadElemId, NotepadElemId);
                    el.Attribute("list_of_tools_view").Value.Replace(WordPadElemId, NotepadElemId);
                }

                list =
                     from Dataset in util.GetElementsBy("Dataset").SearchList
                     where Dataset.Attribute("tool_used").Value.Contains(WordPadElemId)
                     select Dataset;

                foreach (XElement el in list)
                {
                    el.Attribute("tool_used").Value = NotepadElemId;
                }

                WordPadNode.Remove();
            }


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Other Post Changes

            list = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                   select el;

            foreach (XElement el in list)
            {
                string s = el.Attribute("gnm8_issue_no").Value;
                el.SetAttributeValue("gnm8_issue_no", s.ToUpper());
            }

            //util.GetElementsBy("GNM8_CADItemRevision").ToUpperValue("gnm8_issue_no");


            list = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                   select el;

            foreach (XElement el in list)
            {
                string s = el.Attribute("gnm8_issue_no").Value;
                el.SetAttributeValue("gnm8_issue_no", s.ToUpper());
            }

            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemRevision").SetAttribute("object_class", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItem").SetAttribute("object_class", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemRevisionMaster").SetAttribute("object_class", "GNM8_CADItemRevisionMaster");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemMaster").SetAttribute("object_class", "GNM8_CADItemMaster");

            util.GetElementsBy("POM_stub", "object_type", "GNM8_Reference").SetAttribute("object_class", "GNM8_Reference");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_ReferenceRevision").SetAttribute("object_class", "GNM8_ReferenceRevision");

            //remove dia3 properties
            list = from el in HelperUtility.xmlFile.Descendants()
                    where el.Attribute("dia3_NDI_ECI_number") != null
                    || el.Attribute("dia3_Split_Number") != null
                    || el.Attribute("dia3_partNumber") != null
                    select el;

            foreach (XElement el in list)
            {
                if (el.Attribute("dia3_NDI_ECI_number") != null)
                    el.Attribute("dia3_NDI_ECI_number").Remove();

                if (el.Attribute("dia3_Split_Number") != null)
                    el.Attribute("dia3_Split_Number").Remove();

                if (el.Attribute("dia3_partNumber") != null)
                    el.Attribute("dia3_partNumber").Remove();

            }

            //truncate values

            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("gnm8_issue_no", 12);
            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("gnm8_part_name", 40);
            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("object_desc", 240);

            //change car model

            Dictionary<string, string> CarModel = new Dictionary<string, string>();

            CarModel.Add("FIAT 500", "FIAT,500");
            CarModel.Add("= - (Common)", "Common");
            CarModel.Add("FIAT DSUV", "FIAT,DSUV");
            CarModel.Add("051A/071A", "051A,071A");
            CarModel.Add("JS27/41/49", "JS27,41/49");

            IEnumerable<XElement> carModelList = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                                                 where el.Attribute("gnm8_car_model") != null && CarModel.ContainsKey(el.Attribute("gnm8_car_model").Value)
                                                 select el;

            foreach (XElement el in carModelList)
            {
                string key = el.Attribute("gnm8_car_model").Value;
                string newValue = "";

                if (CarModel.TryGetValue(key, out newValue))
                    el.SetAttributeValue("gnm8_car_model", newValue);
            }


            #endregion


            #region Last BMIDE Version Object Name
            list = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItem")
                   select el;

            foreach (var el in list)
            {
                el.SetAttributeValue("object_name", el.Attribute("item_id").Value);
            }
            #endregion

            #region PartRenum

            Console.Write("Part Renumbering");

            Processing();


            util.PartReNum();
            WriteLineComplete("Complete");
            Console.WriteLine("");

            #endregion


            var imans = from iman in HelperUtility.xmlFile.Elements(ns + "ImanRelation")
                        select iman;

            foreach (var el in imans)
            {
                TotalIMAN.Add(el.Attribute("puid").Value);
            }

            //Find Broken IMAN rels
            var workingImans = from iman in HelperUtility.xmlFile.Elements(ns + "ImanRelation")
                               join primary in HelperUtility.xmlFile.Elements().Where(x => x.Attribute("puid") != null) on iman.Attribute("primary_object").Value equals primary.Attribute("puid").Value
                               join secondary in HelperUtility.xmlFile.Elements().Where(x => x.Attribute("puid") != null) on iman.Attribute("secondary_object").Value equals secondary.Attribute("puid").Value
                               select iman;

            var brokenImans = imans.Except(workingImans);

            foreach (var el in brokenImans)
            {
                BrokenIMANRel.Add(el.Attribute("puid").Value);
            }

            //Fill in Part Names
            var revFix = from rev in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                         join id in util.MasterRevisions on rev.Attribute("puid").Value equals id.PUID
                         where rev.Attribute("gnm8_dn_part_number") == null && id.OldItemID != ""
                         select new { Rev = rev, PartName = id.OldItemID };

            foreach (var el in revFix)
            {
                string s = el.PartName;
                if (el.PartName.Substring(0, 2).ToUpper() == "JP")
                    s = el.PartName.Remove(0, 2);
                el.Rev.SetAttributeValue("gnm8_dn_part_number", s.ToUpper());
            }

            #region Last BMIDE Version Remove dn_part_num
            list = from rev in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                   select rev;

            foreach (var el in list)
            {

                el.SetAttributeValue("object_name", el.Attribute("gnm8_dn_part_number").Value);

            }
            #endregion

            //BMIDE Change
            list = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                   where el.Attribute("gnm8_dn_part_number") != null
                   select el;

            foreach (var el in list)
            {
                el.Attribute("gnm8_dn_part_number").Remove();
            }

            IEnumerable<XElement> listx = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                                          where el.Attribute("gnm8_major_minor").Value.Contains(".") //&& el.Attribute("gnm8_major_minor").Value.Count() > 6
                                          select el;

            foreach (XElement el in listx)
            {
                string major_minor = el.Attribute("gnm8_major_minor").Value;
                int index = major_minor.IndexOf(".");
                string before = major_minor.Substring(0, index);
                string after = major_minor.Remove(0, index + 1);
                el.SetAttributeValue("object_name", el.Attribute("object_name").Value + "-" + "BSL-" + after);
                el.SetAttributeValue("gnm8_major_minor", before);
            }

            listx = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                    where Regex.IsMatch(el.Attribute("gnm8_major_minor").Value, @"(^\d)-(\d\d)R$")
                    select el;

            foreach (XElement el in listx)
            {
                GroupCollection group = Regex.Match(el.Attribute("gnm8_major_minor").Value, @"(^\d)-(\d\d)R$").Groups;
                el.SetAttributeValue("gnm8_major_minor", group[1].ToString() + "-" + group[2] + "-R");
            }

            listx = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
                    where Regex.IsMatch(el.Attribute("gnm8_major_minor").Value, @"(^\d)R$")
                    select el;

            foreach (XElement el in listx)
            {
                GroupCollection group = Regex.Match(el.Attribute("gnm8_major_minor").Value, @"(^\d)R$").Groups;
                el.SetAttributeValue("gnm8_major_minor", group[1].ToString() + "-R");
            }

            #region Post Process

            Console.Write("Post Process");

            Processing();

            util.RunPostProcess();

            WriteLineComplete("Complete");
            Console.WriteLine("");

            #endregion

            Console.WriteLine("");


            Console.WriteLine("-------------------------------------------------------------------");

            //util.SortNodeName();
        }

        private static void WriteLineComplete(String text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void Processing()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("...");
            Console.Write("Processing...");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ReportTime(string desc, TimeSpan ts)
        {
            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("\t " + desc + " - RunTime: " + elapsedTime);
        }

        private static void ShowTitle()
        {
            Console.WriteLine();
            StringBuilder sb = new StringBuilder();

            sb.Clear();
            #region title2

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;

            sb.AppendLine(@"                                                                               ");
            sb.AppendLine(@"      //////////        //////////// /////      ///     ///////     ////////   ");
            sb.AppendLine(@"     ////////////      //////////// //////    ////  //////////   ///      ///  ");
            sb.AppendLine(@"      ///     ////    ////         ///////  ////  ///////       ///       ///  ");
            sb.AppendLine(@"     ////     /////  /////////    /// ///// ///   /////////    ///       ///   ");
            sb.AppendLine(@"    ////    ////    /////////    ////  ///////     ////////   ///       ///    ");
            sb.AppendLine(@"   /////////////   ////         ////    /////  ////  //////  ///       ///     ");
            sb.AppendLine(@" ////////////     ///////////  ////     ////   //////////     /////////        ");
            sb.AppendLine(@"                                                                               ");
            #endregion

            //for (int i = 0; i < sb.Length; i++)
            //{
            //    Console.Write(sb[i]);
            //    //Thread.Sleep(1);
            //}

            Console.Write(sb);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("");

            Console.WriteLine("Mapping utility created by: Mike Germano");
            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine("");

            Thread.Sleep(500);
        }

        #region Validation Code
        public static void ValidateData(string file)
        {

            try
            {
                XElement xmlFile = XElement.Load(file);
            }
            catch
            {
                List<vData> badLines = new List<vData>();
                long lineNum = 1;

                using (StreamReader reader = new StreamReader(file))
                {
                    using (StreamWriter writer = new StreamWriter("FailedValidation.csv", true))
                    {
                        string line;
                        List<char> chars = new List<char>();
                        List<int> badChars = new List<int>();
                        while ((line = reader.ReadLine()) != null)
                        {
                            int charCount = -1;
                            foreach (Char c in line)
                            {
                                charCount++;

                                if (Char.IsControl(c))
                                {
                                    chars.Add(c);
                                    badChars.Add(charCount);
                                }

                            }
                            if (badChars.Count > 0)
                            {
                                vData data = new vData(line, chars.ToArray(), badChars.ToArray(), lineNum);
                                badLines.Add(data);
                                badChars.Clear();
                                chars.Clear();
                            }
                            lineNum++;
                        }

                        writer.Write(reportVErrors(badLines, file));
                    }
                }
            }
        }

        private static void Clean(string[] args)
        {

            if (getFileType(getValidateFile(args)) == 1)//Dir
            {
                String[] files = Directory.GetFiles(getValidateFile(args));

                if (files.Length == 0)
                {
                    Console.WriteLine("The 'Source XML Files' folder is empty.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("");
                    Console.WriteLine("***XML File Validator***");
                    Console.WriteLine("");
                    Console.WriteLine("Files Loaded...Cleaning XML file(s)");
                    foreach (string file in files)
                    {
                        Console.WriteLine("-------------------------------------------------------------------");
                        Console.WriteLine("Loading File " + file);
                        Console.WriteLine("");
                        Console.Write("Cleaning...");
                        CleanFile(file);
                        WriteLineComplete("Complete");
                        Console.WriteLine("");

                    }

                    Console.WriteLine("-------------------------------------------------------------------");
                    Console.WriteLine("");

                    TimeSpan duration = DateTime.Now - startTime;

                    WriteLineComplete("Cleaning complete. Duration - " + duration.Hours + ":" + duration.Minutes + ":" + duration.Seconds + "." + duration.Milliseconds);
                    Console.WriteLine("");
                }
            }
            else if (getFileType(getValidateFile(args)) == 2)//File
            {
                CleanFile(getValidateFile(args));
            }
            else
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Path not Found.");
                Console.ForegroundColor = oldColor;

                return;
            }


        }

        private static void CleanFile(string file)
        {
            StringBuilder sb = new StringBuilder();
            string newFile = Path.Combine(new string[] { Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_Clean" + Path.GetExtension(file) });

            using (TextWriter writer = File.CreateText(newFile))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        foreach (Char c in line)
                        {
                            if (!Char.IsControl(c))
                            {
                                writer.Write(c);
                            }
                        }
                        writer.Write(Environment.NewLine);
                    }
                }
            }

            File.Delete(file);


        }

        public class vData
        {
            public int[] badChars;
            public char[] chars;
            public string badLine;
            public long lineNum;

            public vData(string mBadLine, char[] mChars, int[] mBadChars, long mLineNum)
            {
                badLine = mBadLine;
                badChars = mBadChars;
                chars = mChars;
                lineNum = mLineNum;
            }

        }

        public static string reportVErrors(List<vData> data, string file)
        {
            StringBuilder sb = new StringBuilder();
            List<string> badstringList = new List<string>();

            foreach (vData item in data)
            {
                string goodString = getGoodString(item.badLine, item.badChars);

                for (int i = 0; i < item.badChars.Length; i++)
                {
                    string badString = getBadString(item.badLine, item.badChars[i]);
                    badstringList.Add(badString);
                }

                for (int i = 0; i < badstringList.Count; i++)
                {
                    XElement el = XElement.Parse(goodString);
                    sb.Append(el.Attribute("puid").Value);
                    sb.Append(",");
                    sb.Append(el.Name.LocalName);
                    sb.Append(",");
                    sb.Append("\"" + badstringList[i].Replace("\"", "'") + "\"");
                    sb.Append(",");
                    sb.Append(item.lineNum);
                    sb.Append(",");
                    sb.Append(item.badChars[i] + 1);
                    sb.Append(",");
                    sb.Append(Convert.ToByte(item.chars[i]));
                    sb.Append(",");
                    sb.Append(file);
                    sb.AppendLine();
                }
                badstringList.Clear();
            }

            return sb.ToString();
        }

        private static string getBadString(string line, int pos)
        {
            int sIndexPr = line.LastIndexOf("=", pos, pos);
            int sIndexPo = line.LastIndexOf(" ", sIndexPr, sIndexPr);
            int eIndex = line.IndexOf("\"", pos, line.Length - pos);

            string badString = line.Substring(sIndexPo + 1, eIndex - sIndexPo);

            return badString;
        }

        private static string getGoodString(string line, int[] badChars)
        {

            for (int i = 0; i < badChars.Length; i++)
            {
                line = line.Remove(badChars[i] - i, 1);
            }

            return line; ;
        }

        private static bool isValidate(string[] args)
        {
            foreach (string s in args)
            {
                if (s.ToUpper().Equals("-VALIDATE"))
                    return true;
            }

            return false;
        }

        //private static bool isPartRenum(string[] args)
        //{
        //    if (args.Length == 1 && args[0].ToUpper().Equals("-PARTRENUM"))
        //        return true;


        //    return false;
        //}

        private static byte getFileType(string file)
        {


            if (Directory.Exists(file))
            {
                return 1;
            }
            else if (File.Exists(file))
            {
                return 2;
            }

            return 0;


        }

        private static bool isRemoveBadChars(string[] args)
        {
            foreach (string s in args)
            {
                if (s.ToUpper().Equals("-CLEAN"))
                    return true;
            }

            return false;
        }

        private static string getValidateFile(string[] args)
        {
            foreach (string s in args)
            {
                if (!s.ToUpper().Equals("-VALIDATE") && !s.ToUpper().Equals("-CLEAN"))
                {
                    return s;
                }
            }

            return "";
        }

        protected static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        #endregion

    }


}
