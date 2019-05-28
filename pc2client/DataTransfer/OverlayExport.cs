// <copyright file="OverlayExport.cs" company="LoadingReadyRun Community">
// Copyright (c) LoadingReadyRun Community. All rights reserved.
// </copyright>

using System.Numerics;

using LibPCars2.SharedMemory;

namespace PC2Client.DataTransfer
{
    /// <summary>
    /// Contains data points to be exported for the overlay.
    /// </summary>
    public partial class OverlayExport
    {
#pragma warning disable SA1516
#pragma warning disable SA1600
        public GameState GameState { get; private set; }
        public SessionState SessionState { get; private set; }
        public RaceState RaceState { get; private set; }

        public int ViewedParticipantIndex { get; private set; }
        public int NumParticipants { get; private set; }
        public ParticipantInfo[] Participants { get; private set; }
        public ParticipantInfoEx[] ParticipantsEx { get; private set; }

        public float Brake { get; private set; }
        public float Clutch { get; private set; }
        public float Steering { get; private set; }
        public float Throttle { get; private set; }

        public string CarName { get; private set; }
        public string CarClassName { get; private set; }

        public EventInfo EventDetails { get; private set; }
        public WeatherInfo Weather { get; private set; }

        public bool InvalidLap { get; private set; }
        public float BestLapTime { get; private set; }
        public float LastLapTime { get; private set; }
        public float CurrentTime { get; private set; }
        public float SplitTime { get; private set; }
        public float SplitTimeAhead { get; private set; }
        public float SplitTimeBehind { get; private set; }
        public float CurrentSector1Time { get; private set; }
        public float CurrentSector2Time { get; private set; }
        public float CurrentSector3Time { get; private set; }
        public float FastestSector1Time { get; private set; }
        public float FastestSector2Time { get; private set; }
        public float FastestSector3Time { get; private set; }
        public float PersonalFastestLapTime { get; private set; }
        public float PersonalFastestSector1Time { get; private set; }
        public float PersonalFastestSector2Time { get; private set; }
        public float PersonalFastestSector3Time { get; private set; }

        public FlagColor HighestFlagColor { get; private set; }
        public FlagReason HighestFlagReason { get; private set; }

        public PitMode PitMode { get; }
        public PitSchedule PitSchedule { get; }

        public CarFlags CarFlags { get; private set; }
        public float OilTemp { get; private set; }
        public float OilPressure { get; private set; }
        public float WaterTemp { get; private set; }
        public float WaterPressure { get; private set; }
        public float FuelPressure { get; private set; }
        public float FuelLevel { get; private set; }
        public float FuelCapacity { get; private set; }
        public float Speed { get; private set; }
        public float RPM { get; private set; }
        public float MaxRPM { get; private set; }
        public int Gear { get; private set; }
        public int NumGears { get; private set; }
        public float Odometer { get; private set; }
        public bool AntiLockActive { get; private set; }
        public int LastOpponentCollisionIndex { get; private set; }
        public float LastOpponentCollisionMagnitude { get; private set; }
        public bool BoostActive { get; private set; }
        public float BoostAmount { get; private set; }

        public Vector3 Orientation { get; private set; }
        public Vector3 LocalVelocity { get; private set; }
        public Vector3 WorldVelocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public Vector3 LocalAcceleration { get; private set; }
        public Vector3 WorldAcceleration { get; private set; }
        public Vector3 ExtentsCenter { get; private set; }

        public Tire[] Tires { get; private set; }

        public CrashState CrashState { get; private set; }
        public float AeroDamage { get; private set; }
        public float EngineDamage { get; private set; }

        public float EngineSpeed { get; private set; }
        public float EngineTorque { get; private set; }
        public float[] Wings { get; private set; }
        public float HandBrake { get; private set; }

        public int EnforcedPitStopLap { get; private set; }
        public float BrakeBias { get; private set; }
        public float TurboBoostPressure { get; private set; }
    }
}
