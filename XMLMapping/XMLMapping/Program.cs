using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using XMLMapping;
using System.Linq;

namespace Project1
{
    class Class1
    {
        public static ushort RenumIndex = 1;
        public static DateTime startTime;
        public static int totalFiles = 0;
        public static int fileCount = 0;
        public static void Main(string[] args)
        {

            startTime = DateTime.Now;


            //validate
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
            else if (args.Length == 0 || (isPartRenum(args) && args.Length == 1))
            {
                Console.Title = "Denso Mapping Utility";
                Console.WriteLine("Starting Mapping....");

                //Get List of XML Files
                String[] files = Directory.GetFiles(@".\Source XML Files");

                if (files.Length == 0)
                {
                    Console.WriteLine("The 'Source XML Files' folder is empty.");
                }
                else
                {
                    foreach (string file in files)
                    {
                        HelperUtility util = new HelperUtility();
                        totalFiles = files.Count();


                        util.LoadFile(file);
                        string newFile = Path.Combine(@".\Target Mapped XML Files", Path.GetFileNameWithoutExtension(file) + "_Mapped.xml");

                        //Method for Translate Instructions
                        Translate(ref util, file, newFile, isPartRenum(args));


                        //Saves the XML file
                        util.SaveFile(newFile);

                    }
                }

                Console.WriteLine("");
                TimeSpan duration = DateTime.Now - startTime;

                WriteLineComplete("Mapping complete. Duration - " + duration.Hours + ":" + duration.Minutes + ":" + duration.Seconds + "." + duration.Milliseconds);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Invalid Arugments - Did not process");
            }
        }

        public static void Translate(ref HelperUtility util, String file, string mNewFile, bool PartRenumber)
        {


            util.SetFormatting(SaveOptions.None);

            Console.WriteLine("");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Loading File " + file);
            fileCount++;
            Console.WriteLine("File " + fileCount.ToString() + "/" + totalFiles.ToString());
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.White;


            #region Status Changes
            Console.Write("Step 1/10 : Status Change");
            Processing();

            //Production Status Changes
            util.GetElementsBy("ItemRevision", "object_type", "Production Revision")
                .Traverse("release_status_list", "ReleaseStatus", "puid")
                .Filter("name", "Released")
                .SetAttribute("name", "GNM8_ProductionReleased");

            util.GetElementsBy("ItemRevision", "object_type", "Production Revision")
                .Traverse("release_status_list", "ReleaseStatus", "puid")
                .Filter("name", "Engineering_Approved")
                .SetAttribute("name", "GNM8_ProductionReleased");

            util.GetElementsBy("ItemRevision", "object_type", "Production Revision")
                .Traverse("release_status_list", "ReleaseStatus", "puid")
                .Filter("name", "EAD_Approved")
                .SetAttribute("name", "GNM8_ProductionReleased");

            //Prototype Status Changes
            util.GetElementsBy("ItemRevision", "object_type", "Prototype Revision")
                .Traverse("release_status_list", "ReleaseStatus", "puid")
                .Filter("name", "Released")
                .SetAttribute("name", "GNM8_PrototypeReleased");

            //Baseline Status Change
            util.GetElementsBy("ReleaseStatus", "name", "Baseline").SetAttribute("name", "GNM8_Frozen");

            //All Other Status Changes
            util.AllOtherStatuses();


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Change Attribute Names
            Console.Write("Step 2/10 : Attribute Change");
            Processing();

            util.GetElementsBy("Item", "object_type", "Production").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "Production Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "Prototype").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "Prototype Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "PartialProcMatl").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "PartialProcMatl Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");

            util.GetElementsBy("Item", "object_type", "StandardPart").CopyAttribute("item_id", "gnm8_dn_part_number");
            util.GetElementsBy("ItemRevision", "object_type", "StandardPart Revision").CopyAttribute("item_revision_id", "gnm8_major_minor");


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region PartRenum
            if (PartRenumber)
            {

                Console.Write("Step 3/10 : Part Renumbering");

                Processing();
                util.setIndex(RenumIndex);
                util.PartReNum();
                RenumIndex = util.getIndex();
                WriteLineComplete("Complete");
                Console.WriteLine("");
            }
            else
            {
                Console.Write("Step 3/10 : Part Renumbering");

                Processing();
                //util.setIndex(RenumIndex);
                //util.PartReNum();
                //RenumIndex = util.getIndex();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Skipped");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
            }
            #endregion

