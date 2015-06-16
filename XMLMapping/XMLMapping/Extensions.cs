using ExtensionMethods;
using XMLStorageTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XMLMapping
{
    public class Grouping
    {
        public string primary;
        public string secondary;
        public IEnumerable<XElement> els;
        public int count;

        public Grouping(string mprimary, string msecondary, IEnumerable<XElement> o, int mcount)
        {
            primary = mprimary;
            secondary = msecondary;
            els = o;
            count = mcount;
        }
    }

    public class ReleaseItem
    {
        public string RevID;
        public string ReleaseID;
        public string Name;
        public string OwningGroup;

        public ReleaseItem(string mRevID, string mReleaseID, string mName, string mOwningGroup)
        {
            RevID = mRevID;
            ReleaseID = mReleaseID;
            Name = mName;
            OwningGroup = mOwningGroup;
        }

    }

    public class SearchElements
    {
        public List<XElement> SearchList;
        private String Method;
        private String[] Params;

        public String getMethod()
        {
            return Method;
        }

        public String[] getParams()
        {
            return Params;
        }

        public SearchElements(List<XElement> mSearchList, String mMethod, String[] mParams)
        {
            SearchList = mSearchList;
            Method = mMethod;
            Params = mParams;
        }
    }

    public class SearchElement
    {
        public XElement searchElement;

        public SearchElement(XElement element)
        {
            searchElement = element;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }
    }

    public class HelperUtility
    {

        #region Upper Class Stuff
        public static XElement xmlFile = null;
        public SaveOptions Format;
        public IEnumerable<Classes.Item> MasterItems;
        public IEnumerable<Classes.Revision> MasterRevisions;
        public IEnumerable<string> RefCadItems;
        public IEnumerable<string> RefCadRevs;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        public void SortNodeName()
        {
            xmlFile.ReplaceNodes(xmlFile.Elements().OrderBy(e => e.Name.ToString()));
        }

        public int NodeCount()
        {
            return xmlFile.Nodes().Count();
        }

        public void LoadFile(String filePath)
        {

            xmlFile = XElement.Load(filePath);
        }

        public HelperUtility() { }

        public HelperUtility(String filePath)
        {
            xmlFile = XElement.Load(filePath);
        }

        public static XElement GetXML()
        {
            return xmlFile;
        }

        public void SaveFile(String path)
        {
            xmlFile.Save(path, Format);
        }

        /// <summary>
        /// Changes format of file.
        /// </summary>
        /// <param name="sopts">The SaveOption</param>
        /// <example>SetFormatting(Saveoptions.None) - Indents XML file</example>
        /// <example>SetFormatting(SaveOptions.DisableFormatting) - Compresses XML file by removing excess whitespace</example>
        public void SetFormatting(SaveOptions sopts)
        {
            Format = sopts;
        }
        #endregion

        /// <summary>
        /// Copies attribute from Node Name A to B with corresponding value
        /// </summary>
        /// <param name="SourceElementName"></param>
        /// <param name="DestinationElementName"></param>
        /// <param name="AttributeName"></param>
        /// <param name="TraverseAttrSource"></param>
        /// <param name="TraverseAttrDestination"></param>
        public void CopyAttributeByRel(String AttributeName, String SourceElementName, String DestinationElementName, String TraverseAttrSource, String TraverseAttrDestination)
        {
            XNamespace ns = xmlFile.GetDefaultNamespace();

            IEnumerable<XElement[]> result = from source in xmlFile.Elements(ns + SourceElementName)
                                             join dest in xmlFile.Elements(ns + DestinationElementName) on source.Attribute(TraverseAttrSource).Value equals dest.Attribute(TraverseAttrDestination).Value
                                             select new XElement[2] { source, dest };



            if (result.Count() == 0)
            {

                return;
            }

            foreach (XElement[] el in result)
            {


                XAttribute att = el[0].Attribute(AttributeName);
                if (att == null)
                {
                    continue;
                }

                el[1].SetAttributeValue(AttributeName, att.Value);

            }

        }

        /// <summary>
        /// Retrieves set of nodes that match the criteria
        /// </summary>
        /// <param name="ElementName">Specifies Node Name to use in query</param>
        /// <param name="AttributeName">Of the Node that is specified, searches it Attributes by AttributeName</param>
        /// <param name="Value">Compares the Attribute Value with the value used</param>
        /// <example><![CDATA[<Item object_type="Production"]]></example>
        public SearchElements GetElementsBy(String ElementName, String AttributeName, String Value)
        {
            String[] Params = { ElementName, AttributeName, Value };


            IEnumerable<XElement> list1 =
                  from el in xmlFile.Descendants()
                  where el.Name.LocalName.Equals(ElementName) &&
                    el.Attribute(AttributeName).Value == Value
                  select el;

            return new SearchElements(list1.ToList<XElement>(), GetCurrentMethod(), Params);

        }

        /// <summary>
        /// Retrieves set of nodes that match the criteria
        /// </summary>
        /// <param name="ObjectName">>Specifies Node Name to use in query</param>
        public SearchElements GetElementsBy(String ElementName)
        {
            String[] Params = { ElementName };
            IEnumerable<XElement> list1 =
                  from el in xmlFile.Elements(xmlFile.GetDefaultNamespace() + ElementName)
                  select el;

            return new SearchElements(list1.ToList<XElement>(), GetCurrentMethod(), Params);

        }

        public SearchElement GetSingleElementByID(String UID)
        {
            XElement xEl = xmlFile.Descendants().Single(x => x.Attribute("elemId").Value == UID);


            return new SearchElement(xEl);

        }

        public XElement GetSingleElementByAttrID(String ElementName, String AttributeName, String Value)
        {
            IEnumerable<XElement> list1 = null;

            list1 =
               from el in xmlFile.Descendants()
               where el.Name.LocalName.Equals(ElementName) &&
                 el.Attribute(AttributeName) != null &&
                 el.Attribute(AttributeName).Value == Value
               select el;

            if (list1.Count() == 0)
                return null;
            else
                return list1.First();

        }

        public string SubstringBefore(string mString, string Char)
        {
            int index = mString.IndexOf(Char);

            string newString = mString.Substring(0, index);

            return newString;
        }

        public string SubstringAfter(string mString, string Char)
        {
            int index = mString.IndexOf(Char);

            string newString = mString.Substring(index + 1, mString.Count() - index - 1);

            return newString;
        }

        public static bool isDateBefore(string value, DateTime exDate)
        {
            string syear, smonth, sday;
            int year, month, day;
            int index;

            index = value.IndexOf("-");
            syear = value.Substring(0, index);
            value = value.Remove(0, index + 1);
            index = value.IndexOf("-");
            smonth = value.Substring(0, index);
            value = value.Remove(0, index + 1);
            index = value.IndexOf("T");
            sday = value.Substring(0, index);

            int.TryParse(syear, out year);
            int.TryParse(smonth, out month);
            int.TryParse(sday, out day);

            DateTime nDate = new DateTime(year, month, day);

            if (nDate < exDate)
                return true;

            return false;
        }

        public static object LoadSourceData(string path)
        {
            HashSet<string> RefCadRevisions = new HashSet<string>();
            HashSet<string> RefCadItems = new HashSet<string>();

            Dictionary<string, Classes.Item> Items = new Dictionary<string, Classes.Item>();
            Dictionary<string, Classes.Revision> MasterRevisions = new Dictionary<string, Classes.Revision>();
            Dictionary<string, Classes.Dataset> MasterDatasets = new Dictionary<string, Classes.Dataset>();

            string[] files = Directory.GetFiles(path);
            int fileCount = files.Count();

            for (int i = 0; i < fileCount; i++)
            {
                if (i + 1 < fileCount)
                    Console.Write("\rGathering Source Data : {0}% Complete", (100m * (i + 1) / fileCount).ToString("0.00"));
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\rGathering Source Data : {0}% Complete  ", (100m * (i + 1) / fileCount).ToString("0.00"));
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Dictionary<string, Classes.Revision> RevisionInstances = new Dictionary<string, Classes.Revision>();
                Dictionary<string, Classes.Item> ItemInstances = new Dictionary<string, Classes.Item>();

                Dictionary<string, Classes.ReleaseStatus> ReleaseStatuses = new Dictionary<string, Classes.ReleaseStatus>();
                Dictionary<string, Classes.Group> Groups = new Dictionary<string, Classes.Group>();

                Dictionary<string, Classes.IMANRelation> IMANRels = new Dictionary<string, Classes.IMANRelation>();
                Dictionary<string, Classes.Dataset> Datasets = new Dictionary<string, Classes.Dataset>();

                //List<XElement> Item = new List<XElement>();
                //List<XElement> ItemRevision = new List<XElement>();
                List<XElement> PSOccurrence = new List<XElement>();
                List<XElement> PSBOMViewRevision = new List<XElement>();

                using (XmlReader reader = XmlReader.Create(files[i]))
                {
                    reader.MoveToContent();

                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element)
                            continue;

                        switch (reader.Name)
                        {
                            case "Item":
                                {
                                    reader.MoveToAttribute("item_id");
                                    string item_id = reader.Value;

                                    reader.MoveToAttribute("object_type");
                                    string object_type = reader.Value;

                                    if (object_type != "Production" && object_type != "Prototype" && object_type != "PartialProcMatl" && object_type != "StandardPart" && object_type != "Reference")
                                        break;

                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;

                                    Classes.Item item = new Classes.Item(puid, item_id, object_type);

                                    if (!Items.ContainsKey(puid))
                                    {
                                        Items.Add(puid, item);
                                        ItemInstances.Add(puid, item);
                                        //reader.MoveToElement();
                                        //Item.Add(XElement.Parse(reader.ReadOuterXml()));
                                    }

                                    break;
                                }
                            case "ItemRevision":
                                {

                                    reader.MoveToAttribute("creation_date");
                                    string creation_dateS = reader.Value;

                                    DateTime creation_date = XmlConvert.ToDateTime(creation_dateS, XmlDateTimeSerializationMode.Utc);

                                    reader.MoveToAttribute("item_revision_id");
                                    string revision_id = reader.Value;

                                    reader.MoveToAttribute("object_type");
                                    string object_type = reader.Value;

                                    if (object_type != "Production Revision" && object_type != "Prototype Revision" && object_type != "PartialProcMatl Revision" && object_type != "StandardPart Revision" && object_type != "Reference Revision")
                                        break;

                                    reader.MoveToAttribute("owning_group");
                                    string group = reader.Value;

                                    reader.MoveToAttribute("items_tag");
                                    string items_tag = reader.Value;

                                    if (items_tag == "")
                                        break;

                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;

                                    reader.MoveToAttribute("release_status_list");
                                    string release_status_list = reader.Value;

                                    Classes.Revision rev = new Classes.Revision(puid, revision_id, object_type, items_tag, group, release_status_list, creation_date);

                                    if (!MasterRevisions.ContainsKey(puid))
                                    {
                                        reader.MoveToElement();
                                        //ItemRevision.Add(XElement.Parse(reader.ReadOuterXml()));
                                        RevisionInstances.Add(puid, rev);
                                    }

                                    break;
                                }
                            case "PSOccurrence":
                                {
                                    PSOccurrence.Add(XElement.Parse(reader.ReadOuterXml()));
                                    break;
                                }
                            case "PSBOMViewRevision":
                                {
                                    PSBOMViewRevision.Add(XElement.Parse(reader.ReadOuterXml()));
                                    break;
                                }
                            case "ReleaseStatus":
                                {
                                    reader.MoveToAttribute("name");
                                    string name = reader.Value;

                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;

                                    Classes.ReleaseStatus status = new Classes.ReleaseStatus(puid, name);

                                    if (!ReleaseStatuses.ContainsKey(puid))
                                        ReleaseStatuses.Add(puid, status);

                                    break;
                                }
                            case "Group":
                                {
                                    reader.MoveToAttribute("elemId");
                                    string elemId = reader.Value;

                                    reader.MoveToAttribute("full_name");
                                    string full_name = reader.Value;

                                    Classes.Group group = new Classes.Group(elemId, full_name);

                                    if (!Groups.ContainsKey(elemId))
                                        Groups.Add(elemId, group);

                                    break;
                                }
                            case "ImanRelation":
                                {
                                    reader.MoveToAttribute("primary_object");
                                    string primary_object = reader.Value;

                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;

                                    reader.MoveToAttribute("secondary_object");
                                    string secondary_object = reader.Value;

                                    Classes.IMANRelation Rel = new Classes.IMANRelation(puid, primary_object, secondary_object);

                                    if (!IMANRels.ContainsKey(puid))
                                        IMANRels.Add(puid, Rel);

                                    break;
                                }
                            case "Dataset":
                                {
                                    reader.MoveToAttribute("object_name");
                                    string object_name = reader.Value;

                                    reader.MoveToAttribute("object_type");
                                    string object_type = reader.Value;

                                    reader.MoveToAttribute("parent_uid");
                                    string parent_uid = reader.Value;

                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;


                                    if (!MasterDatasets.ContainsKey(puid))
                                        MasterDatasets.Add(puid, new Classes.Dataset(puid, object_type, parent_uid, object_name));

                                    Datasets.Add(puid, new Classes.Dataset(puid, object_type, parent_uid, object_name));

                                    break;
                                }
                        }
                    }
                }

                //add datasets
                var datasets = from rev in RevisionInstances.Values
                               join rels in IMANRels.Values on rev.PUID equals rels.Primary
                               join dataset in Datasets.Values on rels.Secondary equals dataset.PUID
                               select new object[2] { rev, dataset };

                foreach (var rev in datasets)
                {
                    Classes.Revision revision = (Classes.Revision)rev[0];
                    Classes.Dataset dataset = (Classes.Dataset)rev[1];

                    revision.AddDataset(dataset.PUID, dataset.Type,dataset.Name);
                }


                var assemListRev = from rev in RevisionInstances.Values
                                   join psOcc in PSOccurrence on rev.PUID equals psOcc.Attribute("child_item").Value
                                   join bvrRev in PSBOMViewRevision on psOcc.Attribute("parent_bvr").Value equals bvrRev.Attribute("puid").Value
                                   join item in ItemInstances.Values on bvrRev.Attribute("parent_uid").Value equals item.PUID
                                   where rev.ObjectType == "Reference Revision" && (item.ObjectType == "Production" || item.ObjectType == "Prototype" || item.ObjectType == "PartialProcMatl" || item.ObjectType == "StandardPart")
                                   select rev;


                foreach (Classes.Revision rev in assemListRev)
                {
                    RefCadItems.Add(rev.ItemTag);
                    RefCadRevisions.Add(rev.PUID);
                }

                //Fill in Group info
                foreach (var key in RevisionInstances)
                {
                    Classes.Revision rev = key.Value;

                    if (rev.ItemID == "")
                    {
                        rev.SetItemID(ItemInstances.Values);
                        rev.SetGroupName(Groups.Values.ToList());
                        rev.SetStatusNames(ReleaseStatuses.Values.ToList());
                    }

                    if (rev.ItemID == "")
                    {
                        rev.PUID = key.Key;
                    }

                    MasterRevisions.Add(key.Key, key.Value);
                }
            }

            //update ReleaseList from Cad - change ref to production

            var nList = from el in Items.Values
                        join refCad in RefCadItems on el.PUID equals refCad
                        select el;

            foreach (Classes.Item item in nList)
            {
                item.ObjectType = "Production";
            }

            nList = null;

            var nList2 = from el in MasterRevisions.Values
                         join refCad in RefCadRevisions on el.PUID equals refCad
                         select el;

            foreach (Classes.Revision rev in nList2)
            {
                rev.ObjectType = "Production Revision";
            }



            foreach (Classes.Revision rev in MasterRevisions.Values.Where(x => x.ReleaseStatusNames != ""))
            {
                rev.Map();
            }

            IEnumerable<Classes.Item> refMasterItems = from item in Items.Values
                                                       where item.ObjectType != "Reference"
                                                       select item;

            IEnumerable<Classes.Revision> refMasterRevs = from rev in MasterRevisions.Values
                                                          where rev.ItemID != null && rev.ObjectType != "Reference Revision"
                                                          select rev;

            Renumber(refMasterItems, refMasterRevs);

            //GenerateIPSReleaseStatus(IPS.ToList());

            return new { Items = Items.Values.Select(x => x), Revisions = MasterRevisions.Values.Select(x => x), RefCadItems = RefCadItems, RefCadRevisions = RefCadRevisions, Datasets = MasterDatasets.Values.Select(x => x) };

        }

        private static void Renumber(IEnumerable<Classes.Item> itemList, IEnumerable<Classes.Revision> revList)
        {
            long index = 0;

            //var t = from item in itemList
            //        join rev in revList on item.PUID equals rev.ItemTag
            //        select item;

            foreach (Classes.Item item in itemList)
            {
                index++;
                item.ItemID = "NAA" + index.ToString("000000000");
            }


            var grouping = from rev in revList
                           join item in itemList on rev.ItemTag equals item.PUID
                           group rev by item.ItemID into newGroup
                           orderby newGroup.Key
                           select new
                           {
                               ItemID = newGroup.Key,
                               Revisons = newGroup.OrderBy(x => x.CreationDate).ThenBy(x => x.CreationDate.TimeOfDay)
                           };


            foreach (var ItemGroup in grouping.Skip(0))
            {
                int revIndex = 0;
                foreach (var rev in ItemGroup.Revisons)
                {
                    rev.RevID = "R" + revIndex.ToString("000");
                    rev.RenumberItemID(ItemGroup.ItemID);
                    revIndex++;
                }
            }

            var revs = from rev in revList
                       where !rev.RevID.StartsWith("R")
                       select rev;

            foreach (var rev in revs)
            {
                rev.RevID = "R000";
            }

        }

        public static void GenerateIPSReleaseStatus(ushort max, string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var groups = Split(revList
                        .Where(r => r.ItemID != "" && r.ReleaseStatusNames != "")
                        .Select(r => string.Join("~", new string[3] { r.ItemID, r.RevID, r.ReleaseStatusNames})).ToList(), max);

            //var rsArr = (from r in revList
            //             where r.ItemID != "" && r.ReleaseStatusNames != ""
            //             select string.Join("~", new string[3] { r.ItemID, r.RevID, r.ReleaseStatusNames })).ToList();

            for (int i = 0; i < groups.Count(); i++)
            {
                groups[i].Insert(0, "!~ItemID~RevID~Status");
                File.WriteAllLines(Path.Combine(path, "ReleaseStatus" + (i + 1) + ".txt"), groups[i].ToArray());
            }
            //rsArr.Insert(0, "!~ItemID~RevID~Status");

            //File.WriteAllLines(Path.Combine(path,"ReleaseStatus.txt"), rsArr.ToArray());

        }
        
        public static void GenerateErrorRevs(string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var revs = (from rev in revList
                       where rev.ItemID == ""
                       select string.Join(",",rev.PUID,rev.ItemTag,rev.ItemID,rev.RevID,rev.ObjectType,rev.OwningGroup,rev.CreationDate)).ToList();


            revs.Insert(0, "Revision UID,Item UID, Item ID,Revision ID,Object Type,Owning Group,Creation Date");

            File.WriteAllLines(Path.Combine(path, "RevisionErrors.csv"), revs.ToArray());

        }

        public static void GenerateDatasetFailures(string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var datasets = (from rev in revList
                            where rev.ItemID == ""
                           from ds in rev.GetDatasets()
                            select string.Join(",", ds.PUID, ds.ParentUID, ds.Name, ds.Type)).ToList();



            datasets.Insert(0, "Dataset UID,Parent UID, Name, Type");

            File.WriteAllLines(Path.Combine(path, "DatasetFailures.csv"), datasets.ToArray());

        }

        public static void GenerateOrphanDatasets(string path, IEnumerable<Classes.Dataset> datasetList, bool Make)
        {
            if (!Make)
                return;

            var datasets = (from ds in datasetList
                            where ds.ParentUID == ""
                            select string.Join(",",ds.PUID, ds.ParentUID, ds.Name, ds.Type)).ToList();



            datasets.Insert(0, "Dataset UID,Parent UID, Name, Type");

            File.WriteAllLines(Path.Combine(path, "OrphanDatasets.csv"), datasets.ToArray());

        }

        public static void GenerateRevisionImport(string path, IEnumerable<Classes.Revision> revisionList, bool Make)
        {
            if (!Make)
                return;

            var revisions = (from rev in revisionList
                            select string.Join(",", rev.PUID, rev.ItemID, rev.RevID, rev.ObjectType, rev.OwningGroup, rev.ReleaseStatusNames)).ToList();

            revisions.Insert(0, "PUID,ItemID,RevID,Old_ObjectType,OwningGroup,ReleaseStatus");

            File.WriteAllLines(Path.Combine(path, "RevisionImport.csv"), revisions.ToArray());
        }


        public static void GenerateRecursiveDatasets(string path, IEnumerable<Classes.Dataset> datasetList, bool Make)
        {
            if (!Make)
                return;

            var datasets = (from ds in datasetList
                            where ds.ParentUID == ds.PUID
                            select string.Join(",", ds.PUID, ds.ParentUID, ds.Name, ds.Type)).ToList();



            datasets.Insert(0, "Dataset UID,Parent UID, Name, Type");

            File.WriteAllLines(Path.Combine(path, "RecursiveDatasets.csv"), datasets.ToArray());

        }


        public static List<List<string>> Split(List<string> source, ushort splitMax)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / splitMax)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }



        public void IMANRelSwap()
        {
            IEnumerable<XElement> list1;
            XNamespace df = xmlFile.GetDefaultNamespace();

            XElement IMAN_specification = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_specification");
            XElement catia_auxiliaryLink = GetSingleElementByAttrID("ImanType", "type_name", "catia_auxiliaryLink");
            XElement IMAN_external_object_link = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_external_object_link");

            if (IMAN_specification != null)
            {
                string IMAN_specificationRef = "#" + IMAN_specification.Attribute("elemId").Value;
                string catia_auxiliaryLinkRef = "";
                string IMAN_external_object_linkRef = "";

                catia_auxiliaryLinkRef = (catia_auxiliaryLink != null) ? "#" + catia_auxiliaryLink.Attribute("elemId").Value : "";
                IMAN_external_object_linkRef = (IMAN_external_object_link != null) ? "#" + IMAN_external_object_link.Attribute("elemId").Value : "";

                list1 =
                    from ImanRel in xmlFile.Elements(df + "ImanRelation")
                    join Item in xmlFile.Elements(df + "ItemRevision") on (string)ImanRel.Attribute("primary_object") equals (string)Item.Attribute("puid").Value
                    join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
                    where Dataset.Attribute("object_type").Value == "CATDrawing"
                             || Dataset.Attribute("object_type").Value == "UGPART"
                             || Dataset.Attribute("object_type").Value == "UGMaster"
                             || Dataset.Attribute("object_type").Value == "CATProduct"
                             || Dataset.Attribute("object_type").Value == "CATPart"
                    select ImanRel;

                foreach (XElement el in list1)
                {
                    el.Attribute("relation_type").SetValue(IMAN_specificationRef);
                }

            }


            #region CATSHAPE -> catia_alternateShapeRep
            XElement catia_alternateShapeRep = GetSingleElementByAttrID("ImanType", "type_name", "catia_alternateShapeRep");
            if (catia_alternateShapeRep != null)
            {
                string catia_alternateShapeRepRef = "#" + catia_alternateShapeRep.Attribute("elemId").Value;
                list1 =
                from ImanRel in xmlFile.Elements(df + "ImanRelation")
                join Item in xmlFile.Elements(df + "ItemRevision") on (string)ImanRel.Attribute("primary_object") equals (string)Item.Attribute("puid").Value
                join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
                where Dataset.Attribute("object_type").Value == "CATSHAPE"
                select ImanRel;

                foreach (XElement el in list1)
                {
                    el.Attribute("relation_type").SetValue(catia_alternateShapeRepRef);
                }
            }
            #endregion


            #region UGALTREP -> IMAN_UG_altrep
            XElement IMAN_UG_altrep = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_UG_altrep");
            if (IMAN_UG_altrep != null)
            {
                string IMAN_UG_altrepRef = "#" + IMAN_UG_altrep.Attribute("elemId").Value;
                list1 =
                from ImanRel in xmlFile.Elements(df + "ImanRelation")
                join Item in xmlFile.Elements(df + "ItemRevision") on (string)ImanRel.Attribute("primary_object") equals (string)Item.Attribute("puid").Value
                join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
                where Dataset.Attribute("object_type").Value == "UGALTREP"
                select ImanRel;

                foreach (XElement el in list1)
                {
                    el.Attribute("relation_type").SetValue(IMAN_UG_altrepRef);
                }
            }
            #endregion

        }

        public void PartReNum()
        {
            var list = from itemS in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "GNM8_CADItem")
                       join itemM in this.MasterItems on itemS.Attribute("puid").Value equals itemM.PUID
                       where itemS.Name.LocalName == "GNM8_CADItem"
                       select new { Item = itemS, NewID = itemM.ItemID };

            foreach (var el in list)
            {
                el.Item.SetAttributeValue("item_id", el.NewID);
            }

            var revisions = from revS in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "GNM8_CADItemRevision")
                            join revM in this.MasterRevisions on revS.Attribute("puid").Value equals revM.PUID
                            where revS.Name.LocalName == "GNM8_CADItemRevision"
                            select new { Revision = revS, NewID = revM };

            foreach (var el in revisions)
            {
                el.Revision.SetAttributeValue("item_revision_id", el.NewID.RevID);
            }

            var datasets = from dataset in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "Dataset")
                           join rev in MasterRevisions on dataset.Attribute("parent_uid").Value equals rev.ItemTag
                           where dataset.Attribute("parent_uid").Value != "" && rev.ItemID != "" && rev.GetDatasets().Select(x => x.PUID).Contains(dataset.Attribute("puid").Value)
                           select new { Dataset = dataset, ItemID = rev.ItemID, RevID = rev.RevID };

            foreach (var el in datasets)
            {
                switch (el.Dataset.Attribute("object_type").Value)
                {
                    case "UGMASTER":
                    case "UGPART":
                    case "UGALTREP":
                    case "CATPart":
                    case "CATProduct":
                    case "CATDrawing":
                    case "CATShape":
                    case "DirectModel":
                        {
                            el.Dataset.SetAttributeValue("object_name", el.ItemID + "/" + el.RevID);
                            break;
                        }
                    case "DrawingSheet":
                    case "TIF":
                        {

                            string newID = el.Dataset.Attribute("object_name").Value;
                            int index = newID.LastIndexOf("_");
                            if (index > -1)
                            {
                                newID = newID.Substring(0, index + 1) + el.RevID;
                                el.Dataset.SetAttributeValue("object_name", newID);
                            }
                            break;
                        }
                }

            }

            var bomViews = from bomview in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "PSBOMView")
                           join item in MasterItems on bomview.Attribute("parent_uid").Value equals item.PUID
                           select new { bom = bomview, ItemID = item.ItemID };

            foreach (var el in bomViews)
            {
                el.bom.SetAttributeValue("object_name", el.ItemID + "-view");
            }

            var bomViewsRevs = from bomviewRev in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "PSBOMViewRevision")
                               join rev in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "GNM8_CADItemRevision") on bomviewRev.Attribute("puid").Value equals ((string)rev.Attribute("structure_revisions") ?? "")
                               join revM in MasterRevisions on rev.Attribute("puid").Value equals revM.PUID
                               where revM.ItemID != "" && rev.Attribute("structure_revisions") != null
                               select new { bom = bomviewRev, ItemID = revM.ItemID, RevID = revM.RevID };

            foreach (var el in bomViewsRevs)
            {
                el.bom.SetAttributeValue("object_name", el.ItemID + "/" + el.RevID + "-view");
            }

        }

        public static void GenerateLog(string path, IEnumerable<Classes.Item> items, IEnumerable<Classes.Revision> revisions, IEnumerable<string> refItem, int TotalDatasets, int OrphanDatasets,int RecursiveDatasets, IEnumerable<string> refIR, int BrokenIMANS, int TotalIMANS, TimeSpan duration, bool Report)
        {
            int totalRevCount = revisions.Count();
            int datasetCount = revisions.AsEnumerable().Sum(o => o.GetDatasets().Count());

            int orphanItemCount = (from item in items
                                   where !(from rev in revisions
                                           select rev.ItemTag)
                                              .Contains(item.PUID)
                                   select item).Count();

            int faildatasets = revisions.AsEnumerable().Where(x => x.ItemID == "" && x.ItemTag != "").Sum(o => o.GetDatasets().Count());

            int orphanRevs = revisions.Where(x => x.ItemID == "" && x.ItemTag != "").Count();

            int percent = Convert.ToInt32((datasetCount - faildatasets + totalRevCount - orphanRevs + TotalIMANS - BrokenIMANS) * 1.0f / (totalRevCount + TotalIMANS + datasetCount) * 100);

            // int percent = Convert.ToInt32((1 - ((orhpanRevs + BrokenIMANS) * 1.0f / (revisions.Count() + TotalIMANS))) * 100);

            using (StreamWriter writer = new StreamWriter(Path.Combine(path, "Mapping.log"), false))
            {
                writer.WriteLine("Log created on " + DateTime.Now);
                if (!Report)
                { 
                writer.WriteLine("Mapping duration - " + duration.Hours.ToString("00") + ":" + duration.Minutes.ToString("00") + ":" + duration.Seconds.ToString("00"));
                writer.WriteLine();
                writer.WriteLine("\t[Maximum Data Import : " + percent + "%]");
                writer.WriteLine();
                }
                else
                {
                    writer.WriteLine("Report duration - " + duration.Hours.ToString("00") + ":" + duration.Minutes.ToString("00") + ":" + duration.Seconds.ToString("00"));
                    writer.WriteLine();
                }
                writer.WriteLine("\t\tPost Mapping");
                writer.WriteLine("________________________________________________");
                writer.WriteLine("\tTotal CAD Items                  : " + items.Where(x => x.ObjectType != "Reference").Count());
                writer.WriteLine("\tTotal CAD Revisions              : " + revisions.Where(x => x.ObjectType != "Reference Revision").Count());
                writer.WriteLine();
                writer.WriteLine("\tTotal Reference Items            : " + items.Where(x => x.ObjectType == "Reference").Count());
                writer.WriteLine("\tTotal Reference Revisions        : " + revisions.Where(x => x.ObjectType == "Reference Revision").Count());
                writer.WriteLine();
                writer.WriteLine("\tTotal Referenced Datasets        : " + datasetCount);
                writer.WriteLine("\tTotal Datasets                   : " + TotalDatasets);
                writer.WriteLine();
                //writer.WriteLine("------------------------------------------------");
                writer.WriteLine("\tTotal Reference -> CAD Items     : " + refItem.Count());
                writer.WriteLine("\tTotal Reference -> CAD Revisions : " + refIR.Count());
                writer.WriteLine("Warnings:");
                writer.WriteLine("\tRecursive Datasets               : " + RecursiveDatasets);
                writer.WriteLine("\tCAD Items with no revisions      : " + orphanItemCount);
                writer.WriteLine("\tOrphan Revisions                 : " + revisions.Where(x => x.ItemID == "" && x.ItemTag == "").Count());
                writer.WriteLine("\tOrphan Datasets                  : " + OrphanDatasets);
                writer.WriteLine("Errors:");
                writer.WriteLine("\tRevisions with missing Items     : " + orphanRevs);
                if(!Report)
                writer.WriteLine("\tBroken IMAN Relations            : " + BrokenIMANS);
                writer.WriteLine("\tDataset failures                 : " + faildatasets);
                writer.WriteLine("------------------------------------------------");
                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine("\t\tPre-Mapping");
                writer.WriteLine("________________________________________________");
                writer.WriteLine("\tTotal source Items : " + items.Count());
                writer.WriteLine();
                writer.WriteLine("\t\t[Breakdown]");
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine("\tProduction      : " + (items.Where(x => x.ObjectType == "Production").Count() - refItem.Count()));
                writer.WriteLine("\tPartialProcMatl : " + items.Where(x => x.ObjectType == "PartialProcMatl").Count());
                writer.WriteLine("\tPrototype       : " + items.Where(x => x.ObjectType == "Prototype").Count());
                writer.WriteLine("\tReference       : " + (items.Where(x => x.ObjectType == "Reference").Count() + refItem.Count()));
                writer.WriteLine("\tStandardPart    : " + items.Where(x => x.ObjectType == "StandardPart").Count());
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine();
                writer.WriteLine("\tTotal source Revisions : " + revisions.Count());
                writer.WriteLine();
                writer.WriteLine("\t\t[Breakdown]");
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine("\tProduction Revision      : " + (revisions.Where(x => x.ObjectType == "Production Revision").Count() - refIR.Count()));
                writer.WriteLine("\tPartialProcMatl Revision : " + revisions.Where(x => x.ObjectType == "PartialProcMatl Revision").Count());
                writer.WriteLine("\tPrototype Revision       : " + revisions.Where(x => x.ObjectType == "Prototype Revision").Count());
                writer.WriteLine("\tReference Revision       : " + (revisions.Where(x => x.ObjectType == "Reference Revision").Count() + refIR.Count()));
                writer.WriteLine("\tStandardPart Revision    : " + revisions.Where(x => x.ObjectType == "StandardPart Revision").Count());
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine("________________________________________________");

            }
        }

        internal static void GeneratePartRenumFile(string path, IEnumerable<Classes.Revision> MasterRevs, bool Make)
        {

            if (!Make)
                return;

            var list = from el in MasterRevs
                       where el.ObjectType != "Reference Revision"
                       select string.Join(",", el.ItemTag, el.PUID, el.ItemID, el.RevID);


            File.WriteAllLines(Path.Combine(path, "PartRenum.csv"), list);

        }
    }

}

