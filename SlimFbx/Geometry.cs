using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class Geometry : LayerContainer
{
    public Deformer[] Deformers = [];

    public Skin? SkinDeformer => Deformers.OfType<Skin>().FirstOrDefault();

    public override void DoScale(float factor)
    {
        base.DoScale(factor);
        foreach(var deformer in Deformers)
        {
            deformer.DoScale(factor);
        }
    }
}
