using Android.Util;
using DescargaEnCaso;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

        public string AudioBoomUrl { get; set; }

        public string ImageUrl { get; set; }

        public static async Task<RssEnCaso[]> GetRssEnCasoAsync(bool first, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }
            List<RssEnCaso> list = new List<RssEnCaso>();
            using (XmlReader xmlReader = XmlReader.Create("http://www.canaltrans.com/podcast/rssaudio.xml", new XmlReaderSettings() { Async = true }))
            {
                await xmlReader.MoveToContentAsync();
                xmlReader.ReadToFollowing("item");

                var rss = new RssEnCaso();

                while (await xmlReader.ReadAsync())
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();

                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.IsStartElement())
                    {
                        switch (xmlReader.Name)
                        {
                            case "title":
                                rss.Title = await xmlReader.ReadElementContentAsStringAsync();
                                break;

                            case "description":
                                rss.Description = await xmlReader.ReadElementContentAsStringAsync();
                                break;

                            case "pubDate":
                                var pub = DateTime.Now;
                                var pubString = await xmlReader.ReadElementContentAsStringAsync();
                                if (!DateTime.TryParseExact(pubString, "ddd, d MMM yyyy HH:mm:ss K", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out pub))
                                {
                                    pub = DateTime.Now;
                                }
                                rss.PubDate = pub;
                                break;

                            case "media:content":
                                string urlValue = string.Empty;
                                string typeValue = string.Empty;
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    switch (xmlReader.Name)
                                    {
                                        case "url": urlValue = xmlReader.Value; break;
                                        case "type": typeValue = xmlReader.Value; break;
                                    }
                                }
                                switch (typeValue)
                                {
                                    case "audio/mpeg": rss.AudioBoomUrl = urlValue; break;
                                    case "image/jpg": rss.ImageUrl = urlValue; break;
                                }
                                break;

                            case "guid":
                                rss.Url = await xmlReader.ReadElementContentAsStringAsync();
                                break;

                            case "item":
                                list.Add(rss);
                                rss = new RssEnCaso();

                                if (first) { break; }
                                break;
                            default:
                                break;
                        }
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
            if (x is null || y is null)
            {
                return false;
            }

            //Check whether the products' properties are equal.
            return x.Title == y.Title
                && x.Url == y.Url;
        }

        public int GetHashCode(RssEnCaso obj)
        {
            //Check whether the object is null
            if (obj is null) return 0;

            //Get hash code for the Name field if it is not null.
            int hashRssEnCasoTitle = obj.Title == null ? 0 : obj.Title.GetHashCode();
            int hashRssEnCasoUrl = obj.Url == null ? 0 : obj.Url.GetHashCode();

            //Calculate the hash code for the product.
            return hashRssEnCasoTitle ^ hashRssEnCasoUrl;
        }
    }
}
