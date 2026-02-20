namespace SlimFbx;

public struct SystemUnit 
{
    public float ScaleFactor;
    public float Multiplier;

    public SystemUnit() { }
    public SystemUnit(float scaleFactor, float multiplier)
    {
        ScaleFactor = scaleFactor;
        Multiplier = multiplier;
    }

    public static SystemUnit CM => new(1, 1);

    public static SystemUnit DM => new(10, 1);

    public static SystemUnit M => new(100, 1);

    public override bool Equals(object? obj)
    {
        return obj is SystemUnit unit &&
               ScaleFactor == unit.ScaleFactor &&
               Multiplier == unit.Multiplier;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ScaleFactor, Multiplier);
    }

    public static bool operator ==(SystemUnit left, SystemUnit right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SystemUnit left, SystemUnit right)
    {
        return !(left == right);
    }
}
