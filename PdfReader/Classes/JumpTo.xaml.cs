using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace PdfReader.Classes {
    public sealed partial class JumpTo {
        private readonly bool _isPass;
        public JumpTo(bool isPassword=false) {
            InitializeComponent();
            _isPass = isPassword;
            if(_isPass) {
                Title = "Enter Password";
                var scope = new InputScope();
                var scopeName = new InputScopeName { NameValue = InputScopeNameValue.Text };
                scope.Names.Add(scopeName);
                Box.InputScope = scope;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args) {
            Tag = Box.Text;
            Hide();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender,ContentDialogButtonClickEventArgs args) {
            Hide();
        }

        private void Box_OnKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key== VirtualKey.Enter) {
                ContentDialog_PrimaryButtonClick(null,null);
            }
        }
    }
}
