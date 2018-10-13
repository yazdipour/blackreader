using PdfReader.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PdfReader {

    public sealed partial class WelcomePage : Page {
        public WelcomePage() {
            InitializeComponent();
        }

        private List<PageType> _gvls = new List<PageType>();

        private async void GridView_ItemClick(object sender,ItemClickEventArgs e) {
            var bi = e.ClickedItem as PageType;
            if(bi == null)
                return;
            var v1 = await StorageFile.GetFileFromPathAsync(bi.Path);
            Frame.Navigate(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]) ? typeof(SingleViewer) : typeof(Viewer),v1);
        }

        private async void Page_Loaded(object sender,RoutedEventArgs e) {
            try {
                _gvls = await GetFiles();
            }
            catch { }
            gv.ItemsSource = _gvls;
            Helper.ClearMyBack(Frame);
        }

        private async Task<List<PageType>> GetFiles() {
            var ls = new List<PageType>();
            var filesInFolder = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            try {
                foreach(var file in filesInFolder) {
                    if(!file.FileType.ToLower().Contains("pdf"))
                        continue;
                    var tImg = "Assets/Icons/paper.png";
                    try {
                        if(File.Exists(file.Path + ".jpg"))
                            tImg = file.Path + ".jpg";
                    }
                    catch(Exception) { }
                    ls.Add(new PageType { Name = file.DisplayName,Img = tImg,Path = file.Path,Date = file.DateCreated });
                }
                ls.Sort((x,y) => y.Date.CompareTo(x.Date));
            }
            catch {
                return null;
            }
            return ls;
        }

        #region AppBar & ImageFailed
        private async void Cancelbtn_OnClicktn_OnClick(object sender,RoutedEventArgs e) {
            if(del_2.Tag.ToString() == "0") {
                var f = await PdfHelper.OpenNewFile();
                if(f != null)
                    Frame.Navigate(LocalSettingManager.ExistsSetting(App.SettingStrings["view"]) ? typeof(SingleViewer) : typeof(Viewer),f);
            }
            else {
                gv.SelectionMode = ListViewSelectionMode.None;
                gv.ItemClick += GridView_ItemClick;
                del_2.Tag = "0";
                del_2.Background = new SolidColorBrush(Colors.LimeGreen);
                del_1.Background = Helper.Convert_HexColor("#2987F7");
                (del_2.Child as AppBarButton).Icon = new SymbolIcon(Symbol.OpenFile);
            }
        }

        private void del_1_Click(object sender,RoutedEventArgs e) {
            if(del_2.Tag.ToString() == "0") {
                if(gv.Items?.Count < 1)
                    return;
                gv.SelectionMode = ListViewSelectionMode.Multiple;
                gv.ItemClick -= GridView_ItemClick;
                del_1.Background = Helper.Convert_HexColor("#e74c3c"); //red
                del_2.Background = new SolidColorBrush(Colors.Gray);
                (del_2.Child as AppBarButton).Icon = new SymbolIcon(Symbol.Cancel);
                del_2.Tag = "1";
            }
            else {
                var slc = gv.SelectedItems;
                if(slc.Count > 0) {
                    foreach(PageType v in slc) {
                        DeletePdf(v?.Path);
                        _gvls.Remove(v);
                    }
                    gv.ItemsSource = null;
                    gv.ItemsSource = _gvls;
                }
                Cancelbtn_OnClicktn_OnClick(null,null);
            }
        }

        private async void DeletePdf(string path) {
            try {
                var pdf = await StorageFile.GetFileFromPathAsync(path);
                LocalSettingManager.RemoveSetting(App.SettingStrings["lastPage"] + path);
                LocalSettingManager.RemoveSetting(App.SettingStrings["bookmark"] + path);
                LocalSettingManager.RemoveSetting(App.SettingStrings["name"] + path);
                await pdf.DeleteAsync();
                var filesInFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("folder_"+pdf.DisplayName);
                foreach(var files in await filesInFolder.GetFilesAsync()) {
                    await files.DeleteAsync();
                }
            }
            catch(Exception) { }
            try {
                var jpg = await StorageFile.GetFileFromPathAsync(path + ".jpg");
                await jpg.DeleteAsync();
            }
            catch(Exception) { }

        }

        private void Image_OnImageFailed(object sender,ExceptionRoutedEventArgs e) {
            ((Image)sender).Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/paper.png"));
        }
        #endregion

    }
}
