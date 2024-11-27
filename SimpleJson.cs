using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Olimi2D
{
#region helper

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SimpleJsonNameAttribute : Attribute
    {
        public SimpleJsonNameAttribute(string name) { Name = name; }
        public string Name { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SimpleJsonNoSerializableAttribute : Attribute
    {
        public SimpleJsonNoSerializableAttribute() { }
    }

    /// <summary>
    /// Unknown attribute - set when property not found
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SimpleJsonUknownAttribute : Attribute
    {
        public SimpleJsonUknownAttribute() { }
    }

    public delegate object SimpleJsonConverter(object val);

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimpleJsonConverterAttribute : Attribute
    {
        public SimpleJsonConverter Converter { get; set; }
        public SimpleJsonConverterAttribute(Type delegateType, string method)
        {
                this.Converter = (SimpleJsonConverter)Delegate.CreateDelegate(delegateType, delegateType.GetMethod(method));
        }

        public object ObjectToString(object val)
        {
            if (this.Converter != null)
                return Converter(val);
            return "";
        }
        public object StringToObject(object val)
        {
            if (this.Converter != null)
                return Converter(val);
            return null;
        }
    }

    public class SimpleJsonItem
    {
        private static string spliter = " ,:;?+-!\r\n\t'\"()[]{}<>=@#$%&*";

        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public List<SimpleJsonItem> List { get; set; }

        public override string ToString()
        {
            return ToJson(0);
        }

        private string GetList(int param)
        {
            var n = new List<string>();

            if (List != null)
                foreach (var l in List)
                {
                    if (l == null || l.IsEmpty())
                        continue;
                
                    var s1 = l.ToJson(param, IsArray);
                    if (string.IsNullOrWhiteSpace(s1)) continue;

                    n.Add(s1);
                }

            if (!n.Any() && (param & SimpleJson.COMPACT_JSON) == SimpleJson.COMPACT_JSON) return "";

            string s = n.Any() ? string.Join(",", n) : "";
            if (IsArray) return "[" + s + "]";
            if (isObject) return "{" + s + "}";

            return s;
        }

        private bool IsEmpty()
        {
            if (List == null)
                return string.IsNullOrWhiteSpace(Value);
            else
                return !List.Any();
        }
        
        public string ToJson(int param, bool p = false)
        { 
            char q = (param & SimpleJson.SMALL_QUOTE) == SimpleJson.SMALL_QUOTE ? '\'' : '"';

            string s = "";

            bool compact = (param & SimpleJson.COMPACT_JSON) == SimpleJson.COMPACT_JSON;

            if (List == null)
            {
                if (string.IsNullOrWhiteSpace(Value) && compact) return "";

                if (CalcType(Type))
                {
                    if(compact && IsDefault()) return "";
                    s += string.Format("{0}", Value);
                }
                else
                    s += string.Format(q + "{0}" + q, Value);
            }
            else
            {
                if (!List.Any()) return "";
                s += GetList(param);                
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(Name))            
                s = string.Format(q + "{0}" + q + ":{1}", (compact && !p ? "" + Name[0] : Name), s);

            if ((param & SimpleJson.LOWER_CASE) == SimpleJson.LOWER_CASE)
                return s.ToLowerInvariant();

            return s;
        }

        private bool IsDefault()
        {
            switch (Type)
            {
                case "Int32":
                case "Int64":
                case "Byte":
                    {
                        long a = 0;
                        if (Int64.TryParse(Value, out a))
                        {
                            if (a == 0) return true;
                        }
                        return false;
                    }

                case "Boolean":
                    return "false".Equals(Value, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        private static bool HaveAnySplit(string Value)
        {
            for (int i = 0; i < spliter.Length; i++)
                if (Value.IndexOf(spliter[i]) > -1) return true;

            return false;
        }

        internal static bool CalcType(string name)
        {
            switch (name)
            {
                case "Int32":
                case "Int64":
                case "Boolean":
                case "Byte":
                case "Single":
                    return true;
                default:
                    return false;
            }
        }

        public bool IsArray { get; set; }

        public bool isObject { get; set; }

        public List<object> CastTo<T>(byte param)
        {
            var caster = new Olimi2D.SimpleJson.Caster<T>();
            var list = new List<object>();
            if (IsArray)
                list.AddRange(caster.GetArray(this, param));
            else
                list.Add(caster.GetObject(this, param));

            return list;
        }
    }

#endregion //helper

    public sealed class SimpleJson
    {
        public const byte DEFAULT = 0;
        public const byte IGNORE_CASE = 1;
        public const byte COMPACT_JSON = 2;
        public const byte SMALL_QUOTE = 4;
        public const byte LOWER_CASE = 8;



#region deserialize
        public static IEnumerable<T> Deserialize<T>(string s, byte param = DEFAULT)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var obj = TryDeserializeToItem(s);

            if (obj == null || !obj.Any()) return null;

            var list = new List<object>();
            if (typeof(T) == typeof(SimpleJsonItem))
            {
                foreach (var o in obj)
                {
                    if (o.IsArray)
                        list.AddRange(o.List);
                    else
                        list.Add(o);
                }
                return StaticCaster.StaticCastList<T>(list);
            }

            var caster = new Caster<T>();
            foreach (var o in obj)
            {
                if(o.IsArray)                    
                    list.AddRange(caster.GetArray(o, param));
                else
                    list.Add(caster.GetObject(o, param));
            }
            return StaticCaster.StaticCastList<T>(list);
        }

        private static List<SimpleJsonItem> TryDeserializeToItem(string s)
        {
            int quote = 0;
            string buffer = "";
            var brackets = new List<char>();
            var list = new List<string>();
            foreach (var c in s.ToCharArray())
            {
                if (quote > 0)
                {
                    if (c == quote)
                    {
                        quote = 0;
                        list.Add(buffer);
                        buffer = "";
                    }
                    else
                    {
                        buffer += c;
                    }
                    continue;
                }

                if (c == '"' || c =='\'')
                {
                    quote = c;
                    if (!string.IsNullOrEmpty(buffer))
                        list.Add(buffer);
                    buffer = "";
                    continue;
                }

                char n = (c == '=') ? ':' : c;

                if (n == '{' || n == '[')
                {
                    brackets.Add(n);
                }
                else if (n == '}' || n == ']')
                {
                    if (brackets.Last() != (n - 2))
                        throw new Exception("Unexpected bracket " + n);
                    else
                    {
                        brackets.RemoveAt(brackets.Count - 1);

                    }
                }
                else if (n == ':' || n == ',')
                {
                    //nothing
                }
                else if (n == '\r' || n == '\n' || n == '\t' || n == ' ')
                {
                    if (!string.IsNullOrWhiteSpace(buffer))
                        list.Add(buffer);
                    buffer = "";
                    continue;
                }
                else
                {
                    buffer += n; continue;
                }


                if (!string.IsNullOrEmpty(buffer))
                    list.Add(buffer);
                list.Add("" + n);
                buffer = "";
            }

            int o = 0;
            return GetIerarhia(list, 0, 0, out o);
        }

        private static List<SimpleJsonItem> DeserializeToItem(string s, char quoteSimbol = '"')
        {
            bool quote = false;
            string buffer = "";

            var list = new List<string>();
            foreach (var c in s.ToCharArray())
            {
                if (quote)
                {
                    if (c == quoteSimbol)
                    {
                        quote = !quote;
                        list.Add(buffer);
                        buffer = "";
                    }
                    else
                    {
                        buffer += c;
                    }
                    continue;
                }

                if (c == quoteSimbol)
                {
                    quote = !quote;
                    if (! string.IsNullOrWhiteSpace(buffer))
                        list.Add(buffer);
                    buffer = "";
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case ':':
                    case ',':
                        if (!string.IsNullOrWhiteSpace(buffer))
                            list.Add(buffer);
                        list.Add(""+c);
                        buffer = "";
                        break;
                    default:
                        buffer += c; break;
                }
            }

            int o = 0;
            return GetIerarhia(list, 0, 0, out o);
        }

        private static List<SimpleJsonItem> GetIerarhia(List<string> list, int index, int ar, out int id)
        {
            var items = new List<SimpleJsonItem>();
            SimpleJsonItem curItem = null;
            int arg = ar;

            for (int i = index; i < list.Count; i++)
            {
                if (list[i] == "{")
                {
                    int o = 0;
                    var res = GetIerarhia(list, i + 1, '{', out o);
                    if (curItem == null) curItem = new SimpleJsonItem();
                    curItem.List = res;
                    curItem.isObject = true;
                    items.Add(curItem);
                    curItem = null;
                    i = o;
                    arg = 0;
                    continue;
                }
                else if (list[i] == "}")
                {
                    arg = 0;
                    id = i;
                    if (curItem != null) items.Add(curItem);
                    return items;
                }
                else if (list[i] == ":")
                {
                    arg = ':';
                    if (curItem != null)
                        curItem = new SimpleJsonItem() { Name = curItem.Value };
                    continue;
                }
                else if (list[i] == ",")
                {
                    arg = ',';
                    if (curItem != null)
                        items.Add(curItem);
                    curItem = null;
                    continue;
                }
                else if (list[i] == "[")
                {
                    int o = 0;
                    var res = GetIerarhia(list, i + 1, '[', out o);
                    if (curItem == null) curItem = new SimpleJsonItem();
                    curItem.List = res;
                    curItem.IsArray = true;
                    items.Add(curItem);
                    curItem = null;
                    i = o;
                    arg = 0;
                    continue;
                }
                else if (list[i] == "]")
                {
                    arg = 0;
                    id = i;
                    if (curItem != null) items.Add(curItem);
                    return items;
                }
                else
                {
                    if (arg == ':')
                    {
                        if (curItem != null) curItem.Value = list[i];
                    }
                    else if (arg == ',' || arg == '{' || arg == '[')
                    {
                        if (curItem == null) curItem = new SimpleJsonItem() { Value = list[i] };
                    }
                    arg = '\0';
                }
            }
            id = list.Count - 1;
            return items;
        }
       
        public interface ICaster
        {
            IEnumerable<object> GetArray(SimpleJsonItem item, byte param);
            IEnumerable<object> ToArray(SimpleJsonItem item, byte param);
            IDictionary<object, object> ToDictionary(SimpleJsonItem item, byte param);
            IEnumerable<IPair> ObjToPairList(object obj);
            object GetObject(SimpleJsonItem item, byte param);
        }

        public abstract class StaticCaster : ICaster
        {
            protected static bool EqualsName(string s, string n, byte param)
            {
                string s1 = s;
                string n1 = n;

                if ((param & SimpleJson.COMPACT_JSON) == SimpleJson.COMPACT_JSON)
                {
                    if (!string.IsNullOrWhiteSpace(s1) && s1.Length > 0) s1 = "" + s1[0];
                    if (!string.IsNullOrWhiteSpace(n1) && n1.Length > 0) n1 = "" + n1[0];
                }
                if ((param & SimpleJson.IGNORE_CASE) == SimpleJson.IGNORE_CASE)
                {
                    s1 = s1.ToLowerInvariant();
                    n1 = n1.ToLowerInvariant();
                }

                return s1.Equals(n1);
            }

            protected static Int32? AsInt(string p, bool nullable = false)
            {
                int l = 0;
                if (Int32.TryParse(p, out l))
                    return l;

                if (nullable) return default(Int32?);
                throw new NotImplementedException("Cannot cast (" + p + ") to int");
            }

            protected static object AsLong(string p, bool nullable = false)
            {
                long l = 0;
                if (Int64.TryParse(p, out l))
                    return l;

                if (nullable) return default(Int64?);

                throw new NotImplementedException("Cannot cast (" + p + ") to long");
            }

            protected static object AsSingle(string p, bool nullable = false)
            {
                float f = 0;
                if (Single.TryParse(p, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out f))
                    return f;

                if (nullable) return default(Single?);

                throw new NotImplementedException("Cannot cast (" + p + ") to single");
            }

            protected static object AsBool(string p, bool nullable = false)
            {
                if ("true".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return true;
                if ("1".Equals(p)) return true;
                if ("t".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return true;
                if ("y".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return true;
                if ("yes".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return true;

                if ("false".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return false;
                if ("0".Equals(p)) return false;
                if ("f".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return false;
                if ("n".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return false;
                if ("no".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return false;

                if (nullable)
                {
                    if ("nil".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return default(Boolean?);
                    if ("null".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return default(Boolean?);
                    if ("".Equals(p, StringComparison.InvariantCultureIgnoreCase)) return default(Boolean?);
                }

                throw new NotImplementedException("Cannot cast (" + p + ") to boolean");
            }

            protected object StaticGetObject<T>(SimpleJsonItem item, byte param)
            {
                if (item.List == null) return null;
                var tT = typeof(T);
                var prop = tT.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var o = Activator.CreateInstance(tT);

                foreach (var elem in item.List)
                {
                    bool unknown = false;
                    bool setValue = false;

                    foreach (PropertyInfo p in prop)
                    {
                        if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() != null)
                        {
                            unknown = true;
                            continue;
                        }

                        var nm = p.GetCustomAttribute<SimpleJsonNameAttribute>();
                        if (!EqualsName((nm != null) ? nm.Name : p.Name, elem.Name, param)) continue;
                        p.SetValue(o, StaticGetValue<T>(p, elem, param), null);
                        setValue = true;
                        break;
                    }

                    if (setValue) continue;

                    if (unknown)
                    {
                        foreach (PropertyInfo p in prop)
                        {
                            if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() == null) continue;
                            p.SetValue(o, StaticGetValue<T>(p, elem, param), null);
                            break;
                        }
                    }
                }
                return o;
            }

            protected object StaticInvokeArray<TT>(SimpleJsonItem elem, byte param)
            {
                var genArg = typeof(TT).GetGenericArguments();
                if (genArg.Count() == 2)
                {
                    ICaster dict = (ICaster)Activator.CreateInstance(typeof(Caster<,>).MakeGenericType(genArg));
                    var dictMethod = this.GetType().GetMethod("CastDict").MakeGenericMethod(genArg);
                    return dictMethod.Invoke(null, new object[] { dict.ToDictionary(elem, param) });
                }
                else if (genArg.Count() == 1)
                {
                    Type t = genArg[0];
                    ICaster created = (ICaster)Activator.CreateInstance(typeof(Caster<>).MakeGenericType(t));
                    var castMethod = this.GetType().GetMethod("CastList").MakeGenericMethod(t);
                    return castMethod.Invoke(null, new object[] { created.GetArray(elem, param) });
                }
                throw new NotSupportedException();
            }

            protected object StaticInvokeArray(PropertyInfo p, SimpleJsonItem elem, byte param)
            {
                var genArg = p.PropertyType.GetGenericArguments();
                if (genArg.Count() == 2)
                {
                    ICaster dict = (ICaster)Activator.CreateInstance(typeof(Caster<,>).MakeGenericType(genArg));
                    var dictMethod = this.GetType().GetMethod("CastDict").MakeGenericMethod(genArg);
                    return dictMethod.Invoke(null, new object[] { dict.ToDictionary(elem, param) });
                }
                else if (genArg.Count() == 1)
                {
                    Type t = genArg[0];
                    ICaster created = (ICaster)Activator.CreateInstance(typeof(Caster<>).MakeGenericType(t));
                    var castMethod = this.GetType().GetMethod("CastList").MakeGenericMethod(t);
                    return castMethod.Invoke(null, new object[] { created.GetArray(elem, param) });
                }
                throw new NotSupportedException();
            }

            protected object StaticGetValue<T>(PropertyInfo p, SimpleJsonItem elem, byte param)
            {
                switch (p.PropertyType.Name)
                {
                    case "Byte": return (byte)AsInt(elem.Value);
                    case "Int32": return AsInt(elem.Value);
                    case "Int64": return AsLong(elem.Value);
                    case "Boolean": return AsBool(elem.Value);
                    case "Single": return AsSingle(elem.Value);
                    case "?Single": return AsSingle(elem.Value, true);
                    case "?Int32": return AsInt(elem.Value, true);
                    case "?Int64": return AsLong(elem.Value, true);
                    case "?Boolean": return AsBool(elem.Value, true);
                    case "String":
                    case "System.String": return elem.Value;
                    default:
                        if (elem.IsArray)
                        {
                            if (!p.PropertyType.IsGenericType) break;
                            return StaticInvokeArray(p, elem, param);
                        }
                        if (elem.List != null)
                        {
                            var t = p.PropertyType;
                            ICaster created = (ICaster)Activator.CreateInstance(typeof(Caster<>).MakeGenericType(t));
                            var castMethod = this.GetType().GetMethod("CastItem").MakeGenericMethod(t);
                            return castMethod.Invoke(null, new object[] { created.GetObject(elem, param) });
                        }

                        if (elem.List == null && string.IsNullOrWhiteSpace(elem.Value))
                        {
                            return null;
                        }

                        if (p.PropertyType.IsArray)
                        {
                            throw new NotImplementedException("Cannot implement array");
                        }

                        if (p.PropertyType.IsGenericType)
                        {
                            var genArg = p.PropertyType.GetGenericArguments();
                            if (genArg.Count() != 1) break;
                            Type t = genArg[0];
                            ICaster created = (ICaster)Activator.CreateInstance(typeof(Caster<>).MakeGenericType(t));

                            //TODO Nullable


                            var castMethod = this.GetType().GetMethod("CastList").MakeGenericMethod(t);
                            return castMethod.Invoke(null, new object[] { created.ToArray(elem, param) });
                        }

                        throw new NotImplementedException("Uncknow type: (" + p.PropertyType.FullName + ")");
                }
                throw new NotImplementedException("Uncknow type: (" + p.PropertyType.Name + ")");
            }

            protected object GetValue<T>(string elem, byte param, Func<object> func)
            {
                var name = typeof(T).Name;
                switch (name)
                {
                    case "Byte": return (byte)AsInt(elem);
                    case "Int32": return AsInt(elem);
                    case "Int64": return AsLong(elem);
                    case "Boolean": return AsBool(elem);
                    case "Single": return AsSingle(elem);
                    case "?Single": return AsSingle(elem, true);
                    case "?Int32": return AsInt(elem, true);
                    case "?Int64": return AsLong(elem, true);
                    case "?Boolean": return AsBool(elem, true);
                    case "String": return elem;
                }
                return func;
            }

            public abstract IEnumerable<object> GetArray(SimpleJsonItem item, byte param);

            public abstract IEnumerable<object> ToArray(SimpleJsonItem item, byte param);

            public abstract object GetObject(SimpleJsonItem item, byte param);

            public abstract IDictionary<object, object> ToDictionary(SimpleJsonItem item, byte param);

            public abstract IEnumerable<IPair> ObjToPairList(object obj);

            public static IDictionary<T1, T2> StaticCastDict<T1, T2>(Dictionary<object, object> o)
            {
                if (o != null)
                {
                    var dic = new Dictionary<T1, T2>();
                    foreach (var a in o)
                        dic.Add((T1)a.Key, (T2)a.Value);
                    return dic;
                }
                return null;
            }

            public static IEnumerable<T> StaticCastList<T>(List<object> o)
            {
                return o.Select(i => (T)i).ToList();
            }

            public static T StaticCastItem<T>(object o)
            {
                return (T)o;
            }
        }

        public class Caster<T1, T2> : StaticCaster
        {
            public static IDictionary<F1, F2> CastDict<F1, F2>(Dictionary<object, object> o) 
            { 
                return StaticCastDict<F1, F2>(o);
            }

            public static IEnumerable<F> CastList<F>(List<object> o)
            {
                return StaticCastList<F>(o);
            }

            public static F CastItem<F>(object o)
            {
                return StaticCastItem<F>(o);
            }

            public override IEnumerable<object> GetArray(SimpleJsonItem item, byte param)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<object> ToArray(SimpleJsonItem item, byte param)
            {
                throw new NotImplementedException();
            }

            public override IDictionary<object, object> ToDictionary(SimpleJsonItem item, byte param)
            {
                var result = new Dictionary<object, object>();

                _.Foreach<SimpleJsonItem>(item.List, container =>

                //if (item.List == null) return null;
                //foreach (var container in item.List)
                {
                    object obj;

                    if (container.IsArray && typeof(T2).IsGenericType)
                    {
                        obj = StaticInvokeArray<T2>(container, param);
                    }
                    else
                    {
                        obj = (container.List == null)
                        ? GetValue<T2>(container.Value, param, () => GetObject(container, param))
                        : StaticGetObject<T2>(container, param);
                    }

                    object name = GetValue<T1>(container.Name, param, null);

                    result.Add(name, obj);
                });
                return result;
            }

            public override object GetObject(SimpleJsonItem item, byte param)
            {
                if (item.List == null) return null;
                var prop = typeof(T2).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var o = Activator.CreateInstance(typeof(T2));

                foreach (var elem in item.List)
                {
                    bool unknown = false;
                    bool setValue = false;

                    foreach (PropertyInfo p in prop)
                    {
                        if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() != null)
                        {
                            unknown = true;
                            continue;
                        }

                        var nm = p.GetCustomAttribute<SimpleJsonNameAttribute>();
                        if (!EqualsName((nm != null) ? nm.Name : p.Name, elem.Name, param)) continue;
                        p.SetValue(o, StaticGetValue<T2>(p, elem, param), null);
                        setValue = true;
                        break;
                    }

                    if (setValue) continue;

                    if (unknown)
                    {
                        foreach (PropertyInfo p in prop)
                        {
                            if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() == null) continue;
                            p.SetValue(o, StaticGetValue<T2>(p, elem, param), null);
                            break;
                        }
                    }
                }

                return o;
            }

            public override IEnumerable<IPair> ObjToPairList(object obj)
            {
                Dictionary<T1, T2> d = (Dictionary<T1, T2>)obj;
                var list = new List<Pair<T1, T2>>();
                foreach(var a in d)
                    list.Add(new Pair<T1,T2>(){ Item1 = a.Key, Item2 = a.Value });

                return list;
            }
        }

        public class Caster<T> : StaticCaster
        {
            public static IDictionary<F1, F2> CastDict<F1, F2>(Dictionary<object, object> o)
            {
                return StaticCastDict<F1, F2>(o);
            }

            public static IEnumerable<F> CastList<F>(List<object> o)
            {
                return StaticCastList<F>(o);
            }

            public static F CastItem<F>(object o)
            {
                return StaticCastItem<F>(o);
            }

            public override IEnumerable<object> ToArray(SimpleJsonItem item, byte param)
            {
                if (item.List != null) return GetArray(item, param);
                var result = new List<object>();
                result.Add((T)GetValue<T>(item.Value, param, ()=>GetObject(item, param)));
                return result;
            }

            public override IEnumerable<object> GetArray(SimpleJsonItem item, byte param)
            {
                if (item.List == null) return null;                
                var result = new List<object>();
                foreach (var container in item.List)
                {
                    if (container.List == null)
                    {
                        result.Add((T)GetValue<T>(container.Value, param, () => GetObject(item, param)));
                        continue;
                    }
                    result.Add((T)GetObject(container, param));
                }
                return result;
            }

            public override IDictionary<object, object> ToDictionary(SimpleJsonItem item, byte param)
            {
                throw new NotImplementedException();
            }

            public override object GetObject(SimpleJsonItem item, byte param)
            {
                if (item.List == null) return null;
                var prop = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var o = Activator.CreateInstance(typeof(T));

                foreach (var elem in item.List)
                {
                    bool unknown = false;
                    bool setValue = false;

                    foreach (PropertyInfo p in prop)
                    {
                        if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() != null)
                        {
                            unknown = true;
                            continue;
                        }

                        var nm = p.GetCustomAttribute<SimpleJsonNameAttribute>();
                        if (!EqualsName((nm != null) ? nm.Name : p.Name, elem.Name, param)) continue;
                        p.SetValue(o, StaticGetValue<T>(p, elem, param), null);
                        setValue = true;
                        break;
                    }

                    if (setValue) continue;

                    if (unknown)
                    {
                        foreach (PropertyInfo p in prop)
                        {
                            if (p.GetCustomAttribute<SimpleJsonUknownAttribute>() == null) continue;
                            p.SetValue(o, StaticGetValue<T>(p, elem, param), null);
                            break;
                        }
                    }
                }

                return o;
            }

            public override IEnumerable<IPair> ObjToPairList(object obj)
            {
                IEnumerable<T> d = (IEnumerable<T>)obj;
                var list = new List<Pair<T>>();
                foreach (var a in d)
                    list.Add(new Pair<T>() { Item1 = a });

                return list;
            }
        }
#endregion


#region serializer
        public static string Serialize(object obj, int compact = 0)
        {
            if (obj == null) return "";
            string s = SerializeItem(obj).ToJson(compact);
            return s;
        }
        private static SimpleJsonItem SerializeItem(object obj)
        {
            var list = new List<SimpleJsonItem>();
            var oT = obj.GetType();

            if (oT.IsGenericType)
            {
                return new SimpleJsonItem() { List = GetItemFromCollection(oT, obj), IsArray = true };
            }
            var prop =  oT.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in prop)
            {
                list.Add(SerializeType(p, obj));
            }
            return new SimpleJsonItem() { List = list, isObject = true };
        }
        
        private static SimpleJsonItem SerializeType(PropertyInfo p, object obj)
        {

            var na = p.GetCustomAttribute<SimpleJsonNoSerializableAttribute>();
            if (na != null) 
                return new SimpleJsonItem();

            var nm = p.GetCustomAttribute<SimpleJsonNameAttribute>();
            SimpleJsonItem item = new SimpleJsonItem() 
            { 
                Type = p.PropertyType.Name, Name = (nm != null) ? nm.Name : p.Name
            };

            var ca = p.GetCustomAttribute(typeof(SimpleJsonConverterAttribute)) as SimpleJsonConverterAttribute;

            var pVal = p.GetValue(obj);

            if (pVal == null) return item;

            if (ca != null)
            {
                item.Type = p.PropertyType.Name;
                var v = ca.ObjectToString(pVal);                
                item.Value = v == null ? "" : v.ToString();
                return item;
            }
            
            if (!p.PropertyType.IsGenericType)
            {
                var propName = p.PropertyType.Name;
                item.Type = propName;
                if (propName == "String")
                {
                    item.Value = pVal.ToString();
                }
                else if (SimpleJsonItem.CalcType(propName))
                {
                    item.Value = pVal.ToString();
                }
                else
                {
                    item.List = new List<SimpleJsonItem>();
                    item.List.Add(SerializeItem(pVal));
                }
                return item;

            }
            else
            {
                item.List = GetItemFromCollection(p.PropertyType, pVal);
                item.IsArray = true;
            }

            return item;
        }

        private static List<SimpleJsonItem> GetItemFromCollection(Type p, object pVal)
        {
            var genArg = p.GetGenericArguments();

            var list = new List<SimpleJsonItem>();
            if (genArg.Count() == 2)
            {
                ICaster dict = (ICaster)Activator.CreateInstance(typeof(Caster<,>).MakeGenericType(genArg));
                foreach (IPair pair in dict.ObjToPairList(pVal))
                {
                    var key = GetItem<object>(pair.Get(1));
                    var val = GetItem<object>(pair.Get(2));

                    if (key.IsArray || key.isObject)
                        throw new NotImplementedException("Only simple type");
                    val.Name = key.Value;
                    list.Add(val);   

                }
                return list;
            }

            if (genArg.Count() == 1)
            {
                ICaster dict = (ICaster)Activator.CreateInstance(typeof(Caster<>).MakeGenericType(genArg));
                foreach (IPair pair in dict.ObjToPairList(pVal))
                {
                    list.Add(GetItem<object>(pair.Get(1)));
                }
                return list;
            }

            throw new NotSupportedException("Not supported Multitype");
        }

        private static SimpleJsonItem GetItem<T>(T obj)
        {
            string name = obj.GetType().Name;
            return name == "String" || SimpleJsonItem.CalcType(name) 
                ? new SimpleJsonItem() { Value = obj.ToString(), Type = name } 
                : SerializeItem(obj);
        }

        

#endregion
    }
}

