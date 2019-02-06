using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RssMonitorBot
{
    [Serializable]
    public class UserDetails
    {
        public bool AuthValid = true;
        public long UserId;
        public long ChatId;
    }

    [Serializable]
    public class RssUrlEntry
    {
        public string Url;
        public string[] Keywords;
    }

    [Serializable]
    public class UserRssSubscriptions
    { 
        public List<RssUrlEntry> RssEntries; 
    }

    [Serializable]
    public class UserMuteState
    {
        public bool Muted;
        public bool Stopped;
    }


    [Serializable]
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        private const string DefaultItemTag = "item";
        private const string DefaultKeyTag = "key";
        private const string DefaultValueTag = "value";
        private static readonly XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
        private static readonly XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

        public SerializableDictionary()
        {
        }

        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected virtual string ItemTagName => DefaultItemTag;

        protected virtual string KeyTagName => DefaultKeyTag;

        protected virtual string ValueTagName => DefaultValueTag;

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;

            reader.Read();
            if (wasEmpty)
            {
                return;
            }

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    this.ReadItem(reader);
                    reader.MoveToContent();
                }
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var keyValuePair in this)
            {
                this.WriteItem(writer, keyValuePair);
            }
        }

        private void ReadItem(XmlReader reader)
        {
            reader.ReadStartElement(this.ItemTagName);
            try
            {
                this.Add(this.ReadKey(reader), this.ReadValue(reader));
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        private TKey ReadKey(XmlReader reader)
        {
            reader.ReadStartElement(this.KeyTagName);
            try
            {
                return (TKey)keySerializer.Deserialize(reader);
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        private TValue ReadValue(XmlReader reader)
        {
            reader.ReadStartElement(this.ValueTagName);
            try
            {
                return (TValue)valueSerializer.Deserialize(reader);
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        private void WriteItem(XmlWriter writer, KeyValuePair<TKey, TValue> keyValuePair)
        {
            writer.WriteStartElement(this.ItemTagName);
            try
            {
                this.WriteKey(writer, keyValuePair.Key);
                this.WriteValue(writer, keyValuePair.Value);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        private void WriteKey(XmlWriter writer, TKey key)
        {
            writer.WriteStartElement(this.KeyTagName);
            try
            {
                keySerializer.Serialize(writer, key);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        private void WriteValue(XmlWriter writer, TValue value)
        {
            writer.WriteStartElement(this.ValueTagName);
            try
            {
                valueSerializer.Serialize(writer, value);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }
    }

    [Serializable]
    public class UserFeedPubDates
    {
        public SerializableDictionary<string, DateTime> PubDates; // feed URI as a key
    }
}
