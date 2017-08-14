using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Xml.Linq;
using System.IO;
using com.LandonKey.OrderByNatural;

namespace PodcastRss
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             * year
             * site url
             * email
             * name
             * image url
             * title
             * category
             * subcat
             * keywords
             * publish date
             * description
             * description (max 255 char)
             * items xml
             */

            var categoryID = ConfigurationManager.AppSettings["CategoryID"];
            var siteURL = ConfigurationManager.AppSettings["SiteURL"];
            var email = ConfigurationManager.AppSettings["Email"];
            var name = ConfigurationManager.AppSettings["Name"];
            var imageURL = ConfigurationManager.AppSettings["ImageURL"];
            var title = ConfigurationManager.AppSettings["Title"];
            var description = ConfigurationManager.AppSettings["Description"];
            var category = ConfigurationManager.AppSettings["Category"];
            var subcategory = ConfigurationManager.AppSettings["SubCategory"];
            var keywords = ConfigurationManager.AppSettings["Keywords"];
            var feedURL = ConfigurationManager.AppSettings["FeedURL"];

            var items = GetBlobs(ConfigurationManager.AppSettings["ContainerName"])
                .Where(x => x.StorageUri.PrimaryUri.ToString().EndsWith("mp3"))
                .OrderByNatural(x => x.Name)
                .Select(x => string.Format(ITEM_TEMPLATE.Trim(),
                    x.Name,
                    x.Name,
                    x.Name.Length > 255 ? x.Name.Substring(0, 254) : x.Name,
                    categoryID,
                    x.StorageUri.PrimaryUri.ToString().Replace(" ", "%20"),
                    "",
                    x.Properties.LastModified));

            var xml = (string.Format(RSS_TEMPLATE.Trim(),
                DateTime.Now.Year,
                siteURL,
                email,
                name,
                imageURL,
                title,
                category,
                subcategory,
                keywords,
                DateTime.Now.ToLongDateString(),
                description,
                description.Length > 255 ? description.Substring(0, 254) : description,
                string.Join("", items),
                feedURL));

            var doc = XDocument.Parse(xml);

            Console.Write(doc.ToString());

            File.WriteAllText("feed.xml", doc.ToString());
        }
        public static List<CloudBlockBlob> GetBlobs(string containerName)
        {
            var blobs = new List<CloudBlockBlob>();
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            foreach (var item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    var blob = (CloudBlockBlob)item;
                    blobs.Add(blob);
                }
            }

            return blobs;
        }

        private static string RSS_TEMPLATE = @"
            <?xml version=""1.0"" encoding=""utf-8""?>
            <rss xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:itunes=""http://www.itunes.com/dtds/podcast-1.0.dtd"" xmlns:itunesu=""http://www.itunesu.com/feed"" version=""2.0"">
              <channel>
                <link>{1}</link>
                <language>en-us</language>
                <copyright>&#xA9;{0}</copyright>
                <webMaster>{2} ({3})</webMaster>
                <managingEditor>{2} ({3})</managingEditor>
                <image>
                  <url>{4}</url>
                  <title>{5}</title>
                  <link>{1}</link>
                </image>
                <itunes:owner>
                  <itunes:name>{3}</itunes:name>
                  <itunes:email>{2}</itunes:email>
                </itunes:owner>
                <itunes:category text=""{6}"">
                  <itunes:category text=""{7}"" />
                </itunes:category>
                <itunes:keywords>{8}</itunes:keywords>
                <itunes:explicit>no</itunes:explicit>
                <itunes:image href=""{4}"" />
                <atom:link href=""{13}"" rel=""self"" type=""application/rss+xml"" />
                <pubDate>{9}</pubDate>
                <title>{5}</title>
                <itunes:author>{3}</itunes:author>
                <description>{10}</description>
                <itunes:summary>{10}</itunes:summary>
                <itunes:subtitle>{11}</itunes:subtitle>
                <lastBuildDate>{9}</lastBuildDate>
                {12}
              </channel>
            </rss>";

        private static string ITEM_TEMPLATE = @"
                <item>
                  <title>{0}</title>
                  <description>{1}</description>
                  <itunes:summary>{1}</itunes:summary>
                  <itunes:subtitle>{2}</itunes:subtitle>
                  <itunesu:category itunesu:code=""{3}"" />
                  <enclosure url=""{4}"" type=""audio/mpeg"" length=""1"" />
                  <guid>{4}</guid>
                  <itunes:duration>{5}</itunes:duration>
                  <pubDate>{6}</pubDate>
                </item>";
    }
}
