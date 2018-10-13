using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;
using PdfReader.Classes;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Newtonsoft.Json;
namespace PdfReader {
    public partial class Viewer {
        private StorageFile _sfile;
        private List<uint> _bookMarkUser = new List<uint>();
        private uint _crntPage;
        private PdfDocument _doc;
        private PdfPageRenderOptions _renderOptions;
        private float _crntZoom = 1;
        private int _nrChanges;
        private readonly ObservableCollection<Image> _pages = new ObservableCollection<Image>();
        private bool _isNigthMode;
        private readonly PdfHelper _pdfHelper = new PdfHelper();
        public double GlobalWidth, GlobalHeight;

        public Viewer() {
            InitializeComponent();
        }
        #region OnLoad
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            _sfile = e.Parameter as StorageFile;
            ApplicationView.GetForCurrentView().Title = _sfile?.DisplayName ?? string.Empty;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            try {
                _pdfHelper.SaveCanvas(InkCanvas,_sfile.DisplayName);
            }
            catch { }
            if(!LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_wnote")) {
                LocalSettingManager.SaveSetting(_sfile.DisplayName + "_wnote",
                InkCanvas.ActualWidth.ToString(CultureInfo.InvariantCulture));
                LocalSettingManager.SaveSetting(_sfile.DisplayName + "_hnote",
                    InkCanvas.ActualHeight.ToString(CultureInfo.InvariantCulture));
            }
            ApplicationView.GetForCurrentView().Title = string.Empty;
            try { Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown; }
            catch(Exception) { }
        }

