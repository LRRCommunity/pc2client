// <copyright file="ReturnData.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC2Client.DataTransfer
{
    /// <summary>
    /// Contains data from external sources to send back upstream.
    /// </summary>
    public class ReturnData
    {
        /// <summary>
        /// Gets the name of the person currently in the hot seat.
        /// </summary>
        public string DriverName { get; internal set; }
    }
}
