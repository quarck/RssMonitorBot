using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace RssMonitorBot
{
    class PersistentState<T>
        where T: class, new()
    {
        private T _data = null;
        private string _saveFile = null;

        public PersistentState(string saveFile)
        {
            _saveFile = saveFile;
            _data = Load(_saveFile) ?? new T();
        }

        public T Data => _data;

        private static T Load(string file)
        {
            T ret = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    ret = (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                ret = null;
            }

            return ret;
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextWriter writer = new StreamWriter(_saveFile))
            {
                serializer.Serialize(writer, _data);
            }
        }
    }
}
