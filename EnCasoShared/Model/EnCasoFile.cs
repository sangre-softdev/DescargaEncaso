using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnCasoShared.Model
{
    public class EnCasoFile : LocalTableId, IEqualityComparer<EnCasoFile>
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime PubDate { get; set; }

        public string ImageUrl { get; set; }

        public string SavedFile { get; set; }

        public DateTime DownloadDateTime { get; set; }

        public bool Equals(EnCasoFile x, EnCasoFile y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode(EnCasoFile obj)
        {
            throw new NotImplementedException();
        }
    }
}
