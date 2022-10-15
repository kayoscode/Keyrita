using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using Keyrita.Interop.NativeAnalysis;

namespace Keyrita.Util
{
    /// <summary>
    /// Just serializes the basic information for the KBD text file.
    /// </summary>
    public static class KBDTextFile
    {
        private static readonly List<(string name, string value)> KEY_NAMES = new List<(string name, string value)>()
        {
            ("01", "Esc"),
            ("0e", "Backspace"),
            ("0f", "Tab"),
            ("1c", "Enter"),
            ("1d", "Ctrl"),
            ("2a", "Shift"),
            ("36", "\"Right Shift\""),
            ("37", "\"Num *\""),
            ("38", "Alt"),
            ("39", "Space"),
            ("3a", "\"Caps Lock\""),
            ("3b", "F1"),
            ("3c", "F2"),
            ("3d", "F3"),
            ("3e", "F4"),
            ("3f", "F5"),
            ("40", "F6"),
            ("41", "F7"),
            ("42", "F8"),
            ("43", "F9"),
            ("44", "F10"),
            ("45", "Pause"),
            ("46", "\"Scroll Lock\""),
            ("47", "\"Num 7\""),
            ("48", "\"Num 8\""),
            ("49", "\"Num 9\""),
            ("4a", "\"Num -\""),
            ("4b", "\"Num 4\""),
            ("4c", "\"Num 5\""),
            ("4d", "\"Num 6\""),
            ("4e", "\"Num +\""),
            ("4f", "\"Num 1\""),
            ("50", "\"Num 2\""),
            ("51", "\"Num 3\""),
            ("52", "\"Num 0\""),
            ("53", "\"Num Del\""),
            ("54", "\"Sys Req\""),
            ("57", "F11"),
            ("58", "F12"),
            ("7c", "F13"),
            ("7d", "F14"),
            ("7e", "F15"),
            ("7f", "F16"),
            ("80", "F17"),
            ("81", "F18"),
            ("82", "F19"),
            ("83", "F20"),
            ("84", "F21"),
            ("85", "F22"),
            ("86", "F23"),
            ("87", "F24"),
        };

        private static readonly List<(string name, string value)> KEY_NAMES_EXT = new List<(string name, string value)>()
        {
            ("1c", "\"Num Enter\""),
            ("1d", "\"Right Ctrl\""),
            ("35", "\"Num /\""),
            ("37", "\"Prnt Scrn\""),
            ("38", "\"Right Alt\""),
            ("45", "\"Num Lock\""),
            ("46", "Break"),
            ("47", "Home"),
            ("48", "Up"),
            ("49", "\"Page Up\""),
            ("4b", "Left"),
            ("4d", "Right"),
            ("4f", "End"),
            ("50", "Down"),
            ("51", "\"Page Down\""),
            ("52", "Insert"),
            ("53", "Delete"),
            ("54", "<00>"),
            ("56", "Help"),
            ("5b", "\"Left Windows\""),
            ("5c", "\"Right Windows\""),
            ("5d", "Application"),
        };
        private static readonly List<string> ROW_ONE_SC_VK = new List<string>()
        {
            "10\tQ",
            "11\tW",
            "12\tE",
            "13\tR",
            "14\tT",
            "15\tY",
            "16\tU",
            "17\tI",
            "18\tO",
            "19\tP"
        };

        private static readonly List<string> ROW_TWO_SC_VK = new List<string>()
        {
            "1e\tA",
            "1f\tS",
            "22\tD",
            "21\tF",
            "22\tG",
            "23\tH",
            "24\tJ",
            "25\tK",
            "26\tL",
            "27\tOEM_1"
        };

        private static readonly List<string> ROW_THREE_SC_VK = new List<string>()
        {
            "2c\tZ",
            "2d\tX",
            "2e\tC",
            "2f\tV",
            "30\tB",
            "31\tN",
            "32\tM",
            "33\tOEM_COMMA",
            "34\tOEM_PERIOD",
            "35\tOEM_2"
        };

        private static readonly List<List<string>> ROWX_OEM_VK = new List<List<string>>()
        {
            ROW_ONE_SC_VK,
            ROW_TWO_SC_VK,
            ROW_THREE_SC_VK
        };

