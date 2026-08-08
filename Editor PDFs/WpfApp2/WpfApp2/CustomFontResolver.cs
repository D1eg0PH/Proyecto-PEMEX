using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System;
using System.IO;
using System.Linq;

public class CustomFontResolver : IFontResolver
{
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        string fontFileName;

        if (isBold && isItalic)
        {
            if (familyName.ToLower() == "arial")
                fontFileName = "arialbi.ttf";
            else
                fontFileName = "arialbi.ttf"; // Fallback
        }
        else if (isBold)
        {
            if (familyName.ToLower() == "arial")
                fontFileName = "arialbd.ttf";
            else
                fontFileName = "arialbd.ttf"; // Fallback
        }
        else if (isItalic)
        {
            if (familyName.ToLower() == "arial")
                fontFileName = "ariali.ttf";
            else
                fontFileName = "ariali.ttf"; // Fallback
        }
        else
        {
            if (familyName.ToLower() == "arial")
                fontFileName = "arial.ttf";
            else
                fontFileName = "arial.ttf"; // Fallback
        }

        return new FontResolverInfo(fontFileName);
    }

    public byte[] GetFont(string faceName)
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), faceName);
        if (File.Exists(fontPath))
        {
            return File.ReadAllBytes(fontPath);
        }
        throw new FileNotFoundException($"No se encontró el archivo de fuente: {faceName}");
    }
}