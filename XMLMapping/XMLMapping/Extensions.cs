using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace XMLMapping
{

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

        ushort index;

        public void setIndex(ushort mIndex)
        {
            index = mIndex;
        }

        public ushort getIndex()
        {
            return index;
        }

        #region Upper Class Stuff
        public static XElement xmlFile = null;
        public SaveOptions Format;


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
            String[] Params = { AttributeName, SourceElementName, DestinationElementName, TraverseAttrSource, TraverseAttrDestination };


            var result = GetElementsBy(SourceElementName).SearchList;
            if (result.Count() == 0)
            {

                return;
            }

            foreach (XElement el in result)
            {
                XAttribute att = el.Attribute(AttributeName);
                if (att == null)
                {
                    continue;
                }
                XAttribute travSource = el.Attribute(TraverseAttrSource);
                if (travSource == null)
                {
                    continue;
                }
                SearchElements mEls = GetElementsBy(DestinationElementName, TraverseAttrDestination, travSource.Value);
                if (mEls.SearchList.Count == 0)
                {
                    continue;
                }

                mEls.SetAttribute(AttributeName, att.Value);
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
                  from el in xmlFile.Descendants()
                  where el.Name.LocalName.Equals(ElementName)
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

        //TODO custom change later**********************************************************************************************************************************
        public void AllOtherStatuses()
        {
            IEnumerable<XElement> list1 =
                 from el in xmlFile.Descendants()
                 where el.Attribute("object_type") != null &&
                 el.Attribute("object_type").Value != "Production" &&
                 el.Attribute("object_type").Value != "Production Revision" &&
                 el.Attribute("object_type").Value != "Prototype" &&
                 el.Attribute("object_type").Value != "Prototype Revision" &&
                 el.Attribute("object_type").Value != "Production Master" &&
                 el.Attribute("object_type").Value != "Production Revision Master" &&
                 el.Attribute("release_status_list") != null &&
                 el.Attribute("release_status_list").Value != "" &&
                 el.Name.LocalName != "Dataset"
                 select el;

            SearchElements elsS = new SearchElements(list1.ToList<XElement>(), GetCurrentMethod(), null);

            if (elsS.SearchList.Count() != 0)
                elsS
                   .Traverse("release_status_list", "ReleaseStatus", "puid")
                   .Filter("name", "Released")
                   .SetAttribute("name", "GNM8_Frozen");
        }

        public void RelationshipSwap()
        {
            IEnumerable<XElement> list1 =
                 from el in xmlFile.Descendants()
                 where el.Attribute("object_type") != null &&
                 el.Attribute("object_type").Value != "Production" &&
                 el.Attribute("object_type").Value != "Production Revision" &&
                 el.Attribute("object_type").Value != "Prototype" &&
                 el.Attribute("object_type").Value != "Prototype Revision" &&
                 el.Attribute("object_type").Value != "Production Master" &&
                 el.Attribute("object_type").Value != "Production Revision Master" &&
                 el.Attribute("release_status_list") != null &&
                 el.Attribute("release_status_list").Value != "" &&
                 el.Name.LocalName != "Dataset"
                 select el;

            SearchElements elsS = new SearchElements(list1.ToList<XElement>(), GetCurrentMethod(), null);

            if (elsS.SearchList.Count() != 0)
                elsS
                   .Traverse("release_status_list", "ReleaseStatus", "puid")
                   .Filter("name", "Released")
                   .SetAttribute("name", "GNM8_Frozen");
        }

        #region PartRenum
        public void PartReNum()
        {
            /* var list = GetElementsBy("GNM8_CADItem").SearchList;

             foreach (XElement el in list)
             {
                 el.Attribute("gnm8_dn_part_number").Value = el.Attribute("gnm8_dn_part_number").Value.Replace("-", "");
             }

             list = null;
             list = GetElementsBy("GNM8_CADItemRevision").SearchList;

             foreach (XElement el in list)
             {
                 XAttribute att = el.Attribute("gnm8_major_minor");
                 if (att == null)
                 {
                     continue;
                 }

                 att.Value = PartReNumRev(att.Value, el.Attribute("elemId").Value);
                 el.Attribute("gnm8_dn_part_number").Value = el.Attribute("gnm8_dn_part_number").Value.Replace("-", "");
             }*/

            XNamespace df = xmlFile.GetDefaultNamespace();

            IEnumerable<XElement> list =
                from el in xmlFile.Elements(df + "Item")
                where el.Attribute("object_type").Value != "Reference"
                select el;

            //index = 1; //NA000000001

            foreach (XElement el in list)
            {
                string newPartNum = "NA" + index.ToString("000000000");
                string oldPartNum = el.Attribute("item_id").Value;

                el.Attribute("item_id").Value = newPartNum;

                IEnumerable<XElement> Form =
                from c in xmlFile.Elements(df + "Form")
                where c.Attribute("object_name").Value.Contains(oldPartNum)
                select c;

                foreach (XElement c in Form)
                {
                    string newValue = c.Attribute("object_name").Value.Replace(oldPartNum, newPartNum);
                    c.Attribute("object_name").SetValue(newValue);
                }

                IEnumerable<XElement> Dataset =
                from c in xmlFile.Elements(df + "Dataset")
                where c.Attribute("object_name").Value.Contains(oldPartNum)
                select c;

                foreach (XElement c in Dataset)
                {
                    string newValue = c.Attribute("object_name").Value.Replace(oldPartNum, newPartNum);
                    c.Attribute("object_name").SetValue(newValue);
                }

                IEnumerable<XElement> PSBomView =
                from c in xmlFile.Elements(df + "PSBomView")
                where c.Attribute("object_name").Value.Contains(oldPartNum)
                select c;

                foreach (XElement c in PSBomView)
                {
                    string newValue = c.Attribute("object_name").Value.Replace(oldPartNum, newPartNum);
                    c.Attribute("object_name").SetValue(newValue);
                }

                IEnumerable<XElement> PSBOMViewRevision =
                from c in xmlFile.Elements(df + "PSBOMViewRevision")
                where c.Attribute("object_name").Value.Contains(oldPartNum)
                select c;

                foreach (XElement c in PSBOMViewRevision)
                {
                    string newValue = c.Attribute("object_name").Value.Replace(oldPartNum, newPartNum);
                    c.Attribute("object_name").SetValue(newValue);
                }

                index++;

            }

        }

        private string PartReNumRev(string mValue, string elemId)
        {


            string value = mValue;

            string p1 = "", p2 = "";

            if (value.Contains("-"))
            {
                p1 = SubstringBefore(value, "-") + "-";
            }


            if (p1 != "")
            {
                p2 = SubstringAfter(value, "-");
            }
            else
            {
                p2 = value;
            }
            decimal val;
            if (decimal.TryParse(p2, out val))
                p2 = val.ToString();
            else
                Console.WriteLine("Error with elemId: " + elemId);


            return p1 + p2;

        }
        #endregion


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

        public void IMANRelSwap()
        {
            string IMAN_specificationRef = "#" + GetSingleElementByAttrID("ImanType", "type_name", "IMAN_specification").Attribute("elemId").Value;
            string IMAN_manifestationRef = "#" + GetSingleElementByAttrID("ImanType", "type_name", "IMAN_manifestation").Attribute("elemId").Value;
            XNamespace df = xmlFile.GetDefaultNamespace();

            IEnumerable<XElement> list1 =
               from ImanRel in xmlFile.Elements(df + "ImanRelation")
               join Dataset in xmlFile.Elements(df + "Dataset") on (string)ImanRel.Attribute("secondary_object") equals (string)Dataset.Attribute("puid").Value
               where (Dataset.Attribute("object_type").Value == "CATDrawing" || Dataset.Attribute("object_type").Value == "UGPART") &&
                     ImanRel.Attribute("relation_type").Value == IMAN_manifestationRef
               select ImanRel;

            foreach (XElement el in list1)
            {
                el.Attribute("relation_type").SetValue(IMAN_specificationRef);
            }
        }


    }

}