        public static char GetUpperCaseKey(char c)
        {
            short vkKeyScanResult = NativeAnalysis.VkKeyScan(c);

            // a result of -1 indicates no key translates to input character
            if (vkKeyScanResult == -1)
            {
                LTrace.Assert(false, "No key mapping for " + c);
            }

            // vkKeyScanResult & 0xff is the base key, without any modifiers
            uint code = (uint)vkKeyScanResult & 0xff;

            // set shift key pressed
            byte[] b = new byte[256];
            b[0x10] = 0x80;

            uint r = 0;
            // return value of 1 expected (1 character copied to r)
            if (1 != NativeAnalysis.ToAscii(code, code, b, out r, 0))
            {
                LTrace.Assert(false, "Could not translate modified state");
            }

            return (char)r;
        }

        private static Dictionary<string, (string, string)> CreateRowX(char[,] keys, int row)
        {
            LTrace.Assert(row < 3, "Invalid VK row.");
            Dictionary<string, (string, string)> rowData = new Dictionary<string, (string, string)>();

            List<string> currentVKs = ROWX_OEM_VK[row];

            for(int i = 0; i < keys.GetLength(1); i++)
            {
                char c = keys[row, i];
                char cUpper = GetUpperCaseKey(keys[row, i]);

                if(char.IsLetter(c) && char.IsLetter(cUpper))
                {
                    rowData.Add(currentVKs[i], ($"{c}", $"{cUpper}"));
                }
                else
                {
                    // Use 4 digits from the charcode.
                    rowData.Add(currentVKs[i], ($"{((int)c).ToString("x4")}", 
                                                $"{((int)cUpper).ToString("x4")}"));
                }
            }

            return rowData;
        }

        private static void WriteHeaderData(StreamWriter writer, string name,
            string description, string copyright, string company,
            CultureInfo culture)
        {
            // Write basic header info.
            writer.Write("KBD\t");
            writer.Write(name);
            writer.Write("\t\"");
            writer.Write(description);
            writer.Write("\"\r\n");
            writer.WriteLine();

            // Write in the copyright.
            if (copyright.Length > 0)
            {
                writer.Write("COPYRIGHT\t\"");
                writer.Write(copyright);
                writer.Write("\"\r\n");
                writer.WriteLine();
            }

            writer.Write("COMPANY\t\"");
            writer.Write(company);
            writer.Write("\"\r\n");
            writer.WriteLine();
            writer.Write("LOCALENAME\t\"");
            writer.Write(culture.Name);
            writer.Write("\"\r\n");
            writer.WriteLine();
            writer.Write("LOCALEID\t\"");
            writer.Write(culture.LCID.ToString("x8"));
            writer.Write("\"\r\n");
            writer.WriteLine();
            writer.WriteLine("VERSION\t1.0\r\n");
        }

        private static void WriteRow(StreamWriter writer, Dictionary<string, (string, string)> row)
        {
            foreach(var k in row)
            {
                writer.Write(k.Key);

                // Write shift state.
                if(k.Value.Item1.Length != 1)
                {
                    writer.Write("\t0\t");
                }
                else
                {
                    writer.Write("\t1\t");
                }

                // Write keycodes
                writer.Write($"{k.Value.Item1}\t{k.Value.Item2}");

                // Finalize with -1
                writer.Write("\t-1");
                writer.WriteLine();
            }
        }

