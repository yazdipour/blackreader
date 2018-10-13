using Windows.UI.Xaml.Controls;

namespace PdfReader.Classes {
    public sealed partial class ChooseColor {
        public ChooseColor(string def) {
            InitializeComponent();
            View.ItemsSource = _colors;
            View.SelectedItem= def;
            Tag = def;
        }

        private readonly string[] _colors = { "#ffffff","#FFC0C0C0","#FF808080","#1f1f1f","#000000","#5d4037","#FFD2B48C","#FFFFE4C4" };

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args) {
            Hide();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args) {
            try {
                Tag = _colors[View.SelectedIndex];
            }
            catch { }
            Hide();
        }
    }
}
