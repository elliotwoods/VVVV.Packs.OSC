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
	[PluginInfo(Name = "R", Category = "OSC", Help = "Receive OSC packets from across the graph", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class RNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Channel", IsSingle = true, DefaultString = "Rx")]
		ISpread<string> FPinInChannel;

		[Output("Output")]
		ISpread<OSCPacket> FPinOutOutput;

		[Import]
		ILogger FLogger;

		object FLockPackets = new object();
		SRComms.Queue FPackets = new SRComms.Queue("Rx");
		#endregion fields & pins

		[ImportingConstructor]
		public RNode(IPluginHost host)
		{
			SRComms.MessageSent+=new EventHandler(SRComms_MessageSent);
		}

		void  SRComms_MessageSent(object sender, EventArgs e)
		{
			var q = sender as SRComms.Queue;
			if (q == null)
				return;

			if (q.Channel == FPackets.Channel)
				lock (FLockPackets)
					foreach (var p in q)
						FPackets.Add(p);
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPackets.Channel != FPinInChannel[0])
				FPackets = new SRComms.Queue(FPinInChannel[0]);

			lock (FLockPackets)
			{
				int count = FPackets.Count;
				FPinOutOutput.SliceCount = count;
				if (count > 0)
				{
					int i = 0;
					foreach (var p in FPackets)
					{
						FPinOutOutput[i++] = p;
					}
					FPackets.Clear();
				}
				else
				{
					FPinOutOutput.SliceCount = 1;
					FPinOutOutput[0] = null;
				}
			}
		}
	}
}
