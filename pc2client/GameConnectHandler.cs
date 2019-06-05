// <copyright file="GameConnectHandler.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using LibPCars2.SharedMemory;
using LiteDB;

namespace PC2Client
{
    /// <summary>
    /// Handler methods for managing the PC2 game "connection".
    /// </summary>
    public static class GameConnectHandler
    {
        private const int SequenceNumberOffset = 0x1C98;
        private const string SharedMemoryTag = "$pcars2$";

        // Game connection objects
        private static BackgroundWorker gameConnectionWorker = null;
        private static bool gameIsAlive = false;
        private static uint lastSequenceNumber = 0;
        private static MemoryMappedFile pCarsFile = null;
        private static MemoryMappedViewAccessor pCarsView = null;
        private static byte[] rawData = null;
        private static LiteDatabase logDatabase = null;

        /// <summary>
        /// Gets a value indicating whether the shared memory is currently open.
        /// </summary>
        internal static bool GameConnected { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether we are attempting to open shared memory.
        /// </summary>
        internal static bool GameConnectionPending { get; private set; } = false;

        /// <summary>
        /// Gets the most recent telemetry data from Project CARS 2.
        /// </summary>
        internal static TelemetryData Telemetry { get; private set; } = null;

        /// <summary>
        /// Performs actions based on the state of the game "connection".
        /// </summary>
        /// <param name="sender">The control that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void GameConnectButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if ((!GameConnected) && (!GameConnectionPending))
            {
                // Connect
                window.gameConnectionToggle.Content = "Cancel";
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
                GameConnectionPending = true;

                InitializeDatabase();

                gameConnectionWorker = new BackgroundWorker();
                gameConnectionWorker.WorkerSupportsCancellation = true;
                gameConnectionWorker.DoWork += BeginConnect;
                gameConnectionWorker.RunWorkerCompleted += FinishConnect;
                gameConnectionWorker.RunWorkerAsync();
            }
            else if (GameConnected && (!GameConnectionPending))
            {
                // Disconnect
                Reset();
            }
            else if ((!GameConnected) && GameConnectionPending)
            {
                // Cancel
                if (gameConnectionWorker != null)
                {
                    ((Button)sender).IsEnabled = false;
                    gameConnectionWorker.CancelAsync();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Releases memory and restores the GameConnectHandler to its default state.
        /// </summary>
        internal static void Reset()
        {
            ReleaseMappedMemory();
            rawData = null;
            gameConnectionWorker = null;

            gameIsAlive = false;
            lastSequenceNumber = 0;
            GameConnected = false;
            GameConnectionPending = false;
            Telemetry = null;

            if (logDatabase != null)
            {
                logDatabase.Dispose();
                logDatabase = null;
            }

            MainWindow window = (MainWindow)Application.Current.MainWindow;
            if (window != null)
            {
                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectionToggle.IsEnabled = true;
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];
                window.SequenceNumberLabel.Content = "0 (Not Connected)";
            }
        }

        /// <summary>
        /// Performs upkeep tasks at a regular interval.
        /// </summary>
        /// <param name="sender">The object that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void Tick(object sender, EventArgs e)
        {
            if (gameIsAlive)
            {
                Telemetry = ReadTelemetry();
                if (Telemetry != null)
                {
                    ((MainWindow)Application.Current.MainWindow).SequenceNumberLabel.Content = string.Format("{0:D}", Telemetry.SequenceNumber);
                }
            }
            else
            {
                int processCount = Process.GetProcesses().Count(p => p.ProcessName.StartsWith("pcars2", true, null));
                if (processCount > 0)
                {
                    TelemetryData t = ReadTelemetry(true);
                    if (t != null && t.SequenceNumber == 0)
                    {
                        gameIsAlive = true;
                        Telemetry = t;
                        ((MainWindow)Application.Current.MainWindow).SequenceNumberLabel.Content = string.Format("{0:D}", Telemetry.SequenceNumber);
                        ((MainWindow)Application.Current.MainWindow).gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];
                    }
                }
            }
        }

        private static void BeginConnect(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            int remainingAttempts = 5;
            e.Result = null;

            while (remainingAttempts > 0)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                try
                {
                    pCarsFile = MemoryMappedFile.OpenExisting(SharedMemoryTag, MemoryMappedFileRights.Read);
                    pCarsView = pCarsFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                    rawData = new byte[pCarsView.Capacity];
                    TelemetryData t = ReadTelemetry(true);

                    e.Result = t;
                    return;
                }
                catch (FileNotFoundException)
                {
                    // Game hasn't finished loading, just wait.
                }

                if (--remainingAttempts > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                }
            }
        }

