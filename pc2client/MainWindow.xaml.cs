// <copyright file="MainWindow.xaml.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PC2Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer clock = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            this.Closed += this.OnExit;

            this.gameConnectionToggle.Click += GameConnectHandler.GameConnectButton_Click;
            this.httpListenerToggle.Click += OverlayServerHandler.ListenerEnableButton_Click;

            this.clock = new DispatcherTimer();
            this.clock.Interval = TimeSpan.FromMilliseconds(1000);
            this.clock.Tick += this.OnTick;
            this.clock.Start();
        }

        /// <summary>
        /// Performs cleanup tasks when the application closes.
        /// </summary>
        /// <param name="sender">The control that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        public void OnExit(object sender, EventArgs e)
        {
            GameConnectHandler.Reset();
        }

        /// <summary>
        /// Updates data on each timer tick.
        /// </summary>
        /// <param name="sender">The timer that triggered this event.</param>
        /// <param name="e">State information for processing the event.</param>
        public void OnTick(object sender, EventArgs e)
        {
            if (GameConnectHandler.GameConnected)
            {
                GameConnectHandler.Tick(sender, e);
            }
        }
    }
}
