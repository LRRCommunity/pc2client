// <copyright file="OverlayServerHandler.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace PC2Client
{
    /// <summary>
    /// Handler methods for providing data to the stream overlay.
    /// </summary>
    public static class OverlayServerHandler
    {
        private static HttpListener listener = null;
        private static string localAddress = null;
        private static ushort localPort = 0;

        /// <summary>
        /// Gets a value indicating whether the HTTP listener is active.
        /// </summary>
        internal static bool ListenerEnabled { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the HTTP listener is attempting to come online.
        /// </summary>
        internal static bool ListenerPending { get; private set; } = false;

        /// <summary>
        /// Inspects the local system for local (non-routable) IPv4 addresses.
        /// </summary>
        /// <returns>An IEnumerable of discovered local addresses.</returns>
        internal static IEnumerable<string> GetLocalIpAddresses()
        {
            List<string> addressList = new List<string>();
            NetworkInterfaceType[] allowableTypes = new NetworkInterfaceType[] { NetworkInterfaceType.Ethernet, NetworkInterfaceType.Wireless80211, NetworkInterfaceType.GigabitEthernet };
            Tuple<IPAddress, IPAddress>[] privateRanges = new Tuple<IPAddress, IPAddress>[]
            {
                Tuple.Create(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("255.0.0.0")),
                Tuple.Create(IPAddress.Parse("172.16.0.0"), IPAddress.Parse("255.240.0.0")),
                Tuple.Create(IPAddress.Parse("192.168.0.0"), IPAddress.Parse("255.255.0.0")),
            };

            IEnumerable<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .Where(i => allowableTypes.Contains(i.NetworkInterfaceType));
            foreach (NetworkInterface iface in interfaces)
            {
                var ifAddressInfo = iface.GetIPProperties().UnicastAddresses.Where(i => i.Address.AddressFamily == AddressFamily.InterNetwork);
                foreach (UnicastIPAddressInformation ifAddress in ifAddressInfo)
                {
                    IPAddress address = ifAddress.Address;
                    foreach (var range in privateRanges)
                    {
                        if (address.GetNetworkAddress(range.Item2).Equals(range.Item1))
                        {
                            addressList.Add(address.ToString());
                        }
                    }
                }
            }

            return addressList;
        }

        /// <summary>
        /// Extracts the IPv4 network address from a device address and subnet mask.
        /// </summary>
        /// <param name="deviceAddress">The device address being processed.</param>
        /// <param name="subnetMask">The subnet mask to apply.</param>
        /// <returns>The address of the IPv4 network.</returns>
        internal static IPAddress GetNetworkAddress(this IPAddress deviceAddress, IPAddress subnetMask)
        {
            byte[] deviceAddressBytes = deviceAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
            if (deviceAddressBytes.Length != subnetMaskBytes.Length)
            {
                throw new ArgumentException();
            }

            byte[] networkAddressBytes = new byte[deviceAddressBytes.Length];
            for (int i = 0; i < networkAddressBytes.Length; ++i)
            {
                networkAddressBytes[i] = (byte)(deviceAddressBytes[i] & subnetMaskBytes[i]);
            }

            return new IPAddress(networkAddressBytes);
        }

        /// <summary>
        /// Performs actions based on the state of the HTTP listener.
        /// </summary>
        /// <param name="sender">The control that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void ListenerEnableButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if ((!ListenerEnabled) && (!ListenerPending))
            {
                // Enable
                window.httpListenerToggle.Content = "Cancel";
                window.listenerActiveStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
                ListenerPending = true;

                localAddress = Properties.Settings.Default.IpAddressOverride;
                if (localAddress == string.Empty)
                {
                    localAddress = GetLocalIpAddresses().First();
                }

                localPort = Properties.Settings.Default.LocalListenerPort;
                string prefix = string.Format("http://*:{0}/", localPort);

                listener = new HttpListener();
                listener.Prefixes.Add(prefix);
                listener.Start();
                listener.BeginGetContext(SendResponse, null);

                window.httpListenerToggle.Content = "Disable";
                window.listenerActiveStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];
                window.IpAddressLabel.Content = string.Format("http://{0}:{1}/", localAddress, localPort);
                ListenerPending = false;
                ListenerEnabled = true;
            }
            else if (ListenerEnabled && (!ListenerPending))
            {
                // Disable
                listener.Close();

                window.httpListenerToggle.Content = "Enable";
                window.listenerActiveStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];
                window.IpAddressLabel.Content = "(Not Enabled)";
                ListenerPending = false;
                ListenerEnabled = false;
            }
            else if ((!ListenerEnabled) && ListenerPending)
            {
                // Cancel
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void SendResponse(IAsyncResult state)
        {
            try
            {
                HttpListenerContext ctx = listener.EndGetContext(state);
                if (ctx.Request.RawUrl.Equals("/"))
                {
                    LibPCars2.SharedMemory.TelemetryData telemetry = GameConnectHandler.Telemetry;
                    if (telemetry == null)
                    {
                        ctx.Response.StatusCode = 500;
                        ctx.Response.OutputStream.Close();
                    }
                    else
                    {
                        ctx.Response.ContentEncoding = Encoding.UTF8;
                        ctx.Response.ContentType = "application/json";

                        StreamWriter s = new StreamWriter(ctx.Response.OutputStream, new UTF8Encoding(false));
                        JsonSerializer jsonCodec = new JsonSerializer();
                        jsonCodec.Serialize(s, telemetry);
                        s.Close();
                    }
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.OutputStream.Close();
                }

                listener.BeginGetContext(SendResponse, null);
            }
            catch (ObjectDisposedException)
            {
                listener = null;
            }
        }
    }
}
