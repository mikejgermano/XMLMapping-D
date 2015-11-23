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
        public IEnumerable<string[]> UsedDatasets;
        public IEnumerable<string> RefCadItems;
        public IEnumerable<Classes.Revision> RefCadRevs;
        public Config config = null;

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


        public static object LoadSourceData(string path)
        {

            HashSet<string> RefCadItems = new HashSet<string>();

            Dictionary<string, Classes.Item> Items = new Dictionary<string, Classes.Item>();
            Dictionary<string, Classes.Revision> MasterRevisions = new Dictionary<string, Classes.Revision>();
            Dictionary<string, Classes.Dataset> MasterDatasets = new Dictionary<string, Classes.Dataset>();

            if (File.Exists(@".\Cache\Items.xml") &&
                File.Exists(@".\Cache\DSCount.xml") &&
                File.Exists(@".\Cache\RefCadItems.xml") &&
                File.Exists(@".\Cache\RefCadRevs.xml") &&
                File.Exists(@".\Cache\Revs.xml") &&
                File.Exists(@".\Cache\UsedDS.xml"))
            {
                //IEnumerable<Classes.Item> iItems = Config.DeSerializeObject<Classes.Item[]>(@".\Cache\Items.xml").AsEnumerable();
                //IEnumerable<Classes.Revision> iRevs = Config.DeSerializeObject<Classes.Revision[]>(@".\Cache\Revs.xml").AsEnumerable();
                //HashSet<string> iRefCadItems = new HashSet<string>(Config.DeSerializeObject<string[]>("RefCadItems.xml").AsEnumerable());
                //IEnumerable<Classes.Revision> iRefCadRevs = Config.DeSerializeObject<Classes.Revision[]>("RefCadRevs.xml").AsEnumerable();
                //int iCount = Config.DeSerializeObject<int>("DSCount.xml");
                //IEnumerable<string[]> iUsedDS = Config.DeSerializeObject<string[][]>("UsedDS.xml").AsEnumerable();

                IEnumerable<Classes.Item> iItems = Config.ReadObject<Classes.Item[]>(@".\Cache\Items.xml").AsEnumerable();
                IEnumerable<Classes.Revision> iRevs = Config.ReadObject<Classes.Revision[]>(@".\Cache\Revs.xml").AsEnumerable();
                HashSet<string> iRefCadItems = new HashSet<string>(Config.ReadObject<string[]>(@".\Cache\RefCadItems.xml").AsEnumerable());
                IEnumerable<Classes.Revision> iRefCadRevs = Config.ReadObject<Classes.Revision[]>(@".\Cache\RefCadRevs.xml").AsEnumerable();
                int iCount = Config.ReadObject<int>(@".\Cache\DSCount.xml");
                IEnumerable<string[]> iUsedDS = Config.ReadObject<string[][]>(@".\Cache\UsedDS.xml").AsEnumerable();

                return new { Items = iItems, Revisions = iRevs, RefCadItems = iRefCadItems, RefCadRevisions = iRefCadRevs, DatasetCount = iCount, UsedDatasets = iUsedDS };
            }

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
                Dictionary<string, Classes.RevisionAnchor> RevisionAnchors = new Dictionary<string, Classes.RevisionAnchor>();

                Dictionary<string, Classes.ReleaseStatus> ReleaseStatuses = new Dictionary<string, Classes.ReleaseStatus>();
                Dictionary<string, Classes.Group> Groups = new Dictionary<string, Classes.Group>();

                Dictionary<string, Classes.IMANRelation> IMANRels = new Dictionary<string, Classes.IMANRelation>();
                Dictionary<string, Classes.IMANType> IMANTypes = new Dictionary<string, Classes.IMANType>();
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
                                    string item_id = reader.Value.ToUpper();

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
                                    string revision_id = reader.Value.ToUpper();

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

                                    reader.MoveToAttribute("relation_type");
                                    string relation_type = reader.Value;

                                    Classes.IMANRelation Rel = new Classes.IMANRelation(puid, primary_object, secondary_object, relation_type);

                                    if (!IMANRels.ContainsKey(puid))
                                        IMANRels.Add(puid, Rel);

                                    break;
                                }
                            case "ImanType":
                                {
                                    reader.MoveToAttribute("elemId");
                                    string elemId = reader.Value;

                                    reader.MoveToAttribute("type_name");
                                    string type_name = reader.Value;

                                    Classes.IMANType IMANType = new Classes.IMANType(elemId, type_name);

                                    if (!IMANRels.ContainsKey(elemId))
                                        IMANTypes.Add(elemId, IMANType);

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

                                    reader.MoveToAttribute("rev_chain_anchor");
                                    string rev_chain_anchor = reader.Value;


                                    if (!MasterDatasets.ContainsKey(puid))
                                        MasterDatasets.Add(puid, new Classes.Dataset(puid, object_type, parent_uid, object_name, rev_chain_anchor));

                                    Datasets.Add(puid, new Classes.Dataset(puid, object_type, parent_uid, object_name, rev_chain_anchor));

                                    break;
                                }
                            case "RevisionAnchor":
                                {
                                    reader.MoveToAttribute("puid");
                                    string puid = reader.Value;

                                    reader.MoveToAttribute("revisions");
                                    string revisions = reader.Value;

                                    RevisionAnchors.Add(puid, new Classes.RevisionAnchor(puid, revisions));

                                    break;
                                }
                        }
                    }
                }


                var assemListRev = from rev in RevisionInstances.Values
                                   join psOcc in PSOccurrence on rev.PUID equals psOcc.Attribute("child_item").Value
                                   join bvrRev in PSBOMViewRevision on psOcc.Attribute("parent_bvr").Value equals bvrRev.Attribute("puid").Value
                                   join item in ItemInstances.Values on bvrRev.Attribute("parent_uid").Value equals item.PUID
                                   where rev.ObjectType == "Reference Revision" && (item.ObjectType == "Production" || item.ObjectType == "Prototype" || item.ObjectType == "PartialProcMatl" || item.ObjectType == "StandardPart")
                                   select rev;


                foreach (Classes.Revision rev in assemListRev)
                {
                    rev.Ref2CAD = true;
                    RefCadItems.Add(rev.ItemTag);
                    //RefCadRevisions.Add(rev.PUID);
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

                //add datasets To revisions
                var datasets = from rev in RevisionInstances.Values
                               join rels in IMANRels.Values on rev.PUID equals rels.Primary
                               join types in IMANTypes.Values on rels.TypeRef equals types.ID
                               join dataset in Datasets.Values on rels.Secondary equals dataset.PUID
                               //join revAnchor in RevisionAnchors.Values on dataset.Rev_chain_anchor equals revAnchor.PUID
                               select new object[3] { rev, dataset, types };

                foreach (var rev in datasets)
                {
                    Classes.Revision revision = (Classes.Revision)rev[0];
                    Classes.Dataset dataset = (Classes.Dataset)rev[1];
                    Classes.IMANType type = (Classes.IMANType)rev[2];

                    var anchor = (from revAnchor in RevisionAnchors.Values
                                  where dataset.Rev_chain_anchor == revAnchor.PUID
                                  select revAnchor).Single();

                    dataset.Revisions = anchor.Revisions;
                    dataset.RelationType = Classes.Dataset.MappedRelation(dataset.Type, type.Type);
                    revision.AddDataset(dataset);

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

            var RefCadRevisions = from rev in MasterRevisions.Values
                                  join refCad in RefCadItems on rev.ItemTag equals refCad
                                  select rev;

            foreach (var rev in RefCadRevisions)
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


            var dsM = from rev in MasterRevisions.Values
                      from dataset in rev.GetDatasets()
                      //from anchor in dataset.Revisions.Split(',')
                      //join datasetM in MasterDatasets.Values on anchor equals datasetM.PUID
                      where rev.ItemID != ""
                      select new DAnchor() { ItemID = rev.ItemID, RevID = rev.RevID, Dataset = dataset };

            foreach (var el in dsM)
            {

                switch (el.Dataset.Type)
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
                            el.Dataset.Name = el.ItemID + "/" + el.RevID;
                            break;
                        }
                    case "DrawingSheet":
                    case "TIF":
                        {

                            string newID = el.Dataset.Name;
                            int index = newID.LastIndexOf("_");
                            if (index > -1)
                            {
                                newID = newID.Substring(0, index + 1) + el.RevID;
                                el.Dataset.Name = newID;
                            }
                            break;
                        }
                }

            }

            //REMOVE LATER --HACKY
            //Fill in missing ItemIDs if they exist
            var fix = from i in MasterRevisions.Values
                      join item in Items.Values on i.ItemTag equals item.PUID
                      where i.OldItemID == ""
                      select new { Item = item, Rev = i };

            foreach (var el in fix)
            {
                el.Rev.OldItemID = el.Item.OldItemID;
            }


            var usedDatasets = from ds in MasterRevisions.Values.SelectMany(x => x.GetDatasets())
                               from version in ds.Revisions.Split(',')
                               select new string[2] { version, ds.Name };

            //Save stuff

            //Item
            //Config.SerializeObject<Classes.Item[]>(Items.Values.ToArray(), Path.Combine("Cache", "Items.xml"));
            Config.WriteObject<Classes.Item[]>(Path.Combine("Cache", "Items.xml"), Items.Values.ToArray());

            //Revision
            //Config.SerializeObject<Classes.Revision[]>(MasterRevisions.Values.ToArray(), Path.Combine("Cache", "Revs.xml"));
            Config.WriteObject<Classes.Revision[]>(Path.Combine("Cache", "Revs.xml"), MasterRevisions.Values.ToArray());

            //RefCadItems
            //Config.SerializeObject<HashSet<string>>(RefCadItems, Path.Combine("Cache", "RefCadItems.xml"));
            Config.WriteObject<HashSet<string>>(Path.Combine("Cache", "RefCadItems.xml"), RefCadItems);

            //RefCadRevs
            //Config.SerializeObject<Classes.Revision[]>(RefCadRevisions.ToArray(), Path.Combine("Cache", "RefCadRevs.xml"));
            Config.WriteObject<Classes.Revision[]>(Path.Combine("Cache", "RefCadRevs.xml"), RefCadRevisions.ToArray());

            //Dataset Count
            //Config.SerializeObject<int>(MasterDatasets.Count(), Path.Combine("Cache","DSCount.xml"));
            Config.WriteObject<int>(Path.Combine("Cache", "DSCount.xml"), MasterDatasets.Count());

            //Used Datasets
            //Config.SerializeObject<string[][]>(usedDatasets.ToArray(), Path.Combine("Cache", "UsedDS.xml"));
            Config.WriteObject<string[][]>(Path.Combine("Cache", "UsedDS.xml"), usedDatasets.ToArray());

            

            return new { Items = Items.Values.Select(x => x), Revisions = MasterRevisions.Values.Select(x => x), RefCadItems = RefCadItems, RefCadRevisions = RefCadRevisions, DatasetCount = MasterDatasets.Count(), UsedDatasets = usedDatasets };

        }

        public class DAnchor
        {
            public Classes.Dataset Dataset;
            public string ItemID;
            public string RevID;
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
                        .Select(r => string.Join("~", new string[3] { r.ItemID, r.RevID, r.ReleaseStatusNames })).ToList(), max);

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

        private static string GetParamCode(string objectType)
        {
            string pc = "";

            objectType = objectType.ToUpper();

            if (objectType == "UGPART" || objectType == "CATDRAWING")
            {
                pc = "d";
            }
            else if (objectType == "UGMASTER" || objectType == "CATPART" || objectType == "CATPRODUCT")
            {
                pc = "c";
            }

            return pc;
        }

        public static void GenerateSQLDatasetRename(ushort max, string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

             var datasets = (from rev in revList
                            from dataset in rev.GetDatasets()
                            where GetParamCode(dataset.Type) != ""
                             select new { PUID = dataset.PUID, dataset.Name});

             List<string> list = new List<string>(); 

             foreach (var ds in datasets)
             {
                 StringBuilder sb = new StringBuilder();

                 sb.Append("UPDATE PWORKSPACEOBJECT ");
                 sb.Append("SET POBJECT_NAME = '" + ds.Name + "' ");
                 sb.Append("WHERE PUID = '" + ds.PUID + "';");

                 list.Add(sb.ToString());
             }

             var groups = Split(list, max);


             for (int i = 0; i < groups.Count(); i++)
             {
                 //groups[i].Insert(0, "!~ItemID~RevID~DsetType~DsetName~RelationName~NewDsetName");
                 File.WriteAllLines(Path.Combine(path, "SQL_DS_Rename" + (i + 1) + ".sql"), groups[i].ToArray());
             }
        }

        public static void GenerateIPSDatasetRename(ushort max, string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var datasets = (from rev in revList
                            from dataset in rev.GetDatasets()
                            where GetParamCode(dataset.Type) != ""
                            select string.Join("~", new string[6] { rev.ItemID, rev.RevID, dataset.Type, (dataset.OldName.ToUpper().StartsWith("JP")) ? dataset.OldName.Remove(0, 2) : dataset.OldName, dataset.RelationType, dataset.Name })).ToList();
           
            var groups = Split(datasets, max);

            //var rsArr = (from r in revList
            //             where r.ItemID != "" && r.ReleaseStatusNames != ""
            //             select string.Join("~", new string[3] { r.ItemID, r.RevID, r.ReleaseStatusNames })).ToList();

            for (int i = 0; i < groups.Count(); i++)
            {
                groups[i].Insert(0, "!~ItemID~RevID~DsetType~DsetName~RelationName~NewDsetName");
                File.WriteAllLines(Path.Combine(path, "IPS_DS_Rename" + (i + 1) + ".txt"), groups[i].ToArray());
            }
            //rsArr.Insert(0, "!~ItemID~RevID~Status");

            //File.WriteAllLines(Path.Combine(path,"ReleaseStatus.txt"), rsArr.ToArray());

        }

        public static void GenerateIPSParameterCode(ushort max, string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var datasets = (from rev in revList
                            from dataset in rev.GetDatasets()
                            where GetParamCode(dataset.Type) != "" && GetParamCode(dataset.Type) != "d"
                            select string.Join("~", new string[6] { rev.ItemID, rev.RevID, dataset.Type, dataset.Name, dataset.RelationType, GetParamCode(dataset.Type) })).ToList();

            var groups = Split(datasets, max);

            //var rsArr = (from r in revList
            //             where r.ItemID != "" && r.ReleaseStatusNames != ""
            //             select string.Join("~", new string[3] { r.ItemID, r.RevID, r.ReleaseStatusNames })).ToList();

            for (int i = 0; i < groups.Count(); i++)
            {
                groups[i].Insert(0, "!~ItemID~RevID~DsetType~DsetName~RelationName~DSET:gnm8_parameter_code");
                File.WriteAllLines(Path.Combine(path, "ParamCode" + (i + 1) + ".txt"), groups[i].ToArray());
            }
            //rsArr.Insert(0, "!~ItemID~RevID~Status");

            //File.WriteAllLines(Path.Combine(path,"ReleaseStatus.txt"), rsArr.ToArray());

        }



        public static void GenerateRef2CADFile(string path, IEnumerable<Classes.Revision> refAssmList, bool Make)
        {
            if (!Make)
                return;

            var revs = (from rev in refAssmList
                        select string.Join(",", rev.PUID, rev.ItemTag, rev.ItemID, rev.RevID, rev.ObjectType, rev.OwningGroup, rev.CreationDate)).ToList();


            revs.Insert(0, "Revision UID,Item UID, Item ID,Revision ID,Object Type,Owning Group,Creation Date");

            File.WriteAllLines(Path.Combine(path, "ReferenceToCAD.csv"), revs.ToArray());

        }

        public static void GenerateErrorRevs(string path, IEnumerable<Classes.Revision> revList, bool Make)
        {
            if (!Make)
                return;

            var revs = (from rev in revList
                        from dataset in rev.GetDatasets()
                        where rev.ItemID == ""
                        select string.Join(",", rev.PUID, rev.ItemTag, rev.ItemID, "=\"" + rev.OldRevID + "\"", rev.ObjectType, rev.OwningGroup, dataset.Name)).ToList();


            revs.Insert(0, "Revision UID,Item UID, Item ID,Revision ID,Object Type,Owning Group, Dataset Name");

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
                            select string.Join(",", ds.PUID, ds.ParentUID, ds.Name, ds.Type)).ToList();



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

        public void RunPostProcess()
        {
            foreach (string script in config.ScriptPaths)
            {
                Mike_G_ScriptMapper.Script s = new Mike_G_ScriptMapper.Script(script, ref xmlFile);
                s.Run();
            }
        }

        public void IMANRelSwap()
        {
            IEnumerable<XElement> list1;
            XNamespace df = xmlFile.GetDefaultNamespace();

            XElement IMAN_manifestation = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_manifestation");
            XElement IMAN_specification = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_specification");
            XElement catia_auxiliaryLink = GetSingleElementByAttrID("ImanType", "type_name", "catia_auxiliaryLink");
            XElement IMAN_external_object_link = GetSingleElementByAttrID("ImanType", "type_name", "IMAN_external_object_link");


            string IMAN_specificationRef = "#" + IMAN_specification.Attribute("elemId").Value;

            #region CATSHAPE -> catia_alternateShapeRep
            XElement catia_alternateShapeRep = GetSingleElementByAttrID("ImanType", "type_name", "catia_alternateShapeRep");
            if (catia_alternateShapeRep != null)
            {
                string catia_alternateShapeRepRef = "#" + catia_alternateShapeRep.Attribute("elemId").Value;
                list1 =
                from ImanRel in xmlFile.Elements(df + "ImanRelation")
                join Item in xmlFile.Elements(df + "GNM8_CADItemRevision") on (string)ImanRel.Attribute("primary_object") equals (string)Item.Attribute("puid").Value
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
                join Item in xmlFile.Elements(df + "GNM8_CADItemRevision") on (string)ImanRel.Attribute("primary_object") equals (string)Item.Attribute("puid").Value
                join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
                where Dataset.Attribute("object_type").Value == "UGALTREP"
                select ImanRel;

                foreach (XElement el in list1)
                {
                    el.Attribute("relation_type").SetValue(IMAN_UG_altrepRef);
                }
            }
            #endregion

            if (IMAN_manifestation != null)
            {
                string IMAN_maifestationRef = "#" + IMAN_manifestation.Attribute("elemId").Value;

                string catia_auxiliaryLinkRef = (catia_auxiliaryLink != null) ? "#" + catia_auxiliaryLink.Attribute("elemId").Value : "";
                string IMAN_external_object_linkRef = (IMAN_external_object_link != null) ? "#" + IMAN_external_object_link.Attribute("elemId").Value : "";


                var attributes = from el in xmlFile.Elements().Attributes()
                                 where el.Value == IMAN_maifestationRef
                                 select el;

                foreach (XAttribute attr in attributes)
                {
                    attr.SetValue(IMAN_specificationRef);
                }
            }

            //list1 =
            //    from Item in xmlFile.Elements(df + "GNM8_CADItemRevision")
            //    join ImanRel in xmlFile.Elements(df + "ImanRelation") on (string)Item.Attribute("puid").Value equals (string)ImanRel.Attribute("primary_object")
            //    join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
            //    where Dataset.Attribute("object_type").Value == "CATDrawing"
            //             || Dataset.Attribute("object_type").Value == "UGPART"
            //             || Dataset.Attribute("object_type").Value == "UGMASTER"
            //             || Dataset.Attribute("object_type").Value == "CATProduct"
            //             || Dataset.Attribute("object_type").Value == "CATPart"
            //    select ImanRel;

            //foreach (XElement el in list1)
            //{
            //    el.Attribute("relation_type").SetValue(IMAN_specificationRef);
            //}

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

            //var datasets = from dataset in xmlFile.Elements(xmlFile.GetDefaultNamespace() + "Dataset")
            //               join uds in UsedDatasets on dataset.Attribute("puid").Value equals uds[0]
            //               select new { Dataset = dataset, newName = uds[1] };


            //foreach (var el in datasets)
            //{
            //    el.Dataset.SetAttributeValue("object_name", el.newName);
            //}

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

        public static void GenerateLog(string path, IEnumerable<Classes.Item> items, IEnumerable<Classes.Revision> revisions, int refItem, int TotalDatasets, int refIR, int BrokenIMANS, int TotalIMANS, TimeSpan duration, bool Report)
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
                writer.WriteLine("\tTotal Reference -> CAD Items     : " + refItem);
                writer.WriteLine("\tTotal Reference -> CAD Revisions : " + refIR);
                writer.WriteLine("Warnings:");
                //writer.WriteLine("\tRecursive Datasets               : " + RecursiveDatasets);
                writer.WriteLine("\tCAD Items with no revisions      : " + orphanItemCount);
                writer.WriteLine("\tOrphan Revisions                 : " + revisions.Where(x => x.ItemID == "" && x.ItemTag == "").Count());
                //writer.WriteLine("\tOrphan Datasets                  : " + OrphanDatasets);
                writer.WriteLine("Errors:");
                writer.WriteLine("\tRevisions with missing Items     : " + orphanRevs);
                if (!Report)
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
                writer.WriteLine("\tProduction      : " + (items.Where(x => x.ObjectType == "Production").Count() - refItem));
                writer.WriteLine("\tPartialProcMatl : " + items.Where(x => x.ObjectType == "PartialProcMatl").Count());
                writer.WriteLine("\tPrototype       : " + items.Where(x => x.ObjectType == "Prototype").Count());
                writer.WriteLine("\tReference       : " + (items.Where(x => x.ObjectType == "Reference").Count() + refItem));
                writer.WriteLine("\tStandardPart    : " + items.Where(x => x.ObjectType == "StandardPart").Count());
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine();
                writer.WriteLine("\tTotal source Revisions : " + revisions.Count());
                writer.WriteLine();
                writer.WriteLine("\t\t[Breakdown]");
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine("\tProduction Revision      : " + (revisions.Where(x => x.ObjectType == "Production Revision").Count() - refIR));
                writer.WriteLine("\tPartialProcMatl Revision : " + revisions.Where(x => x.ObjectType == "PartialProcMatl Revision").Count());
                writer.WriteLine("\tPrototype Revision       : " + revisions.Where(x => x.ObjectType == "Prototype Revision").Count());
                writer.WriteLine("\tReference Revision       : " + (revisions.Where(x => x.ObjectType == "Reference Revision").Count() + refIR));
                writer.WriteLine("\tStandardPart Revision    : " + revisions.Where(x => x.ObjectType == "StandardPart Revision").Count());
                writer.WriteLine("\t--------------------------------");
                writer.WriteLine("________________________________________________");

            }
        }

        internal static void GeneratePartRenumFile(string path, IEnumerable<Classes.Revision> MasterRevs, bool Make)
        {

            if (!Make)
                return;

            var list = (from el in MasterRevs
                       //where el.ObjectType != "Reference Revision"
                        select string.Join(",", el.ItemTag, el.PUID, el.OldItemID, "=\"" + el.OldRevID + "\"", el.ItemID, "=\"" + el.RevID + "\"", el.Ref2CAD)).ToList();

            list.Insert(0, "Item UID, Rev UID, Old ItemID, Old RevID, New ItemId, New RevID, Ref2CAD");
            

            File.WriteAllLines(Path.Combine(path, "PartRenum.csv"), list);

        }
    }

}

