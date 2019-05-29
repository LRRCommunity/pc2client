// <copyright file="ApiPostHandler.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Media;

using LibPCars2.SharedMemory;
using Newtonsoft.Json;

namespace PC2Client
{
    /// <summary>
    /// Handler methods for posting data to cheetoJack's web API.
    /// </summary>
    public static class ApiPostHandler
    {
        private static HttpClient client = null;
        private static uint updatesSent = 0;

        /// <summary>
        /// Gets a value indicating whether the HTTP sender is currently active.
        /// </summary>
        internal static bool SenderEnabled { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the HTTP sender is becoming active.
        /// </summary>
        internal static bool SenderPending { get; private set; } = false;

        /// <summary>
        /// Responds to UI events based on the sender's current status.
        /// </summary>
        /// <param name="sender">The control that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void SenderEnableButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if ((!SenderEnabled) && (!SenderPending))
            {
                // Enable
                client = new HttpClient();
                if (!string.IsNullOrEmpty(Properties.Settings.Default.ApiUsername))
                {
                    string credentialString = string.Format("{0}:{1}", Properties.Settings.Default.ApiUsername, Properties.Settings.Default.ApiPassword);
                    string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentialString));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
                }

                updatesSent = 0;

                window.UpdateCountLabel.Content = updatesSent.ToString();
                window.serverConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
                window.databaseConnectionToggle.Content = "Disconnect";

                SenderEnabled = true;
            }
            else if (SenderEnabled && (!SenderPending))
            {
                // Disable
                client.CancelPendingRequests();
                client.Dispose();
                client = null;

                SenderEnabled = false;

                window.UpdateCountLabel.Content = "0 (Not Connected)";
                window.serverConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];
                window.databaseConnectionToggle.Content = "Connect";
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Performs upkeep tasks at a regular interval.
        /// </summary>
        /// <param name="sender">The object that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static async void Tick(object sender, EventArgs e)
        {
            TelemetryData telemetry = GameConnectHandler.Telemetry;
            if (telemetry == null || client == null)
            {
                return;
            }

            DataTransfer.DatabaseExport jsonOutput = new DataTransfer.DatabaseExport(telemetry);

            MemoryStream outStream = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(outStream, new UTF8Encoding(false), 1024, true))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                JsonSerializer codec = new JsonSerializer();
                codec.Serialize(jsonTextWriter, jsonOutput);
                jsonTextWriter.Flush();
            }

            outStream.Seek(0, SeekOrigin.Begin);

            HttpContent content = new StreamContent(outStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            MainWindow window = (MainWindow)Application.Current.MainWindow;
            try
            {
                HttpResponseMessage response = await client.PostAsync(Properties.Settings.Default.RemotePostEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    ++updatesSent;
                    window.UpdateCountLabel.Content = updatesSent.ToString();
                    window.serverConnectedStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];
                }
                else
                {
                    window.serverConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
                }
            }
            catch (HttpRequestException)
            {
                window.serverConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
            }
        }
    }
}
