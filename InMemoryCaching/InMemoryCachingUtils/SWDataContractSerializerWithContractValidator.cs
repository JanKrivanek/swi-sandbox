using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SolarWinds.InMemoryCachingUtils
{
    public class SWDataContractSerializerWithContractValidator<T> : SWDataContractSerializer<T>
    {
        protected override void Validate(Stream xmlReader)
        {
            XsdDataContractExporter xsdExp = new XsdDataContractExporter();
            xsdExp.Export(typeof(T));
            XmlSchemaSet xsdSet = xsdExp.Schemas;

            var xDoc = XDocument.Load(xmlReader);
            try
            {
                xDoc.Validate(xsdSet, null);
            }
            catch (Exception e)
            {
                StringWriter writer = new StringWriter();
                XmlSchema xsdSchema = xsdSet.Schemas(null).Cast<XmlSchema>().First();
                xsdSchema.Write(writer);
                writer.Flush();
                string xsdString = writer.ToString();

                throw new XmlValidationException(string.Format("Error validating xml against xsd.{0}{1}{0}AdjustedXml:{0}{2}{0}Validating xsd:{0}{3}{0}",
                    Environment.NewLine, e.Message, xDoc.ToString(), xsdString), e);
            }
        }
    }
}