using System;
using Windows.UI.Xaml;

namespace PdfReader.Classes {
    class PageType {
        public string Name { get; set; }
        public string Img = "Assets/Icons/paper.png";
        public string Path { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
