using PeterO.Cbor;
using Stride.Animations;
using Stride.Core.Mathematics;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SlimFbx;

internal class CborConverter
{
    class TypeInfo
    {
        public required Func<CBORObject, object?> reader;
        public Func<CBORObject, object?>? compactReader;
        public Func<CBORObject, Type>? getActualType;
    }

    public class CompactDataArrayAttribute : Attribute
    {}

    Dictionary<Type, TypeInfo> typeInfos = new();

    public struct CompactDataArray
    {
        byte[] data;
        CborTag tag;

        public CompactDataArray(CBORObject cbor)
        {
            tag = (CborTag)cbor.MostOuterTag.ToInt32Checked();
            data = cbor.GetByteString();
        }

        public float[] ToFloatArray()
        {
            if (tag == CborTag.Float32Array)
            {
                if (data.Length % sizeof(float) != 0)
                    throw new Exception($"Invalid array data length {data.Length} for float[]");
                return MemoryMarshal.Cast<byte, float>(data).ToArray();
            }
            else if (tag == CborTag.Float64Array)
            {
                if (data.Length % sizeof(double) != 0)
                    throw new Exception($"Invalid array data length {data.Length} for double[]");
                var darr = MemoryMarshal.Cast<byte, double>(data);
                var farr = new float[darr.Length];
                for (int i = 0; i < darr.Length; i++)
                    farr[i] = (float)darr[i];
                return farr;
            }
            else
                throw new Exception($"Unsupported cbor tag {tag} for float[]");
        }

        public Vector3[] ToVector3Array()
        {
            if (tag == CborTag.Float32Array)
            {
                if (data.Length % (3 * sizeof(float)) != 0)
                    throw new Exception($"Invalid array data length {data.Length} for Vector3[]");
                return MemoryMarshal.Cast<byte, Vector3>(data).ToArray();
            }
            else if (tag == CborTag.Float64Array)
            {
                if (data.Length % (3 * sizeof(double)) != 0)
                    throw new Exception($"Invalid array data length {data.Length} for Vector3(double)");
                int count = data.Length / (3 * sizeof(double));
                var darr = MemoryMarshal.Cast<byte, double>(data);
                var result = new Vector3[count];
                var farr = MemoryMarshal.Cast<Vector3, float>(result.AsSpan());
                for (int i = 0; i < darr.Length; i++)
                    farr[i] = (float)darr[i];
                return result;
            }
            else
                throw new Exception($"Unsupported cbor tag {tag} for Vector3[]");
        }

        public int[] ToIntArray()
        {
            if (tag == CborTag.Uint32Array || tag == CborTag.Int32Array)
            {
                if (data.Length % sizeof(int) != 0)
                    throw new Exception($"Invalid array data length {data.Length} for int[]");
                return MemoryMarshal.Cast<byte, int>(data).ToArray();
            }
            else
                throw new Exception($"Unsupported cbor tag {tag} for int[]");
        }
    }

    public CborConverter()
    {
        RegisterDefault();
    }

    public void Register(Type type, Func<CBORObject, object?> reader)
    {
        if(typeInfos.ContainsKey(type))
            throw new InvalidOperationException($"Type {type} is already registered.");
        typeInfos[type] = new TypeInfo(){
            reader = reader
        };
    }

    public void Register<T>()
        => CreateTypeInfo(typeof(T));

    TypeInfo _Register<T>()
        => CreateTypeInfo(typeof(T));

    [return: MaybeNull]
    public T Convert<T>(CBORObject val)
    {
        if (!typeInfos.TryGetValue(typeof(T), out var info))
            throw new NotImplementedException($"Unsupported type {typeof(T)}");
        object? result = info.reader(val);
        if (result == null)
            return default;
        if (result is not T t)
            throw new InvalidOperationException($"Cannot convert to type {typeof(T)}");
        return t;
    }

    enum CborTag
    {
        Unknown = -1,
        Uint16Array = 69,
        Uint32Array = 70,
        Int16Array = 77,
        Int32Array = 78,

