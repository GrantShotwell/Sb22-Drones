using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Sb22.ScriptHelpers {

	public class ProgramSettings {

		private static Regex JsonRegex { get; } = new Regex(@"(\w+)\s*=\s*{\s*(?:((\w+):\s*(.*),)\s*)*\s*}", RegexOptions.Compiled);

		private IReadOnlyDictionary<string, SettingsValue<object>> Values { get; }

		public SettingsValue<object> this[string name] => Values[name];


		private ProgramSettings(IReadOnlyDictionary<string, SettingsValue<object>> values) {
			Values = values;
		}


		public struct SettingsValue<T> {
			public T Value { get; }
			public SettingsValue(T value) {
				Value = value;
			}
		}

	}

}
