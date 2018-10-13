using System;
using Windows.UI.Xaml.Controls;

namespace PdfReader {
    public sealed partial class UpdateNote : ContentDialog {

        public UpdateNote() {
            this.InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args) {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:REVIEW?PFN=3783mindprojects.BlackReader_6c8ydbw054cyy"));
        }
    }
}
