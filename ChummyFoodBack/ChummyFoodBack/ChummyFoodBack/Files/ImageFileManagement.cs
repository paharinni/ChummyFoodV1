using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace ChummyFoodBack.Files
{
    public class ImageFileManagement
    {
        private readonly string _basePath;

        public ImageFileManagement(IWebHostEnvironment webHostEnvironment)
        {
            _basePath = webHostEnvironment.WebRootPath;
        }

        public string GenerateFilePath(string fileName)
            => Path.Combine(_basePath, fileName);


        public async Task<byte[]> ReadImageStream(Stream fileStream)
        {
            fileStream.Seek(0, SeekOrigin.End);
            var streamLength = fileStream.Position + 1;
            fileStream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[streamLength];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
