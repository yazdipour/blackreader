using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using PdfReader.Classes;

namespace PdfReader {
    sealed partial class App {
        public App() {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            InitializeComponent();
            Suspending += OnSuspending;
        }

        public static Dictionary<string,string> SettingStrings = new Dictionary<string,string> {
            {"rate","ratepopup5" },
            {"night","Setting_Night" },
            {"zoom","Setting_Zoom" },
            {"view","Setting_ViewMode" },
            {"bgColor","PageColor2" },
            {"recent","recentopened_v3" },
            {"bookmark","bm2-" },
            {"lastPage","last=" },
            {"name","name=" },
            {"ink","_notes5.isf" }
        };

        protected override void OnFileActivated(FileActivatedEventArgs args) {
            base.OnFileActivated(args);
            var rootFrame = new Frame();
            rootFrame.Navigate(typeof(Home),args);
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        //protected override void OnActivated(IActivatedEventArgs args)
        //{
        //    base.OnActivated(args);
        //    if (args.Kind == ActivationKind.File)
        //    {
        //        //Windows.Storage.FileIO.ReadBufferAsync(args)
        //    }
        //}

        private async void RatePopup() {
            const string ver = "ratepopup5";
            var times = 0;
            if(LocalSettingManager.ExistsSetting(ver)) {
                var st = LocalSettingManager.ReadSetting(ver);
                if(st == "[done]")
                    return;
                if(st == "2") {
                    await new UpdateNote().ShowAsync();
                    LocalSettingManager.SaveSetting(ver,"[done]");
                    return;
                }
                if(st.Length > 0) {
                    times = Convert.ToInt32(st);
                }
            }
            LocalSettingManager.SaveSetting(ver,(++times).ToString());
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            var rootFrame = Window.Current.Content as Frame;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if(rootFrame == null) {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                //if(e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                //TODO: Load state from previously suspended application
                //}
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if(rootFrame.Content == null) {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(Home),e.Arguments);
                RatePopup();
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender,NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender,SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
