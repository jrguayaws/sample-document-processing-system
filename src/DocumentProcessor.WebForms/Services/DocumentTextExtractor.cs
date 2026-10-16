using System;
using System.IO;
using System.Text;
using DocumentProcessor.WebForms.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocumentProcessor.WebForms.Services
{
    public class DocumentTextExtractor
    {
        private readonly BedrockOptions _options;

        public DocumentTextExtractor(IOptions<BedrockOptions> bedrockOptions)
        {
            _options = bedrockOptions.Value;
        }

        public static bool CanExtract(string extension)
        {
            if (extension == null)
            {
                return false;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                case ".txt":
                case ".log":
                    return true;
                default:
                    return false;
            }
        }

        public string Extract(string fileExtension, Stream content)
        {
            switch (fileExtension)
            {
                case ".pdf":
                    return ExtractFromPdf(content);
                case ".txt":
                case ".log":
                    return ExtractFromText(content);
                default:
                    throw new NotSupportedException(
                        "Cannot extract text from '" + fileExtension + "' files.");
            }
        }

        private string ExtractFromPdf(Stream content)
        {
            var maxPages = _options.MaxPdfPages;
            var maxCharacters = _options.MaxInputCharacters;

            var text = new StringBuilder();

            using (var pdf = PdfDocument.Open(content))
            {
                var page = 1;
                foreach (Page current in pdf.GetPages())
                {
                    text.AppendLine(ContentOrderTextExtractor.GetText(current));

                    if (page >= maxPages || text.Length >= maxCharacters)
                    {
                        break;
                    }

                    page++;
                }
            }

            return Truncate(text.ToString(), maxCharacters);
        }

        private string ExtractFromText(Stream content)
        {
            using (var reader = new StreamReader(content))
            {
                return Truncate(reader.ReadToEnd(), _options.MaxInputCharacters);
            }
        }

        private static string Truncate(string text, int maxCharacters)
        {
            return text.Length > maxCharacters ? text.Substring(0, maxCharacters) : text;
        }
    }
}
