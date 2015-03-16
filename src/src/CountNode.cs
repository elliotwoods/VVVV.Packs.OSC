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

namespace VVVV.Nodes.OSC
{

	#region PluginInfo
	[PluginInfo(Name = "Count", Category = "OSC", Help = "Count incoming packets", Tags = "")]
	#endregion PluginInfo
	public class CountNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInput;

		[Output("Count")]
		ISpread<int> FPinOutput;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public CountNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInput.SliceCount > 0 && FPinInput[0] != null)
				FPinOutput[0] = FPinInput.SliceCount;
			else
				FPinOutput[0] = 0;
		}

	}
}
