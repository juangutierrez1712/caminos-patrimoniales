using System;
using System.Collections.Generic;
using System.Text;

// Parser JSON mínimo — evita depender de paquetes externos que Kaspersky
// pueda bloquear. Devuelve Dictionary<string, object> para objetos JSON
// y List<object> para arrays. Números como double, todo lo demás string/bool/null.
public static class SimpleJson
{
    public static object Parse(string json)
    {
        int index = 0;
        return ParseValue(json, ref index);
    }

    private static object ParseValue(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        char c = s[i];

        if (c == '{') return ParseObject(s, ref i);
        if (c == '[') return ParseArray(s, ref i);
        if (c == '"') return ParseString(s, ref i);
        if (c == 't') { i += 4; return true; }
        if (c == 'f') { i += 5; return false; }
        if (c == 'n') { i += 4; return null; }
        return ParseNumber(s, ref i);
    }

    private static Dictionary<string, object> ParseObject(string s, ref int i)
    {
        var dict = new Dictionary<string, object>();
        i++; // {
        SkipWhitespace(s, ref i);
        if (s[i] == '}') { i++; return dict; }

        while (true)
        {
            SkipWhitespace(s, ref i);
            string key = ParseString(s, ref i);
            SkipWhitespace(s, ref i);
            i++; // :
            object value = ParseValue(s, ref i);
            dict[key] = value;
            SkipWhitespace(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == '}') { i++; break; }
        }
        return dict;
    }

    private static List<object> ParseArray(string s, ref int i)
    {
        var list = new List<object>();
        i++; // [
        SkipWhitespace(s, ref i);
        if (s[i] == ']') { i++; return list; }

        while (true)
        {
            object value = ParseValue(s, ref i);
            list.Add(value);
            SkipWhitespace(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == ']') { i++; break; }
        }
        return list;
    }

    private static string ParseString(string s, ref int i)
    {
        var sb = new StringBuilder();
        i++; // "
        while (s[i] != '"')
        {
            if (s[i] == '\\')
            {
                i++;
                switch (s[i])
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'u':
                        string hex = s.Substring(i + 1, 4);
                        sb.Append((char)Convert.ToInt32(hex, 16));
                        i += 4;
                        break;
                    default: sb.Append(s[i]); break;
                }
                i++;
            }
            else
            {
                sb.Append(s[i]);
                i++;
            }
        }
        i++; // closing "
        return sb.ToString();
    }

    private static double ParseNumber(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
            i++;
        return double.Parse(s.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }
}