        Float32Array = 85,
        Float64Array = 86,
    }

    TypeInfo RegisterBasic<T>()
        => typeInfos[typeof(T)] = new TypeInfo()
        {
            reader = (v) => {
                if (typeof(T) == typeof(bool)) return v.AsBoolean();
                if (typeof(T) == typeof(string)) return v.AsString();
                if (typeof(T) == typeof(byte)) return (byte)v.AsInt32();
                if (typeof(T) == typeof(int)) return v.AsInt32();
                if (typeof(T) == typeof(uint)) return (uint)v.AsNumber().ToInt64Checked();
                if (typeof(T) == typeof(long)) return v.AsNumber().ToInt64Checked();
                if (typeof(T) == typeof(float)) return v.AsSingle();
                if (typeof(T) == typeof(double)) return v.AsDouble();
                throw new NotSupportedException($"Type {typeof(T)} not supported");
            }
        };

    void RegisterDefault()
    {
        RegisterBasic<bool>();
        RegisterBasic<string>();
        RegisterBasic<byte>();
        RegisterBasic<int>();
        RegisterBasic<uint>();
        RegisterBasic<long>();
        RegisterBasic<float>();
        RegisterBasic<double>();
        typeInfos[typeof(byte[])] = new TypeInfo()
        {
            reader = v =>
            {
                if (v.Type != CBORType.ByteString)
                    throw new Exception($"Invalid cbor type {v.Type} for byte[]");
                return v.GetByteString();
            }
        };
        Register(typeof(CBORObject), v =>
        {
            return v.Type == CBORType.Array || v.Type == CBORType.Map ? v : throw new Exception($"{v?.GetType()} cannot be converted to CBORObject array");
        });
        Register(typeof(Vector3), v =>
        {
            if (v.Type != CBORType.Array || v.Count != 3)
                throw new Exception($"Invalid cbor type {v.Type} for Vector3");
            return new Vector3(
                v[0].AsSingle(),
                v[1].AsSingle(),
                v[2].AsSingle());
        });
        Register(typeof(Vector4), v =>
        {
            if (v.Type != CBORType.Array || v.Count != 4)
                throw new Exception($"Invalid cbor type {v.Type} for Vector4");
            return new Vector4(
                v[0].AsSingle(),
                v[1].AsSingle(),
                v[2].AsSingle(),
                v[3].AsSingle());
        });
        Register(typeof(Matrix), v =>
        {
            if (v.Type != CBORType.Array || v.Count != 16)
                throw new Exception($"Invalid cbor type {v.Type} for Matrix");
            Matrix m = new();
            Span<float> r = MemoryMarshal.CreateSpan(ref m.M11, 16);
            for (int i = 0; i < 16; i++)
                r[i] = v[i].AsSingle();
            m.Transpose();
            return m;
        });
        Register(typeof(AnimCurveKey), v =>
        {
            if (v.Type != CBORType.Array && v.Count != 9)
                throw new Exception($"Invalid cbor type {v.Type} for AnimCurveKey");
            long time = v[0].AsInt64Value();
            float value = v[1].AsSingle();
            uint flags = v[2].AsNumber().ToUInt32Unchecked();
            var key = new AnimCurveKey(time, value, flags,
                v[3].AsSingle(), v[4].AsSingle(), v[5].AsSingle(),
                v[6].AsSingle(), v[7].AsSingle(), v[8].AsSingle());
            return key;
        });
        _Register<float[]>().compactReader = v =>
        {
            CompactDataArray arrayData = new(v);
            return arrayData.ToFloatArray();
        };
        _Register<Vector3[]>().compactReader = v =>
        {
            CompactDataArray arrayData = new(v);
            return arrayData.ToVector3Array();
        };
        _Register<int[]>().compactReader = v =>
        {
            CompactDataArray arrayData = new(v);
            return arrayData.ToIntArray();
        };
    }

