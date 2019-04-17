using DescargaEnCaso;
using EnCasoShared.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EnCasoShared
{
    public class LocalTableId
    {
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int Id { get; set; }
    }
    
    public class LocalDatabase<T> : SQLiteConnection where T : LocalTableId, new()
    {
        private static LocalDatabase<RssEnCaso> rssEnCaso;
        private static LocalDatabase<EnCasoFile> enCasoFile;
        static readonly object locker = new object();

        public static string DatabaseFilePath
        {
            get
            {
#if NETFX_CORE
				var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, Consts.DATABASE_RSS);
#else

#if SILVERLIGHT
				// Windows Phone expects a local path, not absolute
				var path = Consts.DATABASE_RSS;
#else

#if __ANDROID__
                // Just use whatever directory SpecialFolder.Personal returns
                string libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
#else
				// we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
				// (they don't want non-user-generated data in Documents)
				string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
				string libraryPath = Path.Combine (documentsPath, "../Library/"); // Library folder
#endif
                var path = Path.Combine(libraryPath, General.LOCAL_DATABASE_ENCASO);
#endif

#endif
                return path;
            }
        }

        public static LocalDatabase<RssEnCaso> getRssEnCasoDb()
        {
            if (rssEnCaso == null)
                rssEnCaso = new LocalDatabase<RssEnCaso>(LocalDatabase<RssEnCaso>.DatabaseFilePath);
            return rssEnCaso;
        }

        public static LocalDatabase<EnCasoFile> getEnCasoFileDb()
        {
            if (enCasoFile == null)
                enCasoFile = new LocalDatabase<EnCasoFile>(LocalDatabase<EnCasoFile>.DatabaseFilePath);
            return enCasoFile;
        }


        private LocalDatabase(string path) : base(path)
        {
            // create the tables
            CreateTable<T>();
        }

        public T[] GetAll()
        {
            lock (locker)
            {                
                return (from i in Table<T>() select i).ToArray();
            }
        }

        public T GetById(int id)
        {
            lock (locker)
            {
                return Table<T>().FirstOrDefault(x => x.Id == id);
            }
        }

        public int Save(T item)
        {
            lock (locker)
            {
                if (item.Id != 0)
                {
                    Update(item);
                    return item.Id;
                }
                else
                {
                    return Insert(item);
                }
            }
        }

        public int SaveAll(IEnumerable<T> items)
        {
            int count = 0;
            foreach (T item in items)
            {
                Save(item);
                count++;
            }
            return count;
        }

        public int Delete(T item)
        {
            lock (locker)
            {
                return Delete<T>(item.Id);
            }
        }
    }
}
