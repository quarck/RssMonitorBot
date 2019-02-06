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
        private static string UsersFolder = "users";
        private static string UsersFolderFullPath = Path.Combine(Configuration.SERVER_ROOT, UsersFolder);

        private long _userId;

        public T Data { get; private set; }

        private UserState(long userId)
        {
            _userId = userId;
        }

        public static IEnumerable<KeyValuePair<long, UserState<T>>> EnumerateAllIndexed(Func<long,bool> idFilter = null)
        {
            foreach (var dir in Directory.EnumerateDirectories(UsersFolderFullPath))
            {
                var name = Path.GetFileName(dir);
                if (long.TryParse(name, out var userId))
                {
                    if (idFilter != null && !idFilter(userId))
                        continue;

                    var data = Load(userId);
                    yield return new KeyValuePair<long, UserState<T>>(userId, data);
                }
            }
        }

        public static IEnumerable<UserState<T>> EnumerateAll(Func<long, bool> idFilter = null)
        {
            if (!Directory.Exists(UsersFolderFullPath))
                yield break;

            foreach (var dir in Directory.EnumerateDirectories(UsersFolderFullPath))
            {
                var name = Path.GetFileName(dir);
                if (long.TryParse(name, out var userId))
                {
                    if (idFilter != null && !idFilter(userId))
                        continue;

                    var data = Load(userId);
                    if (data != null)
                    {
                        yield return data;
                    }
                }
            }
        }

        public static bool ExistsFor(long userId)
        {
            return File.Exists(
                Path.Combine(
                    Configuration.SERVER_ROOT,
                    UsersFolder,
                    userId.ToString(),
                    typeof(T).Name
                    ));
        }

        public static void Drop(long userId)
        {
            File.Delete(Path.Combine(
                    Configuration.SERVER_ROOT,
                    UsersFolder,
                    userId.ToString(),
                    typeof(T).Name
                    ));
        }

        public static UserState<T> Load(long userId)
        {
            var file = Path.Combine(
                Configuration.SERVER_ROOT,
                UsersFolder,
                userId.ToString(),
                typeof(T).Name
                );

            if (!File.Exists(file))
                return null;

            T data = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    data = (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); // parse failed 
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
                    UsersFolder,
                    _userId.ToString()));

            var file = Path.Combine(
                    Configuration.SERVER_ROOT,
                    UsersFolder,
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
