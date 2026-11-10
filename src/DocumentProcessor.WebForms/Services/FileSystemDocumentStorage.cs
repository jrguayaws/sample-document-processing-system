using System;
using System.IO;
using DocumentProcessor.WebForms.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace DocumentProcessor.WebForms.Services
{
    /// <summary>
    /// Stores uploads on the local file system under a date-partitioned path below
    /// App_Data (outside wwwroot, so the static-file middleware never serves them).
    /// </summary>
    public class FileSystemDocumentStorage : IDocumentStorage
    {
        private readonly string _rootPath;

        public FileSystemDocumentStorage(AppSettings settings, IWebHostEnvironment environment)
        {
            // HostingEnvironment.MapPath(Storage.RootPath) -> ContentRootPath-relative resolution.
            _rootPath = AppSettings.ResolveContentPath(environment.ContentRootPath, settings.StorageRootPath);
        }

        public string Save(Stream content, string fileName)
        {
            var now = DateTimeOffset.UtcNow;

            var uniqueName = Path.GetFileNameWithoutExtension(fileName)
                + "_" + now.ToString("yyyyMMddHHmmssfff")
                + Path.GetExtension(fileName);

            var relativePath = Path.Combine(
                now.Year.ToString("D4"),
                now.Month.ToString("D2"),
                now.Day.ToString("D2"),
                uniqueName);

            var fullPath = Path.Combine(_rootPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            using (var file = File.Create(fullPath))
            {
                content.CopyTo(file);
            }

            return relativePath;
        }

        public Stream OpenRead(string storagePath)
        {
            return File.OpenRead(Path.Combine(_rootPath, storagePath));
        }
    }
}
