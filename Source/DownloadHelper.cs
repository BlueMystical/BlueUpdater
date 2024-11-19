using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UtilConsole;

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
		public static void UnzipFile(string zipFilePath, string extractPath, bool excludeContainingFolder)
		{
			try
			{
				if (File.Exists(zipFilePath))
				{
					using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
					{
						Directory.CreateDirectory(extractPath);

						// Find the root folder in the ZIP file
						string rootFolder = archive.Entries
							.Where(e => e.IsDirectory())
							.Select(e => e.FullName.TrimEnd('/'))
							.FirstOrDefault();

						foreach (ZipArchiveEntry entry in archive.Entries)
						{
							string destinationPath = excludeContainingFolder && entry.FullName.StartsWith(rootFolder)
								? Path.Combine(extractPath, entry.FullName.Substring(rootFolder.Length + 1))
								: Path.Combine(extractPath, entry.FullName);

							if (!entry.IsDirectory())
							{
								// Rename existing files
								if (File.Exists(destinationPath))
								{
									string backupPath = destinationPath + ".old";
                                    File.Delete(destinationPath + ".old");
									File.Move(destinationPath, backupPath);
									//Console.WriteLine($"Renamed existing file {destinationPath} to {backupPath}");
								}

								// Ensure the directory exists
								Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

								// Extract the new file
								entry.ExtractToFile(destinationPath, overwrite: true);
								//Console.WriteLine($"Extracted {destinationPath}");
							}
						}
					}
				}
				else
				{
					throw new Exception($"Error 404 - Not Found\r\n'{zipFilePath}'");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}", ConsoleColor.Red);
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
