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

using DSPlane = Autodesk.DesignScript.Geometry.Plane;


/// <summary>
/// An object which knows how to draw itself in the background preview and uses a transform to take 
/// advantage of the GPU to alter that background visualization. The original geometry remains unaltered,
/// only the visualization is transformed.
/// </summary>
public class TransformableExample : IGraphicItem
{
    public Geometry Geometry { get; private set; }
    private CoordinateSystem transform { get; set; }

    //want to hide this constructor
    [Autodesk.DesignScript.Runtime.IsVisibleInDynamoLibrary(false)]
    public TransformableExample(Geometry geometry)
    {
        Geometry = geometry;
        //initial transform is just at the origin
        transform = CoordinateSystem.ByOrigin(0, 0, 0);
    }

    /// <summary>
    /// Create a TranformableExample class which stores a Geometry object and a Transform.
    /// </summary>
    /// <param name="geometry"> a geometry object</param>
    /// <returns></returns>
    public static TransformableExample ByGeometry(Autodesk.DesignScript.Geometry.Geometry geometry)
    {
        var newTransformableThing = new TransformableExample(geometry);
        return newTransformableThing;
    }

    /// <summary>
    /// This method sets the transform on the object and returns a reference to the object so
    /// the tessellate method is called and the new visualization shows in the background preview.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public TransformableExample TransformObject(CoordinateSystem transform)
    {
        this.transform = transform;
        return this;
    }

    /// <summary>
    /// This method is actually called by Dynamo when it attempts to render the TransformableExample. 
    /// class.
    /// </summary>
    /// <param name="package"></param>
    /// <param name="parameters"></param>
    //hide this method from search
    [Autodesk.DesignScript.Runtime.IsVisibleInDynamoLibrary(false)]

    public void Tessellate(IRenderPackage package, TessellationParameters parameters)
    {
        //could increase performance further by cacheing this tesselation
        Geometry.Tessellate(package, parameters);

        //we use reflection here because this API was added in Dynamo 1.1 and might not exist for a user in Dynamo 1.0
        //if you do not care about ensuring comptability of your zero touch node with 1.0 you can just call SetTransform directly
        //by casting the rendering package to ITransformable.

        //look for the method SetTransform with the double[] argument list.
        var method = package.GetType().
        GetMethod("SetTransform", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(double[]) }, null);

        //if the method exists call it using our current transform.
        if (method != null)
        {
            method.Invoke(package, new object[] { new double[]
        {transform.XAxis.X,transform.XAxis.Y,transform.XAxis.Z,0,
        transform.YAxis.X,transform.YAxis.Y,transform.YAxis.Z,0,
        transform.ZAxis.X,transform.ZAxis.Y,transform.ZAxis.Z,0,
        transform.Origin.X,transform.Origin.Y,transform.Origin.Z,1
        }});
        }

    }

}

public class HMDMesh : IGraphicItem
{
    #region MESHDATA
    private readonly float[] vertices = new float[] {
        0, 0.22f, 0,
        0.1f, 0.22f, 0,
        0.1f, -0.06f, 0,
        0, -0.13f, 0,
        -0.1f, -0.06f, 0,
        -0.1f, 0.20f, 0
    };

    private readonly float[] normals = new float[]
    {
        0,0,1,
        0,0,1,
        0,0,1,
        0,0,1,
        0,0,1,
        0,0,1
    };

    private readonly uint[] faces = new uint[]
    {
        0,2,1,
        0,3,2,
        0,4,3,
        0,5,4
    };
    #endregion

    private float[] verticesTrans;
    private float[] normalTrans;
    bool applyTransform = false;
    private Matrix4x4 _transform;

    internal HMDMesh()
    {
        verticesTrans = new float[vertices.Length];
        normalTrans = new float[normals.Length];
    }

    private void PushTriangleVertex(IRenderPackage package, Point p, Vector n)
    {
        package.AddTriangleVertex(p.X, p.Y, p.Z);
        package.AddTriangleVertexColor(255, 255, 255, 255);
        package.AddTriangleVertexNormal(n.X, n.Y, n.Z);
        package.AddTriangleVertexUV(0, 0);
    }

