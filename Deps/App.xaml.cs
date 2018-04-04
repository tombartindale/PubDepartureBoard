using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Text;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.System.Diagnostics.DevicePortal;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace DepartureBoard
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private BackgroundTaskDeferral taskDeferral;
        private DevicePortalConnection devicePortalConnection;
        private static Uri statusUri = new Uri("/departureboard/api/status", UriKind.Relative);
        private static Uri updateUri = new Uri("/departureboard/api/update", UriKind.Relative);


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // Take a deferral to allow the background task to continue executing 
            var taskInstance = args.TaskInstance;
            this.taskDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            // Create a DevicePortal client from an AppServiceConnection 
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            var appServiceConnection = details.AppServiceConnection;
            this.devicePortalConnection =
              DevicePortalConnection.GetForAppServiceConnection(
                appServiceConnection);

            // Add handlers for RequestReceived and Closed events
            devicePortalConnection.RequestReceived += DevicePortalConnection_RequestReceived;
            devicePortalConnection.Closed += DevicePortalConnection_Closed;
        }

        private void Close()
        {
            this.devicePortalConnection = null;
            this.taskDeferral.Complete();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender,
          BackgroundTaskCancellationReason reason)
        {
            Close();
        }

        private void DevicePortalConnection_Closed(DevicePortalConnection sender,
          DevicePortalConnectionClosedEventArgs args)
        {
            Close();
        }

        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppSettings));

        private void DevicePortalConnection_RequestReceived(DevicePortalConnection sender, DevicePortalConnectionRequestReceivedEventArgs args)
        {
            var req = args.RequestMessage;
            var res = args.ResponseMessage;

            if (req.RequestUri.AbsolutePath.ToString() == statusUri.ToString())
            {
                args.ResponseMessage.StatusCode = HttpStatusCode.Ok;
                MemoryStream mem = new MemoryStream();
                serializer.WriteObject(mem,AppSettings.Current);
                mem.Position = 0;
                args.ResponseMessage.Content = new HttpStringContent(new StreamReader(mem).ReadToEnd());
                args.ResponseMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                return;
            }

            if (req.RequestUri.AbsolutePath.ToString() == updateUri.ToString())
            {
                WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(req.RequestUri.Query);
                AppSettings.Current.DARKSKY_API_KEY = decoder.GetFirstValueByName("DARKSKY_API_KEY");
                AppSettings.Current.RAIL_API_KEY = decoder.GetFirstValueByName("RAIL_API_KEY");
                AppSettings.Current.STATION_CRS = decoder.GetFirstValueByName("STATION_CRS");
                AppSettings.Current.LOCATION_LAT = double.Parse(decoder.GetFirstValueByName("LOCATION_LAT"));
                AppSettings.Current.LOCATION_LNG = double.Parse(decoder.GetFirstValueByName("LOCATION_LNG"));
                AppSettings.Current.GET_DRINK_TIME = int.Parse(decoder.GetFirstValueByName("GET_DRINK_TIME"));
                AppSettings.Current.DRINK_UP_TIME = int.Parse(decoder.GetFirstValueByName("DRINK_UP_TIME"));
                AppSettings.Current.WALK_TIME_TO_STATION = int.Parse(decoder.GetFirstValueByName("WALK_TIME_TO_STATION"));

                AppSettings.Current.Save();
                args.ResponseMessage.StatusCode = HttpStatusCode.Ok;
                args.ResponseMessage.Content = new HttpStringContent("{ \"status\": \"good\" }");
                args.ResponseMessage.Content.Headers.ContentType =
                  new HttpMediaTypeHeaderValue("application/json");
                return;
            }

            if (req.RequestUri.LocalPath.ToLower().Contains("/www/") || req.RequestUri.LocalPath.ToLower().EndsWith("/"))
            {
                var filePath = req.RequestUri.AbsolutePath.Replace('/', '\\').ToLower();
                //filePath = filePath.Replace("\\departureboard", "");
                if (filePath.EndsWith(@"\"))
                    filePath += @"\www\index.html";
                try
                {
                    var fileStream = Windows.ApplicationModel.Package.Current.InstalledLocation.OpenStreamForReadAsync(filePath).GetAwaiter().GetResult();
                    res.StatusCode = HttpStatusCode.Ok;
                    res.Content = new HttpStreamContent(fileStream.AsInputStream());
                    res.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("text/html");
                }
                catch (FileNotFoundException e)
                {
                    string con = String.Format("<h1>{0} - not found</h1>\r\n", filePath);
                    con += "Exception: " + e.ToString();
                    res.Content = new HttpStringContent(con);
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("text/html");
                }
                return;
            }

            args.ResponseMessage.StatusCode = HttpStatusCode.NotFound;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name = "e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }

#endif
            Frame rootFrame = Window.Current.Content as Frame;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name = "sender">The Frame which failed navigation</param>
        /// <param name = "e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name = "sender">The source of the suspend request.</param>
        /// <param name = "e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}