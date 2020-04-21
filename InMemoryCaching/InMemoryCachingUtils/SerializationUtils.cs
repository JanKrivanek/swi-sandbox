using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SolarWinds.InMemoryCachingUtils
{
    public class SerializationUtils
    {
        /// <summary>
        /// Serializes object to byte array using DataContractSerializer
        /// </summary>
        /// <typeparam name="T">type to be serialized</typeparam>
        /// <param name="obj">object to be serialized</param>
        /// <returns>binary representation of the data</returns>
        public static byte[] SerializeUsingDataContractSerializer<T>(T obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {

                new DataContractSerializer(typeof(T)).WriteObject(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes object from byte array using DataContractSerializer
        /// </summary>
        /// <typeparam name="T">type to be deserialized</typeparam>
        /// <param name="data">the binary data to be deserialized</param>
        /// <param name="validateXmlAgainstContract">indication whether strong type validation should be performed first</param>
        /// <returns>The deserialized data</returns>
        public static T DeserializeUsingDataContractSerializer<T>(byte[] data, bool validateXmlAgainstContract = false)
        {
            using (var ms = new MemoryStream(data))
            {
                return (T)DeserializeUsingDataContractSerializer(ms, validateXmlAgainstContract, typeof(T));
            }
        }

        public static void ValidateXmlAgainstDataContractSerializer(Stream sortedXmlReader, Type expectedType)
        {
            XsdDataContractExporter xsdExp = new XsdDataContractExporter();
            xsdExp.Export(expectedType);
            XmlSchemaSet xsdSet = xsdExp.Schemas;

            var xDoc = XDocument.Load(sortedXmlReader);
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

        public static void ValidateXmlAgainstDataContractSerializer<T>(Stream sortedXmlReader)
        {
            ValidateXmlAgainstDataContractSerializer(sortedXmlReader, typeof(T));
        }

        private static object DeserializeUsingDataContractSerializer(Stream reader, bool validateXmlAgainstContract,
            Type expectedType)
        {
            var dcs = new DataContractSerializer(expectedType);
            if (validateXmlAgainstContract)
            {
                ValidateXmlAgainstDataContractSerializer(reader, expectedType);
            }

            reader.Position = 0;
            return dcs.ReadObject(reader);
        }
    }

    [Serializable]
    public class XmlValidationException : Exception
    {
        public XmlValidationException() { }
        public XmlValidationException(string message) : base(message) { }
        public XmlValidationException(string message, Exception inner) : base(message, inner) { }
        protected XmlValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
