using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using LMeter.Helpers;

namespace LMeter.Act
{
    public partial class TextTagFormatter
    {
        [GeneratedRegex(@"\[(\w*)(:k)?\.?(\d+)?\]", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();
        public static Regex TextTagRegex { get; } = GeneratedRegex();

        private readonly string _format;
        private readonly Dictionary<string, MemberInfo> _members;
        private readonly object _source;

        public TextTagFormatter(
            object source,
            string format,
            Dictionary<string, MemberInfo> members)
        {
            _source = source;
            _format = format;
            _members = members;
        }

        public string Evaluate(Match m)
        {
            if (m.Groups.Count != 4)
            {
                return m.Value;
            }

            string format = string.IsNullOrEmpty(m.Groups[3].Value)
                ? $"{_format}0"
                : $"{_format}{m.Groups[3].Value}";

            string? value = null;
            string key = m.Groups[1].Value;

            if (!_members.TryGetValue(key, out MemberInfo? memberInfo))
            {
                return value ?? m.Value;
            }

            object? memberValue = memberInfo?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).GetValue(_source),
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetValue(_source),
                // Default should null because we don't want people accidentally trying to access a method and then throw an exception
                _ => null
            };

            if (memberValue is LazyFloat lazyFloat)
            {
                //if (!Utils.IsHealingStat(key))
                //{
                    bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                    return lazyFloat.ToString(format, kilo) ?? m.Value;
                /*}
                else
                {
                    try
                    {
                        float amount = lazyFloat.Value;
                        object? overHealPct = _fields["overhealpct"].GetValue(_source);
                            
                        if (overHealPct is not null)
                        {
                            amount *= (100 - float.Parse((overHealPct.ToString() ?? "0%")[0..^1])) / 100;
                        }
                        LazyFloat newValue = new(amount);

                        bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                        return newValue.ToString(format, kilo) ?? m.Value;
                    }
                    catch
                    {
                        bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                        return lazyFloat.ToString(format, kilo) ?? m.Value;
                    }
                }*/
            }
            else
            {
                value = memberValue?.ToString();
                if (!string.IsNullOrEmpty(value) &&
                    int.TryParse(m.Groups[3].Value, out int trim) &&
                    trim < value.Length)
                {
                    value = memberValue?.ToString().AsSpan(0, trim).ToString();
                }
            }
            return value ?? m.Value;
        }
    }
 }