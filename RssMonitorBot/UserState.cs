using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace RssMonitorBot
{
    class UserState<T>
        where T : class, new()
    {
        private long _userId;

        public T Data { get; private set; }

        private UserState(long userId)
        {
            _userId = userId;
        }

        public static bool ExistsFor(long userId)
        {
            return File.Exists(
                Path.Combine(
                    Configuration.SERVER_ROOT,
                    userId.ToString(),
                    typeof(T).Name
                    ));
        }

        public static void Drop(long userId)
        {
            File.Delete(Path.Combine(
                    Configuration.SERVER_ROOT,
                    userId.ToString(),
                    typeof(T).Name
                    ));
        }

        public static UserState<T> Load(long userId)
        {
            T data = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                var file = Path.Combine(
                    Configuration.SERVER_ROOT, 
                    userId.ToString(),
                    typeof(T).Name
                    );

                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    data = (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                data = null;
            }

            return data != null ? new UserState<T>(userId) { Data = data } : null;
        }

        public static UserState<T> LoadOrDefault(long userId)
        {
            UserState<T> ret = Load(userId);
            if (ret == null)
            {
                ret = new UserState<T>(userId) { Data = new T() };
            }
            return ret;
        }


        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            Directory.CreateDirectory(
                Path.Combine(
                    Configuration.SERVER_ROOT,
                    _userId.ToString()));

            var file = Path.Combine(
                    Configuration.SERVER_ROOT,
                    _userId.ToString(),
                    typeof(T).Name
                    );

            using (TextWriter writer = new StreamWriter(file))
            {
                serializer.Serialize(writer, Data);
            }
        }

        public void Drop() => Drop(_userId);
    }
}