        private async void Page_Loaded(object sender,RoutedEventArgs e) {
            try {
                _doc = await _pdfHelper.OpenLocal(_sfile);
            }
            catch {
                var dialog = new MessageDialog("Error! Wrong Password or corrupted file");
                dialog.Commands.Add(new UICommand("Close"));
                await dialog.ShowAsync();
            }
            if(_doc == null) {
                Frame.Navigate(typeof(WelcomePage));
                return;
            }
            _renderOptions = new PdfPageRenderOptions();
            _isNigthMode = LocalSettingManager.ExistsSetting(App.SettingStrings["night"]);
            _pdfHelper.ChangeRenderBg(_renderOptions);
            MaxPageText.Text = _doc.PageCount.ToString();
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["lastPage"] + _sfile.Path)) {
                var saveLast = LocalSettingManager.ReadSetting(App.SettingStrings["lastPage"] + _sfile.Path);
                if(saveLast != "1") {
                    FindName("GoLastPageBtn");
                    GoLastPageBtn.Tag = saveLast;
                    GoLastPageBtn.Visibility = Visibility.Visible;
                    var an = Resources["LastFadeOut"] as Storyboard;
                    an.Completed += (o,o1) => {
                        GoLastPageBtn.Visibility = Visibility.Collapsed;
                        GoLastPageBtn.Click -= GoLastPageBtn_OnClick;
                        GoLastPageBtn = null;
                    };
                    an.Begin();
                }
            }
            try {
                await Load();
            }
            catch {
                Frame.Navigate(typeof(WelcomePage));
                return;
            }

            _bookMarkUser = _pdfHelper.LoadBookMarks(_sfile.Path);
            _pdfHelper.Save4WelcomePage(_sfile,_doc.PageCount);
            await _pdfHelper.TakeAShot(_doc,_sfile.DisplayName);
            InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            BkMrkBtn.IsChecked = _bookMarkUser.Contains(1);
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["zoom"])) {
                FindName("ZoomTool");
                (ZoomTool.Children[0] as AppBarButton).Click += ZoomOut_Click;
                (ZoomTool.Children[1] as AppBarButton).Click += ZoomIn_Click;
            }
            if(AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Desktop")) // "Desktop"
            {
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                FindName("F11Btn");
            }
        }

        #endregion

        #region Functions

        #region Loading Pages
        private async Task Load() {
            for(uint i = 0;i < _doc.PageCount;i++)
                _pages.Add(null);
            await CallPage(0);
            if(_doc.PageCount > 1)
                await CallPage(1);
        }

        private async Task CallPage(int i) {
            if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1 || Math.Abs(_crntZoom - Scroll.ZoomFactor) > .4)
                return;//--------
            WriteableBitmap bmp;
            using(var stream = new InMemoryRandomAccessStream()) {
                var pdfPage = _doc.GetPage((uint)i);
                _renderOptions.DestinationWidth = (uint)(Scroll.ActualWidth * _crntZoom) + 250;
                await pdfPage.RenderToStreamAsync(stream,_renderOptions);
                if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1)
                    return; //--------
                if(GlobalWidth == 0) {
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    bmp = new WriteableBitmap((int)decoder.PixelWidth,(int)decoder.PixelHeight);
                }
                else {
                    bmp = new WriteableBitmap((int)GlobalWidth,(int)GlobalHeight);
                }
                if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1)
                    return; //--------
                bmp.SetSource(stream);
            }
            if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1)
                return;//--------
            try {
                if(_pages[i] != null) {
                    if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1)
                        return;//--------
                    _pages[i].Source = _isNigthMode ? bmp.Invert() : bmp;
                    _pages[i].Tag = _nrChanges;
                    _pages[i].Height = GlobalHeight;
                }
                else {
                    try {
                        if(i < (int)_crntPage - 1 || i > (int)_crntPage + 1)
                            return;//--------
                        if(i == 0)
                            GlobalHeight = Scroll.ViewportHeight;
                        _pages[i] = new Image {
                            Source = _isNigthMode ? bmp.Invert() : bmp,
                            Tag = _nrChanges,
                            Name = "p" + i,
                            Stretch = Stretch.Uniform,
                            Height = GlobalHeight,
                            RenderTransformOrigin = new Point(0.5,0.5),
                            RenderTransform = _rotateTransform
                        };
                        if(i == 0) {
                            PageGridView.Height = Scroll.ActualHeight * _doc.PageCount;
                            GlobalWidth = _pages[i].ActualWidth;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        private async void PageChanged(bool singlePage = false) {
            try {
                if(singlePage) {
                    var el = _pages[(int)_crntPage];
                    if(!el.Tag.ToString().Equals(_nrChanges.ToString())) {
                        await CallPage((int)_crntPage);
                    }
                }
                else {
                    for(var i = (int)_crntPage + 1;i >= 0;i--) {
                        if(_crntPage != 0)
                            if(i < _crntPage - 1)
                                break;
                        var el = _pages[i];
                        if(el != null) {
                            if(el.Name.Equals("p" + i) && el.Tag.ToString().Equals(_nrChanges.ToString()))
                                continue;
                        }
                        await CallPage(i);
                    }
                }
            }
            catch { }
            try {
                if(_crntPage > 5) {
                    _pages[(int)(_crntPage - 3)] = null;
                    _pages[(int)(_crntPage - 2)] = null;
                }
            }
            catch { }
            GC.Collect();
        }
        private void GoToPage(uint o) {
            if(o > _doc.PageCount)
                return;
            o--;
            var image = _pages[0];
            if(image != null) {

                Scroll.ChangeView(null,o * image.ActualHeight,1);
            }
        }

        #endregion

        #region Chaning

        private void ScrollViewer_ViewChanged(object sender,ScrollViewerViewChangedEventArgs e) {
            if(Math.Abs(_crntZoom - Scroll.ZoomFactor) >= 0.5) {
                if(e != null && e.IsIntermediate)
                    return;
                _nrChanges++;
                _crntZoom = Scroll.ZoomFactor;
                PageChanged(true);
                return;
            }
            var offset = Scroll.VerticalOffset + Scroll.ViewportHeight / 3;
            var index = (uint)(offset / (Scroll.ExtentHeight / _doc.PageCount));
            if(_crntPage == index)
                return;
            _crntPage = index;
            CrntPageText.Text = (_crntPage + 1).ToString();
            BkMrkBtn.IsChecked = _bookMarkUser.Contains(_crntPage + 1);
            LocalSettingManager.SaveSetting(App.SettingStrings["lastPage"] + _sfile.Path,(1 + _crntPage).ToString());
            PageChanged();
        }
        private void CoreWindow_KeyDown(CoreWindow sender,KeyEventArgs args) {
            switch(args.VirtualKey) {
                case VirtualKey.H:
                    Frame.Navigate(typeof(WelcomePage));
                    break;
                case VirtualKey.Right:
                    if(!(Scroll.ViewportWidth > 80 + _pages[0].ActualWidth * Scroll.ZoomFactor))
                        Scroll.ChangeView(Scroll.HorizontalOffset + 25,Scroll.VerticalOffset,Scroll.ZoomFactor);
                    break;
                case VirtualKey.Left:
                    if(!(Scroll.ViewportWidth > 80 + _pages[0].ActualWidth * Scroll.ZoomFactor))
                        Scroll.ChangeView(Scroll.HorizontalOffset - 25,Scroll.VerticalOffset,Scroll.ZoomFactor);
                    break;
                case VirtualKey.Up:
                    Scroll.ChangeView(Scroll.HorizontalOffset,Scroll.VerticalOffset - 48,Scroll.ZoomFactor);
                    break;
                case VirtualKey.Space:
                case VirtualKey.Down:
                    Scroll.ChangeView(Scroll.HorizontalOffset,Scroll.VerticalOffset + 48,Scroll.ZoomFactor);
                    break;
                case VirtualKey.Add:
                case (VirtualKey)187:
                case VirtualKey.X:
                    ZoomIn_Click(null,null);
                    break;
                case VirtualKey.Subtract:
                case (VirtualKey)189:
                case VirtualKey.Z:
                    ZoomOut_Click(null,null);
                    break;
                case VirtualKey.N:
                    NightView_click(null,null);
                    break;
                case VirtualKey.F11:
                    Frame.FindName("F11Btn");
                    F11View_click(null,null);
                    break;
                case VirtualKey.B:
                    BkMrkBtn.IsChecked = !BkMrkBtn.IsChecked;
                    BkMrkBtn_Click(null,null);
                    break;
                case VirtualKey.S:
                    ShowNote_Click(null,null);
                    break;
            }
        }
        #endregion

        #endregion

        #region AppBar
        private void F11View_click(object sender,RoutedEventArgs e) {
            _pdfHelper.GoF11();
        }
        private void ButtonBase_OnClick(object sender,RoutedEventArgs e) {
            if(_bookMarkUser.Count != 0) {
                FindName("bkList");
                bkList.ItemsSource = null;
                _bookMarkUser.Sort();
                bkList.ItemsSource = _bookMarkUser;
            }
            bkPanel.IsPaneOpen = true;
        }
        private void BkMrkBtn_Click(object sender,RoutedEventArgs e) {
            if(BkMrkBtn.IsChecked == true)
                _bookMarkUser.Add(_crntPage + 1);
            else
                _bookMarkUser.Remove(_crntPage + 1);
            LocalSettingManager.SaveSetting(App.SettingStrings["bookmark"] + _sfile.Path,JsonConvert.SerializeObject(_bookMarkUser));
        }
        private async void UIElement_OnTapped(object sender,TappedRoutedEventArgs e) {
            var j = new JumpTo();
            await j.ShowAsync();
            try {
                var pg = Convert.ToUInt32(j.Tag);
                if(pg > 0)
                    GoToPage(pg);
            }
            catch { }
        }
        private void BkList_OnSelectionChanged(object sender,SelectionChangedEventArgs e) {
            if(bkList.SelectedIndex == -1 || bkList.SelectedItem == null)
                return;
            bkPanel.IsPaneOpen = false;
            GoToPage((uint)bkList.SelectedItem);
            bkList.SelectedIndex = -1;
        }
        private void GoLastPageBtn_OnClick(object sender,RoutedEventArgs e) {
            try {
                GoToPage(Convert.ToUInt32(GoLastPageBtn.Tag));
            }
            catch { }
        }
        private void NightView_click(object sender,RoutedEventArgs e) {
            _isNigthMode = !_isNigthMode;
            _crntZoom = 1;
            _nrChanges++;
            Scroll.ChangeView(null,null,1);
            PageChanged();
        }
        private readonly RotateTransform _rotateTransform = new RotateTransform { Angle = 0,CenterX = .5,CenterY = .5 };
        private void ZoomOut_Click(object sender,RoutedEventArgs e) {
            Scroll.ChangeView(Scroll.ViewportWidth / 2,Scroll.VerticalOffset,Scroll.ZoomFactor - .25f);
        }
        private void ZoomIn_Click(object sender,RoutedEventArgs e) {
            Scroll.ChangeView(Scroll.ViewportWidth / 2,Scroll.VerticalOffset,Scroll.ZoomFactor + .25f);
        }
        private void Rott_Right(object sender,RoutedEventArgs e) {
            if((sender as MenuFlyoutItem).Tag.ToString() == "Right")
                _rotateTransform.Angle += 90;
            else
                _rotateTransform.Angle -= 90;
            _pages[0].Height = Scroll.ViewportWidth;
        }
        #endregion

        private void PenBtn_Click(object sender,RoutedEventArgs e) {
            ShowHideInk.IsEnabled = !ShowHideInk.IsEnabled;
            if(!ShowHideInk.IsEnabled) {
                //Ok-Load-1st here
                ShowNote_Click(null,null);
                //FindName("InkToolbar");
                InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen |
                                                          CoreInputDeviceTypes.Touch;
                InkToolbar.Visibility = Visibility.Visible;
            }
            else {
                //Save-Bye
                InkToolbar.Visibility = Visibility.Collapsed;
                InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
                _pdfHelper.SaveCanvas(InkCanvas,_sfile.DisplayName);
                if(!LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_wnote")) {
                    LocalSettingManager.SaveSetting(_sfile.DisplayName + "_wnote",
                    InkCanvas.ActualWidth.ToString(CultureInfo.InvariantCulture));
                    LocalSettingManager.SaveSetting(_sfile.DisplayName + "_hnote",
                        InkCanvas.ActualHeight.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
        private void ShowNote_Click(object sender,RoutedEventArgs e) {
            if(ShowHideInk.Label.Contains("Show")) {
                RotateBtns.IsEnabled = false;
                if(InkCanvas == null || InkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0) {
                    //LOAD SCALE
                    Viewboxi.Height = Scroll.ExtentHeight;
                    Viewboxi.Width = _pages[0].ActualWidth;
                    var h = PageGridView.ActualHeight;
                    var w = _pages[0].ActualWidth;
                    if(LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_wnote") && LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_hnote")) {
                        var temp = Convert.ToDouble(LocalSettingManager.ReadSetting(_sfile.DisplayName + "_wnote"));
                        if(temp > 200)
                            w = temp;
                        temp = Convert.ToDouble(LocalSettingManager.ReadSetting(_sfile.DisplayName + "_hnote"));
                        if(temp > 200)
                            h = temp;
                    }
                    InkCanvas.Width = w;
                    InkCanvas.Height = h;
                    _pdfHelper.LoadCanvas(InkCanvas,_sfile.DisplayName);
                }
                ShowHideInk.Label = "Hide Notes";
                InkCanvas.Visibility = Visibility.Visible;
            }
            else {
                if(PenBtn.IsChecked == true)
                    return;
                RotateBtns.IsEnabled = true;
                ShowHideInk.Label = "Show Notes";
                InkCanvas.Visibility = Visibility.Collapsed;
            }
        }
    }
}