    internal void Transform(CoordinateSystem cs)
    {
        _transform = Util.CoordinateSystemToMatrix4x4(cs);

        Vector3 v = new Vector3();
        Vector3 n = new Vector3();

        for (int i = 0; i < vertices.Length; i += 3)
        {
            v.X = vertices[i];
            v.Y = vertices[i + 1];
            v.Z = vertices[i + 2];

            n.X = normals[i];
            n.Y = normals[i + 1];
            n.Z = normals[i + 2];

            v = Vector3.Transform(v, _transform);
            n = Vector3.Transform(n, _transform);

            verticesTrans[i] = v.X;
            verticesTrans[i + 1] = v.Y;
            verticesTrans[i + 2] = v.Z;

            normalTrans[i] = n.X;
            normalTrans[i + 1] = n.Y;
            normalTrans[i + 2] = n.Z;
        }
    }

    
    void IGraphicItem.Tessellate(IRenderPackage package, TessellationParameters parameters)
    {
        uint fid;
        for (int i = 0; i < faces.Length; i++)
        {
            fid = 3 * faces[i];
            package.AddTriangleVertex(verticesTrans[fid], verticesTrans[fid + 1], verticesTrans[fid + 2]);
            package.AddTriangleVertexColor(255, 0, 0, 255);
            package.AddTriangleVertexNormal(normalTrans[fid], normalTrans[fid + 1], normalTrans[fid + 2]);
            package.AddTriangleVertexUV(0, 0);
        }
    }
}


public class Devices
{
    internal Devices() { }


    //  ██╗  ██╗███╗   ███╗██████╗ 
    //  ██║  ██║████╗ ████║██╔══██╗
    //  ███████║██╔████╔██║██║  ██║
    //  ██╔══██║██║╚██╔╝██║██║  ██║
    //  ██║  ██║██║ ╚═╝ ██║██████╔╝
    //  ╚═╝  ╚═╝╚═╝     ╚═╝╚═════╝ 
    //                             

    // Due to Zero Touch nature, these instances will be shared by all nodes... Not ideal, but good enough for 99% of situations.
    private static VrTrackedDevice _HMD_CurrentTrackedDevice;
    private static CoordinateSystem _HMD_CurrentCS;
    private static CoordinateSystem _HMD_OldCS;
    private static DSPlane _HMD_OldPlane;
    private static HMDMesh _HMD_Mesh = new HMDMesh();

    /// <summary>
    /// Tracking of HTC Vive Head Mounted Display (HMD).
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem" })]
    public static Dictionary<string, object> HMD(object Vive, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["HMD"];
        if (list.Count == 0)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("No HMD detected.");
            return null;
        }


