using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;

namespace XMLStorageTypes
{
    public class Config
    {
        public string SourcePath;
        public string TargetPath;
        public string OutputPath;
        public ushort MaxSplitRsIPS;
        public ushort MaxSplitPcIPS;
        public HashSet<FilesEnum> Reports = new HashSet<FilesEnum>();

        public enum FilesEnum
        {
            Log,
            ReleaseStatusIPS,
            ItemRenum,
            MissingItems,
            DatasetFailures,
            OrphanDatasets,
            RecursiveDatasets,
            RevisionImport,
            ReferenceToCAD,
            DatasetParamCodeIPS
        }

        public Config(string path)
        {
            XElement config = XElement.Load(path);

            SourcePath = (config.Descendants("SourceFiles").Single().Attribute("path").Value == "") ? null : config.Descendants("SourceFiles").Single().Attribute("path").Value;
            TargetPath = (config.Descendants("TargetFiles").Single().Attribute("path").Value == "") ? null : config.Descendants("TargetFiles").Single().Attribute("path").Value;
            OutputPath = (config.Descendants("OutputFiles").Single().Attribute("path").Value == "") ? null : config.Descendants("OutputFiles").Single().Attribute("path").Value;
            MaxSplitRsIPS = ushort.Parse((config.Descendants("ReleaseStatusIPS").Single().Attribute("max").Value == "") ? null : config.Descendants("ReleaseStatusIPS").Single().Attribute("max").Value);
            MaxSplitPcIPS = ushort.Parse((config.Descendants("DatasetParamCodeIPS").Single().Attribute("max").Value == "") ? null : config.Descendants("DatasetParamCodeIPS").Single().Attribute("max").Value);

            #region Report Files

            if (config.Attribute("log").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.Log);
            }

            if (config.Descendants("ReleaseStatusIPS").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.ReleaseStatusIPS);
            }

