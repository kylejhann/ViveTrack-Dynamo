﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Numerics;

using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;

using Valve.VR;

using ViveTrack;

public class Start
{
    internal Start() { }

    public static OpenvrWrapper Vive = new OpenvrWrapper();
    public static Matrix4x4 CalibrationTransform = Matrix4x4.Identity;
    //public static CoordinateSystem CalibrationTransform = CoordinateSystem.Identity();
    //public static Plane CalibrationPlane;
    public static string OutMsg;


    /// <summary>
    /// Start HTC Vive. Make sure SteamVR is running, and Dynamo is set to Periodic update.
    /// </summary>
    /// <param name="Connect">Connect to SteamVR?</param>
    /// <returns name = "Msg">Summary of the VR setting.</returns>
    /// <returns name = "Vive">The main Vive object.</returns>
    [CanUpdatePeriodically(true)]
    [MultiReturn(new[] { "Msg", "Vive" })]
    public static Dictionary<string, object> ConnectToVive(bool Connect = true)
    {
        if (!Connect) return null;

        if (!DetectSteamVR())
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("SteamVR not running.Please run SteamVR first.");
            return null;
        }

        Vive.Connect();
        Vive.Update();
        if (Vive.Success)
        {
            OutMsg = Vive.TrackedDevices.Summary();
            Vive.TrackedDevices.UpdatePoses();
        }
        else
        {
            OutMsg = "Vive is not setup correctly!! Detailed Reason:\n" + Vive.errorMsg + "\nCheck online the error code for more information.";
            Vive.Connect();
        }

        return new Dictionary<string, object> {
            { "Msg", OutMsg },
            { "Vive", Vive }
        };
    }


    internal static bool DetectSteamVR()
    {
        Process[] vrServer = Process.GetProcessesByName("vrserver");
        Process[] vrMonitor = Process.GetProcessesByName("vrmonitor");
        if ((vrServer.Length == 0) || (vrMonitor.Length == 0)) return false;
        if (!OpenVR.IsRuntimeInstalled()) return false;

        return true;
    }




    public static object CalibrateOrigin()
    {
        return null;
    }
}

