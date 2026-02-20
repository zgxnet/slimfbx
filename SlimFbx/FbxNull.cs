namespace SlimFbx;

public class FbxNull : NodeAttribute
{
    /** \enum ELook         Null node look types.
     * - \e eNone
     * - \e eCross
     */
    public enum ELook
    {
        None,
        Cross,
    }

    public override EType AttributeType => EType.Null;

    public const float DefaultSize = 100;
    public const ELook DefaultLook = ELook.Cross;

    public float Size = DefaultSize;
    public ELook Look = DefaultLook;
}
