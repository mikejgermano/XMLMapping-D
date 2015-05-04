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
            XNamespace ns = HelperUtility.xmlFile.GetDefaultNamespace();
            //IEnumerable<XElement> releaseStatus;

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

            HashSet<string> assemListItemIDs = new HashSet<string>();



            var assemListRev = (from rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision")
                                join psOcc in HelperUtility.xmlFile.Elements(ns + "PSOccurrence") on rev.Attribute("puid").Value equals psOcc.Attribute("child_item").Value
                                join bvrRev in HelperUtility.xmlFile.Elements(ns + "PSBOMViewRevision") on psOcc.Attribute("parent_bvr").Value equals bvrRev.Attribute("puid").Value
                                join item in HelperUtility.xmlFile.Elements(ns + "Item") on bvrRev.Attribute("parent_uid").Value equals item.Attribute("puid").Value
                                where rev.Attribute("object_type").Value == "Reference Revision" && (item.Attribute("object_type").Value == "Production" || item.Attribute("object_type").Value == "Prototype" || item.Attribute("object_type").Value == "PartialProcMatl" || item.Attribute("object_type").Value == "StandardPart")
                                select rev).Distinct();


            /*assemListRev = (from rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision")
                               join psOcc in HelperUtility.xmlFile.Elements(ns + "PSOccurrence") on (string)rev.Attribute("puid") equals (string)psOcc.Attribute("child_item").Value
                               join bvrRev in HelperUtility.xmlFile.Elements(ns + "PSBOMViewRevision") on (string)psOcc.Attribute("parent_bvr") equals (string)bvrRev.Attribute("puid").Value
                               join item in HelperUtility.xmlFile.Elements(ns + "Item") on (string)bvrRev.Attribute("parent_uid") equals (string)item.Attribute("puid").Value
                               where rev.Attribute("object_type").Value == "Reference Revision" && rev.Attribute("parent_uid").Value != ""
                               select rev);*/

            foreach (XElement rev in assemListRev)
            {
                rev.SetAttributeValue("object_type", "Production Revision");
                assemListItemIDs.Add(rev.Attribute("items_tag").Value);
            }

            assemListItemIDs.Distinct();


            var assemListItem = from item in HelperUtility.xmlFile.Elements(ns + "Item")
                                where assemListItemIDs.Contains(item.Attribute("puid").Value)
                                select item;


            foreach (XElement item in assemListItem)
            {
                item.SetAttributeValue("object_type", "Production");
            }

            string[] assemListIDs = (from el in assemListItem
                                     select el.Attribute("puid").Value).ToArray();


            WriteLineComplete("Complete");
            Console.WriteLine("");
            #endregion


            #region Status Changes
            Console.Write("Status Change");
            Processing();

            var relItemListT = from rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision")
                               from revSts in rev.Attribute("release_status_list").Value.Split(',')
                               join owningGroup in HelperUtility.xmlFile.Elements(ns + "Group") on rev.Attribute("owning_group").Value.Remove(0, 1) equals owningGroup.Attribute("elemId").Value
                               select new ReleaseItem(rev.Attribute("puid").Value, revSts.ToString(), "", owningGroup.Attribute("full_name").Value);

            var relItemList = from revSts in relItemListT
                              join status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus") on revSts.ReleaseID equals (string)status.Attribute("puid")
                              select new ReleaseItem(revSts.RevID, revSts.ReleaseID, status.Attribute("name").Value, revSts.OwningGroup);

            relItemListT = null;

            #region Top Level Change


            //Has EAD
            var releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                                join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                                join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                                where rev.Attribute("object_type").Value == "Production Revision" && relItem.Name == "EAD_Approved"
                                select new Object[3] { status, rev, relItem };



            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_ProductionReleased");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_ProductionReleased";
            }


            //Has Released for Production/Partial/Standard
            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            where (rev.Attribute("object_type").Value == "Production Revision" || rev.Attribute("object_type").Value == "PartialProcMatl Revision" || rev.Attribute("object_type").Value == "StandardPart Revision")
                            && relItem.Name == "Released"
                            select new Object[3] { status, rev, relItem };

            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_ProductionReleased");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_ProductionReleased";
            }


            //Prototype

            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            where rev.Attribute("object_type").Value == "Prototype Revision" && relItem.Name == "Released"
                            select new Object[3] { status, rev, relItem };



            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_PrototypeReleased");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_PrototypeReleased";
            }

            //Reference and baseline only
            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            where rev.Attribute("object_type").Value == "Reference Revision" || (!rev.Attribute("release_status_list").Value.Contains(",") && relItem.Name == "Baseline")
                            select new Object[3] { status, rev, relItem };



            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_Frozen");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_Frozen";
            }


            #endregion


            #region Status Exceptions


            //Production or Prototype owned by PG1, where prefix does not equal AA,MX - change to GNM8_Frozen
            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            join item in HelperUtility.xmlFile.Elements(ns + "Item") on (string)rev.Attribute("items_tag") equals (string)item.Attribute("puid")
                            where (rev.Attribute("object_type").Value == "Production Revision" || rev.Attribute("object_type").Value == "Prototype Revision") && (relItem.OwningGroup.Contains("PG1") || relItem.OwningGroup.Contains("PG3"))
                            && (item.Attribute("item_id").Value.Substring(0, 2).ToUpper() != "AA" && item.Attribute("item_id").Value.Substring(0, 2).ToUpper() != "MX" && item.Attribute("item_id").Value.Substring(0, 2).ToUpper() != "TN" && item.Attribute("item_id").Value.Substring(0, 2).ToUpper() != "TD")
                            select new Object[3] { status, rev, relItem };


            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_Frozen");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_Frozen";
            }


            //Anything owned by PG2 change to Frozen
            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            join item in HelperUtility.xmlFile.Elements(ns + "Item") on (string)rev.Attribute("items_tag") equals (string)item.Attribute("puid")
                            where relItem.OwningGroup.Contains("PG2")
                            select new Object[3] { status, rev, relItem };


            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_Frozen");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_Frozen";
            }

            //Everything else Frozen

            releaseStatus = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                            join relItem in relItemList on status.Attribute("puid").Value equals relItem.ReleaseID
                            join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on relItem.RevID equals rev.Attribute("puid").Value
                            where !status.Attribute("name").Value.StartsWith("GNM8_")
                            select new Object[3] { status, rev, relItem };



            foreach (var el in releaseStatus)
            {
                XElement status = (XElement)el[0];
                status.SetAttributeValue("name", "GNM8_Frozen");

                ((XElement)el[1]).SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                ((ReleaseItem)el[2]).Name = "GNM8_Frozen";
            }

            #endregion


            #region change Datasets

            var relDatasetListT = from dataset in HelperUtility.xmlFile.Elements(ns + "Dataset")
                                  from datasetSts in dataset.Attribute("release_status_list").Value.Split(',')
                                  select new Object[2] { dataset.Attribute("puid").Value, datasetSts.ToString() };

            var relDatasetList = from datasetSts in relDatasetListT
                                 join dataset in HelperUtility.xmlFile.Elements(ns + "Dataset") on datasetSts[0] equals dataset.Attribute("puid").Value
                                 join status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus") on datasetSts[1] equals status.Attribute("puid").Value
                                 select new XElement[2] { dataset, status };


            relDatasetList.Distinct();

            var relRevDatasetList = from relDataset in relDatasetList
                                    join imanRel in HelperUtility.xmlFile.Elements(ns + "ImanRelation") on relDataset[0].Attribute("puid").Value equals imanRel.Attribute("secondary_object").Value
                                    join rev in HelperUtility.xmlFile.Elements(ns + "ItemRevision") on imanRel.Attribute("primary_object").Value equals rev.Attribute("puid").Value
                                    join revSts in relItemList on rev.Attribute("puid").Value equals revSts.RevID
                                    select new Object[2] { revSts, relDataset };

            foreach (var el in relRevDatasetList)
            {
                string revStatus = ((ReleaseItem)el[0]).Name;

                XElement dataset = ((XElement[])el[1])[0];
                XElement status = ((XElement[])el[1])[1];

                dataset.SetAttributeValue("release_status_list", status.Attribute("puid").Value);

                status.Attribute("name").Value = revStatus;
            }


            #endregion


            #region everthing else

            IEnumerable<XElement> list = from el in HelperUtility.xmlFile.Elements()
                                         where (el.Name.LocalName == "Form" && el.Attribute("release_status_list").Value != "") || (el.Attribute("release_status_list") != null && el.Attribute("release_status_list").Value.Contains(','))
                                         select el;


            foreach (var el in list)
            {
                el.SetAttributeValue("release_status_list", "");
            }

            //change rest of statuses to frozen
            list = from status in HelperUtility.xmlFile.Elements(ns + "ReleaseStatus")
                   where !status.Attribute("name").Value.StartsWith("GNM8_")
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
            Console.Write("Remove JP & Extra IMAN Rel");
            Processing();
      
            //IMAN Rel remove duplicates
            var ImanRels = from rel in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "ImanRelation")
                           group rel by new
                           {
                               primary = rel.Attribute("primary_object").Value,
                               secondary = rel.Attribute("secondary_object").Value
                           } into rels
                           where rels.Count() > 1
                           select new Object[2]{new Grouping(rels.Key.primary,rels.Key.secondary, rels),rels.Count()};

            int count = 1;
            foreach (var el in ImanRels)
            {
                if (count == 1 && count < (int)el[1])
                {
                    count++;
                    continue;
                }
                else if (count > 1 && count < (int)el[1])
                {
                    Grouping g = (Grouping)el[0];
                    g.els.ElementAt<XElement>(count - 1).Remove();
                    count++;
                }
                else if (count > 1 && count == (int)el[1])
                {
                    Grouping g = (Grouping)el[0];
                    g.els.ElementAt<XElement>(count - 1).Remove();
                   
                    count = 1;
                }
                
            }


            IEnumerable<XElement>  listSd = from item in HelperUtility.xmlFile.Elements(HelperUtility.xmlFile.GetDefaultNamespace() + "Item")
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
            util.GetElementsBy("Item", "object_type", "Reference").SetAttribute("object_type", "GNM8_Reference");
            util.GetElementsBy("ItemRevision", "object_type", "Reference Revision").SetAttribute("object_type", "GNM8_ReferenceRevision");
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
                    case "UGMASTER":
                    case "CATProduct":
                    case "CATPart":
                        rev.SetAttributeValue("gnm8_parameter_code", "c");
                        break;
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

            IEnumerable<XElement> listx = from el in HelperUtility.xmlFile.Elements(ns + "GNM8_CADItemRevision")
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

            list = from el in HelperUtility.xmlFile.Elements()
                   where el.Name.LocalName == "DIAMProductionMaster000" ||
                   el.Name.LocalName == "DIAMProductionRevMaster000" ||
                   el.Name.LocalName == "DIAMReferenceMaster000" ||
                   el.Name.LocalName == "DIAMReferenceRevMaster000" ||
                   el.Name.LocalName == "DIAMTemplateMaster000" ||
                   el.Name.LocalName == "DIAMTemplateRevMaster000"
                   select el;

            foreach (XElement el in list)
            {
                el.Remove();
            }

            /*util.GetElementsBy("DIAMProductionMaster000").RemoveNodes();
            util.GetElementsBy("DIAMProductionRevMaster000").RemoveNodes();
            util.GetElementsBy("DIAMReferenceMaster000").RemoveNodes();
            util.GetElementsBy("DIAMReferenceRevMaster000").RemoveNodes();
            util.GetElementsBy("DIAMTemplateMaster000").RemoveNodes();
            util.GetElementsBy("DIAMTemplateRevMaster000").RemoveNodes();*/


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
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItemRevision Master").SetAttribute("object_class", "GNM8_CADItemRevision Master");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_CADItem Master").SetAttribute("object_class", "GNM8_CADItem Master");

            util.GetElementsBy("POM_stub", "object_type", "GNM8_Reference").SetAttribute("object_class", "GNM8_Reference");
            util.GetElementsBy("POM_stub", "object_type", "GNM8_ReferenceRevision").SetAttribute("object_class", "GNM8_ReferenceRevision");

            //remove dia3 properties
            listx = from el in HelperUtility.xmlFile.Descendants()
                    where el.Attribute("dia3_NDI_ECI_number") != null
                    || el.Attribute("dia3_Split_Number") != null
                    || el.Attribute("dia3_partNumber") != null
                    select el;

            foreach (XElement el in listx)
            {
                if (el.Attribute("dia3_NDI_ECI_number") != null)
                    el.Attribute("dia3_NDI_ECI_number").Remove();

                if (el.Attribute("dia3_Split_Number") != null)
                    el.Attribute("dia3_Split_Number").Remove();

                if (el.Attribute("dia3_partNumber") != null)
                    el.Attribute("dia3_partNumber").Remove();

            }

            //trunate values

            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("gnm8_issue_no",12);
            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("gnm8_part_name", 40);
            util.GetElementsBy("GNM8_CADItemRevision").TrimAttributeLength("gnm8_car_model", 6);

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
