using PdfSharp.Fonts;
using System;
using System.IO;

namespace WpfApp2
{
    public class CustomFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var name = familyName.ToLower();

            if (name.Contains("arial"))
            {
                if (isBold && isItalic) return new FontResolverInfo("arialbi.ttf");
                if (isBold) return new FontResolverInfo("arialbd.ttf");
                if (isItalic) return new FontResolverInfo("ariali.ttf");
                return new FontResolverInfo("arial.ttf");
            }

            // Fallback a Arial
            return new FontResolverInfo("arial.ttf");
        }

        public byte[] GetFont(string faceName)
        {
            var fontPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                faceName
            );

            if (File.Exists(fontPath))
                return File.ReadAllBytes(fontPath);

            throw new FileNotFoundException($"Fuente no encontrada: {faceName}");
        }
    }
}