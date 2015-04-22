using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using XMLMapping;
using System.Linq;
using System.Text.RegularExpressions;

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
                        Translate(ref util, file, newFile, false);


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
            XNamespace df = HelperUtility.xmlFile.GetDefaultNamespace();
            IEnumerable<XElement> releaseStatus;

            util.SetFormatting(SaveOptions.None);

            Console.WriteLine("");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Loading File " + file);
            fileCount++;
            Console.WriteLine("File " + fileCount.ToString() + "/" + totalFiles.ToString());
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.White;

            #region Reference to GNM8
            Console.Write("Apply Logic for Reference to GNM8");
            Processing();

            var assemList = (from rev in util.GetElementsBy("ItemRevision", "object_type", "Reference Revision").SearchList
                             join psOcc in util.GetElementsBy("PSOccurrence").SearchList on (string)rev.Attribute("puid") equals (string)psOcc.Attribute("child_item").Value
                             join bvrRev in util.GetElementsBy("PSBOMViewRevision").SearchList on (string)psOcc.Attribute("parent_bvr") equals (string)bvrRev.Attribute("puid").Value
                             join item in util.GetElementsBy("Item").SearchList on (string)bvrRev.Attribute("parent_uid") equals (string)item.Attribute("puid").Value
                             select rev);

            foreach (XElement rev in assemList)
            {
                rev.SetAttributeValue("object_type", "Production Revision");
            }


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Status Changes
            Console.Write("Status Change");
            Processing();

            
            //Clear release status for Forms
            IEnumerable<XElement> Forms = from item in HelperUtility.xmlFile.Elements(df + "Form")
                                          select item;

            releaseStatus = from status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus")
                            select status;


            foreach (XElement form in Forms)
            {
                
                var query = from status in releaseStatus
                            where form.Attribute("release_status_list").Value.Contains(status.Attribute("puid)").Value)
                            select status;
            }


           

           

            foreach (XElement status in releaseStatus)
            {
                string s;
            }

            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "Item")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.Remove();
            }

            //Production Status Changes
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }

            /*
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                   (string)status.Attribute("name").Value == "Engineering_Approved"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }
             */

            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                   (string)status.Attribute("name").Value == "EAD_Approved"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }


            //StandardPart

            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "StandardPart Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }

            //Partial
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "PartialProcMatl Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }

            //Prototype Status Changes
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Prototype Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_PrototypeReleased");
            }

            //Reference Status Changes
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where rev.Attribute("object_type").Value == "Reference Revision" &&
                                   (string)status.Attribute("name").Value == "Released"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }

            //Baseline Status Changes
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            where (string)status.Attribute("name").Value == "Baseline"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }

            #region Status Exceptions


            /*Production owned by PG3, with a release status before November 2013 - change to GNM8_Frozen
            DateTime exDate = new DateTime(2013, 11, 1);
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(1, 1) equals (string)user.Attribute("elemId")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                user.Attribute("user_id").Value.ToUpper().Contains("PG3BCS") &&
                                (rev.Attribute("date_released").Value != "" && HelperUtility.isDateBefore(rev.Attribute("date_released").Value, exDate))
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }*/

            //Production or Prototype owned by PG1, where prefix does not equal "AA" or "MX" - change to GNM8_Frozen
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join item in HelperUtility.xmlFile.Elements(df + "Item") on (string)rev.Attribute("parent_uid") equals (string)item.Attribute("puid")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where (rev.Attribute("object_type").Value == "Production Revision" || rev.Attribute("object_type").Value == "Prototype Revision") &&
                                user.Attribute("user_id").Value.ToUpper() == "PG1" &&
                                (item.Attribute("item_id").Value.ToUpper().Substring(0, 2) != "AA" && item.Attribute("item_id").Value.ToUpper().Substring(0, 2) != "MX")
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }

            //Production owned by PG1, where prefix starts with "aw063600-" - change to GNM8_ProductionReleased
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join item in HelperUtility.xmlFile.Elements(df + "Item") on (string)rev.Attribute("parent_uid") equals (string)item.Attribute("puid")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where rev.Attribute("object_type").Value == "Production Revision" &&
                                user.Attribute("user_id").Value.ToUpper() == "PG1" &&
                                item.Attribute("item_id").Value.Contains("aw063600-")
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }


            //Production or Prototype owned by PG3BCS(1-4), where prefix does not equal "TN" or "MX" or "TD" - change to GNM8_Frozen
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join item in HelperUtility.xmlFile.Elements(df + "Item") on (string)rev.Attribute("parent_uid") equals (string)item.Attribute("puid")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where (rev.Attribute("object_type").Value == "Production Revision" || rev.Attribute("object_type").Value == "Prototype Revision") &&
                                user.Attribute("user_id").Value.ToUpper().Contains("PG3BCS") &&
                                (item.Attribute("item_id").Value.ToUpper().Substring(0, 2) != "TN"
                                && item.Attribute("item_id").Value.ToUpper().Substring(0, 2) != "MX"
                                && item.Attribute("item_id").Value.ToUpper().Substring(0, 2) != "TD")
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }

            //Production or Prototype owned by PG3BCS(1-4), where prefix equalss "TN" or "MX" or "TD" - change to GNM8_ProductionReleased
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join item in HelperUtility.xmlFile.Elements(df + "Item") on (string)rev.Attribute("parent_uid") equals (string)item.Attribute("puid")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where (rev.Attribute("object_type").Value == "Production Revision" || rev.Attribute("object_type").Value == "Prototype Revision") &&
                                user.Attribute("user_id").Value.ToUpper().Contains("PG3BCS") &&
                                (item.Attribute("item_id").Value.ToUpper().Substring(0, 2) == "TN"
                                && item.Attribute("item_id").Value.ToUpper().Substring(0, 2) == "MX"
                                && item.Attribute("item_id").Value.ToUpper().Substring(0, 2) == "TD")
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_ProductionReleased");
            }




            //Prototype owned by PG1, where prefix starts with "aw063600-" - change to GNM8_PrototypeReleased
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join item in HelperUtility.xmlFile.Elements(df + "Item") on (string)rev.Attribute("parent_uid") equals (string)item.Attribute("puid")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where rev.Attribute("object_type").Value == "Prototype Revision" &&
                                user.Attribute("user_id").Value.ToUpper() == "PG1" &&
                                item.Attribute("item_id").Value.Contains("aw063600-")
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_PrototypeReleased");
            }

            //any item type owned by PG2, with any status - change to GNM8_Frozen
            releaseStatus = from rev in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                            join status in HelperUtility.xmlFile.Elements(df + "ReleaseStatus") on (string)rev.Attribute("release_status_list") equals (string)status.Attribute("puid")
                            join user in HelperUtility.xmlFile.Elements(df + "User") on (string)rev.Attribute("owning_user").Value.Remove(0, 1) equals (string)user.Attribute("elemId")
                            where user.Attribute("user_id").Value.ToUpper() == "PG2"
                            select status;

            foreach (XElement status in releaseStatus)
            {
                status.SetAttributeValue("name", "GNM8_Frozen");
            }

            #endregion


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove JP
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

            listSd = from el in HelperUtility.xmlFile.Elements(df + "ItemRevision")
                     where el.Attribute("object_type").Value != "Reference Revision"
                     select el;

            foreach (XElement el in listSd)
            {
                el.SetAttributeValue("gnm8_part_name", el.Attribute("object_name").Value.ToUpper());
            }

            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region PartRenum
            if (PartRenumber)
            {

                Console.Write("Part Renumbering");

                Processing();
                util.setIndex(RenumIndex);
                util.PartReNum();
                RenumIndex = util.getIndex();
                WriteLineComplete("Complete");
                Console.WriteLine("");
            }
            else
            {
                Console.Write("Part Renumbering");

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

            util.GetElementsBy("Form", "object_type", "Production Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Production Revision Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "PartialProcMatl Revision Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Prototype Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Prototype Revision Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Reference Master").RemoveAttribute("data_file");
            util.GetElementsBy("Form", "object_type", "Reference Revision Master").RemoveAttribute("data_file");

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


            util.GetElementsBy("POM_stub", "object_class", "Item").Filter("object_type", "Reference").SetAttribute("object_type", "GNM5_Reference");
            util.GetElementsBy("POM_stub", "object_class", "ItemRevision").Filter("object_type", "Reference Revision").SetAttribute("object_type", "GNM5_ReferenceRevision");

            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Production Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "PartialProcMatl Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "Prototype Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "StandardPart Master").SetAttribute("object_type", "GNM8_CADItem Master");
            util.GetElementsBy("POM_stub", "object_class", "Form").Filter("object_type", "StandardPart Revision Master").SetAttribute("object_type", "GNM8_CADItemRevision Master");

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
            //util.GetElementsBy("Form", "object_type", "Reference Master").SetAttribute("object_type", "GNM5_ReferenceMasterS");
            //util.GetElementsBy("Form", "object_type", "Reference Revision Master").SetAttribute("object_type", "GNM5_ReferenceRevision Master");

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

            util.GetElementsBy("DIAMReferenceMaster000").RenameNodes("GNM5_ReferenceMasterS");
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameNodes("GNM5_ReferenceRevMasterS");

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

            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_NDI_ECI_number");
            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_Split_Number");
            util.GetElementsBy("GNM5_ReferenceRevision").RemoveAttribute("dia3_partNumber");



            //change attributes on DIAMRefMaster
            util.GetElementsBy("GNM5_ReferenceMasterS").RenameAttribute("Customer", "gnm5_Customer");
            util.GetElementsBy("GNM5_ReferenceMasterS").RenameAttribute("Description", "gnm5_Description");
            util.GetElementsBy("GNM5_ReferenceMasterS").RenameAttribute("Lead_Program", "gnm5_Lead_Program");
            util.GetElementsBy("GNM5_ReferenceRevMasterS").RenameAttribute("ECI_Number", "gnm5_ECI_Number");
            util.GetElementsBy("GNM5_ReferenceRevMasterS").RenameAttribute("Description", "gnm5_Description");


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

            /*REF
            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Customer", "gnm5_Customer");
            util.CopyAttributeByRel("gnm5_Customer", "DIAMReferenceMaster000", "", "", "Form", "object_type", "GNM5_ReferenceMasterS", "parent_uid", "parent_uid");

            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Description", "gnm5_Description");
            util.CopyAttributeByRel("gnm5_Description", "DIAMReferenceMaster000", "", "", "Form", "object_type", "GNM5_ReferenceMasterS", "parent_uid", "parent_uid");

            util.GetElementsBy("DIAMReferenceMaster000").RenameAttribute("Lead_Program", "gnm5_Lead_Program");
            util.CopyAttributeByRel("gnm5_Lead_Program", "DIAMReferenceMaster000", "", "", "Form", "object_type", "GNM5_Reference Master", "parent_uid", "parent_uid");

            //REF REV
            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("Description", "object_desc");
            util.CopyAttributeByRel("object_desc", "DIAMReferenceRevMaster000", "", "", "Form", "object_type", "GNM5_ReferenceRevision Master", "parent_uid", "parent_uid");

            util.GetElementsBy("DIAMReferenceRevMaster000").RenameAttribute("ECI_Number", "gnm5_ECI_Number");
            util.CopyAttributeByRel("gnm5_ECI_Number", "DIAMReferenceRevMaster000", "", "", "Form", "object_type", "GNM5_ReferenceRevision Master", "parent_uid", "parent_uid");
            */
            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region ParameterCode
            Console.Write("Dataset - add Parameter Code");
            Processing();

            var paramList = (from rev in util.GetElementsBy("GNM8_CADItemRevision").SearchList
                             join Dataset in util.GetElementsBy("Dataset").SearchList on (string)rev.Attribute("parent_uid") equals (string)Dataset.Attribute("parent_uid").Value
                             select new { rev, Dataset }).ToList();

            for (int i = 0; i < paramList.Count(); i++)
            {
                XElement rev = paramList[i].rev;
                XElement dataset = paramList[i].Dataset;

                string type = (string)dataset.Attribute("object_type").Value;


                switch (type)
                {
                    /*case "UGMASTER":
                    case "CATProduct":
                    case "CATPart":
                        rev.SetAttributeValue("gnm8_parameter_code", "c");
                        break;*/
                    case "UGPART":
                    case "CATDrawing":
                        rev.SetAttributeValue("gnm8_parameter_code", "d");
                        break;
                    /*case "UGALTREP":
                    case "CATShape":
                        rev.SetAttributeValue("gnm8_parameter_code", "s");
                        break;*/
                }
            }


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion

            #region Remove Nodes
            Console.Write("Remove Nodes & Baselines > 6 and fix temp. 'R' revisions");
            Processing();

            IEnumerable<XElement> listx = from el in util.GetElementsBy("GNM8_CADItemRevision").SearchList
                                          where el.Attribute("gnm8_major_minor").Value.Contains(".") && el.Attribute("gnm8_major_minor").Value.Count() > 6
                                          select el;

            foreach (XElement el in listx)
            {
                string major_minor = el.Attribute("gnm8_major_minor").Value;
                int index = major_minor.IndexOf(".");
                string before = major_minor.Substring(0, index);
                string after = major_minor.Remove(0, index + 1);
                el.SetAttributeValue("gnm8_part_name", "BASELINE-" + after + "-" + el.Attribute("gnm8_part_name").Value);
                el.SetAttributeValue("gnm8_major_minor", before);
            }

            listx = from el in util.GetElementsBy("GNM8_CADItemRevision").SearchList
                    where Regex.IsMatch(el.Attribute("gnm8_major_minor").Value, @"(^\d)-(\d\d)R$")
                    select el;

            foreach (XElement el in listx)
            {
                GroupCollection group = Regex.Match(el.Attribute("gnm8_major_minor").Value, @"(^\d)-(\d\d)R$").Groups;
                el.SetAttributeValue("gnm8_major_minor", group[1].ToString() + "-" + group[2] + "-R");
            }

            listx = from el in util.GetElementsBy("GNM8_CADItemRevision").SearchList
                    where Regex.IsMatch(el.Attribute("gnm8_major_minor").Value, @"(^\d)R$")
                    select el;

            foreach (XElement el in listx)
            {
                GroupCollection group = Regex.Match(el.Attribute("gnm8_major_minor").Value, @"(^\d)R$").Groups;
                el.SetAttributeValue("gnm8_major_minor", group[1].ToString() + "-R");
            }

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
            util.GetElementsBy("GNM8_CADItemRevision").ToUpperValue("gnm8_issue_no");

            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemRevision").SetAttribute("object_class", "GNM8_CADItemRevision");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItem").SetAttribute("object_class", "GNM8_CADItem");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemRevision Master").SetAttribute("object_class", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItem Master").SetAttribute("object_class", "GNM8_CADItem Master");

            util.GetElementsBy("POM_stub", "object_type", "GNM5_Reference").SetAttribute("object_class", "GNM5_Reference");
            util.GetElementsBy("POM_stub", "object_type", "GNM5_ReferenceRevision").SetAttribute("object_class", "GNM5_ReferenceRevision");
          

            
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
