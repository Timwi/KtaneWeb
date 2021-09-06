using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RT.Serialization;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

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
                    if (attr == null || attr.ReadableName == null)
                        continue;

                    var fType = f.FieldType;
                    if (f.FieldType.TryGetGenericParameters(typeof(Nullable<>), out var fTypes))
                        fType = fTypes[0];

                    var val = req.Post[f.Name].Value;
                    try
                    {
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

                        if (fType == typeof(string))
                            f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? null : val.Trim());
                        else if (fType.IsEnum)
                        {
                            var enumVal = val == null ? null : Enum.Parse(fType, val);
                            if (enumVal != null)
                                f.SetValue(obj, enumVal);
                        }
                        else if (fType == typeof(DateTime))
                            f.SetValue(obj, DateTime.ParseExact(val, "yyyy-MM-dd", null));
                        else if (fType == typeof(string[]))
                            f.SetValue(obj, val.Split(attr.AllowedSeparators).Select(str => str.Trim()).ToArray().Apply(list => list.Length == 0 || (list.Length == 1 && string.IsNullOrWhiteSpace(list[0])) ? null : list));
                        else if (fType == typeof(Dictionary<string, string>))
                        {
                            if (val.Trim() == "") continue;
                            else if (!attr.AllowedDictSeparators.Any(sep => val.Contains(sep)))
                                f.SetValue(obj, new Dictionary<string, string>() { { attr.DefaultKey, string.IsNullOrWhiteSpace(val) ? null : val.Trim() } });
                            else
                                f.SetValue(obj, val.Split(attr.AllowedSeparators).Select(str => str.Split(attr.AllowedDictSeparators)).ToDictionary(x => x[0].Trim(), x => x[1].Trim()));
                        }
                        else if (fType == typeof(int))
                            f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? 0 : int.Parse(val));
                        else if (fType == typeof(decimal))
                            f.SetValue(obj, string.IsNullOrWhiteSpace(val) ? 0m : decimal.Parse(val));
                        else if (fType == typeof(bool))
                            f.SetValue(obj, val == "on");
                        else
                            throw new InvalidOperationException($"Unrecognized field type: {fType.FullName}");
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Generate JSON: unrecognized value. Field: {f.Name}, Type: {fType}, Value: “{val ?? "<null>"}”, Exception: {e.Message} ({e.GetType().FullName})");
                    }
                }
            }

            var m = new KtaneModuleInfo();
            populateObject(m, typeof(KtaneModuleInfo));
            if (string.IsNullOrWhiteSpace(m.Name))
                return HttpResponse.PlainText("You did not specify a module name.");
            var json = ClassifyJson.Serialize(m);
            // Now deserialize and then re-serialize this to force KtaneModuleInfo to perform some sanity things
            var m2 = ClassifyJson.Deserialize<KtaneModuleInfo>(json);
            return HttpResponse.PlainText(ClassifyJson.Serialize(m2).ToStringIndented());
        }
    }
}
