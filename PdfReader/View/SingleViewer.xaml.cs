using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PdfReader.Classes;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Newtonsoft.Json;
using Point = Windows.Foundation.Point;

namespace PdfReader {
    public sealed partial class SingleViewer {
        #region Param
        private bool _isNigthMode;
        private uint _currentPgNr = 1, _maxPage;
        private PdfDocument _doc;
        private PdfPage _pg;
        private PdfPageRenderOptions _ro;
        private WriteableBitmap _bmp;
        private double _maxZoomFact = 1;
        private StorageFile _sfile;
        private int _imgAngle;
        private readonly PdfHelper _pdfHelper = new PdfHelper();
        private List<uint> _bookMarkUser = new List<uint>();
        #endregion

        #region Load
        public SingleViewer() {
            InitializeComponent();
            SizeChanged += (sender,args) => {
                if(PdfImg.Source != null) { PdfImg.Height = Viewboxi.Height = args.NewSize.Height; }
            };
        }
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            _sfile = e.Parameter as StorageFile;
            if(_sfile == null)
                return;
            if(LocalSettingManager.ExistsSetting(App.SettingStrings["lastPage"] + _sfile.Path)) {
                _currentPgNr = Convert.ToUInt32(LocalSettingManager.ReadSetting(App.SettingStrings["lastPage"] + _sfile.Path));
            }
        }
        private async void Page_Loaded(object sender,RoutedEventArgs e) {
            #region LoadFile
            try {
                _doc = await _pdfHelper.OpenLocal(_sfile);
            }
            catch {
                var dialog = new MessageDialog("Error! Wrong Password or corrupted file");
                dialog.Commands.Add(new UICommand("Close"));
                await dialog.ShowAsync();
            }
            #endregion
            if(_doc == null) {
                Frame.Navigate(typeof(WelcomePage));
                return;
            }
            #region Load UI & Settings
            _ro = new PdfPageRenderOptions { DestinationWidth = (uint)(Sc.ActualWidth * 1.4),IsIgnoringHighContrast = true };

            await _pdfHelper.TakeAShot(_doc,_sfile.DisplayName);
            _maxPage = _doc.PageCount;
            maxPgNr.Text = _maxPage.ToString();
            current.Text = _currentPgNr.ToString();
            #region Bookmark
            _bookMarkUser = _pdfHelper.LoadBookMarks(_sfile.Path);
            BkMrkBtn.IsChecked = _bookMarkUser.Contains(_currentPgNr);
            #endregion
            _isNigthMode = LocalSettingManager.ExistsSetting(App.SettingStrings["night"]);
            ApplicationView.GetForCurrentView().Title = _sfile.DisplayName;
            InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            #endregion
            GoToPage(_currentPgNr);
            if(_pdfHelper.ChangeRenderBg(_ro))
                AttachRenderToImage();
            _pdfHelper.Save4WelcomePage(_sfile,_doc.PageCount);
            #region If Mobile
            if(AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Desktop")) {
                Window.Current.CoreWindow.KeyUp += CoreWindow_KeyDown;
                FindName("F11Btn");
                if(LocalSettingManager.ExistsSetting(App.SettingStrings["zoom"])) {
                    FindName("ZoomTool");
                    (ZoomTool.Children[0] as AppBarButton).Click += ZoomOut_Click;
                    (ZoomTool.Children[1] as AppBarButton).Click += ZoomIn_Click;
                }
            }
            else {
                PdfImg.ManipulationMode = ManipulationModes.System;
                PdfImg.ManipulationDelta += (x,y) => { y.Handled = false; };
            }
            #endregion
        }
        #endregion

        #region PdfImg
        private async void Img_ImageFailed(object sender,ExceptionRoutedEventArgs e) {
            Helper.GarbageCollector();
            await new MessageDialog("Error on loading this page!\nPlease Try again").ShowAsync();
        }

        private void sc_ViewChanged(object sender,ScrollViewerViewChangedEventArgs e) {
            if(e != null && e.IsIntermediate)
                return;
            if((Sc.ZoomFactor <= _maxZoomFact + .25 && _isNigthMode) || (Math.Abs(Sc.ZoomFactor - _maxZoomFact) < .1))
                return;
            _maxZoomFact = Sc.ZoomFactor;
            _ro.DestinationWidth = (uint)(Sc.ViewportWidth * Sc.ZoomFactor);
            AttachRenderToImage();
        }
        #endregion

