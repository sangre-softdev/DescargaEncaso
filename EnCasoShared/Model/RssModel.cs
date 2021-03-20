using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnCasoShared.Model
{
    /// <summary>
    /// /////////////////////////////////////HAY QUE ELIMINAR ESTO DESPUÉS, ES SOLO RETROCOMPATIBILIDAD
    /// </summary>
    public class RssModel : LocalTableId
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime PubDate { get; set; }

        [Indexed]
        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string SaveFile { get; set; }

        public DateTime downloadDateTime { get; set; }
    }
}
