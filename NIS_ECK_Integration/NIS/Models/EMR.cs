using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NIS.Models;
using System.Data;
using System.Xml;
using System.IO;
using NIS.WebService;

namespace NIS.Models
{
    public class EMR
    {

        private XmlElement xmlAddChild(XmlElement parent, String name, String value)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
            return element;
        }

        private XmlElement xmlAddChild(XmlElement parent, String name)
        {
            XmlElement element = parent.OwnerDocument.CreateElement(name);
            parent.AppendChild(element);
            return element;
        }

        private XmlDocument createSuccessXmlDoc()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("result");
            element.SetAttribute("status", "success");
            doc.AppendChild(element);
            xmlAddChild(element, "code", "1");
            xmlAddChild(element, "note", "成功");
            return doc;
        }
    }
}