    TypeInfo CreateTypeInfo(Type type)
    {
        Func<CBORObject, object?>? reader;
        if (typeInfos.TryGetValue(type, out var info))
            return info;
        { //check nullable
            Type? type1 = Nullable.GetUnderlyingType(type);
            if(type1 != null)
            {
                var vreader = CreateTypeInfo(type1).reader;
                reader = (v) =>
                {
                    if (v.IsNull)
                        return null;
                    return vreader(v);
                };
                typeInfos[type] = info = new TypeInfo() { reader = reader };
                return info;
            }
        }
        Func<CBORObject, Type>? getActualType = null;
        if (type.IsEnum)
        {
            reader = (v) =>
            {
                string sv = v.AsString();
                return Enum.Parse(type, sv);
            };
        }
        else if (type.IsArray)
        {
            if(type.GetArrayRank() != 1)
                throw new NotSupportedException("Only single dimensional arrays are supported.");
            var etype = type.GetElementType()!;
            var ereader = CreateTypeInfo(etype).reader;
            reader = (v) =>
            {
                if (v.Type != CBORType.Array)
                    throw new InvalidOperationException($"Cannot convert CBOR type {v.Type} to array of {etype}.");
                Array result = Array.CreateInstance(etype, v.Count);
                for(int i = 0; i < v.Count; i++)
                {
                    var ev = v[i];
                    var eobj = ereader(ev);
                    result.SetValue(eobj, i);
                }
                return result;
            };
        }
        else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var gargs = type.GetGenericArguments();
            var etype = gargs[0];
            var ereader = CreateTypeInfo(etype).reader;
            reader = (v) =>
            {
                if (v.Type != CBORType.Array)
                    throw new InvalidOperationException($"Cannot convert CBOR type {v.Type} to List of {etype}.");
                var listType = typeof(List<>).MakeGenericType(etype);
                var result = (IList)Activator.CreateInstance(listType)!;
                for (int i = 0; i < v.Count; i++)
                {
                    var ev = v[i];
                    var eobj = ereader(ev);
                    result.Add(eobj);
                }
                return result;
            };
        }
        else if (type.IsClass || type.IsValueType)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).ToArray();
            var fieldReaders = fields.Select(p => CreateTypeInfo(p.FieldType).reader).ToArray();
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (f.GetCustomAttribute<CompactDataArrayAttribute>() != null)
                {
                    var finfo = typeInfos[f.FieldType];
                    if (finfo.compactReader == null)
                        throw new InvalidOperationException($"Field {type.FullName}.{f.Name} is marked as CompactDataArray but its type {f.FieldType} does not support compact representation.");
                    fieldReaders[i] = finfo.compactReader;
                }
            }
            getActualType = type.GetMethod("GetActualType", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<CBORObject, Type>>();
            reader = (v) =>
            {
                if (v.IsNull)
                    return null;
                if (v.Type != CBORType.Map)
                    throw new InvalidOperationException($"Cannot convert CBOR type {v.Type} to class {type}.");
                if (getActualType != null)
                {
                    Type actualType = getActualType(v);
                    if (actualType != type)
                        return typeInfos[actualType].reader(v);
                }
                var instance = Activator.CreateInstance(type);
                if (type.IsClass)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var f = fields[i];
                        var fr = fieldReaders[i];
                        if (v.ContainsKey(f.Name))
                        {
                            var fv = v[f.Name];
                            var fobj = fr(fv);
                            f.SetValue(instance, fobj);
                        }
                    }
                }
                else
                {
                    TypedReference ri = __makeref(instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var f = fields[i];
                        var fr = fieldReaders[i];
                        if (v.ContainsKey(f.Name))
                        {
                            var fv = v[f.Name];
                            var fobj = fr(fv);
                            f.SetValueDirect(ri, fobj!);
                        }
                    }
                }
                return instance;
            };
        }
        else
            throw new NotImplementedException("Unsupported type " + type.FullName);
        typeInfos[type] = info = new TypeInfo() { 
            reader = reader,
            getActualType = getActualType
        };
        return info;
    }
}
