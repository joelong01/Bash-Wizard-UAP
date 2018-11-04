
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace starterBash
{

    public static class StaticHelpers
    {

        public static Dictionary<string, Color> StringToColorDictionary { get; } = new Dictionary<string, Color>()
        {
            {"Blue", Colors.Blue },
            {"Red", Colors.Red },
            {"White", Colors.White },
            {"Yellow", Colors.Yellow },
            {"Green", Colors.Green },
            {"Brown", Colors.Brown },
            {"DarkGray", Colors.DarkGray },
            {"Black", Colors.Black }
        };
        public static Dictionary<Color, string> ColorToStringDictionary { get; } = new Dictionary<Color, string>()
        {
            { Colors.Blue     ,  "Blue"     },
            { Colors.Red      ,  "Red"      },
            { Colors.White    ,  "White"    },
            { Colors.Yellow   ,  "Yellow"   },
            { Colors.Green    ,  "Green"    },
            { Colors.Brown    ,  "Brown"    },
            { Colors.DarkGray ,  "DarkGray" },
            { Colors.Black    ,  "Black"    }
        };

        public static Dictionary<string, string> BackgroundToForegroundDictionary { get; } = new Dictionary<string, string>()
        {
            { "Blue"     , "White" },
            { "Red"      , "White" },
            { "White"    , "Black" },
            { "Yellow"   , "Black" },
            { "Green"    , "Black" },
            { "Brown"    , "White" },
            { "DarkGray" , "White" },
            { "Black"    , "White" }

        };

        public static ObservableCollection<string> AvailableColors { get; } = new ObservableCollection<string>()
        {
            { "Blue"     },
            { "Red"      },
            { "White"    },
            { "Yellow"   },
            { "Green"    },
            { "Brown"    },
            { "DarkGray" },
            { "Black"    }

        };

        public static Dictionary<Color, Color> BackgroundToForegroundColorDictionary { get; } = new Dictionary<Color, Color>()
        {
            { Colors.Blue     , Colors.White },
            { Colors.Red      , Colors.White },
            {Colors.Transparent, Colors.Transparent },
            { Colors.White    , Colors.Black },
            { Colors.Yellow   , Colors.Black },
            { Colors.Green    , Colors.Black },
            { Colors.Brown, Colors.White},
            { Colors.DarkGray , Colors.White},
            { Colors.Black   , Colors.White },
            { Colors.HotPink   , Colors.Purple},


        };

        
        public static bool IsInVisualStudioDesignMode => !(Application.Current is App);

        public static async Task<StorageFolder> GetSaveFolder([CallerMemberName] string cmb = "", [CallerLineNumber] int cln = 0, [CallerFilePath] string cfp = "")
        {
            // System.Diagnostics.Debug.WriteLine($"GetSaveFolder called.  File: {cfp}, Method: {cmb}, Line Number: {cln}");

            string token = "default";
            StorageFolder folder = null;
            try
            {
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                {

                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                    return folder;
                }
            }
            catch { }

            string content = "After clicking on \"Close\" pick the default location for all your Catan saved state";
            MessageDialog dlg = new MessageDialog(content, "Catan");
            try
            {
                await dlg.ShowAsync();

                FolderPicker picker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };

                picker.FileTypeFilter.Add("*");

                folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder);
                }
                else
                {
                    folder = ApplicationData.Current.LocalFolder;
                }


                return folder;
            }
            catch (Exception except)
            {
                Debug.WriteLine(except.ToString());
            }

            return null;
        }

        public static bool ExcludeCommonKeys(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Shift || e.Key == VirtualKey.Control || e.Key == VirtualKey.Menu || e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
                return true;
            }

            return false;
        }
        public static bool FilterKeys(string strToCheck, Regex regex)
        {
            return (regex.IsMatch(strToCheck) == true);
        }
        public class KeyValuePair
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }

        }


        public static void TraceMessage(this object o, string toWrite, [CallerMemberName] string cmb = "", [CallerLineNumber] int cln = 0, [CallerFilePath] string cfp = "")
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{cfp}({cln}):{toWrite}\t\t[Caller={cmb}]");
#endif

        }
        public static void Assert(this object o, bool assert, string toWrite, [CallerMemberName] string cmb = "", [CallerLineNumber] int cln = 0, [CallerFilePath] string cfp = "")
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(assert, $"{cfp}({cln}):{toWrite}\t\t[Caller={cmb}]");
#endif

        }


        public static double SetupFlipAnimation(bool flipToFaceUp, DoubleAnimation back, DoubleAnimation front, double animationTimeInMs, double startAfter = 0)
        {

            if (flipToFaceUp)
            {
                back.To = -90;
                front.To = 0;
                front.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                back.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                back.BeginTime = TimeSpan.FromMilliseconds(startAfter);
                front.BeginTime = TimeSpan.FromMilliseconds(startAfter + animationTimeInMs);
            }
            else
            {
                back.To = 0;
                front.To = 90;
                back.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                front.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                front.BeginTime = TimeSpan.FromMilliseconds(startAfter);
                back.BeginTime = TimeSpan.FromMilliseconds(startAfter + animationTimeInMs);

            }
            return animationTimeInMs;

        }
        public static void Assert(bool val, string message, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            string msg = string.Format($"File: {file}, Method: {memberName}, Line Number: {lineNumber}\n\n{message}");
            Debug.Assert(val, msg);


        }

        public static string GetErrorMessage(string sErr, Exception e, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            return string.Format($"{sErr}\nFile: {file}\n{memberName}: {lineNumber}\n\n{e.ToString()}");
        }

        public static bool IsNumber(VirtualKey key)
        {

            if ((int)key >= (int)VirtualKey.Number0 && (int)key <= (int)VirtualKey.Number9)
            {
                return true;
            }

            if ((int)key >= (int)VirtualKey.NumberPad0 && (int)key <= (int)VirtualKey.NumberPad9)
            {
                return true;
            }

            return false;
        }

        public static bool IsOnKeyPad(VirtualKey key)
        {

            if (IsNumber(key))
            {
                return true;
            }

            switch (key)
            {
                case VirtualKey.Divide:
                case VirtualKey.Multiply:
                case VirtualKey.Subtract:
                case VirtualKey.Decimal:
                case VirtualKey.Enter:
                case VirtualKey.Add:
                case (VirtualKey)187: // '+' 
                case (VirtualKey)189: // '-'
                case (VirtualKey)190: // '.'
                case (VirtualKey)191: // '/'
                    return true;
                default:
                    break;

            }

            return false;
        }

        /// <summary>
        ///  used by KeyDown handlers to filter out invalid rolls and keys that aren't on the NumPad
        /// </summary>
        /// <param name="key"></param>
        /// <param name="chars"></param>
        /// <returns> returns the value to pass in e.Handled </returns>
        public static bool FilterNumpadKeys(VirtualKey key, char[] chars)
        {


            //
            //  filter out everythign not on Keypad (but other keyboard works too)
            if (!StaticHelpers.IsOnKeyPad(key))
            {
                return false;

            }

            if (chars.Length == 0) // first char
            {
                if (key == VirtualKey.Number0 || key == VirtualKey.NumberPad0)
                {

                    return true;
                }

            }
            if (chars.Length == 1)
            {
                if (chars[0] != '1')
                {

                    return true;
                }

                if (key == VirtualKey.Number0 || key == VirtualKey.Number1 || key == VirtualKey.Number2 ||
                    key == VirtualKey.NumberPad0 || key == VirtualKey.NumberPad1 || key == VirtualKey.NumberPad2)
                {
                    // allow 10, 11, 12
                    return false;

                }


                return true;

            }

            return false;
        }

        public static void AddDeltaToIntProperty<T>(this T t, string propName, int delta)
        {
            PropertyInfo propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(propName);
            int n = (int)propInfo.GetValue(t, null);
            n += delta;
            propInfo.SetValue(t, n);
        }




        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            foreach (T item in collection)
            {
                oc.Add(item);
            }


        }



        public const string lineSeperator = "\r\n";
        public const string propertySeperator = ",";
        public const string objectSeperator = "|";
        public const string listSeperator = ";";
        public const char listSeperatorChar = ';';
        public const string kvpSeperator = "=";
        public const string kvpSeperatorOneLine = "="; // can't use : because that is used to serialize time objects...but now we arne't serializing Time
        public const char kvpSeperatorChar = '=';

        //
        //  this only supports List<> collections.  beware... :P
        //
        public static string SerializeObject<T>(this T t, IEnumerable<string> propNames, string kvpSep = kvpSeperatorOneLine, string propSep = propertySeperator)
        {
            string s = "";

            foreach (string prop in propNames)
            {
                PropertyInfo propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(prop);


                if (propInfo == null)
                {
                    t.TraceMessage($"No property named {prop} in SerializeObject");
                    continue;
                }
                TypeInfo typeInfo = propInfo.PropertyType.GetTypeInfo();
                object propValue = propInfo.GetValue(t, null);
                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {

                    IList listInstance = (IList)propValue;
                    s += prop + kvpSep;
                    foreach (object o in listInstance)
                    {
                        s += o.ToString() + listSeperator;

                    }
                    s += propSep;
                }
                else
                {
                    s += string.Format($"{prop}{kvpSep}{propValue}{propSep}");

                }


            }

            return s;
        }
        public static Type GetEnumeratedType(this Type type)
        {
            // provided by Array
            Type elType = type.GetElementType();
            if (null != elType)
            {
                return elType;
            }

            // otherwise provided by collection
            Type[] elTypes = type.GetGenericArguments();
            if (elTypes.Length > 0)
            {
                return elTypes[0];
            }

            // otherwise is not an 'enumerated' type
            return null;
        }

        public static Dictionary<string, object> DictionaryFromType(object atype)
        {
            if (atype == null)
            {
                return new Dictionary<string, object>();
            }

            Type t = atype.GetType();
            PropertyInfo[] props = t.GetProperties();
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (PropertyInfo prp in props)
            {
                object value = prp.GetValue(atype, new object[] { });
                dict.Add(prp.Name, value);
            }
            return dict;
        }

        //
        //  an interface called by the drag and drop code so we can simlulate the DragOver behavior
        public interface IDragAndDropProgress
        {

            void Report(Point value);
            void PointerUp(Point value);
        }

        public static Task<Point> DragAsync(UIElement control, PointerRoutedEventArgs origE, IDragAndDropProgress progress = null)
        {
            TaskCompletionSource<Point> taskCompletionSource = new TaskCompletionSource<Point>();
            UIElement mousePositionWindow = Window.Current.Content;
            Point pointMouseDown = origE.GetCurrentPoint(mousePositionWindow).Position;

            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;

            pointerMovedHandler = (object s, PointerRoutedEventArgs e) =>
            {

                Point pt = e.GetCurrentPoint(mousePositionWindow).Position;

                Point delta = new Point
                {
                    X = pt.X - pointMouseDown.X,
                    Y = pt.Y - pointMouseDown.Y
                };

                if (!(control.RenderTransform is CompositeTransform compositeTransform))
                {
                    compositeTransform = new CompositeTransform();
                    control.RenderTransform = compositeTransform;
                }
                compositeTransform.TranslateX += delta.X;
                compositeTransform.TranslateY += delta.Y;
                control.RenderTransform = compositeTransform;
                pointMouseDown = pt;
                if (progress != null)
                {
                    progress.Report(pt);
                }

            };

            pointerReleasedHandler = (object s, PointerRoutedEventArgs e) =>
            {
                UIElement localControl = (UIElement)s;
                localControl.PointerMoved -= pointerMovedHandler;
                localControl.PointerReleased -= pointerReleasedHandler;
                localControl.ReleasePointerCapture(origE.Pointer);
                Point exitPoint = e.GetCurrentPoint(mousePositionWindow).Position;


                taskCompletionSource.SetResult(exitPoint);
            };

            control.CapturePointer(origE.Pointer);
            control.PointerMoved += pointerMovedHandler;
            control.PointerReleased += pointerReleasedHandler;
            return taskCompletionSource.Task;
        }

        public static void SetKeyValue<T>(this T t, string key, string value)
        {
            PropertyInfo propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(key);
            if (propInfo == null)
            {
                t.TraceMessage($"No property named {key} in DeserializeObject");
                return;
            }
            TypeInfo typeInfo = propInfo.PropertyType.GetTypeInfo();
            if (typeInfo.Name == "Guid")
            {
                // typeInfo.TraceMessage("Need to support Guid in deserializer!");
                return;
            }
            if (typeInfo.IsEnum)
            {
                propInfo.SetValue(t, Enum.Parse(propInfo.PropertyType, value));
            }
            else if (typeInfo.IsPrimitive)
            {
                propInfo.SetValue(t, Convert.ChangeType(value, propInfo.PropertyType));
            }
            else if (propInfo.PropertyType == typeof(System.TimeSpan))
            {
                propInfo.SetValue(t, TimeSpan.Parse(value));
            }
            else if (propInfo.PropertyType == typeof(string))
            {
                propInfo.SetValue(t, value);
            }
            else if (propInfo.PropertyType == typeof(bool?))
            {
                if (bool.TryParse(value, out bool res))
                {
                    propInfo.SetValue(t, res);
                }
                else
                {
                    propInfo.SetValue(t, null);
                }

            }
            else if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = typeInfo.GenericTypeArguments[0];
                string[] arrayValues = value.Split(listSeperator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                Type listType = typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments);
                IList listInstance = (IList)Activator.CreateInstance(listType);

                bool isPrimitive = elementType.GetTypeInfo().IsPrimitive;
                bool isEnum = elementType.GetTypeInfo().IsEnum;
                foreach (string val in arrayValues)
                {
                    if (isPrimitive)
                    {
                        object o = Convert.ChangeType(val, elementType);
                        listInstance.Add(o);
                    }
                    else if (isEnum)
                    {
                        object e = Enum.Parse(elementType, val);
                        listInstance.Add(e);
                    }
                    else
                    {
                        t.TraceMessage($"Can't deserialize list of type {elementType.GetTypeInfo()}");
                        break;
                    }

                }
                propInfo.SetValue(t, listInstance);
            }
            else
            {
                string error = string.Format($"need to support {propInfo.PropertyType.ToString()} in the deserilizer to load {key} whose value is {value}");
                t.TraceMessage(error);
                throw new Exception(error);

            }
        }

        public static void DeserializeObject<T>(this T t, string s, string kvpSep = kvpSeperatorOneLine, string propSep = propertySeperator)
        {




            string[] properties = s.Split(propSep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            KeyValuePair kvp = null;
            foreach (string line in properties)
            {
                kvp = StaticHelpers.GetKeyValue(line, kvpSep[0]);
                t.SetKeyValue(kvp.Key, kvp.Value);

            }

        }

        public static List<int> GetIntegerList(string s, char sep = StaticHelpers.listSeperatorChar)
        {

            string[] strings = s.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);
            List<int> ret = new List<int>();
            foreach (string v in strings)
            {
                if (v != "")
                {
                    ret.Add(Convert.ToInt32(v));
                }
            }

            return ret;
        }

        public static Stack<int> GetStack(string s, string sep = StaticHelpers.listSeperator)
        {

            string[] strings = s.Split(sep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Stack<int> ret = new Stack<int>();
            for (int i = strings.Count() - 1; i >= 0; i--)
            {

                ret.Push(Convert.ToInt32(strings[i]));

            }

            return ret;
        }

        public static string GetValue(string s, char sep = kvpSeperatorChar)
        {
            string[] values = s.Split(sep);
            return values[1];
        }

        public static bool GetBoolValue(string s, char sep = kvpSeperatorChar)
        {
            string[] values = s.Split(sep);
            return Convert.ToBoolean(values[1]);

        }

        public static int GetIntValue(string s, char sep = kvpSeperatorChar)
        {
            string[] values = s.Split(sep);
            return Convert.ToInt32(values[1]);
        }

        public static string SetValue(string name, object value)
        {
            return string.Format($"{name}={value}{lineSeperator}");

        }

        public static KeyValuePair GetKeyValue(string s, char sep = kvpSeperatorChar)
        {

            string[] tokens = s.Split(sep);
            KeyValuePair kvp = new KeyValuePair("", "");
            if (tokens.Length == 2)
            {
                kvp = new KeyValuePair(tokens[0], tokens[1]);
            }

            return kvp;
        }

        /*
               Given an file with the following form:
               [section1]
               key1=value
               key2=value
               key3=value
               [section2]
               key1=value
               key2=value

           

           Users: dict["section"] = "key1=value\nkey2=value\nkey3=value\n"
       */

        public static Dictionary<string, string> GetSections(string file)
        {
            char[] sep1 = new char[] { '[' };
            char[] sep2 = new char[] { ']' };

            string[] tokens = file.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> sections = new Dictionary<string, string>();
            foreach (string s in tokens)
            {
                string[] tok1 = s.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                sections.Add(tok1[0], tok1[1]);

            }

            return sections;
        }

        public static async Task<Dictionary<string, string>> LoadSectionsFromFile(StorageFolder folder, string filename)
        {

            StorageFile file = await folder.GetFileAsync(filename);
            string contents = await FileIO.ReadTextAsync(file);
            contents = contents.Replace('\r', '\n');
            Dictionary<string, string> sectionsDict = null;
            try
            {
                sectionsDict = StaticHelpers.GetSections(contents);
            }
            catch (Exception e)
            {
                string content = string.Format($"Error parsing file {filename}.\nIn File: {folder.Path}\n\nSuggest deleting it.\n\nError parsing sections.\nException info: {e.ToString()}");
                MessageDialog dlg = new MessageDialog(content);
                await dlg.ShowAsync();
            }

            return sectionsDict;

        }


        /*
                Given an file with the following form:
                [section1]
                key1=value
                key2=value
                key3=value
                [section2]
                key1=value
                key2=value

            Then parse the file into a Dictionary<string,string> and return it.  

            Users: dict["section"]["key2"] is "value"
        */

        public static async Task<Dictionary<string, Dictionary<string, string>>> LoadSettingsFile(StorageFolder folder, string filename)
        {
            KeyValuePair<string, string> currentKvp = new KeyValuePair<string, string>();
            Dictionary<string, Dictionary<string, string>> returnDictionary = new Dictionary<string, Dictionary<string, string>>();


            StorageFile file = await folder.GetFileAsync(filename);
            string contents = await FileIO.ReadTextAsync(file);
            contents = contents.Replace('\r', '\n');
            Dictionary<string, string> sectionsDict = null;
            try
            {
                sectionsDict = StaticHelpers.GetSections(contents);
            }
            catch (Exception e)
            {
                string content = string.Format($"Error parsing file {filename}.\nIn File: {folder.Path}\n\nSuggest deleting it.\n\nError parsing sections.\nException info: {e.ToString()}");
                MessageDialog dlg = new MessageDialog(content);
                await dlg.ShowAsync();
                return returnDictionary;
            }

            if (sectionsDict.Count == 0)
            {
                string content = string.Format($"There appears to be no sections in {filename}.\nIn File: {folder.Path}\n\nSuggest deleting it.\n\nError parsing sections.");
                MessageDialog dlg = new MessageDialog(content);
                await dlg.ShowAsync();
                return returnDictionary;
            }

            try
            {
                foreach (KeyValuePair<string, string> kvp in sectionsDict)
                {
                    currentKvp = kvp;
                    Dictionary<string, string> dict = DeserializeDictionary(kvp.Value);
                    returnDictionary[kvp.Key] = dict;
                }

            }
            catch
            {
                string content = string.Format($"Error parsing values {folder.Path}\\{filename}.\nSuggest deleting it.\n\nError in section '{currentKvp.Key}' and value '{currentKvp.Value}'");
                MessageDialog dlg = new MessageDialog(content);
                await dlg.ShowAsync();

            }


            return returnDictionary;
        }

        public static async Task ShowErrorText(string s)
        {
            MessageDialog dlg = new MessageDialog(s);
            await dlg.ShowAsync();
        }

        public static string SerializeDictionary(Dictionary<string, string> dictionary, string seperator = StaticHelpers.lineSeperator)
        {
            string ret = "";

            foreach (KeyValuePair<string, string> kvp in dictionary)
            {
                ret += string.Format("{0}={1}{2}", kvp.Key, kvp.Value, seperator);

            }

            return ret;

        }

        /*  creates something thant looks like

            Log-1=<>
            Log-2=<>

        */

        public static string SerilizeListToSection<T>(this IList<T> list, string prefix)
        {
            string s = "";
            int n = 0;
            if (list != null)
            {
                MethodInfo methodInfo = typeof(T).GetTypeInfo().GetDeclaredMethod("Serialize");
                foreach (T item in list)
                {
                    n++;
                    string propValue = methodInfo.Invoke(item, null).ToString();
                    s += string.Format($"{prefix}-{n}{StaticHelpers.kvpSeperator}{propValue}{StaticHelpers.lineSeperator}");
                }
            }



            return s;
        }

        public static string SerializeList<T>(this IList<T> list, string sep = StaticHelpers.listSeperator)
        {

            string s = "";
            if (list != null)
            {
                foreach (T item in list)
                {
                    s += item.ToString() + sep;
                }
            }

            return s;
        }
        /// <summary>
        ///     This will serialize a IList<> into a string that can be deserialized. You can pass in an arbitrary list of thingies
        ///     and it will serialize the property passed in 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propName"></param>
        /// <param name="sep"></param>
        /// <returns></returns>

        public static string SerializeListWithProperty<T>(this IList<T> list, string propName, string sep = StaticHelpers.listSeperator)
        {

            string s = "";
            if (list != null)
            {
                PropertyInfo propInfo = typeof(T).GetTypeInfo().GetDeclaredProperty(propName);
                foreach (T item in list)
                {
                    string propValue = propInfo.GetValue(item, null).ToString();
                    s += propValue + sep;
                }
            }

            return s;
        }
        public static bool TryParse<T>(this Enum theEnum, string valueToParse, out T returnValue)
        {
            returnValue = default;
            if (int.TryParse(valueToParse, out int intEnumValue))
            {
                if (Enum.IsDefined(typeof(T), intEnumValue))
                {
                    returnValue = (T)(object)intEnumValue;
                    return true;
                }
            }
            return false;
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static List<T> DeserializeList<T>(this string s, string sep = StaticHelpers.listSeperator)
        {
            List<T> list = new List<T>();
            char[] charSep = sep.ToCharArray();
            string[] tokens = s.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tokens)
            {
                T value = (T)Convert.ChangeType(t, typeof(T));
                list.Add(value);
            }
            return list;
        }

        public static List<T> DeserializeEnumList<T>(this string s, string sep = StaticHelpers.listSeperator)
        {
            List<T> list = new List<T>();
            char[] charSep = sep.ToCharArray();
            string[] tokens = s.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tokens)
            {
                T value = default;
                if (Enum.IsDefined(typeof(T), t))
                {
                    value = (T)Enum.Parse(typeof(T), t);
                    list.Add(value);
                }


            }
            return list;
        }



        public static Dictionary<string, string> DeserializeDictionary(string section, string lineSeperator = StaticHelpers.lineSeperator)
        {

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            char[] sep1 = lineSeperator.ToCharArray();
            char[] sep2 = new char[] { '=' };

            string[] tokens = section.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in tokens)
            {
                string[] pairs = s.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Count() == 2)
                {
                    dictionary.Add(pairs[0], pairs[1]);
                }
                else if (pairs.Count() == 1)
                {

                    dictionary.Add(pairs[0], "");
                }
                else
                {
                    Debug.Assert(false, string.Format($"Bad token count in DeserializeDictionary. Pairs.Count: {pairs.Count()} "));
                }


            }


            return dictionary;
        }

        public static Dictionary<string, string> GetSection(string file, string section)
        {
            Dictionary<string, string> sections = StaticHelpers.GetSections(file);
            if (sections == null)
            {
                return null;
            }

            return StaticHelpers.DeserializeDictionary(sections[section]);

        }



        static public async Task RunStoryBoard(Storyboard sb, bool callStop = true, double ms = 500, bool setTimeout = true)
        {
            if (setTimeout)
            {
                foreach (Timeline animations in sb.Children)
                {
                    animations.Duration = new Duration(TimeSpan.FromMilliseconds(ms));
                }
            }

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            void completed(object s, object e) => tcs.TrySetResult(null);
            try
            {
                sb.Completed += completed;
                sb.Begin();
                await tcs.Task;
            }
            finally
            {
                sb.Completed -= completed;
                if (callStop)
                {
                    sb.Stop();
                }
            }

        }

        public static List<T> DestructiveIterator<T>(this List<T> list)
        {
            List<T> copy = new List<T>(list);
            return copy;

        }
        public static T Pop<T>(this List<T> list)
        {
            T t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }

        public static T Pop<T>(this ObservableCollection<T> list)
        {
            T t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }
        public static void Push<T>(this ObservableCollection<T> list, T t)
        {
            list.Add(t);
        }


        public static void Push<T>(this List<T> list, T t)
        {
            list.Add(t);
        }

        public static T Peek<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                return list.Last();
            }

            return default;
        }

        public static Task<object> ToTask(this Storyboard storyboard, CancellationTokenSource cancellationTokenSource = null)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(TaskCreationOptions.AttachedToParent);

            if (cancellationTokenSource != null)
            {
                // when the task is cancelled, 
                // Stop the storyboard
                cancellationTokenSource.Token.Register
                (
                    () =>
                    {
                        storyboard.Stop();
                    }
                );
            }

            void onCompleted(object s, object e)
            {
                storyboard.Completed -= onCompleted;

                tcs.SetResult(null);
            }

            storyboard.Completed += onCompleted;

            // start the storyboard during the conversion.
            storyboard.Begin();

            return tcs.Task;
        }


        static public void RunStoryBoardAsync(Storyboard sb, double ms = 500, bool setTimeout = true)
        {
            if (setTimeout)
            {
                foreach (Timeline animations in sb.Children)
                {
                    animations.Duration = new Duration(TimeSpan.FromMilliseconds(ms));
                }
            }

            sb.Begin();
        }

        static public void SetFlipAnimationSpeed(Storyboard sb, double milliseconds)
        {

            foreach (Timeline animation in sb.Children)
            {
                if (animation.Duration != TimeSpan.FromMilliseconds(0))
                {
                    animation.Duration = TimeSpan.FromMilliseconds(milliseconds);
                }

                if (animation.BeginTime != TimeSpan.FromMilliseconds(0))
                {
                    animation.BeginTime = TimeSpan.FromMilliseconds(milliseconds);
                }

            }
        }

        static public async Task<bool> AskUserYesNoQuestion(string question, string button1, string button2)
        {

            bool saidYes = false;




            ContentDialog dlg = new ContentDialog()
            {
                Title = "Catan",
                Content = "\n" + question,
                PrimaryButtonText = button1,
                SecondaryButtonText = button2
            };

            dlg.PrimaryButtonClick += (o, i) =>
           {
               saidYes = true;
           };


            await dlg.ShowAsync();


            return saidYes;

        }


    }

}
