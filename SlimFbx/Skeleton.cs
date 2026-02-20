using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class Skeleton : NodeAttribute
{
    public override NodeAttribute.EType AttributeType => NodeAttribute.EType.Skeleton;

    /** \enum EType Skeleton types.
	  * \remarks \e eEffector is synonymous to \e eRoot.
	  * \remarks The \e eLimbNode type is a bone defined uniquely by a transform and a size value while
	  * \remarks the \e eLimb type is a bone defined by a transform and a length.
	  */
    public new enum EType
    {
        Root,          /*!< First element of a chain. */
        Limb,          /*!< Chain element. */
        LimbNode,      /*!< Chain element. */
        Effector		/*!< Last element of a chain. */
    }

    public EType SkeletonType;

    /** This property handles the limb node size.
  *
  * To access this property do: Size.Get().
  * To set this property do: Size.Set(FbxDouble).
  *
  * Default value is 100.0
  */
    public float Size;

    /** This property handles the skeleton limb length.
	*
	* To access this property do: LimbLength.Get().
	* To set this property do: LimbLength.Set(FbxDouble).
	*
	* FbxSkeleton is a node attribute and it will be attached to a FbxNode which represents the transform.
	* Given a chain of skeleton nodes the parent and child skeletons will be attached to a parent node and a child node.
	* The orientation of the limb is computed from the vector between the parent and child position (from parent to child). 
	* The LimbLength represents the proportion 
	* of the parent node's position to the child node's position which is used to compute the actual limb length.
	* The default value of 1.0 means the LimbLength is equal to the length between the parent and child node's position.
	* So if the value is 0.5, it means the LimbLength will be half of the length between the parent and child node's position.
	*/
    public float LimbLength;
}
