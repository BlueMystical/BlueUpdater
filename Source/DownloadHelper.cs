using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlueUpdater
{
    public static class DownloadHelper
	{
        private static readonly HttpClient client = new HttpClient();

        static DownloadHelper()
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHub API client)");
        }

        public static event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public static event EventHandler DownloadCompleted;
        public static event EventHandler<Exception> DownloadFailed;

        public static async Task DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        bool isMoreToRead = true;

                        do
                        {
                            int read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                                TriggerDownloadProgressChanged(totalRead, totalBytes);
                                continue;
                            }

                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            TriggerDownloadProgressChanged(totalRead, totalBytes);
                        }
                        while (isMoreToRead);
                    }
                }

                DownloadCompleted?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                DownloadFailed?.Invoke(null, ex);
            }
        }

        private static void TriggerDownloadProgressChanged(long totalRead, long totalBytes)
        {
            DownloadProgressChanged?.Invoke(null, new DownloadProgressEventArgs
            {
                TotalBytes = totalBytes,
                BytesRead = totalRead
            });
        }

        public static void UnzipFile(string zipFilePath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);

                    if (!entry.IsDirectory())
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
        }

        // Extension method to check if a ZipArchiveEntry is a directory
        private static bool IsDirectory(this ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/");
        }
    }

    public class DownloadProgressEventArgs : EventArgs
	{
		public long TotalBytes { get; set; }
		public long BytesRead { get; set; }
		public double ProgressPercentage => (double)BytesRead / TotalBytes * 100;
	}

}
