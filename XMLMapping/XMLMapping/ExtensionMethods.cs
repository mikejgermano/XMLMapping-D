using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using XMLMapping;


namespace ExtensionMethods
{

    //************************ EXTENSION METHODS***********************************//
    static class Extensions
    {
        public static SearchElements Filter(this SearchElements source, String AttrName, String AttrValue)
        {
            if (source == null)
                return null;

            String[] Params = { AttrName, AttrValue };
            //Console.WriteLine("<Sequence method='" + GetCurrentMethod(source, Params) + "'>");

            if (source.SearchList.Count() == 0)
            {
               // Console.WriteLine("<Error>");
               // Console.WriteLine("<Message>" + "There are no nodes named [" + source.getParams()[0] + "] in the xml document." + "</Message>");
                //Console.WriteLine("</Error>");
                //Console.WriteLine("</Sequence>");
                return null;
            }


            IEnumerable<XElement>  list1 =
                    from el in source.SearchList
                    where el.Attribute(AttrName).Value == AttrValue
                    select el;

            


            //Console.WriteLine("<completed />");
           // Console.WriteLine("</Sequence>");
            return new SearchElements(list1.ToList<XElement>(), HelperUtility.GetCurrentMethod(), Params);
        }

        public static SearchElements Traverse(this SearchElements source,String SrcAttrName, String RelNodeName, String RelAttrName)
        {
            String[] Params = { RelAttrName, RelNodeName, RelAttrName };
          //  Console.WriteLine("<Sequence method='" + GetCurrentMethod(source, Params) + "'>");

            if (source.SearchList.Count() == 0)
            {
                return null; 
            }


            IEnumerable<XElement> list1 = null;

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(SrcAttrName);
                string srcAttrValue = att.Value;

                list1 =
                    from elT in HelperUtility.GetXML().Descendants()
                    where elT.Name.LocalName.Equals(RelNodeName) &&
                      elT.Attribute(RelAttrName).Value == srcAttrValue
                    select elT;

            }
          
            return  new SearchElements(list1.ToList<XElement>(),HelperUtility.GetCurrentMethod(), Params);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod(SearchElements s, String[] Params)
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            string joined = s.getMethod() + "(" + string.Join(",", s.getParams()) + ").";
            joined += sf.GetMethod().Name + "(" + string.Join(",", Params) + ")";
            return joined;
        }


        /// <summary>
        /// Changes attribute value text to upper case
        /// </summary>
        /// <param name="AttributeName">The attribute to use</param>
        /// <returns></returns>
        public static void ToUpperValue(this SearchElements source, String AttributeName)
        {
            String[] Params = { AttributeName };
            //Console.WriteLine("<Sequence method='" + GetCurrentMethod(source, Params) + "'>");

            if (source.SearchList.Count() == 0)
            {
                return;
            }

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(AttributeName);

                if (att == null)
                {
                    return;
                }
                att.SetValue(el.Attribute(AttributeName).Value.ToUpper());
            }

        }

        /// <summary>
        /// Changes Attribute Value text to lower case.
        /// </summary>
        /// <param name="AttributeName">The Attribute to use</param>
        /// <returns></returns>
        public static void ToLowerValue(this SearchElements source, String AttributeName)
        {
            String[] Params = { AttributeName };

            if (source.SearchList.Count() == 0)
            {
                return;
            }

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(AttributeName);

                if (att == null)
                {
                    return;
                }
                att.SetValue(el.Attribute(AttributeName).Value.ToLower());
            }

        }

        /// <summary>
        /// Adds Attribute to element
        /// </summary>
        /// <param name="AttributeName">The attribute to use</param>
        /// <param name="Value">The value of the attribute</param>
        public static void SetAttribute(this SearchElements source, String AttributeName, String Value)
        {
            String[] Params = { AttributeName, Value };
            if (source == null) return;
            foreach (XElement el in source.SearchList)
            {
                el.SetAttributeValue(AttributeName, Value);
            }
        }

        public static void AddAttribute(this SearchElements source, XElement el, String AttributeName, String Value)
        {
            String[] Params = { AttributeName, Value };

            el.SetAttributeValue(AttributeName, Value);

        }

        /// <summary>
        /// Removes the attribute from the Element
        /// </summary>
        /// <param name="AttributeName">The Name of the attribute</param>
        public static void RemoveAttribute(this SearchElements source, String AttributeName)
        {
            String[] Params = { AttributeName };

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(AttributeName);

                if (att == null)
                {
                    return;
                }
                att.Remove();
            }
        }

        public static void RemoveAttribute(this SearchElements source, XElement el, String AttributeName)
        {
            String[] Params = { AttributeName };


            XAttribute att = el.Attribute(AttributeName);

            if (att == null)
            {
                return;
            }
            att.Remove();

        }

        public static void RenameAttribute(this SearchElements source, String OldAttributeName, String NewAttributeName)
        {
            String[] Params = { OldAttributeName, NewAttributeName };

            if (source.SearchList.Count() == 0)
            {
                return;
            }

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(OldAttributeName);

                if (att == null)
                {
                    return;
                }
                String oldValue = att.Value;
                AddAttribute(source, el, NewAttributeName, oldValue);
                RemoveAttribute(source, el, OldAttributeName);
            }

        }

        public static void RenameNodes(this SearchElements source, String NewNodeName)
        {
            String[] Params = { NewNodeName };

            if (source.SearchList.Count() == 0)
            {
                return;
            }

            foreach (XElement el in source.SearchList)
            {
                el.Name = el.Parent.GetDefaultNamespace() + NewNodeName;
            }

        }

        public static void RemoveNodes(this SearchElements source)
        {
            String[] Params = { "" };

            if (source.SearchList.Count() == 0)
            {
                return;
            }

            foreach (XElement el in source.SearchList)
            {
                el.Remove();
            }

        }

      
        public static void CopyAttribute(this SearchElements source, String AttributeName, String NewAttributeName)
        {
            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(AttributeName);

                if (att == null)
                {
                    return;
                }
                string value = att.Value;

                el.SetAttributeValue(NewAttributeName, value);
            }

        }

        /// <summary>
        /// Trims the attribute to a specified length.
        /// </summary>
        /// <param name="AttributeName">Name of the Attribute to Trim</param>
        /// <param name="Length">the max length of the attribute</param>
        public static void TrimAttributeLength(this SearchElements source, String AttributeName, byte Length)
        {

            foreach (XElement el in source.SearchList)
            {
                XAttribute att = el.Attribute(AttributeName);

                if (att == null)
                {
                    return;
                }

                if (att.Value.Length >= Length)
                {
                    att.Value = att.Value.Substring(0, Length);
                }
            }
        }
    }
}
