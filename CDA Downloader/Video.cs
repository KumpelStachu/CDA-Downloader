using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDA_Downloader
{
    class Video
    {
        public string Id { get; set; }
        public string Name { get; set; }
        private string _Url;
        public string Url {
            get => _Url + "?wersja=" + (int)Quality + "p";
            set { _Url = value; }
        }
        public string VideoUrl { get; set; }
        public EQuality Quality { get; set; }
        public string QualityS { get { return (int)Quality + "p"; } }

        public Video(string name, string url, EQuality quality)
        {
            Name = name;
            Url = url;
            Quality = quality;
        }

        public Video(string name, string url)
        {
            Name = name;
            Url = url;
        }

        public enum EQuality
        {
            LQ = 360,
            SD = 480,
            HD = 720,
            FHD = 1080
        }
    }
}