        #region Pdf Handling
        private void GoToPage(uint nr) {
            if(nr > _maxPage)
                nr = _maxPage;
            if(nr < 1)
                nr = 1;
            _pg = _doc.GetPage(nr - 1);
            _currentPgNr = nr;
            //First time
            if(_bmp == null) {
                _bmp = new WriteableBitmap((int)Sc.ViewportWidth,(int)Sc.ViewportHeight);
                PdfImg.Height = Frame.ActualHeight;
            }
            AttachRenderToImage();
            current.Text = _currentPgNr.ToString();
            BkMrkBtn.IsChecked = _bookMarkUser.Contains(_currentPgNr);
            if(ShowHideInk.Label.Contains("Hide")) {
                InkCanvas.InkPresenter.StrokeContainer.Clear();
                _pdfHelper.LoadCanvas(InkCanvas,_sfile.DisplayName,_currentPgNr.ToString());
                ResizingInkCanvas();
            }

        }
        private async void AttachRenderToImage() {
            if(_isNigthMode)
                try { await UpdatePdf(); }
                catch { }
            else
                using(var stream = new InMemoryRandomAccessStream()) {
                    try {
                        await _pg.RenderToStreamAsync(stream,_ro);
                        _bmp.SetSource(stream);
                        PdfImg.Source = _bmp;
                    }
                    catch { }
                }
            Helper.GarbageCollector();
        }
        private async Task<WriteableBitmap> InvertPageAsync() {
            using(var stream = new InMemoryRandomAccessStream()) {
                await _pg.RenderToStreamAsync(stream,_ro);
                var newWb = await _bmp.FromStream(stream);
                return newWb.Invert();
            }
        }

        private async Task UpdatePdf() {
            // Load the document, page, etc. and PreparePageAsync
            // Invert the page and show it in pageImage
            PdfImg.Source = await InvertPageAsync();
        }
        #endregion

        #region Next Prev Page Func
        private void NextPage_Click(object sender,RoutedEventArgs e) {
            if(_currentPgNr == _maxPage)
                return;
            var thebtn = (Button)sender;
            thebtn.IsEnabled = false;
            Page_Next_Prev(1);
            thebtn.IsEnabled = true;
        }
        private void PrePage_Click(object sender,RoutedEventArgs e) {
            if(_currentPgNr <= 1)
                return;
            var thebtn = (Button)sender;
            thebtn.IsEnabled = false;
            Page_Next_Prev(-1);
            thebtn.IsEnabled = true;
        }

        private void Page_Next_Prev(int offset) {
            _maxZoomFact = 1;
            _ro.DestinationWidth = (uint)(Sc.ViewportWidth * 1.4);
            _currentPgNr += (uint)offset;
            GoToPage(_currentPgNr);
            Sc.ChangeView(null,0,1);
            LocalSettingManager.SaveSetting(App.SettingStrings["lastPage"] + _sfile.Path,_currentPgNr.ToString());
        }
        private void ZoomIn_Click(object sender,RoutedEventArgs e) {
            Sc.ChangeView(Sc.HorizontalOffset,Sc.VerticalOffset,Sc.ZoomFactor + .25f);
        }
        private void ZoomOut_Click(object sender,RoutedEventArgs e) {
            Sc.ChangeView(Sc.HorizontalOffset,Sc.VerticalOffset,Sc.ZoomFactor - .25f);
        }
        #endregion

        #region  Appbar
        private void BookMark_Click(object sender,RoutedEventArgs e) {
            if(BkMrkBtn.IsChecked == true)
                _bookMarkUser.Add(_currentPgNr);
            else
                _bookMarkUser.Remove(_currentPgNr);
            LocalSettingManager.SaveSetting(App.SettingStrings["bookmark"] + _sfile.Path,JsonConvert.SerializeObject(_bookMarkUser));
        }
        private void MakeF11_Click(object sender,RoutedEventArgs e) {
            _pdfHelper.GoF11();
        }
        private void HighCon(object sender,RoutedEventArgs e) {
            _isNigthMode = !_isNigthMode;
            _maxZoomFact = 1;
            Sc.ChangeView(null,null,1);
            _ro.DestinationWidth = (uint)(Sc.ActualWidth * 1.3);
            AttachRenderToImage();
        }
        private void Rott_Right(object sender,RoutedEventArgs e) {
            var rt = new RotateTransform {
                Angle = _imgAngle + 90,
                CenterX = .5,
                CenterY = .5,
            };
            _imgAngle += 90;
            PdfImg.RenderTransformOrigin = new Point(0.5,0.5);
            PdfImg.RenderTransform = rt;
        }
        private void Rott_Left(object sender,RoutedEventArgs e) {
            var rt = new RotateTransform {
                Angle = _imgAngle - 90,
                CenterX = .5,
                CenterY = .5,
            };
            _imgAngle -= 90;
            PdfImg.RenderTransformOrigin = new Point(0.5,0.5);

            PdfImg.RenderTransform = rt;
        }
        #endregion