        private static void FinishConnect(object sender, RunWorkerCompletedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if (e.Error != null)
            {
                ReleaseMappedMemory();
                rawData = null;

                if (logDatabase != null)
                {
                    logDatabase.Dispose();
                    logDatabase = null;
                }

                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];

                GameConnected = false;
                GameConnectionPending = false;

                throw e.Error;
            }
            else if (e.Cancelled)
            {
                ReleaseMappedMemory();
                rawData = null;

                if (logDatabase != null)
                {
                    logDatabase.Dispose();
                    logDatabase = null;
                }

                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectionToggle.IsEnabled = true;
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];

                GameConnected = false;
                GameConnectionPending = false;
            }
            else
            {
                if (e.Result != null)
                {
                    // Opened successfully
                    gameIsAlive = true;
                    Telemetry = (TelemetryData)e.Result;
                    window.SequenceNumberLabel.Content = string.Format("{0:D}", Telemetry.SequenceNumber);

                    window.gameConnectionToggle.Content = "Disconnect";
                    window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];

                    GameConnected = true;
                    GameConnectionPending = false;
                }
                else
                {
                    // Failed to open file
                    ReleaseMappedMemory();
                    rawData = null;

                    if (logDatabase != null)
                    {
                        logDatabase.Dispose();
                        logDatabase = null;
                    }

                    window.gameConnectionToggle.Content = "Connect";
                    window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];

                    GameConnected = false;
                    GameConnectionPending = false;
                }
            }

            gameConnectionWorker = null;
        }

        private static void InitializeDatabase()
        {
            string userName = Properties.Settings.Default.ApiUsername;
            string fileName = "./ProjectCARS.ldb";

            if (!string.IsNullOrWhiteSpace(userName))
            {
                fileName = string.Format("./{0}.ProjectCARS.ldb", userName);
            }

            logDatabase = new LiteDatabase(fileName);
            logDatabase.Log.Logging += WriteDatabaseLog;
            logDatabase.Log.Level = Logger.ERROR | Logger.RECOVERY;
            logDatabase.GetCollection<TelemetryData>("telemetryLog").EnsureIndex(t => t.SequenceNumber);
        }

        private static uint? ReadRawData(int attempts = 5)
        {
            int i = attempts;
            uint sequenceNumberBegin, sequenceNumberEnd = 0;
            do
            {
                int j = attempts;
                do
                {
                    sequenceNumberBegin = pCarsView.ReadUInt32(SequenceNumberOffset);
                }
                while ((sequenceNumberBegin % 2 == 1) && (--j > 0));

                if (j == 0)
                {
                    continue;
                }

                pCarsView.ReadArray(0, rawData, 0, rawData.Length);

                sequenceNumberEnd = BitConverter.ToUInt32(rawData, SequenceNumberOffset);
            }
            while ((sequenceNumberBegin != sequenceNumberEnd) && (--i > 0));

            if (i == 0)
            {
                return null;
            }
            else
            {
                return sequenceNumberEnd;
            }
        }

        private static TelemetryData ReadTelemetry(bool force = false)
        {
            uint? sequenceNumberEnd = ReadRawData();
            if (sequenceNumberEnd == null)
            {
                return Telemetry;
            }

            TelemetryData tData = null;

            if (force)
            {
                tData = new TelemetryData(rawData);
                logDatabase.GetCollection<TelemetryData>("telemetryLog").Insert(DateTime.Now, tData);
                return tData;
            }

            if (sequenceNumberEnd > 0 && sequenceNumberEnd == lastSequenceNumber)
            {
                int processCount = Process.GetProcesses().Count(p => p.ProcessName.StartsWith("pcars2", true, null));
                if (processCount == 0)
                {
                    // Game crashed, clean up the mess
                    gameIsAlive = false;
                    ((MainWindow)Application.Current.MainWindow).gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["yellowStoplight"];
                    return null;
                }
            }
            else
            {
                lastSequenceNumber = sequenceNumberEnd.Value;
            }

            tData = new TelemetryData(rawData);
            logDatabase.GetCollection<TelemetryData>("telemetryLog").Insert(DateTime.Now, tData);
            return tData;
        }

        private static void ReleaseMappedMemory()
        {
            if (pCarsView != null)
            {
                pCarsView.Dispose();
                pCarsView = null;
            }

            if (pCarsFile != null)
            {
                pCarsFile.Dispose();
                pCarsFile = null;
            }
        }

        private static void WriteDatabaseLog(string logEntry)
        {
            using (var logFile = new StreamWriter("./ProjectCARS.LiteDB.log", true, new UTF8Encoding(false)))
            {
                logFile.WriteLine(logEntry);
            }
        }
    }
}
