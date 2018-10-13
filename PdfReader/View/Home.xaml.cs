using PdfReader.Classes;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;

namespace PdfReader {
    class BurgerItems {
        public string Symbol { get; set; } = "\uE295";
        public string Label { get; set; }
        public Type ToGo
        {
            get;
            internal set;
        }

        public string Color = "White";
        public string SymColor = "White";
    }
    public sealed partial class Home : Page {
        private readonly SystemNavigationManager _backView = SystemNavigationManager.GetForCurrentView();
        #region LOAD
        public Home() {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Windows.ApplicationModel.Activation.IActivatedEventArgs;
            if(args?.Kind != Windows.ApplicationModel.Activation.ActivationKind.File)
                return;
            var fileArgs = args as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
            var fl = (StorageFile)fileArgs?.Files[0];
            if(fl == null)
                return;
            var fileCopy = await fl.CopyAsync(ApplicationData.Current.LocalFolder,
                fl.DisplayName + ".pdf",NameCollisionOption.ReplaceExisting);
            insideFrame.Navigate(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]) ? typeof(SingleViewer) : typeof(Viewer),fileCopy);
            Helper.GarbageCollector();
        }

        private void Page_Loaded(object sender,RoutedEventArgs e) {
            var burgerList = new List<BurgerItems> {
                new BurgerItems {Label="Black Reader", Symbol = "",SymColor="Transparent"},
                new BurgerItems {Label="Home", Symbol = "",ToGo = typeof(WelcomePage)},
                new BurgerItems {Label="Open new file...", Symbol = ""},
                new BurgerItems {Label="Recent", Symbol = ""},
                new BurgerItems {Label="Settings", Symbol = "",ToGo = typeof(Settings)},
                new BurgerItems {Label="Shortcuts", Symbol = "\uE764" ,ToGo = typeof(HowTo)},
                new BurgerItems {Label="About", Symbol = "" ,ToGo = typeof(About)},
            };
            lsv.ItemsSource = burgerList;
            insideFrame.Navigate(typeof(WelcomePage));
            ChangeStatusBarColor();
            TriggerBackButton();
            //FindName("listFiles");
            //FilesLsStatic = listFiles;
        }
        private void TriggerBackButton() {
            _backView.BackRequested += (sender,e) => {
                if(!insideFrame.CanGoBack)
                    return;
                e.Handled = true;
                insideFrame.GoBack();
            };
        }
        private void ChangeStatusBarColor() {
            if(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) {
                // StatusBar
                Helper.Hide_StatusBar();
            }
            else {
                // TitleBar
                Helper.Color_BtnTitleBar();
                Window.Current.SetTitleBar(mTitleBar);
            }
        }

        #endregion

        #region NAV        
        private void SetTitleOfInsideFrame(string txt) {
            TitleHeader.Text = txt.ToUpper();
        }
        private void MenuList_ItemClick(object sender,ItemClickEventArgs e) {
            var bi = e.ClickedItem as BurgerItems;
            var tag = bi?.Label;
            if(tag != null && tag.Contains("Black")) {
                SV.IsPaneOpen = !(SV.IsPaneOpen);
            }
            else {
                if (tag.Contains("Open")) {
                    OpenNewFile();
                }
                else if(tag.Contains("Recent")) {
                    OpenFlyout_RecentFiles();
                }
                else {
                    insideFrame.Navigate(bi?.ToGo);
                    SetTitleOfInsideFrame(tag);
                }
            }
        }
        private void insideFrame_Navigated(object sender,NavigationEventArgs e) {
            var tag = e.SourcePageType.Name;
            if(tag.Contains("Welcome")) {
                Helper.ClearMyBack(insideFrame);
                SetTitleOfInsideFrame("home");
                Helper.Visibility_BackButton(false,_backView);
                TitleBox.Visibility = mTitleBar.Visibility = Visibility.Visible;
                insideFrame.Margin = new Thickness(0,44,0,0);
                return;
            }
            SV.IsPaneOpen = false;
            Helper.Visibility_BackButton(true,_backView);
            if(tag.Contains("Viewer") || tag.Contains("How")) {
                TitleBox.Visibility = mTitleBar.Visibility = Visibility.Collapsed;
                insideFrame.Margin = new Thickness(0);
            }
            else {
                SetTitleOfInsideFrame(tag.Contains("About") ? "about" : tag);
                TitleBox.Visibility = mTitleBar.Visibility = Visibility.Visible;
                insideFrame.Margin = new Thickness(0,44,0,0);
            }
        }
        private void OpenPan_Click(object sender,RoutedEventArgs e) {
            SV.IsPaneOpen = !(SV.IsPaneOpen);
        }

        #endregion

        #region Opening Funcs
        private void OpenFlyout_RecentFiles() {
            if(!LocalSettingManager.ExistsSetting(App.SettingStrings["recent"]))
                return;
            var sv = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(LocalSettingManager.ReadSetting(App.SettingStrings["recent"]));
            var m = new MenuFlyout {
                Placement = FlyoutPlacementMode.Full,
                MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["CenteredMenuFlyoutPresenterStyle"],
            };
            sv.Sort();
            for(var i = 0;i < 5;i++) {
                string path;
                try {
                    path = sv[i];
                }
                catch {
                    break;
                }
                if(path.Length < 6)
                    continue;
                var name = LocalSettingManager.ReadSetting(App.SettingStrings["name"] + path);
                if(name.Length < 1)
                    continue;
                var tt = new MenuFlyoutItem() { Text = name };
                tt.Click += async (a,b) => {
                    try {
                        var v1 = await StorageFile.GetFileFromPathAsync(path);
                        insideFrame.Navigate(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]) ? typeof(SingleViewer) : typeof(Viewer),v1);
                    }
                    catch { return; }
                };
                m.Items?.Add(tt);
            }
            if(m.Items.Count > 0)
                m.ShowAt(Frame);
        }
        private async void OpenNewFile() {
            var f = await PdfHelper.OpenNewFile();
            if(f != null)
                insideFrame.Navigate(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]) ? typeof(SingleViewer) : typeof(Viewer),f);
        }
        #endregion

        private void MenuList2_ItemClick(object sender,ItemClickEventArgs e) {

        }

        private void AppBarButton_Click(object sender,RoutedEventArgs e) {
            //Closing FIle
        }

        private bool _onMobile = false;
        private void Home_OnSizeChanged(object sender,SizeChangedEventArgs e) {
            if(e.NewSize.Width < 620 && !_onMobile) {
                SV.DisplayMode = SplitViewDisplayMode.Overlay;
                _onMobile = true;
            }
            else if(e.NewSize.Width >= 620 && _onMobile) {
                _onMobile = false;
                SV.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            }
        }
    }
}
