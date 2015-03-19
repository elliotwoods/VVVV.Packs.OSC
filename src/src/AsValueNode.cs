#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using VVVV.Utils.OSC;

#endregion usings

namespace VVVV.Packs.OSC
{

	#region PluginInfo
	[PluginInfo(Name = "AsValue", Category = "OSC", Help = "Convert OSC packets into values", Tags = "")]
	#endregion PluginInfo
	public class AsValueNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInput;

		[Output("Output")]
		ISpread<ISpread<double>> FPinOutput;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public AsValueNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInput.SliceCount > 0 && FPinInput[0] != null)
			{
				int count = FPinInput.SliceCount;
				FPinOutput.SliceCount = count;

				int innerCount;
				for (int i=0; i<count; i++)
				{
					innerCount = FPinInput[i].Values.Count;
					FPinOutput[i].SliceCount = innerCount;

					for (int j = 0; j < innerCount; j++)
					{
						if (FPinInput[i].Values[j].GetType() == typeof(float))
							FPinOutput[i][j] = (float)FPinInput[i].Values[j];
						else
							FPinOutput[i][j] = 0;
					}
				}
			}
			else
				FPinOutput.SliceCount = 0;
		}

	}
}
