using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using static CDA_Downloader.Video;

namespace CDA_Downloader
{
    public partial class MainWindow : Window
    {
        string videoPattern = "https?:\\/\\/(www\\.)?cda.pl\\/video\\/[a-z0-9]{9}";
        string folderPattern = "https?:\\/\\/(www\\.)?cda.pl\\/([0-9]|[A-Z]|[a-z])+\\/folder\\/[0-9]{8}";

        ObservableCollection<Video> toDownloadList, downloadableList;
        ObservableCollection<string> toSearchList;

        public MainWindow()
        {
            InitializeComponent();

            toDownloadList = new ObservableCollection<Video>();
            downloadableList = new ObservableCollection<Video>();
            toSearchList = new ObservableCollection<string>();

            toDownloadListView.ItemsSource = toDownloadList;
            downloadableListView.ItemsSource = downloadableList;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(downloadableListView.ItemsSource);
            view.Filter = QualityFilter;

            if (!Directory.Exists("CDA_Downloader"))
                Directory.CreateDirectory("CDA_Downloader");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            webbrowser.Close();
        }

        private void TextBox_KeyEnter(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                root.IsEnabled = false;

                var url = ((TextBox)sender).Text;
                if (Regex.IsMatch(url, videoPattern))
                {
                    SearchVideos(Regex.Match(url, videoPattern).Value);
                }
                else if (Regex.IsMatch(url, folderPattern))
                {
                    webbrowser.Navigate(Regex.Match(url, folderPattern).Value);
                }
            }
        }

        private void Szukaj_Click(object sender, RoutedEventArgs e)
        {
            var kea = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter);
            TextBox_KeyEnter(urlText, kea);
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).DataContext as Video;
            if (!toDownloadList.Contains(item))
                toDownloadList.Add(item);
            else
                toDownloadList.Remove(item);
        }

        private async void webbrowser_DOMContentLoaded(object sender, WebViewControlDOMContentLoadedEventArgs e)
        {
            if (Regex.IsMatch(e.Uri.ToString(), videoPattern/*"\\?wersja=((360p)|(480p)|(720p)|(1080p))"*/))
            {
                var videoExists = await webbrowser.InvokeScriptAsync("eval", new string[] {
                    "document.querySelector('.text-alert').innerText ? 'false' : 'true';"
                });
                if (videoExists == "true")
                {
                    var videoUrl = await webbrowser.InvokeScriptAsync("eval", new string[] {
                        "document.querySelector('video').src;"
                    });
                    var title = await webbrowser.InvokeScriptAsync("eval", new string[] {
                        "document.querySelector('.title-name > span').innerHTML;"
                    });
                    var quality = Regex.Match(e.Uri.ToString(), "(360p)|(480p)|(720p)|(1080p)").Value;

                    var video = new Video(title, Regex.Match(e.Uri.ToString(), videoPattern).Value);
                    video.VideoUrl = videoUrl;
                    video.Id = Regex.Match(e.Uri.ToString(), "[a-z0-9]{9}").Value;

                    switch (quality)
                    {
                        case "360p":
                            video.Quality = EQuality.LQ;
                            break;
                        case "720p":
                            video.Quality = EQuality.HD;
                            break;
                        case "1080p":
                            video.Quality = EQuality.FHD;
                            break;
                        default:
                            video.Quality = EQuality.SD;
                            break;
                    }

                    if (!downloadableList.Contains(video))
                        downloadableList.Add(video);

                    if (toSearchList.Count > 0)
                        toSearchList.RemoveAt(0);
                }
            }
            else if (Regex.IsMatch(e.Uri.ToString(), folderPattern))
            {
                var videos = await webbrowser.InvokeScriptAsync("eval", new string[] {
                    "var videos = '';document.querySelectorAll('#folder-replace .thumb-wrapper-just .caption-label a').forEach(a => videos += a.href + ' ');videos.trim();"
                });

                foreach(var url in videos.Split(' '))
                {
                    if (q360.IsChecked.Value)
                        toSearchList.Add(url + "?wersja=360p");
                    if (q480.IsChecked.Value)
                        toSearchList.Add(url + "?wersja=480p");
                    if (q720.IsChecked.Value)
                        toSearchList.Add(url + "?wersja=720p");
                    if (q1080.IsChecked.Value)
                        toSearchList.Add(url + "?wersja=1080p");
                }
            }

            if (toSearchList.Count > 0)
            {
                webbrowser.Navigate(toSearchList[0]);
                return;
            }

            root.IsEnabled = true;
        }

        private void webbrowser_Loaded(object sender, RoutedEventArgs e)
        {
            tabs.SelectedIndex = 0;
            root.IsEnabled = true;
        }

        private bool QualityFilter(object item)
        {
            var video = item as Video;
            if (video.Quality == EQuality.LQ && q360.IsChecked.Value)
                return true;
            else if (video.Quality == EQuality.SD && q480.IsChecked.Value)
                return true;
            else if (video.Quality == EQuality.HD && q720.IsChecked.Value)
                return true;
            else if (video.Quality == EQuality.FHD && q1080.IsChecked.Value)
                return true;
            else
                return false;
        }

        private void quality_Click(object sender, RoutedEventArgs e)
        {
           CollectionViewSource.GetDefaultView(downloadableListView.ItemsSource).Refresh();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (toDownloadList.Count > 0)
            {
                downloadProgress.Visibility = Visibility.Visible;
                downloadProgress.Maximum = toDownloadList.Count * 2;
                root.IsEnabled = false;
                DownloadVideo(toDownloadList[0]);
            }
        }

        private void DownloadVideo(Video video)
        {
            if (!Directory.Exists("CDA_Downloader/" + video.QualityS))
                Directory.CreateDirectory("CDA_Downloader/" + video.QualityS);

            using (WebClient client = new WebClient())
            {
                downloadProgress.Value += 1;
                client.DownloadFileAsync(new Uri(video.VideoUrl), $"./CDA_Downloader/{video.QualityS}/{video.Name}.mp4");
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            downloadProgress.Value += 1;
            toDownloadList.RemoveAt(0);
            if (toDownloadList.Count > 0)
                DownloadVideo(toDownloadList[0]);
            else
            {
                root.IsEnabled = true;
                downloadProgress.Visibility = Visibility.Hidden;
            }
        }

        private void SearchVideos(string url)
        {
            if (q360.IsChecked.Value)
                toSearchList.Add(url + "?wersja=360p");
            if (q480.IsChecked.Value)
                toSearchList.Add(url + "?wersja=480p");
            if (q720.IsChecked.Value)
                toSearchList.Add(url + "?wersja=720p");
            if (q1080.IsChecked.Value)
                toSearchList.Add(url + "?wersja=1080p");

            if(toSearchList.Count > 0)
                webbrowser.Navigate(toSearchList[0]);
        }
    }
}
