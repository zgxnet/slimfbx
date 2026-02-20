using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class SubDeformer : FbxObject
{
    /** \enum EType Sub-deformer type
    */
    public enum EType
    {
        Unknown,			//!< Untyped sub-deformer            
        Cluster,			//!< Type FbxCluster            
        BlendShapeChannel	//!< Type FbxBlendShapeChannel
    }

    public virtual EType SubDeformerType => EType.Unknown;

    public bool MultiLayer;
}
