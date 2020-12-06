using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;

namespace Cabinet
{
    public static class Paths
    {
        public static string LOGO => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "logo.ico");
        public static string LOADING => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png"); // TODO: switch to actual loading image
        public static string MISSING => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png"); // TODO: switch to actual missing image
        public static string UNAUTHORIZED => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png"); // TODO: switch to actual unauthorized image
        public static string[] ICONS => Directory.GetFiles(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons"), @"*.png");
        public static string ICON_PATH(string name) => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons", name + ".png");
        public static string LOCAL_IMAGE_CLIP_PATH(string name) => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Clips", name); // TODO: create method to optimize folder by merging duplicate files
    }

    public static class Recent
    {
        public static readonly int ID = -1;
        public static readonly string NAME = "Recent";
        public static readonly string ICON_PATH = "recent.png";

        public static readonly string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
    }

    public static class ColorSet
    {
        public static readonly string ERROR = "#FF9C0404";
        public static readonly string LOADING = "#FF0592A8";

        public static readonly string CLIPBOARD_LABEL_BG = "#FF818181";
        public static readonly string CLIPBOARD_BORDER = "#FF666666";
    }

    public enum LoadStatus
    {
        UNLOADED,
        LOADING,
        LOADED
    }

    public static class ClipboardObjectUtils
    {
        public static ClipboardObject CreateClipboardObjectByType(MainWindow parentWindow, long id, string name, string type, string content)
        {
            switch (type)
            {
                case "TextClipboardObject":
                    return new TextClipboardObject(parentWindow, id, name, content);

                case "ImageClipboardObject":
                    return new ImageClipboardObject(parentWindow, id, name, content);

                case "FileDropListClipboardObject":
                    return new FileDropListClipboardObject(parentWindow, id, name, content);

                default:
                    return null;
            }
        }
    }
}