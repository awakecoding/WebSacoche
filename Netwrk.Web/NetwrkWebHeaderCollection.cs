using System.Collections.Generic;
using System.Linq;

namespace Netwrk.Web
{
    public class NetwrkWebHeaderCollection
    {
        private Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();

		public string this[string key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}

		public bool HasValue(string key) => headers.ContainsKey(key);

		public string[] GetKeys() => headers.Keys.Cast<string>().ToArray();

        public int GetValueCount(string key) => headers.TryGetValue(key, out var values) ? values.Count : 0;

        public string GetValue(string key) => headers.TryGetValue(key, out var values) ? string.Join(",", values.Select(s => s.Contains(',') ? $@"""{s}""" : s)) : null;

        public string[] GetValues(string key) => headers.TryGetValue(key, out var values) ? values.ToArray() : new string[0];

        public void SetValue(string key, string value)
        {
            if (!headers.ContainsKey(key))
            {
                headers.Add(key, new List<string>());
            }
            else
            {
                headers[key].Clear();
            }

            headers[key].Add(value);
        }

        public void SetValues(string key, params string[] values)
        {
            if (values == null)
            {
                headers.Remove(key);
                return;
            }

            if (!headers.ContainsKey(key))
            {
                headers.Add(key, new List<string>());
            }
            else
            {
                headers[key].Clear();
            }

            headers[key].AddRange(values);
        }

        public void AddValue(string key, string value)
        {
            if (!headers.ContainsKey(key))
            {
                headers.Add(key, new List<string>());
            }

            headers[key].Add(value);
        }

        public void AddValues(string key, params string[] values)
        {
            if (values == null)
            {
                return;
            }

            if (!headers.ContainsKey(key))
            {
                headers.Add(key, new List<string>());
            }

            headers[key].AddRange(values);
        }
    }
}