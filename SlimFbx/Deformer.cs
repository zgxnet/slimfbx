using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class Deformer : FbxObject
{
    public bool MultiLayer;

    /**
    * \name Deformer types
    */
    public enum EType
    {
        Unknown,    //!< Unknown deformer type
        Skin,       //!< Type FbxSkin
        BlendShape, //!< Type FbxBlendShape
        VertexCache //!< Type FbxVertexCacheDeformer
    }

    public virtual EType DeformerType => EType.Unknown;

    public virtual void DoScale(float factor) { }
}

public class UnsupportedDeformer(Deformer.EType deformerType) : Deformer
{
    public override EType DeformerType => deformerType;
}
