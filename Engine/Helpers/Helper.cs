using System.IO.Compression;

namespace Engine.Helpers
{
    internal static class Helper
    {
        public static async Task<bool> DownloadAndUnzipFile(string url, string filename)
        {
            if (File.Exists(filename))
            {
                Console.WriteLine("File already exists, skipping download");
                return true;
            }

            var tempFile = Path.GetTempFileName();
            try
            {
                // Create a temp file to download the zip file
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fileStream = new FileStream(tempFile, FileMode.Create))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                }

                // Unzip the file
                using (var archive = ZipFile.OpenRead(tempFile))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Name != filename) continue;
                        entry.ExtractToFile(filename, true);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading data: " + ex.Message);
                return false;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}