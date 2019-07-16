using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse generateJson(HttpRequest req)
        {
            if (req.Method != HttpMethod.Post)
                return HttpResponse.PlainText("Only POST requests allowed.", HttpStatusCode._405_MethodNotAllowed);

            void populateObject(object obj, Type type)
            {
                foreach (var f in type.GetFields())
                {
                    var attr = f.GetCustomAttribute<EditableFieldAttribute>();
                    if (attr == null)
                        continue;

                    var val = req.Post[f.Name].Value;
                    if (f.GetCustomAttribute<EditableNestedAttribute>() != null)
                    {
                        if (val != "on")
                            f.SetValue(obj, null);
                        else
                        {
                            var nestedObj = Activator.CreateInstance(f.FieldType);
                            populateObject(nestedObj, f.FieldType);
                            f.SetValue(obj, nestedObj);
                        }
                        continue;
                    }

                    if (f.FieldType == typeof(string))
                        f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? null : val.Trim());
                    else if (f.FieldType.IsEnum)
                    {
                        var enumVal = val == null ? null : Enum.Parse(f.FieldType, val);
                        if (enumVal != null)
                            f.SetValue(obj, enumVal);
                    }
                    else if (f.FieldType.TryGetGenericParameters(typeof(Nullable<>), out var types) && types[0].IsEnum)
                        f.SetValue(obj, val == null ? null : Enum.Parse(types[0], val));
                    else if (f.FieldType == typeof(DateTime))
                        f.SetValue(obj, DateTime.ParseExact(val, "yyyy-MM-dd", null));
                    else if (f.FieldType == typeof(string[]))
                        f.SetValue(obj, val.Split(';').Select(str => str.Trim()).ToArray());
                    else if (f.FieldType == typeof(int))
                        f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? 0 : int.Parse(val));
                    else if (f.FieldType == typeof(decimal))
                        f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? 0m : decimal.Parse(val));
                    else if (f.FieldType == typeof(bool))
                        f.SetValue(obj, val == "on");
                    else
                        Debugger.Break();
                }
            }
            var m = new KtaneModuleInfo();
            populateObject(m, typeof(KtaneModuleInfo));
            var json = ClassifyJson.Serialize(m);
            // Now deserialize and then re-serialize this to force KtaneModuleInfo to perform some sanity things
            var m2 = ClassifyJson.Deserialize<KtaneModuleInfo>(json);
            return HttpResponse.PlainText(ClassifyJson.Serialize(m2).ToStringIndented());
        }
    }
}
