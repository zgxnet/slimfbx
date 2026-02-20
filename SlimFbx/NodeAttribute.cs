using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class NodeAttribute
{
    /** \enum EType Node attribute types.
      * - \e eUnknown
      * - \e eNull
      * - \e eMarker
      * - \e eSkeleton
      * - \e eMesh
      * - \e eNurbs
      * - \e ePatch
      * - \e eCamera
      * - \e eCameraStereo,
      * - \e eCameraSwitcher
      * - \e eLight
      * - \e eOpticalReference
      * - \e eOpticalMarker
      * - \e eNurbsCurve
      * - \e eTrimNurbsSurface
      * - \e eBoundary
      * - \e eNurbsSurface
      * - \e eShape
      * - \e eLODGroup
      * - \e eSubDiv
      * - \e eCachedEffect
      * - \e eLine
      */
    public enum EType
    {
        Unknown,
        Null,
        Marker,
        Skeleton,
        Mesh,
        Nurbs,
        Patch,
        Camera,
        CameraStereo,
        CameraSwitcher,
        Light,
        OpticalReference,
        OpticalMarker,
        NurbsCurve,
        TrimNurbsSurface,
        Boundary,
        NurbsSurface,
        Shape,
        LODGroup,
        SubDiv,
        CachedEffect,
        Line
    }

    public string? Name;

    public virtual EType AttributeType => EType.Unknown;

    //nodes using this attribute
    public List<Node> Nodes = [];

    public virtual void DoScale(float factor) { }

    public Node SingleNode => Nodes.Count == 1 ? Nodes[0] : throw new InvalidOperationException($"Attribute {Name} has {Nodes.Count} nodes");
}
