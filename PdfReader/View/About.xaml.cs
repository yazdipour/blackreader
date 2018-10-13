using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PdfReader.Classes;

namespace PdfReader {
    public sealed partial class About : Page {
        public About() {
            this.InitializeComponent();
        }

        private async void OtherApp(object sender,RoutedEventArgs e) {
            if((sender as Button).Content.ToString().Contains("OTHER"))
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:Publisher?name=Yazdipour"));
            else
                await Windows.System.Launcher.LaunchUriAsync(new Uri("http://shahriar.in/voices/"));
        }

        private async void Button_Click(object sender,RoutedEventArgs e) {
            var button = sender as Button;
            if(button == null)
                return;
            var tag = button.Content?.ToString().ToLower();
            if(tag.Contains("rate")) {
                LocalSettingManager.SaveSetting(App.SettingStrings["rate"],"[done]");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:REVIEW?PFN=3783mindprojects.BlackReader_6c8ydbw054cyy"));
            }
            else if(tag.Contains("note"))
                await new UpdateNote().ShowAsync();
            else {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:shahriar.yazdipour@outlook.com?subject=BlackReader Err Rep "+ VersionLabel.Text));
            }
        }
    }
}
