using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace PdfReader.Classes {
    public class PdfHelper {
        public bool ChangeRenderBg(PdfPageRenderOptions renderOptions) {
            var temp = LocalSettingManager.ExistsSetting(App.SettingStrings["bgColor"]);
            if(temp)
                renderOptions.BackgroundColor = Helper.Convert_HexColor(LocalSettingManager.ReadSetting(App.SettingStrings["bgColor"])).Color;
            return temp;
        }

        public static async Task<StorageFile> OpenNewFile() {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            var fl = await picker.PickSingleFileAsync();
            if(fl == null || !fl.IsAvailable || (await fl.GetBasicPropertiesAsync()).Size <= 0)
                return null;
            return await fl.CopyAsync(ApplicationData.Current.LocalFolder,
                fl.DisplayName + ".pdf",NameCollisionOption.ReplaceExisting);
        }

        public void Save4WelcomePage(StorageFile sfile,uint max) {
            LocalSettingManager.SaveSetting(App.SettingStrings["name"] + sfile.Path,sfile.DisplayName);
            LocalSettingManager.SetRecentToOpened(sfile.Path);
        }

        public List<uint> LoadBookMarks(string path) {
            return LocalSettingManager.ExistsSetting(App.SettingStrings["bookmark"] + path) ? JsonConvert.DeserializeObject<List<uint>>(LocalSettingManager.ReadSetting(App.SettingStrings["bookmark"] + path)) : new List<uint>();
        }

        #region Openning functions
        public async Task<PdfDocument> OpenLocal(IStorageFile fl) {
            PdfDocument doc = null;
            try {
                doc = await PdfDocument.LoadFromFileAsync(fl);
            }
            catch {
                var jp2 = new JumpTo(true);
                await jp2.ShowAsync();
                doc = await PdfDocument.LoadFromFileAsync(fl,jp2.Tag.ToString());
            }
            return doc;
        }

        //public async Task OpenRemote(string pdfUrl) {
        //    var client = new System.Net.Http.HttpClient();
        //    using(var stream = await client.GetStreamAsync(pdfUrl)) {
        //        using(var memStream = new MemoryStream()) {
        //            await stream.CopyToAsync(memStream);
        //            memStream.Position = 0;
        //            _doc = await PdfDocument.LoadFromStreamAsync(memStream.AsRandomAccessStream());
        //        }
        //    }
        //}
        #endregion

        public async Task TakeAShot(PdfDocument doc,string name) {
            var fld = ApplicationData.Current.LocalFolder;
            if(!name.Contains(".pdf"))
                name += ".pdf";
            try {
                await fld.GetFileAsync(name + ".jpg");
            }
            catch(FileNotFoundException) {
                var r = new PdfPageRenderOptions { DestinationHeight = 190,DestinationWidth = 160 };
                using(var stream = new InMemoryRandomAccessStream()) {
                    try {
                        await doc.GetPage(0).RenderToStreamAsync(stream,r);
                        var fll = await fld.CreateFileAsync(name + ".jpg",CreationCollisionOption.ReplaceExisting);
                        using(var fs = await fll.OpenAsync(FileAccessMode.ReadWrite)) {
                            await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0),fs.GetOutputStreamAt(0));
                        }
                    }
                    catch {
                        Helper.GarbageCollector();
                    }
                }
            }
        }


        public void GoF11() {
            var view = ApplicationView.GetForCurrentView();
            if(view.IsFullScreenMode)
                view.ExitFullScreenMode();
            else
                view.TryEnterFullScreenMode();
        }

        public async void SaveCanvas(InkCanvas canvas,string name,string pageNr = "") {
            try {
                if(canvas == null || canvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0)
                    return;
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("folder_" + name,CreationCollisionOption.OpenIfExists);
                var file = await folder.CreateFileAsync(name + pageNr + App.SettingStrings["ink"],CreationCollisionOption.ReplaceExisting);
                using(var stream = await file.OpenAsync(FileAccessMode.ReadWrite)) {
                    await canvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                }
            }
            catch(Exception) { }
        }

        public async void LoadCanvas(InkCanvas canvas,string name,string pageNr = "") {
            try {
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("folder_" + name);
                var fll = await folder.GetFileAsync(name + pageNr + App.SettingStrings["ink"]);
                using(var stream = await fll.OpenSequentialReadAsync()) {
                    await canvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                }
            }
            catch { }
        }
    }
}
