namespace SlimFbx;

public class FbxObject
{
    public string? Name;

    //userdata
    Dictionary<Type, object>? userDataDict = null;
    Dictionary<Type, object> UserDataDict => userDataDict ??= [];

    public void SetUserData<T>(T data) where T : notnull
    {
        UserDataDict[typeof(T)] = data;
    }

    public T? TryGetUserData<T>() where T : notnull
    {
        if (userDataDict != null && userDataDict.TryGetValue(typeof(T), out var data))
            return (T)data;
        return default;
    }

    public T GetUserData<T>() where T : notnull
        => TryGetUserData<T>() ?? throw new InvalidOperationException($"No user data of type {typeof(T)}");
}
