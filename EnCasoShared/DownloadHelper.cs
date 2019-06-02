using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EnCasoShared
{
    public class DownloadHelper
    {
        public static readonly int BufferSize = 4096;

        public static async Task<byte[]> CreateDownloadTask(string urlToDownload, IProgress<DownloadBytesProgress> progessReporter)
        {
            int receivedBytes = 0;
            int totalBytes = 0;

            WebClient client = new WebClient();
            using (var stream = await client.OpenReadTaskAsync(urlToDownload))
            {
                byte[] buffer = new byte[BufferSize];
                totalBytes = Int32.Parse(client.ResponseHeaders[HttpResponseHeader.ContentLength]);
                var file = new byte[totalBytes];

                for (; ; )
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        await Task.Yield();
                        break;
                    }
                    Array.Copy(buffer, 0, file, receivedBytes, bytesRead);

                    receivedBytes += bytesRead;
                    if (progessReporter != null)
                    {
                        DownloadBytesProgress args = new DownloadBytesProgress(urlToDownload, receivedBytes, totalBytes);
                        progessReporter.Report(args);
                    }
                }
                return file;
            }


        }
    }
}
