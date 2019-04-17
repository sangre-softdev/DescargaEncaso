using DescargaEnCaso;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace EnCasoShared.Model
{
    public class RssEnCaso : LocalTableId, IEqualityComparer<RssEnCaso>
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime PubDate { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public static async Task<RssEnCaso[]> GetRssEnCasoAsync(bool first)
        {
            List<RssEnCaso> list = new List<RssEnCaso>();
            using (XmlReader xmlReader = XmlReader.Create("http://www.canaltrans.com/podcast/rssaudio.xml", new XmlReaderSettings() { Async = true }))
            {
                await xmlReader.MoveToContentAsync();
                while (xmlReader.ReadToFollowing("item"))
                {
                    xmlReader.ReadToFollowing("title");
                    string tit = xmlReader.ReadInnerXml();
                    xmlReader.ReadToFollowing("description");
                    string des = xmlReader.ReadInnerXml();
                    xmlReader.ReadToFollowing("pubDate");
                    DateTime pub;
                    try
                    { pub = DateTime.ParseExact(xmlReader.ReadInnerXml(), "ddd, dd MMM yyyy HH:mm:ss K", System.Globalization.CultureInfo.InvariantCulture); }
                    catch { pub = DateTime.Now; };
                    xmlReader.ReadToFollowing("guid");
                    string gui = xmlReader.ReadInnerXml();
                    xmlReader.ReadToFollowing("googleplay:image");
                    xmlReader.MoveToFirstAttribute();
                    string ima = xmlReader.Value;

                    list.Add(new RssEnCaso()
                    {
                        Title = tit,
                        Description = des,
                        PubDate = pub,
                        Url = gui,
                        ImageUrl = ima
                    });

                    if (first)
                    {
                        break;
                    }
                }
            }
            return list.ToArray();
        }

        public bool Equals(RssEnCaso x, RssEnCaso y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check whether the products' properties are equal.
            return x.Title == y.Title
                && x.Url == y.Url;
        }

        public int GetHashCode(RssEnCaso obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashRssEnCasoTitle = obj.Title == null ? 0 : obj.Title.GetHashCode();
            int hashRssEnCasoUrl = obj.Url == null ? 0 : obj.Url.GetHashCode();
            
            //Calculate the hash code for the product.
            return hashRssEnCasoTitle ^ hashRssEnCasoUrl;
        }
    }
}
