namespace Mango.Core.Extensions;

public static class TypeExtensions
{
    public static string GetGenericTypeName(this Type type)
    {
        if (type.IsGenericType)
        {
            string text = string.Join(",", (from t in type.GetGenericArguments()
                                            select t.Name).ToArray());
            return type.Name.Remove(type.Name.IndexOf('`')) + "<" + text + ">";
        }

        return type.Name;
    }

    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
}