            #region Uppercase
            Console.Write("Step 4/10 : Uppercase");
            Processing();
            util.GetElementsBy("Item").ToUpperValue("gnm8_dn_part_number");
            util.GetElementsBy("Form").ToUpperValue("object_name");
            util.GetElementsBy("ItemRevision").ToUpperValue("item_revision_id");
            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove Attributes
            Console.Write("Step 5/10 : Remove Attributes");
            Processing();

            util.GetElementsBy("Form", "object_type", "Production Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Production Revision Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Revision Master").RemoveAttribute("data_file");

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Change Object Types and Node Names
            Console.Write("Step 6/10 : Change Object Types and Node Names");
            Processing();

            #region PomStubs
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Production").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Production Revision").SetAttribute("object_type", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "PartialProcMatl").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "PartialProcMatl Revision").SetAttribute("object_type", "GNM8_CADItemRevision");

            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Prototype").SetAttribute("object_type", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Prototype Revision").SetAttribute("object_type", "GNM8_CADItemRevision");

            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Reference").SetAttribute("object_type", "GNM5_Reference");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Reference Revision").SetAttribute("object_type", "GNM5_ReferenceRevision");

            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");

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
            util.GetElementsBy("Item", "object_type", "Reference").SetAttribute("object_type", "GNM5_Reference");
            util.GetElementsBy("ItemRevision", "object_type", "Reference Revision").SetAttribute("object_type", "GNM5_ReferenceRevision");
            util.GetElementsBy("Form", "object_type", "Reference Master").SetAttribute("object_type", "GNM5_Reference Master");
            util.GetElementsBy("Form", "object_type", "Reference Revision Master").SetAttribute("object_type", "GNM5_ReferenceRevision Master");

            util.GetElementsBy("Form", "object_type", "Production Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("Form", "object_type", "Production Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");

            util.GetElementsBy("Form", "object_type", "Prototype Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("Form", "object_type", "Prototype Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");

            util.GetElementsBy("Form", "object_type", "StandardPart Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("Form", "object_type", "StandardPart Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");

            util.GetElementsBy("Form", "object_type", "PartialProcMatl Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            #endregion

            #region Node Names
            //Production
            util.GetElementsBy("Item", "object_type", "GNM8_CADItem").RenameNodes("GNM8_CADItem");
            util.GetElementsBy("ItemRevision", "object_type", "GNM8_CADItemRevision").RenameNodes("GNM8_CADItemRevision");

            //Reference
            util.GetElementsBy("Item", "object_type", "GNM5_Reference").RenameNodes("GNM5_Reference");
            util.GetElementsBy("ItemRevision", "object_type", "GNM5_ReferenceRevision").RenameNodes("GNM5_ReferenceRevision");

            #endregion

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Move Attribute
            Console.Write("Step 7/10 : Move Attributes");
            Processing();

            //change attributes on DIAMMaster and GNM8Rev
            util.GetElementsBy("GNM8_CADItemRevision").RenameAttribute("dia3_NDI_ECI_number", "gnm8_issue_no");
            util.GetElementsBy("GNM8_CADItemRevision").RenameAttribute("dia3_Split_Number", "gnm8_Issue_split_no");
            util.GetElementsBy("GNM8_CADItemRevision").RemoveAttribute("dia3_partNumber");

            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_NDI_ECI_number");
            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_Split_Number");
            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_partNumber");
           


            //change attributes on DIAMRefMaster
            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Customer", "gnm5_Customer");
            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Description", "gnm5_Description");
            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Lead_Program", "gnm5_Lead_Program");
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("ECI_Number", "gnm5_ECI_Number");
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("Description", "gnm5_Description");


            util.CopyAttributeByRel("gnm8_dn_part_number", "GNM8_CADItem", "GNM8_CADItemRevision", "puid", "parent_uid");
            util.GetElementsBy("GNM8_CADItem").RemoveAttribute("gnm8_dn_part_number");

            #region transfer to master form
            util.GetElementsBy("DIAMProductionMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMProductionMaster000","GNM8_CADItem","parent_uid","puid");

            util.GetElementsBy("DIAMProductionMaster000").RenameAttribute("Lead_Program", "gnm8_car_model");
            util.CopyAttributeByRel("gnm8_car_model", "DIAMProductionMaster000", "GNM8_CADItem", "parent_uid", "puid");

            //Cad Item Master REV

            util.GetElementsBy("DIAMProductionRevMaster000").RenameAttribute("ECI_Number", "gnm8_issue_no");

            util.GetElementsBy("DIAMProductionRevMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMProductionRevMaster000", "GNM8_CADItemRevision", "parent_uid", "parent_uid");

            #endregion

            

            //If only dia3_Split_Number IR Attribute is filled in, map to gnm8_issue_no
            //If both dia3_Split_Number & ECI_Number are filled in, map dia3_Split_Number to gnm8_issue_no
            IEnumerable<XElement> list =
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
            list =
            from rev in util.GetElementsBy("GNM8_CADItemRevision").SearchList
            join diamMaster in util.GetElementsBy("DIAMProductionRevMaster000").SearchList on (string)rev.Attribute("parent_uid") equals (string)diamMaster.Attribute("parent_uid").Value
            where
            rev.Attribute("gnm8_issue_no").Value == "" &&
            diamMaster.Attribute("gnm8_issue_no").Value != ""
            select diamMaster;

            if (list.Count() > 0)
            {
                foreach (XElement diam in list)
                {
                    util.GetSingleElementByAttrID("GNM8_CADItemRevision", "parent_uid", diam.Attribute("parent_uid").Value).SetAttributeValue("gnm8_issue_no", diam.Attribute("gnm8_issue_no").Value);
                }
            }

            //REF
            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Customer", "gnm5_Customer");
            util.CopyAttributeByRel("gnm5_Customer", "DIAMReferenceMaster000", "GNM5_Reference", "parent_uid", "puid");

            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Description", "gnm5_Description");
            util.CopyAttributeByRel("gnm5_Description", "DIAMReferenceMaster000", "GNM5_Reference", "parent_uid", "parent_uid");

            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Lead_Program", "gnm5_Lead_Program");
            util.CopyAttributeByRel("gnm8_car_model", "DIAMReferenceMaster000", "GNM5_Reference", "parent_uid", "parent_uid");

            //REF REV
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMReferenceRevMaster000", "GNM5_ReferenceRevision", "parent_uid", "parent_uid");

            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("ECI_Number", "gnm5_ECI_Number");
            util.CopyAttributeByRel("gnm5_ECI_Number", "DIAMReferenceRevMaster000", "GNM5_ReferenceRevision", "parent_uid", "parent_uid");

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove Nodes
            Console.Write("Step 8/10 : Remove Nodes");
            Processing();

            util.GetElementsBy("DIAMProductionMaster000").RemoveNodes();
            util.GetElementsBy("DIAMProductionRevMaster000").RemoveNodes();
            util.GetElementsBy("DIAMReferenceMaster000").RemoveNodes();
            util.GetElementsBy("DIAMReferenceRevMaster000").RemoveNodes();
            util.GetElementsBy("DIAMTemplateMaster000").RemoveNodes();
            util.GetElementsBy("DIAMTemplateRevMaster000").RemoveNodes();


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Relationship Swap
            Console.Write("Step 9/10 : Relationship Swap");
            Processing();
            util.IMANRelSwap();
            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Tool Type Change
            Console.Write("Step 10/10 : Tool Type Change");
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

                WordPadNode.Remove();
            }


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion


            Console.WriteLine("");
            if (PartRenumber)
                Console.WriteLine("Next Part Renumber Index: " + "NA" + RenumIndex.ToString("000000000"));

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

                        writer.Close();
                    }
                    reader.Close();
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
                    reader.Close();
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
                    sb.Append(file.Substring(19, file.Length - 19));
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

        private static bool isPartRenum(string[] args)
        {
            if (args.Length == 1 && args[0].ToUpper().Equals("-PARTRENUM"))
                return true;


            return false;
        }

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
