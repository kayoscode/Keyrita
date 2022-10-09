using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Serialization
{
    /// <summary>
    /// Interface outlining conversions of a type to and from strings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITextSerializer<T>
    {
        string ToText(T obj);
        bool TryParse(string text, out T obj);
    }

    /// <summary>
    /// Base class which all serializers must inherit from.
    /// </summary>
    public abstract class TextSerializer : ITextSerializer<object>
    {
        public abstract string ToText(object obj);
        public abstract bool TryParse(string text, out object obj);
    }

    /// <summary>
    /// Converts and enum to text, and vice versa.
    /// </summary>
    public class EnumSerializer : TextSerializer, ITextSerializer<Enum>
    {
        public string ToText(Enum obj)
        {
            return $"{obj.GetType()} {obj}";
        }

        public override string ToText(object obj)
        {
            return ToText((Enum)obj);
        }

        public bool TryParse(string text, out Enum obj)
        {
            // Get the enum value from the text.
            string[] value = text.Split(" ");

            if(value.Length == 2)
            {
                var v = Utils.GetEnumValue(value[0], value[1]);
                obj = v;
                return true;
            }

            obj = null;
            return false;
        }

        public override bool TryParse(string text, out object obj)
        {
            return TryParse(text, out obj);
        }
    }

    /// <summary>
    /// Converts a char to text and vice versa.
    /// </summary>
    public class CharSerializer : TextSerializer, ITextSerializer<char>
    {
        public override string ToText(object obj)
        {
            return ToText(obj);
        }

        public string ToText(char obj)
        {
            return $"{obj}";
        }

        public override bool TryParse(string text, out object obj)
        {
            return TryParse(text, out obj);
        }

        public bool TryParse(string text, out char obj)
        {
            if(text.Length >= 1)
            {
                obj = text[0];
                return true;
            }

            obj = default(char);
            return false;
        }
    }

    /// <summary>
    /// Converts a char to text and vice versa.
    /// </summary>
    public class UIntSerializer : TextSerializer, ITextSerializer<uint>
    {
        public override string ToText(object obj)
        {
            return ToText(obj);
        }

        public string ToText(uint obj)
        {
            return $"{obj}";
        }

        public override bool TryParse(string text, out object obj)
        {
            return TryParse(text, out obj);
        }

        public bool TryParse(string text, out uint obj)
        {
            if(text.Length >= 1)
            {
                obj = uint.Parse(text);
                return true;
            }

            obj = default(uint);
            return false;
        }
    }

    /// <summary>
    /// Class storing a dictionary mapping a type to a serializer.
    /// </summary>
    public static class TextSerializers
    {
        private static IReadOnlyDictionary<Type, TextSerializer> Serializers { get; } = new Dictionary<Type, TextSerializer>()
        {
            { typeof(Enum), new EnumSerializer() },
            { typeof(char), new CharSerializer() },
            { typeof(uint), new UIntSerializer() }
        };

        /// <summary>
        /// Converts a generic object to a text string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToText<T>(T value)
        {
            if (Serializers.TryGetValue(typeof(T), out TextSerializer serializer)) 
            {
                ITextSerializer<T> tSerializer = serializer as ITextSerializer<T>;
                LTrace.Assert(tSerializer != null, "Invalid serializer for given type.");

                return tSerializer.ToText(value);
            }
            else
            {
                LTrace.Assert(false, "Could not find serializer for given type.");
            }

            return "";
        }

        /// <summary>
        /// Attempts to parse the given text to a value using a text serializer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryParse<T>(string str, out T value)
        {
            if (Serializers.TryGetValue(typeof(T), out TextSerializer serializer)) 
            {
                ITextSerializer<T> tSerializer = serializer as ITextSerializer<T>;
                LTrace.Assert(tSerializer != null, "Invalid serializer for given type.");

                if (tSerializer.TryParse(str, out value))
                {
                    return true;
                }
            }
            else
            {
                LTrace.Assert(false, "Could not find serializer for given type.");
            }

            value = default(T);
            return false;
        }
    }
}
