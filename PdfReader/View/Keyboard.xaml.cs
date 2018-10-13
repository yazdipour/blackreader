using Windows.UI.Xaml.Controls;

namespace PdfReader.View {
    public sealed partial class Keyboard : UserControl {
        public Keyboard() {
            InitializeComponent();
        }

        public string KeyText { get; set; } = "";
    }
}
