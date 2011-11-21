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
	[PluginInfo(Name = "Send", Category = "OSC", Version = "Value", Help = "Send values as OSC packets across the graph ", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class SendValueNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<ISpread<double>> FInput;

		[Input("Address")]
		IDiffSpread<string> FPinInAddress;

		[Input("Channel", IsSingle = true, DefaultString = "Tx")]
		IDiffSpread<string> FPinInChannel;

		[Import]
		ILogger FLogger;
		SRComms.Queue FPackets = new SRComms.Queue("Tx");
		#endregion fields & pins

		[ImportingConstructor]
		public SendValueNode(IPluginHost host)
		{
		}


		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPackets.Channel != FPinInChannel[0])
				FPackets = new SRComms.Queue(FPinInChannel[0]);

			if (FPinInAddress.IsChanged || FInput.IsChanged)
				for (int i = 0; i < SpreadMax; i++)
				{
					if (FPinInAddress[i] == "")
						continue;

					OSCMessage p = new OSCMessage(FPinInAddress[i]);
					for (int j = 0; j < FInput[i].SliceCount; j++)
						p.Append((float)FInput[i][j]);
					FPackets.Add(p);
				}
			if (FPackets.Count > 0)
				SRComms.OnMessageSent(FPackets);
			FPackets.Clear();
		}
	}
}