        #region Page changer Control
        private void CoreWindow_KeyDown(CoreWindow sender,KeyEventArgs args) {
            switch(args.VirtualKey) {
                case VirtualKey.H:
                    Frame.Navigate(typeof(WelcomePage));
                    break;
                case VirtualKey.Right:
                    if(Sc.ViewportWidth > 80 + PdfImg.ActualWidth * Sc.ZoomFactor) {
                        Page_Next_Prev(1);
                    }
                    else
                        Sc.ChangeView(Sc.HorizontalOffset + 25,Sc.VerticalOffset,Sc.ZoomFactor);
                    break;
                case VirtualKey.Left:
                    if(Sc.ViewportWidth > 80 + PdfImg.ActualWidth * Sc.ZoomFactor) {
                        Page_Next_Prev(-1);
                    }
                    else
                        Sc.ChangeView(Sc.HorizontalOffset - 25,Sc.VerticalOffset,Sc.ZoomFactor);
                    break;
                case VirtualKey.Up:
                    Sc.ChangeView(Sc.HorizontalOffset,Sc.VerticalOffset - 48,Sc.ZoomFactor);
                    break;
                case VirtualKey.Space:
                case VirtualKey.Down:
                    Sc.ChangeView(Sc.HorizontalOffset,Sc.VerticalOffset + 48,Sc.ZoomFactor);
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
                    HighCon(null,null);
                    break;
                case VirtualKey.F11:
                    FindName("F11Btn");
                    _pdfHelper.GoF11();
                    break;
                case VirtualKey.B:
                    BkMrkBtn.IsChecked = !BkMrkBtn.IsChecked;
                    BookMark_Click(null,null);
                    break;
                case VirtualKey.S:
                    ShowNote_Click(null,null);
                    break;
            }
        }

        #region Slider ----- Alpha
        //private void MySlider_OnLostFocus(object sender,RoutedEventArgs e) {
        //    var f = Resources["FadeIn"] as Storyboard;
        //    f?.Stop();
        //    f = Resources["FadeOut"] as Storyboard;
        //    f?.Begin();
        //    MySlider.Visibility = Visibility.Collapsed;
        //}
        //private void MySlider_OnGotFocus(object sender,RoutedEventArgs e) {
        //    MySlider.Visibility = Visibility.Visible;
        //    var f = Resources["FadeOut"] as Storyboard;
        //    f?.Stop();
        //    f = Resources["FadeIn"] as Storyboard;
        //    f?.Begin();
        //}
        //private void UIElement_OnGotFocus(object sender,RoutedEventArgs e) {
        //    Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyDown;
        //    FindName("MySlider");
        //    MySlider.Maximum = _maxPage;
        //    MySlider.Value = _currentPgNr;
        //    MySlider_OnGotFocus(null,null);
        //}
        //private void UIElement_OnLostFocus(object sender,RoutedEventArgs e) {
        //    var thebtn = (TextBox)sender;
        //    try {
        //        try {
        //            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyDown;
        //        }
        //        catch { }
        //        var nr = Convert.ToUInt32(thebtn.Text);
        //        if(nr > _maxPage) {
        //            thebtn.Text = _currentPgNr.ToString();
        //            return;
        //        }
        //        thebtn.IsEnabled = false;
        //        RenderPage(nr);
        //        Sc.ChangeView(0f,0f,1);
        //        _currentPgNr = nr;
        //        SaveNewPageInSettings();

        //        Window.Current.CoreWindow.KeyUp += CoreWindow_KeyDown;
        //    }
        //    catch { }
        //    finally { thebtn.IsEnabled = true; }
        //    MySlider_OnLostFocus(null,null);
        //}
        //private void MySlider_ValueChanged(object sender,Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e) {
        //    if ((uint) e.NewValue == _currentPgNr) return;
        //    var nr = (uint)e.NewValue;
        //    RenderPage(nr);
        //    Sc.ChangeView(0f,0f,1);
        //    _currentPgNr = nr;
        //    SaveNewPageInSettings();
        //}
        #endregion