            if (config.Descendants("DatasetParamCodeIPS").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.DatasetParamCodeIPS);
            }

            if (config.Descendants("ItemRenum").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.ItemRenum);
            }

            if (config.Descendants("ReferenceToCad").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.ReferenceToCAD);
            }

            if (config.Descendants("MissingItems").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.MissingItems);
            }

            if (config.Descendants("DatasetFailures").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.DatasetFailures);
            }

            //******************DISABLED******************************
            if (config.Descendants("OrphanDatasets").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                //Reports.Add(FilesEnum.OrphanDatasets);
            }
            //********************************************************

            //******************DISABLED******************************
            if (config.Descendants("RecursiveDatasets").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                //Reports.Add(FilesEnum.RecursiveDatasets);
            }
            //********************************************************

            if (config.Descendants("RevisionImport").Single().Attribute("make").Value.ToUpper() == "YES")
            {
                Reports.Add(FilesEnum.RevisionImport);
            }

            #endregion
        }

        public bool IsMade(FilesEnum item)
        {
            if (Reports.Contains(item))
                return true;

            return false;
        }
    }

    public class Classes
    {
        public class Item
        {
            public string PUID;
            public string ItemID;
            public string ObjectType;


            public Item(string mPUID, string mItemID, string mObjectType)
            {
                PUID = mPUID;
                ItemID = mItemID;
                ObjectType = mObjectType;
            }
        }

        public class Group
        {
            public string GroupRef;
            public string GroupName;

            public Group(string mGroupRef, string mGroupName)
            {
                GroupName = mGroupName;
                GroupRef = mGroupRef;
            }
        }

        public class ReleaseStatus
        {
            public string PUID;
            public string Name;

            public ReleaseStatus(string mPUID, string mName)
            {
                PUID = mPUID;
                Name = mName;
            }
        }

        public class Revision
        {
            private string _itemID;

            public string ItemID
            {
                get
                {
                    string mitemID = (_itemID == null) ? "" : _itemID;
                    return mitemID;
                }

            }

            public string PUID;
            public string RevID;
            public string ItemTag;
            public string GroupRef;
            public string ObjectType;
            public string ReleaseStatusList;
            public DateTime CreationDate;

            private Dictionary<string, Classes.Dataset> _datasets = new Dictionary<string, Classes.Dataset>();

            public IEnumerable<Classes.Dataset> GetDatasets()
            {
                var dataset = from d in this._datasets.Values
                              select d;

                return dataset;
            }


            public void AddDataset(string mPUID, string mType, string mName, string revChain)
            {
                if (!_datasets.ContainsKey(mPUID))
                {
                    _datasets.Add(mPUID, new Dataset(mPUID, mType, this.ItemTag, mName, revChain));
                }
            }

            public void AddDataset(Classes.Dataset ds)
            {
                if (!_datasets.ContainsKey(ds.PUID))
                {
                    _datasets.Add(ds.PUID, ds);
                }
            }

            private string _owningGroup;

            public string OwningGroup
            {
                get
                {
                    return _owningGroup;
                }

            }

            private string _releaseStatusName;

            public string ReleaseStatusNames
            {
                get
                {
                    string mReleaseStatusName = (_releaseStatusName == null) ? "" : _releaseStatusName;
                    return mReleaseStatusName;
                }

            }

            public Revision(string puid, string mRevID, string mObjectType, string mItemTag, string mGroupRef, string mReleaseStatusList, DateTime mCreationDate)
            {
                PUID = puid;
                RevID = mRevID;
                ObjectType = mObjectType;
                ItemTag = mItemTag;
                GroupRef = mGroupRef;
                ReleaseStatusList = mReleaseStatusList;
                CreationDate = mCreationDate;
            }

            public void Map()
            {
                if (this.ReleaseStatusList == "")
                    return;

                switch (this.ObjectType)
                {
                    case "Production Revision":
                    case "PartialProcMatl Revision":
                    case "StandardPart Revision":
                        {
                            if (this.ReleaseStatusNames.Contains("Released") || this.ReleaseStatusNames.Contains("EAD_Approved"))
                            {
                                this.SetStatus("GNM8_ProductionReleased");
                            }
                            break;
                        }
                    case "Prototype Revision":
                        {
                            if (this.ReleaseStatusNames.Contains("Released"))
                            {
                                this.SetStatus("GNM8_PrototypeReleased");
                            }
                            break;
                        }
                    case "Reference Revision":
                        {
                            this.SetStatus("GNM8_Frozen");
                            break;
                        }
                    default:
                        {
                            this.SetStatus("GNM8_Frozen");
                            break;
                        }
                }


                if (this.ReleaseStatusNames == "Baseline")
                    this.SetStatus("GNM8_Frozen");

                //Second Exception
                switch (this.ObjectType)
                {
                    case "Production Revision":
                        {
                            if (this.OwningGroup.Contains("PG1"))
                            {
                                if (this.ItemID.ToUpper().ToUpper().StartsWith("AA") == false && this.ItemID.ToUpper().StartsWith("MX") == false)
                                    this.SetStatus("GNM8_Frozen");

                                if (this.ItemID.ToUpper().StartsWith("AW063600-"))
                                    this.SetStatus("GNM8_ProductionReleased");
                            }
                            else if (this.OwningGroup.Contains("PG3"))
                            {
                                if (!this.ItemID.ToUpper().StartsWith("TN") && !this.ItemID.ToUpper().StartsWith("MX") && !this.ItemID.ToUpper().StartsWith("TD"))
                                    this.SetStatus("GNM8_Frozen");

                                if ((this.ItemID.ToUpper().StartsWith("TN") || this.ItemID.ToUpper().StartsWith("MX") || this.ItemID.ToUpper().StartsWith("TD")) && this.ReleaseStatusNames.Contains("Released"))
                                    this.SetStatus("GNM8_ProductionReleased");
                            }
                            break;
                        }
                    case "Prototype Revision":
                        {
                            if (this.OwningGroup.Contains("PG1"))
                            {
                                if (this.ItemID.ToUpper().StartsWith("AA") == false && this.ItemID.ToUpper().StartsWith("MX") == false)
                                    this.SetStatus("GNM8_Frozen");

                                if (this.ItemID.ToUpper().StartsWith("AW5-063600-"))
                                    this.SetStatus("GNM8_PrototypeReleased");
                            }
                            else if (this.OwningGroup.Contains("PG3"))
                            {
                                if (!this.ItemID.ToUpper().StartsWith("TN") && !this.ItemID.ToUpper().StartsWith("MX") && !this.ItemID.ToUpper().StartsWith("TD"))
                                    this.SetStatus("GNM8_Frozen");

                                if (this.ItemID.ToUpper().StartsWith("TN") || this.ItemID.ToUpper().StartsWith("MX") || this.ItemID.ToUpper().StartsWith("TD"))
                                    this.SetStatus("GNM8_PrototypeReleased");
                            }
                            break;
                        }
                }

                if (this.OwningGroup.Contains("PG2"))
                {
                    SetStatus("GNM8_Frozen");
                }

                if (!this.ReleaseStatusNames.Contains("GNM8_"))
                    SetStatus("GNM8_Frozen");

                this._itemID = (this.ItemID == null) ? null : this.ItemID.ToUpper();
            }

            public void SetGroupName(List<Classes.Group> groups)
            {
                var gName = (from g in groups
                             where g.GroupRef == this.GroupRef.Remove(0, 1)
                             select g.GroupName);

                if (gName.Count() == 1)
                    _owningGroup = gName.Single();
            }

            public void SetItemID(IEnumerable<Classes.Item> items)
            {
                var item_id = (from i in items
                               where i.PUID == this.ItemTag
                               select i.ItemID);

                if (item_id.Count() == 1)
                {
                    _itemID = item_id.Single();
                }
            }

            public void SetStatus(string newStatus)
            {
                this._releaseStatusName = newStatus;
            }

            public void RenumberItemID(string mNewVal)
            {
                _itemID = mNewVal;
            }

            public void SetStatusNames(List<Classes.ReleaseStatus> statuses)
            {
                string[] rList = this.ReleaseStatusList.Split(',');

                if (rList[0] == "")
                    return;

                HashSet<string> rsNames = new HashSet<string>();

                foreach (string rs in rList)
                {
                    var rName = (from s in statuses
                                 where s.PUID == rs
                                 select s.Name);

                    if (rName.Count() != 0)
                        rsNames.Add(rName.Single());
                }

                _releaseStatusName = string.Join("|", rsNames);
            }

        }

        public class IMANRelation
        {
            public string PUID;
            public string Primary;
            public string Secondary;
            public string TypeRef;

            public IMANRelation(string mPUID, string mPrimary, string mSecondary, string mTypeRef)
            {
                PUID = mPUID;
                Primary = mPrimary;
                Secondary = mSecondary;
                TypeRef = mTypeRef.Remove(0, 1);
            }
        }

        public class IMANType
        {
            public string ID;
            public string Type;

            public IMANType(string mID, string mType)
            {
                ID = mID;
                Type = mType;
            }
        }

        public class Dataset
        {
            public string PUID;
            public string Type;
            public string ParentUID;
            public string Name;
            public string Rev_chain_anchor;
            public string Revisions;
            public string RelationType;
           

            public static string MappedRelation(string type, string value)
            {
                switch (type)
                {
                    case "UGPART":
                    case "CATDrawing":
                    case "UGMaster":
                    case "CATProduct":
                    case "CATPART":
                        {
                            if ((value == "IMAN_external_object_link" || value == "catia_auxiliaryLink") && type == "CATDrawing")
                            {
                                return value;
                            }

                            return "IMAN_specification";
                        }
                    case "UGALTREP":
                        {
                            return "IMAN_UG_altrep";
                        }
                    case "CATSHAPE":
                        {
                            return "catia_alternateShapeRep";
                        }
                }

                return value;
            }


            public Dataset(string mPUID, string mType, string mParentUID, string mName, string mRev_chain_anchor)
            {
                PUID = mPUID;
                Type = mType;
                ParentUID = mParentUID;
                Name = mName;
                Rev_chain_anchor = mRev_chain_anchor;
            }
        }

        public class RevisionAnchor
        {
            public string PUID;
            public string Revisions;

            public RevisionAnchor(string mPUID, string mRevisions)
            {
                PUID = mPUID;
                Revisions = mRevisions;
            }
        }
    }

}