        public static void Serialize(string fileName, string name, string description, CultureInfo culture,
            string company, string copyright, char[,] keys)
        {
            StringBuilder currentLineSb = null;
            StringBuilder columnSb = null;

            if (fileName.Length == 0)
            {
                fileName = name + ".txt";
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            StreamWriter writer = new StreamWriter(fileName, false, Encoding.Unicode);
            WriteHeaderData(writer, name, description, copyright, company, culture);

            writer.WriteLine("SHIFTSTATE\r\n");
            int column = 3;
            currentLineSb = new StringBuilder();
            columnSb = new StringBuilder();

            column++;
            currentLineSb.Append("\t0");
            columnSb.Append("\t----");
            writer.Write("0\t//Column ");
            writer.Write(column.ToString());
            writer.WriteLine();

            column++;
            currentLineSb.Append("\t1");
            columnSb.Append("\t----");
            writer.Write("1\t//Column ");
            writer.Write(column.ToString());
            writer.Write(" : Shft");
            writer.WriteLine();

            column++;
            currentLineSb.Append("\t2");
            columnSb.Append("\t----");
            writer.Write("2\t//Column ");
            writer.Write(column.ToString());
            writer.Write(" :       Ctrl");
            writer.WriteLine();

            writer.WriteLine();

            writer.WriteLine("LAYOUT\t\t;an extra '@' at the end is a dead key\r\n");
            writer.Write("//SC\tVK_\t\tCap");
            writer.Write(currentLineSb.ToString());
            writer.WriteLine();
            writer.Write("//--\t----\t\t----");
            writer.Write(columnSb.ToString());
            writer.WriteLine();
            writer.WriteLine();

            // Write numbers, these will not change from layout to layout.
            writer.WriteLine("02	1		0	1	0021	-1		// DIGIT ONE, EXCLAMATION MARK, <none>");
            writer.WriteLine("03	2		0	2	0040	-1		// DIGIT TWO, COMMERCIAL AT, <none>");
            writer.WriteLine("04	3		0	3	0023	-1		// DIGIT THREE, NUMBER SIGN, <none>");
            writer.WriteLine("05	4		0	4	0024	-1		// DIGIT FOUR, DOLLAR SIGN, <none>");
            writer.WriteLine("06	5		0	5	0025	-1		// DIGIT FIVE, PERCENT SIGN, <none>");
            writer.WriteLine("07	6		0	6	005e	-1		// DIGIT SIX, CIRCUMFLEX ACCENT, <none>");
            writer.WriteLine("08	7		0	7	0026	-1		// DIGIT SEVEN, AMPERSAND, <none>");
            writer.WriteLine("09	8		0	8	002a	-1		// DIGIT EIGHT, ASTERISK, <none>");
            writer.WriteLine("0a	9		0	9	0028	-1		// DIGIT NINE, LEFT PARENTHESIS, <none>");
            writer.WriteLine("0b	0		0	0	0029	-1		// DIGIT ZERO, RIGHT PARENTHESIS, <none>");
            writer.WriteLine("0c	OEM_MINUS	0	002d	005f	-1		// HYPHEN-MINUS, LOW LINE, <none>");
            writer.WriteLine("0d	OEM_PLUS	0	003d	002b	-1		// EQUALS SIGN, PLUS SIGN, <none>");

            // Write first row.
            var row1 = CreateRowX(keys, 0);
            WriteRow(writer, row1);

            writer.WriteLine("1a	OEM_4		0	005b	007b	-1		// LEFT SQUARE BRACKET, LEFT CURLY BRACKET, <none>");
            writer.WriteLine("1b	OEM_6		0	005d	007d	-1		// RIGHT SQUARE BRACKET, RIGHT CURLY BRACKET, <none>");

            // Write second row.
            var row2 = CreateRowX(keys, 1);
            WriteRow(writer, row2);
            writer.WriteLine("29	OEM_3		0	0060	007e	-1		// GRAVE ACCENT, TILDE, <none>");
            writer.WriteLine("2b	OEM_5		0	005c	007c	-1		// REVERSE SOLIDUS, VERTICAL LINE, <none>");

            var row3 = CreateRowX(keys, 2);
            WriteRow(writer, row3);
            writer.WriteLine("39	SPACE		0	0020	0020	-1		// SPACE, SPACE, <none>");
            writer.WriteLine("53	DECIMAL	0	002e	002e	-1		// FULL STOP, FULL STOP, ");

            writer.WriteLine();
            writer.WriteLine("KEYNAME\r\n");
            foreach ((string name, string value) keynames in KEY_NAMES)
            {
                writer.Write(keynames.name);
                writer.Write('\t');
                writer.Write(keynames.value);
                writer.WriteLine();
            }
            writer.WriteLine();
            writer.WriteLine("KEYNAME_EXT\r\n");

            foreach ((string name, string value) keyNamesExt in KEY_NAMES_EXT)
            {
                writer.Write(keyNamesExt.name);
                writer.Write('\t');
                writer.Write(keyNamesExt.value);
                writer.WriteLine();
            }

            writer.WriteLine();

            writer.WriteLine("DESCRIPTIONS\r\n");
            writer.Write("0409");
            writer.Write('\t');
            writer.Write(description);
            writer.WriteLine();

            writer.WriteLine("LANGUAGENAMES\r\n");
            writer.Write("0409");
            writer.Write('\t');
            writer.Write(culture.EnglishName);
            writer.WriteLine();

            writer.WriteLine("ENDKBD");
            writer.Flush();
            writer.Close();
        }
    }
}
