using Keyrita.Gui;
using System;
using System.Collections;
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
                throw new ArgumentException($"{enumName } type could not be found");
            }

            Enum value = Enum.Parse(enumType, enumConst) as Enum;
            return value;
        }

        public static T[] CopyArray<T>(T[] arr)
        {
            T[] result = new T[arr.Length];

            for(int i = 0; i < arr.Length; i++)
            {
                result[i] = arr[i];
            }

            return result;
        }

        public static bool CompareArray<T>(T[] arr, T[] arr2)
        {
            if(arr.Length != arr2.Length)
            {
                return false;
            }

            for(int i = 0; i < arr.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(arr[i], arr2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static T[][] CopyDoubleArray<T>(T[][] arr)
        {
            T[][] result = new T[arr.Length][];

            for(int i = 0; i < arr.Length; i++)
            {
                result[i] = new T[arr[i].Length];
                for(int j = 0; j < arr[i].Length; j++)
                {
                    result[i][j] = arr[i][j];
                }
            }

            return result;
        }

        public static bool CompareDoubleArray<T>(T[][] arr, T[][] arr2)
        {
            if(arr.Length != arr2.Length)
            {
                return false;
            }

            for(int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Length != arr2[i].Length)
                {
                    return false;
                }

                for(int j = 0; j < arr[i].Length; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(arr[i][j], arr2[i][j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static T[,] CopyRectArray<T>(T[,] arr)
        {
            T[,] result = new T[arr.GetLength(0), arr.GetLength(1)];

            for(int i = 0; i < arr.GetLength(0); i++)
            {
                for(int j = 0; j < arr.GetLength(1); j++)
                {
                    result[i, j] = arr[i, j];
                }
            }

            return result;
        }

        public static bool CompareRectArray<T>(T[,] arr, T[,] arr2)
        {
            if(arr.GetLength(0) != arr2.GetLength(0) ||
                arr.GetLength(1) != arr2.GetLength(1))
            {
                return false;
            }

            for(int i = 0; i < arr.GetLength(0); i++)
            {
                for(int j = 0; j < arr.GetLength(1); j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(arr[i, j], arr2[i, j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;

            double num1 = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.22044604925031E-11;
            double num2 = value1 - value2;

            if (-num1 < num2)
                return num1 > num2;

            return false;
        }

        public static bool CompareRectArrayDoubles(double[,] arr, double[,] arr2)
        {
            if(arr.GetLength(0) != arr2.GetLength(0) ||
                arr.GetLength(1) != arr2.GetLength(1))
            {
                return false;
            }

            for(int i = 0; i < arr.GetLength(0); i++)
            {
                for(int j = 0; j < arr.GetLength(1); j++)
                {
                    if (!AreClose(arr[i, j], arr2[i, j]))
                    {
                        return false;
                    }
                }
            }

            return true;
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
                LogUtils.Assert(false, "Enum set can only be created on an enumeration type.");
            }

            // Never going to return null.
            return Array.Empty<Enum>();
        }

        /// <summary>
        /// Returns all the tokens given type parameter T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetTokens<T>()
        {
            if (typeof(T).IsEnum)
            {
                return Enum.GetValues(typeof(T)).OfType<T>();
            }
            else
            {
                LogUtils.Assert(false, "Enum set can only be created on an enumeration type.");
            }

            // Never going to return null.
            return Array.Empty<T>();
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

        public static string UIToolTip(this Enum token)
        {
            if (mCachedUIData.TryGetValue(token, out UIDataAttribute uiData))
            {
                return uiData.ToolTip;
            }

            var data = CacheUIText(token);
            return data != null ? data.ToolTip : "";
        }

        private static Dictionary<Enum, UIDataAttribute> mCachedUIData = new();

        public static double Logerp(double a, double b, double t)
        {
            return a * Math.Pow(b / a, t);
        }
    }
}