        if (tracked)
        {
            int index = wrapper.TrackedDevices.IndexesByClasses["HMD"][0];

            _HMD_CurrentTrackedDevice = wrapper.TrackedDevices.AllDevices[index];
            _HMD_CurrentTrackedDevice.ConvertPose();

            _HMD_CurrentCS = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_HMD_CurrentTrackedDevice.CorrectedMatrix4x4, false))
            {
                _HMD_CurrentCS = _HMD_CurrentCS.Transform(cm);
            }

            _HMD_OldCS = _HMD_CurrentCS;
            _HMD_OldPlane = CoordinateSystemToPlane(_HMD_OldCS);

            _HMD_Mesh.Transform(_HMD_OldCS);

        }

        // @TODO: figure out mesh representation

        return new Dictionary<string, object>()
        {
            { "Mesh", _HMD_Mesh },
            { "Plane", _HMD_OldPlane },
            { "CoordinateSystem", _HMD_OldCS }
        };
    }



    //   ██████╗ ██████╗ ███╗   ██╗████████╗██████╗  ██████╗ ██╗     ██╗     ███████╗██████╗ ███████╗
    //  ██╔════╝██╔═══██╗████╗  ██║╚══██╔══╝██╔══██╗██╔═══██╗██║     ██║     ██╔════╝██╔══██╗██╔════╝
    //  ██║     ██║   ██║██╔██╗ ██║   ██║   ██████╔╝██║   ██║██║     ██║     █████╗  ██████╔╝███████╗
    //  ██║     ██║   ██║██║╚██╗██║   ██║   ██╔══██╗██║   ██║██║     ██║     ██╔══╝  ██╔══██╗╚════██║
    //  ╚██████╗╚██████╔╝██║ ╚████║   ██║   ██║  ██║╚██████╔╝███████╗███████╗███████╗██║  ██║███████║
    //   ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝   ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚══════╝╚══════╝╚═╝  ╚═╝╚══════╝
    //                                                                                               

    private static VrTrackedDevice _Controller1_CurrentTrackedDevice;
    private static CoordinateSystem _Controller1_CurrentCS;
    private static CoordinateSystem _Controller1_OldCS;
    private static DSPlane _Controller1_OldPlane;

    /// <summary>
    /// Tracking of HTC Vive Controller #1.
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    /// <returns name = "TriggerPressed">Is the trigger pressed?</returns>
    /// <returns name = "TriggerClicked">Is the trigger clicked (pressed all the way in)?</returns>
    /// <returns name = "TriggerValue">Trigger level from 0 (not pressed) to 1 (fully pressed).</returns>
    /// <returns name = "TouchPadTouched">Is the touchpad being touched?</returns>
    /// <returns name = "TouchPadClicked">Is the touchpad being clicked (pressed all the way in)?</returns>
    /// <returns name = "TouchPadValueX">Touch value from -1 (left) to 1 (right).</returns>
    /// <returns name = "TouchPadValueY">Touch value from -1 (bottom) to 1 (top).</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem", "TriggerPressed", "TriggerClicked", "TriggerValue", "TouchPadTouched", "TouchPadClicked", "TouchPadValueX", "TouchPadValueY" })]
    public static Dictionary<string, object> Controller1(object Vive, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["Controller"];
        if (list.Count == 0)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("No Controller detected.");
            return null;
        }

        if (tracked)
        {
            int index = wrapper.TrackedDevices.IndexesByClasses["Controller"][0];

            _Controller1_CurrentTrackedDevice = wrapper.TrackedDevices.AllDevices[index];
            _Controller1_CurrentTrackedDevice.ConvertPose();

            _Controller1_CurrentCS = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_Controller1_CurrentTrackedDevice.CorrectedMatrix4x4, false))
            {
                _Controller1_CurrentCS = _Controller1_CurrentCS.Transform(cm);
            }

            _Controller1_OldCS = _Controller1_CurrentCS;
            _Controller1_OldPlane = CoordinateSystemToPlane(_Controller1_OldCS);
        }

        // @TODO: figure out mesh representation

        _Controller1_CurrentTrackedDevice.GetControllerTriggerState();

        return new Dictionary<string, object>()
        {
            { "Mesh", null },
            { "Plane", _Controller1_OldPlane },
            { "CoordinateSystem", _Controller1_OldCS },
            { "TriggerPressed", _Controller1_CurrentTrackedDevice.TriggerPressed },
            { "TriggerClicked", _Controller1_CurrentTrackedDevice.TriggerClicked },
            { "TriggerValue", _Controller1_CurrentTrackedDevice.TriggerValue },
            { "TouchPadTouched", _Controller1_CurrentTrackedDevice.TouchPadTouched },
            { "TouchPadClicked", _Controller1_CurrentTrackedDevice.TouchPadClicked },
            { "TouchPadValueX", _Controller1_CurrentTrackedDevice.TouchPadValueX },
            { "TouchPadValueY", _Controller1_CurrentTrackedDevice.TouchPadValueY }
        };
    }



    private static VrTrackedDevice _Controller2_CurrentTrackedDevice;
    private static CoordinateSystem _Controller2_CurrentCS;
    private static CoordinateSystem _Controller2_OldCS;
    private static DSPlane _Controller2_OldPlane;

    /// <summary>
    /// Tracking of HTC Vive Controller #2.
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    /// <returns name = "TriggerPressed">Is the trigger pressed?</returns>
    /// <returns name = "TriggerClicked">Is the trigger clicked (pressed all the way in)?</returns>
    /// <returns name = "TriggerValue">Trigger level from 0 (not pressed) to 1 (fully pressed).</returns>
    /// <returns name = "TouchPadTouched">Is the touchpad being touched?</returns>
    /// <returns name = "TouchPadClicked">Is the touchpad being clicked (pressed all the way in)?</returns>
    /// <returns name = "TouchPadValueX">Touch value from -1 (left) to 1 (right).</returns>
    /// <returns name = "TouchPadValueY">Touch value from -1 (bottom) to 1 (top).</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem", "TriggerPressed", "TriggerClicked", "TriggerValue", "TouchPadTouched", "TouchPadClicked", "TouchPadValueX", "TouchPadValueY" })]
    public static Dictionary<string, object> Controller2(object Vive, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["Controller"];
        if (list.Count < 2)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("No Controller detected.");
            return null;
        }

        if (tracked)
        {
            int index = wrapper.TrackedDevices.IndexesByClasses["Controller"][1];

            _Controller2_CurrentTrackedDevice = wrapper.TrackedDevices.AllDevices[index];
            _Controller2_CurrentTrackedDevice.ConvertPose();

            _Controller2_CurrentCS = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_Controller2_CurrentTrackedDevice.CorrectedMatrix4x4, false))
            {
                _Controller2_CurrentCS = _Controller2_CurrentCS.Transform(cm);
            }

            _Controller2_OldCS = _Controller2_CurrentCS;
            _Controller2_OldPlane = CoordinateSystemToPlane(_Controller2_OldCS);
        }

        // @TODO: figure out mesh representation

        _Controller2_CurrentTrackedDevice.GetControllerTriggerState();

        return new Dictionary<string, object>()
        {
            { "Mesh", null },
            { "Plane", _Controller2_OldPlane },
            { "CoordinateSystem", _Controller2_OldCS },
            { "TriggerPressed", _Controller2_CurrentTrackedDevice.TriggerPressed },
            { "TriggerClicked", _Controller2_CurrentTrackedDevice.TriggerClicked },
            { "TriggerValue", _Controller2_CurrentTrackedDevice.TriggerValue },
            { "TouchPadTouched", _Controller2_CurrentTrackedDevice.TouchPadTouched },
            { "TouchPadClicked", _Controller2_CurrentTrackedDevice.TouchPadClicked },
            { "TouchPadValueX", _Controller2_CurrentTrackedDevice.TouchPadValueX },
            { "TouchPadValueY", _Controller2_CurrentTrackedDevice.TouchPadValueY }
        };
    }




    //  ██╗     ██╗ ██████╗ ██╗  ██╗████████╗██╗  ██╗ ██████╗ ██╗   ██╗███████╗███████╗███████╗
    //  ██║     ██║██╔════╝ ██║  ██║╚══██╔══╝██║  ██║██╔═══██╗██║   ██║██╔════╝██╔════╝██╔════╝
    //  ██║     ██║██║  ███╗███████║   ██║   ███████║██║   ██║██║   ██║███████╗█████╗  ███████╗
    //  ██║     ██║██║   ██║██╔══██║   ██║   ██╔══██║██║   ██║██║   ██║╚════██║██╔══╝  ╚════██║
    //  ███████╗██║╚██████╔╝██║  ██║   ██║   ██║  ██║╚██████╔╝╚██████╔╝███████║███████╗███████║
    //  ╚══════╝╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝ ╚═════╝  ╚═════╝ ╚══════╝╚══════╝╚══════╝
    //                                                                                         

    private static VrTrackedDevice _Lighthouse1_CurrentTrackedDevice;
    private static CoordinateSystem _Lighthouse1_CurrentCS;
    private static CoordinateSystem _Lighthouse1_OldCS;
    private static DSPlane _Lighthouse1_OldPlane;

    /// <summary>
    /// Tracking of HTC Vive Lighthouse #1.
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem" })]
    public static Dictionary<string, object> Lighthouse1(object Vive, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["Lighthouse"];
        if (list.Count == 0)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("No Lighthouse detected.");
            return null;
        }


        if (tracked)
        {
            int index = wrapper.TrackedDevices.IndexesByClasses["Lighthouse"][0];

            _Lighthouse1_CurrentTrackedDevice = wrapper.TrackedDevices.AllDevices[index];
            _Lighthouse1_CurrentTrackedDevice.ConvertPose();

            _Lighthouse1_CurrentCS = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_Lighthouse1_CurrentTrackedDevice.CorrectedMatrix4x4, false))
            {
                _Lighthouse1_CurrentCS = _Lighthouse1_CurrentCS.Transform(cm);
            }

            _Lighthouse1_OldCS = _Lighthouse1_CurrentCS;
            _Lighthouse1_OldPlane = CoordinateSystemToPlane(_Lighthouse1_OldCS);

        }

        // @TODO: figure out mesh representation

        return new Dictionary<string, object>()
        {
            { "Mesh", null },
            { "Plane", _Lighthouse1_OldPlane },
            { "CoordinateSystem", _Lighthouse1_OldCS }
        };
    }



    private static VrTrackedDevice _Lighthouse2_CurrentTrackedDevice;
    private static CoordinateSystem _Lighthouse2_CurrentCS;
    private static CoordinateSystem _Lighthouse2_OldCS;
    private static DSPlane _Lighthouse2_OldPlane;

    /// <summary>
    /// Tracking of HTC Vive Lighthouse #2.
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem" })]
    public static Dictionary<string, object> Lighthouse2(object Vive, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["Lighthouse"];
        if (list.Count < 2)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("No Lighthouse detected.");
            return null;
        }


        if (tracked)
        {
            int index = wrapper.TrackedDevices.IndexesByClasses["Lighthouse"][1];

            _Lighthouse2_CurrentTrackedDevice = wrapper.TrackedDevices.AllDevices[index];
            _Lighthouse2_CurrentTrackedDevice.ConvertPose();

            _Lighthouse2_CurrentCS = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_Lighthouse2_CurrentTrackedDevice.CorrectedMatrix4x4, false))
            {
                _Lighthouse2_CurrentCS = _Lighthouse2_CurrentCS.Transform(cm);
            }

            _Lighthouse2_OldCS = _Lighthouse2_CurrentCS;
            _Lighthouse2_OldPlane = CoordinateSystemToPlane(_Lighthouse2_OldCS);
        }

        // @TODO: figure out mesh representation

        return new Dictionary<string, object>()
        {
            { "Mesh", null },
            { "Plane", _Lighthouse2_OldPlane },
            { "CoordinateSystem", _Lighthouse2_OldCS }
        };
    }



    //  ████████╗██████╗  █████╗  ██████╗██╗  ██╗███████╗██████╗ 
    //  ╚══██╔══╝██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝██╔══██╗
    //     ██║   ██████╔╝███████║██║     █████╔╝ █████╗  ██████╔╝
    //     ██║   ██╔══██╗██╔══██║██║     ██╔═██╗ ██╔══╝  ██╔══██╗
    //     ██║   ██║  ██║██║  ██║╚██████╗██║  ██╗███████╗██║  ██║
    //     ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
    //                                                           

    private static VrTrackedDevice[] _GenericTracker_CurrentTrackedDevice = new VrTrackedDevice[8];
    private static CoordinateSystem[] _GenericTracker_CurrentCS = new CoordinateSystem[8];
    private static CoordinateSystem[] _GenericTracker_OldCS = new CoordinateSystem[8];
    private static DSPlane[] _GenericTracker_OldPlane = new DSPlane[8];

    /// <summary>
    /// Tracking of HTC Vive Generic Tracker.
    /// </summary>
    /// <param name="Vive">The Vive object to read from.</param>
    /// <param name="index">If more than one Tracker, choose index number.</param>
    /// <param name="tracked">Should the device be tracked?</param>
    /// <returns name = "Mesh">Mesh representation of the device.</returns>
    /// <returns name = "Plane">The device's Plane.</returns>
    /// <returns name = "CoordinateSystem">The device's CoordinateSystem.</returns>
    [MultiReturn(new[] { "Mesh", "Plane", "CoordinateSystem" })]
    public static Dictionary<string, object> GenericTracker(object Vive, int index = 0, bool tracked = true)
    {
        OpenvrWrapper wrapper;
        try
        {
            wrapper = Vive as OpenvrWrapper;
        }
        catch
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Please connect a Vive object to this node's input.");
            return null;
        }

        var list = wrapper.TrackedDevices.IndexesByClasses["Tracker"];
        if (list.Count < index + 1)
        {
            DynamoServices.LogWarningMessageEvents.OnLogWarningMessage("Cannot find Generic Tracker. Wrong index?");
            return null;
        }

        if (tracked)
        {
            int id = wrapper.TrackedDevices.IndexesByClasses["Tracker"][index];

            _GenericTracker_CurrentTrackedDevice[index] = wrapper.TrackedDevices.AllDevices[id];
            _GenericTracker_CurrentTrackedDevice[index].ConvertPose();
            _GenericTracker_CurrentTrackedDevice[index].CorrectGenericTrackerMatrix();

            _GenericTracker_CurrentCS[index] = CoordinateSystem.Identity();
            using (CoordinateSystem cm = Matrix4x4ToCoordinateSystem(_GenericTracker_CurrentTrackedDevice[index].CorrectedMatrix4x4, false))
            {
                _GenericTracker_CurrentCS[index] = _GenericTracker_CurrentCS[index].Transform(cm);
            }

            _GenericTracker_OldCS[index] = _GenericTracker_CurrentCS[index];
            _GenericTracker_OldPlane[index] = CoordinateSystemToPlane(_GenericTracker_OldCS[index]);
        }

        // @TODO: figure out mesh representation

        return new Dictionary<string, object>()
        {
            { "Mesh", null },
            { "Plane", _GenericTracker_OldPlane[index] },
            { "CoordinateSystem", _GenericTracker_OldCS[index] }
        };
    }







    //  ██╗   ██╗████████╗██╗██╗     ███████╗
    //  ██║   ██║╚══██╔══╝██║██║     ██╔════╝
    //  ██║   ██║   ██║   ██║██║     ███████╗
    //  ██║   ██║   ██║   ██║██║     ╚════██║
    //  ╚██████╔╝   ██║   ██║███████╗███████║
    //   ╚═════╝    ╚═╝   ╚═╝╚══════╝╚══════╝
    //                                       

    internal static DSPlane CoordinateSystemToPlane(CoordinateSystem cs)
    {
        DSPlane pl = DSPlane.ByOriginXAxisYAxis(cs.Origin, cs.XAxis, cs.YAxis);
        return pl;
    }

    internal static CoordinateSystem Matrix4x4ToCoordinateSystem(Matrix4x4 m, bool transpose)
    {
        double[] mm = Matrix4x4ToDoubleArray(m, transpose);
        CoordinateSystem cs = CoordinateSystem.ByMatrix(mm);
        return cs;
    }

    internal static Matrix4x4 DSPlaneToMatrix4x4(DSPlane plane)
    {
        double[] pl = DSPlaneToDoubleArray(plane, true);
        Matrix4x4 m = new Matrix4x4(
            (float)pl[0], (float)pl[1], (float)pl[2], (float)pl[3],
            (float)pl[4], (float)pl[5], (float)pl[6], (float)pl[7],
            (float)pl[8], (float)pl[9], (float)pl[10], (float)pl[11],
            (float)pl[12], (float)pl[13], (float)pl[14], (float)pl[15]);
        return m;
    }

    internal static double[] Matrix4x4ToDoubleArray(Matrix4x4 m, bool transpose)
    {
        double[] a = new double[16];
        if (transpose)
        {
            a[0] = m.M11; a[1] = m.M21; a[2] = m.M31; a[3] = m.M41;
            a[4] = m.M12; a[5] = m.M22; a[6] = m.M32; a[7] = m.M42;
            a[8] = m.M13; a[9] = m.M23; a[10] = m.M33; a[11] = m.M43;
            a[12] = m.M14; a[13] = m.M24; a[14] = m.M34; a[15] = m.M44;
        }
        else
        {
            a[0] = m.M11; a[1] = m.M12; a[2] = m.M13; a[3] = m.M14;
            a[4] = m.M21; a[5] = m.M22; a[6] = m.M23; a[7] = m.M24;
            a[8] = m.M31; a[9] = m.M32; a[10] = m.M33; a[11] = m.M34;
            a[12] = m.M41; a[13] = m.M42; a[14] = m.M43; a[15] = m.M44;
        }
        return a;
    }

    internal static double[] DSPlaneToDoubleArray(DSPlane pl, bool transpose)
    {
        double[] a = new double[16];
        if (transpose)
        {
            a[0] = pl.XAxis.X; a[1] = pl.XAxis.Y; a[2] = pl.XAxis.Z; a[3] = 0;
            a[4] = pl.YAxis.X; a[5] = pl.YAxis.Y; a[6] = pl.YAxis.Z; a[7] = 0;
            a[8] = pl.Normal.X; a[9] = pl.Normal.Y; a[10] = pl.Normal.Z; a[11] = 0;
            a[12] = pl.Origin.X; a[13] = pl.Origin.Y; a[14] = pl.Origin.Z; a[15] = 1;
        }
        else
        {
            a[0] = pl.XAxis.X; a[1] = pl.YAxis.X; a[2] = pl.Normal.X; a[3] = pl.Origin.X;
            a[4] = pl.XAxis.Y; a[5] = pl.YAxis.Y; a[6] = pl.Normal.Y; a[7] = pl.Origin.Y;
            a[8] = pl.XAxis.Z; a[9] = pl.YAxis.Z; a[10] = pl.Normal.Z; a[11] = pl.Origin.Z;
            a[12] = 0; a[13] = 0; a[14] = 0; a[15] = 1;
        }
        return a;
    }

}
