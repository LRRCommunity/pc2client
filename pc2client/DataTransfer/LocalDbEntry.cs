// <copyright file="LocalDbEntry.cs" company="LoadingReadyRun Community">
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
    /// Contains data to be stored locally in LiteDB.
    /// </summary>
    internal class LocalDbEntry
    {
#pragma warning disable SA1600
        public LiteDB.ObjectId Id { get; internal set; }

        public DateTime Timestamp { get; internal set; }

        public string Driver { get; internal set; }

        public byte[] CompressedTelemetry { get; internal set; }
    }
}
