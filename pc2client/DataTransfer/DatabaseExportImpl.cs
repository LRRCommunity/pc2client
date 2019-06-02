// <copyright file="DatabaseExportImpl.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Reflection;

using LibPCars2.SharedMemory;

namespace PC2Client.DataTransfer
{
    /// <summary>
    /// Contains data points to be inserted into InfluxDB.
    /// </summary>
    public partial class DatabaseExport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseExport"/> class.
        /// </summary>
        /// <param name="t">Full telemetry data set to be reduced.</param>
        public DatabaseExport(TelemetryData t)
        {
            this.Timestamp = DateTime.Now;

            string username = Properties.Settings.Default.ApiUsername;
            this.SourceUser = string.IsNullOrWhiteSpace(username) ? "anonymous" : username;

            Type myType = this.GetType();
            var myProps = myType.GetProperties();

            Type theirType = t.GetType();

            foreach (PropertyInfo property in myProps)
            {
                PropertyInfo theirProp = theirType.GetProperty(property.Name, property.PropertyType);
                if (theirProp != null && property.SetMethod != null)
                {
                    var value = theirProp.GetValue(t);
                    property.SetValue(this, value);
                }
            }

            this.Participants = t.Participants.Take(t.NumParticipants).ToArray();
            this.ParticipantsEx = t.ParticipantsEx.Take(t.NumParticipants).ToArray();
        }
    }
}
