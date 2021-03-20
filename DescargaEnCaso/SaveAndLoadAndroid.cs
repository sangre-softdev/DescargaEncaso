using System.Threading.Tasks;
using System.IO;
using EnCasoShared;
using System;
namespace DescargaEnCaso
{
    public class SaveAndLoadFiles_Android : ISaveAndLoadFiles
    {
        #region ISaveAndLoad implementation

        public string SaveByteAsync(string filename, byte[] file)
        {
            var path = CreatePathToFile(filename);
            File.WriteAllBytes(path, file);
            return path;
        }

        public async Task<byte[]> LoadByteAsync(string filename)
        {
            byte[] result;
            using (FileStream SourceStream = File.Open(filename, FileMode.Open))
            {
                result = new byte[SourceStream.Length];
                await SourceStream.ReadAsync(result, 0, (int)SourceStream.Length);
                SourceStream.Close();
            }
            return result;
        }

        public byte[] LoadByte(string filename)
        {   
            byte[] result;
            using (FileStream SourceStream = File.Open(filename, FileMode.Open))
            {
                result = new byte[SourceStream.Length];
                SourceStream.Read(result, 0, (int)SourceStream.Length);
                SourceStream.Close();
            }
            return result;
        }

        public bool FileExists(string filename)
        {
            return File.Exists(CreatePathToFile(filename));
        }

        #endregion

        private string CreatePathToFile(string filename)
        {
            string docsPath;
            docsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).Path;
            docsPath += "/EnCasoPrograms/";
            if (!Directory.Exists(docsPath))
            {
                Directory.CreateDirectory(docsPath);
            }
            return Path.Combine(docsPath, filename);
        }
    }
}