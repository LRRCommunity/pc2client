// <copyright file="GameConnectHandler.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Media;

using AssettoCorsaSharedMemory;
using LiteDB;

namespace PC2Client
{
    /// <summary>
    /// Handler methods for managing the PC2 game "connection".
    /// </summary>
    public static class GameConnectHandler
    {
        // Game connection objects
        private static LiteDatabase logDatabase = null;

        private static AssettoCorsa assetto = new AssettoCorsa();
        private static int LastPhysicsId = 0;
        private static int LastGraphicsId = 0;

        /// <summary>
        /// Gets a value indicating whether the shared memory is currently open.
        /// </summary>
        internal static bool GameConnected { get; private set; } = false;

        internal static void onGfxUpdate(object sender, GraphicsEventArgs e)
        {
            Graphics gfx = e.Graphics;
            if (gfx.PacketId != LastGraphicsId)
            {
                DataTransfer.LocalDbEntry entry = new DataTransfer.LocalDbEntry
                {
                    Timestamp = DateTime.Now,
                    CompressedTelemetry = CompressObject(gfx),
                };
                logDatabase.GetCollection<DataTransfer.LocalDbEntry>("GraphicsData").Insert(entry);
                LastGraphicsId = gfx.PacketId;
            }
        }

        internal static void onPhysicsUpdate(object sender, PhysicsEventArgs e)
        {
            Physics phy = e.Physics;
            if (phy.PacketId != LastPhysicsId)
            {
                DataTransfer.LocalDbEntry entry = new DataTransfer.LocalDbEntry
                {
                    Timestamp = DateTime.Now,
                    CompressedTelemetry = CompressObject(phy),
                };
                logDatabase.GetCollection<DataTransfer.LocalDbEntry>("PhysicsData").Insert(entry);
                LastPhysicsId = phy.PacketId;
            }
        }

        internal static void onStaticUpdate(object sender, StaticInfoEventArgs e)
        {
            if (!GameConnected)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow window = (MainWindow)Application.Current.MainWindow;
                    window.gameConnectionToggle.Content = "Disconnect";
                    window.gameConnectionToggle.IsEnabled = true;
                    window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];
                    GameConnected = true;
                });
            }

            StaticInfo info = e.StaticInfo;
            DataTransfer.LocalDbEntry entry = new DataTransfer.LocalDbEntry
            {
                Timestamp = DateTime.Now,
                CompressedTelemetry = CompressObject(info),
            };
            logDatabase.GetCollection<DataTransfer.LocalDbEntry>("StaticData").Insert(entry);
        }

        /// <summary>
        /// Performs actions based on the state of the game "connection".
        /// </summary>
        /// <param name="sender">The control that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void GameConnectButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if (!GameConnected)
            {
                // Connect
                window.gameConnectionToggle.IsEnabled = false;
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];

                InitializeDatabase();

                assetto.GraphicsInterval = 1000;
                assetto.GraphicsUpdated += onGfxUpdate;

                assetto.PhysicsInterval = 10;
                assetto.PhysicsUpdated += onPhysicsUpdate;

                assetto.StaticInfoInterval = 5000;
                assetto.StaticInfoUpdated += onStaticUpdate;

                assetto.Start();
            }
            else
            {
                Reset();
            }
        }

        /// <summary>
        /// Releases memory and restores the GameConnectHandler to its default state.
        /// </summary>
        internal static void Reset()
        {
            assetto.Stop();
            assetto.GraphicsUpdated -= onGfxUpdate;
            assetto.PhysicsUpdated -= onPhysicsUpdate;
            assetto.StaticInfoUpdated -= onStaticUpdate;

            GameConnected = false;

            if (logDatabase != null)
            {
                logDatabase.Dispose();
                logDatabase = null;
            }

            MainWindow window = (MainWindow)Application.Current.MainWindow;
            if (window != null)
            {
                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];
            }
        }

        private static byte[] CompressObject(object data)
        {
            if (!data.GetType().IsSerializable)
            {
                return Array.Empty<byte>();
            }

            using (MemoryStream mem = new MemoryStream())
            {
                using (GZipStream compressor = new GZipStream(mem, CompressionLevel.Fastest, true))
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(compressor, data);
                }

                mem.Seek(0, SeekOrigin.Begin);

                byte[] compressedData = new byte[mem.Length];
                mem.Read(compressedData, 0, compressedData.Length);
                return compressedData;
            }
        }

        private static void InitializeDatabase()
        {
            string userName = Properties.Settings.Default.ApiUsername;
            string fileName = "./LRRmans_2020.ldb";

            if (!string.IsNullOrWhiteSpace(userName))
            {
                fileName = string.Format("./{0}.LRRmans_2020.ldb", userName);
            }

            logDatabase = new LiteDatabase(fileName);
            logDatabase.Log.Logging += WriteDatabaseLog;
            logDatabase.Log.Level = Logger.ERROR | Logger.RECOVERY;
            logDatabase.GetCollection<DataTransfer.LocalDbEntry>("StaticData").EnsureIndex(t => t.Timestamp);
            logDatabase.GetCollection<DataTransfer.LocalDbEntry>("GraphicsData").EnsureIndex(t => t.Timestamp);
            logDatabase.GetCollection<DataTransfer.LocalDbEntry>("PhysicsData").EnsureIndex(t => t.Timestamp);
        }

        private static void WriteDatabaseLog(string logEntry)
        {
            using (var logFile = new StreamWriter("./LRRmans.LiteDB.log", true, new UTF8Encoding(false)))
            {
                logFile.WriteLine(logEntry);
            }
        }
    }
}
