using GLib;

namespace Frida;

internal static class VariantMarshal
{
    public static object? ToObject(Variant? value)
    {
        if (value == null)
            return null;

        switch (value.Classify())
        {
            case VariantClass.Boolean:
                return value.GetBoolean();
            case VariantClass.Byte:
                return value.GetByte();
            case VariantClass.Int16:
                return value.GetInt16();
            case VariantClass.Uint16:
                return value.GetUint16();
            case VariantClass.Int32:
                return value.GetInt32();
            case VariantClass.Uint32:
                return value.GetUint32();
            case VariantClass.Int64:
                return value.GetInt64();
            case VariantClass.Uint64:
                return value.GetUint64();
            case VariantClass.Double:
                return value.GetDouble();
            case VariantClass.String:
            case VariantClass.ObjectPath:
            case VariantClass.Signature:
                return value.GetString(out _);
            case VariantClass.Variant:
                return ToObject(value.GetVariant());
            case VariantClass.Maybe:
                return ToObject(value.GetMaybe());
            case VariantClass.Array when value.GetTypeString().StartsWith("a{"):
                return ToDictionary(value);
            case VariantClass.Array:
            case VariantClass.Tuple:
                return ToArray(value);
            default:
                return value.Print(false);
        }
    }

    public static Variant ToVariant(object? value)
    {
        switch (value)
        {
            case null:
                return Variant.NewMaybe(VariantType.New("v"), null);
            case Variant variant:
                return variant;
            case bool b:
                return Variant.NewBoolean(b);
            case byte by:
                return Variant.NewByte(by);
            case short s:
                return Variant.NewInt16(s);
            case ushort us:
                return Variant.NewUint16(us);
            case int i:
                return Variant.NewInt32(i);
            case uint ui:
                return Variant.NewUint32(ui);
            case long l:
                return Variant.NewInt64(l);
            case ulong ul:
                return Variant.NewUint64(ul);
            case double d:
                return Variant.NewDouble(d);
            case string str:
                return Variant.NewString(str);
            case IDictionary<string, object?> dictionary:
                return FromDictionary(dictionary);
            case System.Collections.IEnumerable sequence:
                return FromSequence(sequence);
            default:
                throw new ArgumentException($"Cannot convert {value.GetType()} to a variant");
        }
    }

    private static Dictionary<string, object?> ToDictionary(Variant value)
    {
        var result = new Dictionary<string, object?>();
        var count = (int)(ulong)value.NChildren();
        for (var i = 0; i < count; i++)
        {
            using var entry = value.GetChildValue((nuint)i);
            using var key = entry.GetChildValue((nuint)0);
            using var element = entry.GetChildValue((nuint)1);
            result[key.GetString(out _)] = ToObject(element);
        }
        return result;
    }

    private static object?[] ToArray(Variant value)
    {
        var count = (int)(ulong)value.NChildren();
        var items = new object?[count];
        for (var i = 0; i < count; i++)
        {
            using var child = value.GetChildValue((nuint)i);
            items[i] = ToObject(child);
        }
        return items;
    }

    private static Variant FromDictionary(IDictionary<string, object?> dictionary)
    {
        var entries = dictionary
            .Select(pair => Variant.NewDictEntry(Variant.NewString(pair.Key), Variant.NewVariant(ToVariant(pair.Value))))
            .ToArray();
        return Variant.NewArray(VariantType.New("{sv}"), entries);
    }

    private static Variant FromSequence(System.Collections.IEnumerable sequence)
    {
        var items = sequence.Cast<object?>().Select(item => Variant.NewVariant(ToVariant(item))).ToArray();
        return Variant.NewArray(VariantType.New("v"), items);
    }
}