        private void BkLoad_OnClick(object sender,RoutedEventArgs e) {
            if(_bookMarkUser.Count != 0) {
                FindName("bkList");
                bkList.ItemsSource = null;
                _bookMarkUser.Sort();
                bkList.ItemsSource = _bookMarkUser;
            }
            bkPanel.IsPaneOpen = true;
        }

        private async void GoPg_OnTapped(object sender,TappedRoutedEventArgs e) {
            if(InkToolbar.Visibility == Visibility.Visible)
                return;
            var j = new JumpTo();
            await j.ShowAsync();
            try {
                var pg = Convert.ToUInt32(j.Tag);
                if(pg > 0)
                    GoToPage(pg);
            }
            catch { }
        }

        private void ChangePgNr_Click(object sender,RoutedEventArgs e) {
            GoPg_OnTapped(null,null);
        }

        private void BkList_OnSelectionChanged(object sender,SelectionChangedEventArgs e) {
            if(bkList.SelectedIndex == -1 || bkList.SelectedItem == null)
                return;
            bkPanel.IsPaneOpen = false;
            GoToPage((uint)bkList.SelectedItem);
            bkList.SelectedIndex = -1;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            _pdfHelper.SaveCanvas(InkCanvas,_sfile.DisplayName,current.Text);
            if(PenBtn.IsChecked == true) {
                LocalSettingManager.SaveSetting(_sfile.DisplayName + "_wnote_" + current,
                    InkCanvas.ActualWidth.ToString(CultureInfo.InvariantCulture));
                LocalSettingManager.SaveSetting(_sfile.DisplayName + "_hnote_" + current,
                    InkCanvas.ActualHeight.ToString(CultureInfo.InvariantCulture));
            }
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyDown;
            ApplicationView.GetForCurrentView().Title = string.Empty;
        }

        #endregion

        private void PenBtn_Click(object sender,RoutedEventArgs e) {
            ShowHideInk.IsEnabled = !ShowHideInk.IsEnabled;
            if(PreBtn.Visibility == Visibility.Visible) {
                ShowNote_Click(null,null);
                //Load
                InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen |
                                                          CoreInputDeviceTypes.Touch;
                InkToolbar.Visibility = Visibility.Visible;
                PreBtn.Visibility = Visibility.Collapsed;
                NexBtn.Visibility = Visibility.Collapsed;
            }
            else {
                _pdfHelper.SaveCanvas(InkCanvas,_sfile.DisplayName,current.Text);
                InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
                //Save
                InkToolbar.Visibility = Visibility.Collapsed;
                PreBtn.Visibility = Visibility.Visible;
                NexBtn.Visibility = Visibility.Visible;
                if(!LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_hnote_single")) {
                    LocalSettingManager.SaveSetting(_sfile.DisplayName + "_wnote_single",
                        InkCanvas.ActualWidth.ToString(CultureInfo.InvariantCulture));
                    LocalSettingManager.SaveSetting(_sfile.DisplayName + "_hnote_single",
                        InkCanvas.ActualHeight.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void ShowNote_Click(object sender,RoutedEventArgs e) {
            if(ShowHideInk.Label.Contains("Show")) {
                RotateBtns.IsEnabled = false;
                ShowHideInk.Label = "Hide Notes";
                InkCanvas.Visibility = Visibility.Visible;
                //LOAD SCALE
                ResizingInkCanvas();
                Viewboxi.Height = PdfImg.ActualHeight;
                _pdfHelper.LoadCanvas(InkCanvas,_sfile.DisplayName,current.Text);
            }
            else {
                if(PenBtn.IsChecked == true)
                    return;
                RotateBtns.IsEnabled = true;
                ShowHideInk.Label = "Show Notes";
                InkCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void ResizingInkCanvas() {
            var h = Sc.ViewportHeight;
            var w = PdfImg.ActualWidth;
            if(LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_wnote_" + current) && LocalSettingManager.ExistsSetting(_sfile.DisplayName + "_hnote_" + current)) {
                var temp = Convert.ToDouble(LocalSettingManager.ReadSetting(_sfile.DisplayName + "_wnote_" + current));
                if(temp > 200)
                    w = temp;
                temp = Convert.ToDouble(LocalSettingManager.ReadSetting(_sfile.DisplayName + "_hnote_" + current));
                if(temp > 200)
                    h = temp;
            }
            InkCanvas.Height = h;
            InkCanvas.Width = w;
        }
    }
}