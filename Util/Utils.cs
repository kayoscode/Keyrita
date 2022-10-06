using Keyrita.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Keyrita.Util
{
    internal static class Utils
    {
        /// <summary>
        /// From an enum name and value, returns the result parsed to an Enum.
        /// </summary>
        /// <param name="enumName"></param>
        /// <param name="enumConst"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Enum GetEnumValue(string enumName, string enumConst)
        {
            Type enumType = Type.GetType(enumName);
            if (enumType == null)
            {
                throw new ArgumentException("Specified enum type could not be found", "enumName");
            }

            Enum value = Enum.Parse(enumType, enumConst) as Enum;
            return value;
        }

        /// <summary>
        /// Returns all the tokens in an enumeration as an enumerable object.
        /// </summary>
        /// <param name="enumeration"></param>
        /// <returns></returns>
        public static IEnumerable<Enum> GetTokens(Type enumeration)
        {
            if (enumeration != null && enumeration.IsEnum)
            {
                return Enum.GetValues(enumeration).OfType<Enum>();
            }
            else
            {
                LTrace.Assert(false, "Enum set can only be created on an enumeration type.");
            }

            // Never going to return null.
            return Array.Empty<Enum>();
        }

        private static UIDataAttribute CacheUIText(this Enum token)
        {
            var type = token.GetType();
            var memInfo = type.GetMember(token.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(UIDataAttribute), false);
            var data = (attributes.Length > 0) ? (UIDataAttribute)attributes[0] : null;

            if (data == null)
            {
                return null;
            }

            mCachedUIData[token] = data;
            return data;
        }

        public static string UIText(this Enum token)
        {
            if (mCachedUIData.TryGetValue(token, out UIDataAttribute uiData))
            {
                return uiData.UIText;
            }

            var data = CacheUIText(token);
            return data != null ? data.UIText : "";
        }

        public static string UIAbbreviation(this Enum token)
        {
            if (mCachedUIData.TryGetValue(token, out UIDataAttribute uiData))
            {
                return uiData.Abbreviation;
            }

            var data = CacheUIText(token);
            return data != null ? data.Abbreviation : "";
        }

        private static Dictionary<Enum, UIDataAttribute> mCachedUIData = new();
    }
}
