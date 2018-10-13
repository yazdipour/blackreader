using System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;

namespace PdfReader.Classes {
    static class Helper {
        public static SolidColorBrush Convert_HexColor(string color) {
            if(!color.Contains("#"))
                color = "#" + color;
            if(color.Length > 7) {
                var A = Convert.ToByte(color.Substring(1,2),16);
                var R = Convert.ToByte(color.Substring(3,2),16);
                var G = Convert.ToByte(color.Substring(5,2),16);
                var B = Convert.ToByte(color.Substring(7,2),16);
                return new SolidColorBrush(Color.FromArgb(A,R,G,B));
            }
            if(color.Length > 4) {
                var R = Convert.ToByte(color.Substring(1,2),16);
                var G = Convert.ToByte(color.Substring(3,2),16);
                var B = Convert.ToByte(color.Substring(5,2),16);
                return new SolidColorBrush(Color.FromArgb(255,R,G,B));
            }
            throw new Exception();
        }
        public static void ClearMyBack(Windows.UI.Xaml.Controls.Frame insideFrame) {
            try { insideFrame.BackStack.Clear();}
            catch { }
            GarbageCollector();
        }
        public static async void Color_StatusBar(Color veryDarkGray,Color cl1,Color cl2) {
            var statusBar = StatusBar.GetForCurrentView();
            statusBar.BackgroundColor = veryDarkGray;
            statusBar.ForegroundColor = cl1;
            statusBar.ProgressIndicator.ProgressValue = 0;
            statusBar.BackgroundOpacity = 1;
            await statusBar.ShowAsync();
        }
        public static async void Hide_StatusBar() {
            var statusBar = StatusBar.GetForCurrentView();
            await statusBar.HideAsync();
            //statusBar.BackgroundOpacity = 1;
        }

        public static void Visibility_TitleBar(bool visible) {
            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = !visible;
        }
        public static void Color_BtnTitleBar() {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = titleBar.BackgroundColor = Convert_HexColor("#33222222").Color;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveBackgroundColor = Colors.DimGray;
            titleBar.ButtonInactiveBackgroundColor = Colors.DimGray;
        }
        public static void Visibility_BackButton(bool view,SystemNavigationManager back) {
            back.AppViewBackButtonVisibility = !view ? AppViewBackButtonVisibility.Collapsed : AppViewBackButtonVisibility.Visible;
        }
        public static void GarbageCollector() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
