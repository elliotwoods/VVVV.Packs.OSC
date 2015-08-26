using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace VVVV.Packs.OSC.Splines
{
	#region PluginInfo
	[PluginInfo(Name = "Spline", Category = "OSC", Version = "Split", Help = "Split a spline", Tags = "")]
	#endregion PluginInfo
	public class SplineSplitNode : IPluginEvaluate
	{
		[Input("Input")]
		public ISpread<Spline> FInput;

		[Input("Filter")]
		ISpread<string> FPinInFilter;

		[Input("Mode", IsSingle = true)]
		ISpread<Filter.Type> FPinInFilterMode;

		[Output("Address")]
		public ISpread<String> FOutAddress;

		[Output("Position")]
		public ISpread<Vector3D> FOutPosition;

		[Output("Vertices")]
		public ISpread<ISpread<Vector3D>> FOutVertices;

		[Output("Color")]
		public ISpread<RGBAColor> FOutColor;

		[Output("User Data")]
		public ISpread<ISpread<UserDataEntry>> FOutUserData;

		[Output("Children")]
		public ISpread<ISpread<Spline>> FOutChildren;

		[Output("Child Address")]
		public ISpread<ISpread<string>> FOutChildAddress;

		public void Evaluate(int SpreadMax)
		{
			if (FInput.SliceCount == 1 && FInput[0] == null)
			{
				SpreadMax = 0;
			}

			FOutAddress.SliceCount = SpreadMax;
			FOutPosition.SliceCount = SpreadMax;
			FOutVertices.SliceCount = SpreadMax;
			FOutColor.SliceCount = SpreadMax;
			FOutUserData.SliceCount = SpreadMax;
			FOutChildren.SliceCount = SpreadMax;
			FOutChildAddress.SliceCount = SpreadMax;

			int outputIndex = 0;

			for (int i = 0; i < SpreadMax; i++)
			{
				var spline = FInput[i];
				if (spline != null)
				{
					bool matches = false;
					foreach(string filter in FPinInFilter) {
						matches |= Filter.Test(spline.Address, filter, FPinInFilterMode[0]);
					}
					if(matches) {
						FOutAddress[outputIndex] = spline.Address;
						FOutPosition[outputIndex] = spline.Position;
						FOutVertices[outputIndex].AssignFrom(spline.Vertices);
						FOutColor[outputIndex] = spline.Color;
						FOutUserData[outputIndex].AssignFrom(spline.UserData);
						FOutChildren[outputIndex].AssignFrom(spline.Children.Values);
						FOutChildAddress[outputIndex].AssignFrom(spline.Children.Keys);

						outputIndex++;
					}
				}
			}

			//now crop the list down to the ones that passed
			SpreadMax = outputIndex;
			FOutAddress.SliceCount = SpreadMax;
			FOutPosition.SliceCount = SpreadMax;
			FOutVertices.SliceCount = SpreadMax;
			FOutColor.SliceCount = SpreadMax;
			FOutUserData.SliceCount = SpreadMax;
			FOutChildren.SliceCount = SpreadMax;
			FOutChildAddress.SliceCount = SpreadMax;
		}
	}


	#region PluginInfo
	[PluginInfo(Name = "UserData", Category = "OSC", Version = "Split", Help = "Split a UserDataEntry", Tags = "")]
	#endregion PluginInfo
	public class UserDataSplitNode : IPluginEvaluate
	{
		[Input("Input")]
		public ISpread<UserDataEntry> FInput;

		[Input("Filter")]
		ISpread<string> FPinInFilter;

		[Input("Mode", IsSingle = true)]
		ISpread<Filter.Type> FPinInFilterMode;

		[Output("Name")]
		public ISpread<String> FOutName;

		[Output("Value")]
		public ISpread<ISpread<double>> FOutValue;

		public void Evaluate(int SpreadMax)
		{
			if (FInput.SliceCount == 1 && FInput[0] == null)
			{
				SpreadMax = 0;
			}

			FOutName.SliceCount = SpreadMax;
			FOutValue.SliceCount = SpreadMax;

			int outputIndex = 0;

			for (int i = 0; i < SpreadMax; i++)
			{
				var userDataEntry = FInput[i];
				if (userDataEntry != null)
				{
					bool matches = false;
					foreach (string filter in FPinInFilter)
					{
						matches |= Filter.Test(userDataEntry.Name, filter, FPinInFilterMode[0]);
					}
					if (matches)
					{
						FOutName[outputIndex] = userDataEntry.Name;
						FOutValue[outputIndex].AssignFrom(userDataEntry.Values);
						outputIndex++;
					}
				}
			}

			//crop to selected
			SpreadMax = outputIndex;
			FOutName.SliceCount = SpreadMax;
			FOutValue.SliceCount = SpreadMax;
		}
	}
}
