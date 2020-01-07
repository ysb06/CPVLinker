using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CPVLinker.Tools.Ini
{
    public class IniFile : IEnumerable<KeyValuePair<string, IniSection>>, IDictionary<string, IniSection>
    {
        private Dictionary<string, IniSection> sections;
        public IEqualityComparer<string> StringComparer;

        public bool SaveEmptySections;

        public IniFile()
            : this(DefaultComparer)
        {
        }

        public IniFile(IEqualityComparer<string> stringComparer)
        {
            StringComparer = stringComparer;
            sections = new Dictionary<string, IniSection>(StringComparer);
        }

        public void Save(string path, FileMode mode = FileMode.Create)
        {
            using (var stream = new FileStream(path, mode, FileAccess.Write))
            {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                Save(writer);
            }
        }

        public void Save(StreamWriter writer)
        {
            foreach (var section in sections)
            {
                if (section.Value.Count > 0 || SaveEmptySections)
                {
                    writer.WriteLine(string.Format("[{0}]", section.Key.Trim()));
                    foreach (var kvp in section.Value)
                    {
                        writer.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                    }
                    writer.WriteLine("");
                }
            }
        }

        public void Load(string path, bool ordered = false)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, ordered);
            }
        }

        public void Load(Stream stream, bool ordered = false)
        {
            using (var reader = new StreamReader(stream))
            {
                Load(reader, ordered);
            }
        }

        public void Load(StreamReader reader, bool ordered = false)
        {
            IniSection section = null;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (line != null)
                {
                    var trimStart = line.TrimStart();

                    if (trimStart.Length > 0)
                    {
                        if (trimStart[0] == '[')
                        {
                            var sectionEnd = trimStart.IndexOf(']');
                            if (sectionEnd > 0)
                            {
                                var sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
                                section = new IniSection(StringComparer) { Ordered = ordered };
                                sections[sectionName] = section;
                            }
                        }
                        else if (section != null && trimStart[0] != ';')
                        {
                            string key;
                            IniValue val;

                            if (LoadValue(line, out key, out val))
                            {
                                section[key] = val;
                            }
                        }
                    }
                }
            }
        }

        private bool LoadValue(string line, out string key, out IniValue val)
        {
            var assignIndex = line.IndexOf('=');
            if (assignIndex <= 0)
            {
                key = null;
                val = null;
                return false;
            }

            key = line.Substring(0, assignIndex).Trim();
            var value = line.Substring(assignIndex + 1);

            val = new IniValue(value);
            return true;
        }

        public bool ContainsSection(string section)
        {
            return sections.ContainsKey(section);
        }

        public bool TryGetSection(string section, out IniSection result)
        {
            return sections.TryGetValue(section, out result);
        }

        bool IDictionary<string, IniSection>.TryGetValue(string key, out IniSection value)
        {
            return TryGetSection(key, out value);
        }

        public bool Remove(string section)
        {
            return sections.Remove(section);
        }

        public IniSection Add(string section, Dictionary<string, IniValue> values, bool ordered = false)
        {
            return Add(section, new IniSection(values, StringComparer) { Ordered = ordered });
        }

        public IniSection Add(string section, IniSection value)
        {
            if (value.Comparer != StringComparer)
            {
                value = new IniSection(value, StringComparer);
            }
            sections.Add(section, value);
            return value;
        }

        public IniSection Add(string section, bool ordered = false)
        {
            var value = new IniSection(StringComparer) { Ordered = ordered };
            sections.Add(section, value);
            return value;
        }

        void IDictionary<string, IniSection>.Add(string key, IniSection value)
        {
            Add(key, value);
        }

        bool IDictionary<string, IniSection>.ContainsKey(string key)
        {
            return ContainsSection(key);
        }

        public ICollection<string> Keys
        {
            get { return sections.Keys; }
        }

        public ICollection<IniSection> Values
        {
            get { return sections.Values; }
        }

        void ICollection<KeyValuePair<string, IniSection>>.Add(KeyValuePair<string, IniSection> item)
        {
            ((IDictionary<string, IniSection>)sections).Add(item);
        }

        public void Clear()
        {
            sections.Clear();
        }

        bool ICollection<KeyValuePair<string, IniSection>>.Contains(KeyValuePair<string, IniSection> item)
        {
            return ((IDictionary<string, IniSection>)sections).Contains(item);
        }

        void ICollection<KeyValuePair<string, IniSection>>.CopyTo(KeyValuePair<string, IniSection>[] array, int arrayIndex)
        {
            ((IDictionary<string, IniSection>)sections).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return sections.Count; }
        }

        bool ICollection<KeyValuePair<string, IniSection>>.IsReadOnly
        {
            get { return ((IDictionary<string, IniSection>)sections).IsReadOnly; }
        }

        bool ICollection<KeyValuePair<string, IniSection>>.Remove(KeyValuePair<string, IniSection> item)
        {
            return ((IDictionary<string, IniSection>)sections).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator()
        {
            return sections.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IniSection this[string section]
        {
            get
            {
                IniSection s;
                if (sections.TryGetValue(section, out s))
                {
                    return s;
                }
                s = new IniSection(StringComparer);
                sections[section] = s;
                return s;
            }
            set
            {
                var v = value;
                if (v.Comparer != StringComparer)
                {
                    v = new IniSection(v, StringComparer);
                }
                sections[section] = v;
            }
        }

        public string GetContents()
        {
            using (var stream = new MemoryStream())
            {
                Save(stream);
                stream.Flush();
                var builder = new StringBuilder(Encoding.UTF8.GetString(stream.ToArray()));
                return builder.ToString();
            }
        }

        public static IEqualityComparer<string> DefaultComparer = new CaseInsensitiveStringComparer();

        class CaseInsensitiveStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return String.Compare(x, y, true) == 0;
            }

            public int GetHashCode(string obj)
            {
                return obj.ToLowerInvariant().GetHashCode();
            }

#if JS
        public new bool Equals(object x, object y) {
            var xs = x as string;
            var ys = y as string;
            if (xs == null || ys == null) {
                return xs == null && ys == null;
            }
            return Equals(xs, ys);
        }

        public int GetHashCode(object obj) {
            if (obj is string) {
                return GetHashCode((string)obj);
            }
            return obj.ToStringInvariant().ToLowerInvariant().GetHashCode();
        }
#endif
        }
    }
}