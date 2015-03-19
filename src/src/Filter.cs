using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Packs.OSC
{
	public class Filter {
		public enum Type { All, Matches, Contains, Starts, Ends };

		static public bool Test(string Haystack, string Needle, Type Condition)
		{
			switch (Condition)
			{
				case Type.Matches:
					return Haystack == Needle;

				case Type.Contains:
					return Haystack.Contains(Needle);

				case Type.Starts:
					return Haystack.StartsWith(Needle);

				case Type.Ends:
					return Haystack.EndsWith(Needle);

				case Type.All:
					return true;

				default:
					return true;
			}
		}
	}
}
