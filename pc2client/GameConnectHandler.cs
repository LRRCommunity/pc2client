﻿// <copyright file="GameConnectHandler.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using LibPCars2.SharedMemory;

namespace PC2Client
{
    /// <summary>
    /// Handler methods for managing the PC2 game "connection".
    /// </summary>
    public static class GameConnectHandler
    {
        private const string SharedMemoryTag = "$pcars2$";

        // Game connection objects
        private static MemoryMappedFile pCarsFile = null;
        private static MemoryMappedViewAccessor pCarsView = null;
        private static BackgroundWorker gameConnectionWorker = null;

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
            gameConnectionWorker = null;

            GameConnected = false;
            GameConnectionPending = false;

            MainWindow window = (MainWindow)Application.Current.MainWindow;
            if (window != null)
            {
                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectionToggle.IsEnabled = true;
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];
            }
        }

        /// <summary>
        /// Performs upkeep tasks at a regular interval.
        /// </summary>
        /// <param name="sender">The object that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        internal static void Tick(object sender, EventArgs e)
        {
            if ((pCarsView != null) && (!pCarsView.CanRead))
            {
                Reset();
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

                    e.Result = pCarsView;
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

                window.gameConnectionToggle.Content = "Connect";
                window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];

                GameConnected = false;
                GameConnectionPending = false;

                throw e.Error;
            }
            else if (e.Cancelled)
            {
                ReleaseMappedMemory();

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
                    window.gameConnectionToggle.Content = "Disconnect";
                    window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["greenStoplight"];

                    GameConnected = true;
                    GameConnectionPending = false;
                }
                else
                {
                    // Failed to open file
                    ReleaseMappedMemory();

                    window.gameConnectionToggle.Content = "Connect";
                    window.gameConnectedStoplight.Fill = (Brush)Application.Current.Resources["redStoplight"];

                    GameConnected = false;
                    GameConnectionPending = false;
                }
            }

            gameConnectionWorker = null;
        }

        private static TelemetryData ReadTelemetry()
        {
            return null;
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
    }
}