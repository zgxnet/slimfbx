using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class Skin : Deformer
{
    public override Deformer.EType DeformerType => Deformer.EType.Skin;

    /** Set deformation accuracy.
  * \remarks Use the Accuracy option to set the accuracy of skin deformations. 
  * 100% is full accuracy and 1% is a rough estimation of the envelope deformation. 
  * \param pDeformAccuracy         value for deformation accuracy.
  */
    public float DeformAccuracy = 1;

    public Cluster[] Clusters = [];

    /** \enum EType Skinning type.
	* The skinning type decides which method will be used to do the skinning.
	*      - \e eRigid                       Type eRigid means rigid skinning, which means only one joint can influence each control point.
	*      - \e eLinear                      Type eLinear means the classic linear smooth skinning.
	*      - \e eDualQuaternion              Type eDualQuaternion means the dual quaternion smooth skinning.
	*      - \e eBlend                       Type eBlend means to blend classic linear and dual quaternion smooth skinning according to blend weights.
	*/
    public new enum EType
    {
        Rigid,
        Linear,
        DualQuaternion,
        Blend
    }

    public EType SkinningType;

    public override void DoScale(float factor)
    {
        foreach(var cluster in Clusters)
        {
            cluster.DoScale(factor);
        }
    }
}
