using System;
using System.Linq;
using Windows.System.Profile;
using Windows.UI.Xaml;
using PdfReader.Classes;

namespace PdfReader {
    public sealed partial class Settings {
        public Settings() {
            InitializeComponent();
        }
        private void ContentDialog_Loaded(object sender,RoutedEventArgs e) {
            IsNightMode.IsOn = LocalSettingManager.ExistsSetting(App.SettingStrings["night"]);
            if (AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Desktop")) {
                FindName("IsZoomText");
                FindName("IsZoom");
                IsZoom.IsOn = LocalSettingManager.ExistsSetting(App.SettingStrings["zoom"]);
            }
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["bgColor"]))
                cb.BorderBrush = Helper.Convert_HexColor(LocalSettingManager.ReadSetting(App.SettingStrings["bgColor"]));
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]))
                ViewBox.SelectedIndex = 0;
        }
        private void SaveSetting(object sender,RoutedEventArgs e) {
            if(IsNightMode.IsOn)
                LocalSettingManager.SaveSetting(App.SettingStrings["night"],"true");
            else
                LocalSettingManager.RemoveSetting(App.SettingStrings["night"]);
            if (AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Desktop")) {
                if (IsZoom.IsOn)
                    LocalSettingManager.SaveSetting(App.SettingStrings["zoom"], "true");
                else
                    LocalSettingManager.RemoveSetting(App.SettingStrings["zoom"]);
            }
            if(ViewBox.SelectedIndex == 0)
                LocalSettingManager.SaveSetting(App.SettingStrings["view"],"true");
            else
                LocalSettingManager.RemoveSetting(App.SettingStrings["view"]);
            if(!Frame.CanGoBack)
                return;
            var prev = Frame.BackStack.LastOrDefault();
            if(prev.SourcePageType == typeof(SingleViewer) && ViewBox.SelectedIndex != 0) {
                var param = prev.Parameter;
                Frame.BackStack.Remove(prev);
                Frame.Navigate(typeof(Viewer),param);
            }
            else if(prev.SourcePageType == typeof(Viewer) && ViewBox.SelectedIndex == 0) {
                var param = prev.Parameter;
                Frame.BackStack.Remove(prev);
                Frame.Navigate(typeof(SingleViewer), param);
            }
            else
                Frame.GoBack();
            Helper.GarbageCollector();
        }
        private async void ButtonBase_OnClick(object sender,RoutedEventArgs e) {
            var defaultColor = "#ffffff";
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["bgColor"])) {
                defaultColor = LocalSettingManager.ReadSetting(App.SettingStrings["bgColor"]);
            }
            var cl = new ChooseColor(defaultColor);
            await cl.ShowAsync();
            var ret = cl.Tag.ToString();
            cb.BorderBrush = Helper.Convert_HexColor(ret);
            LocalSettingManager.SaveSetting(App.SettingStrings["bgColor"],ret);
        }
    }
}
