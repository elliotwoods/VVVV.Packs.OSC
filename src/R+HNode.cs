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
	[PluginInfo(Name = "R+H", Category = "OSC", Version = "Value", Help = "Receive OSC packets from across the graph as floats and hold them", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveHoldValueNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Channel", IsSingle = true, DefaultString = "Rx")]
		ISpread<string> FPinInChannel;

		[Input("Address")]
		IDiffSpread<string> FPinInAddress;

		[Output("Output")]
		ISpread<ISpread<float>> FPinOutOutput;

		[Import]
		ILogger FLogger;

		object FLockPackets = new object();
		SRComms.Queue FPackets = new SRComms.Queue("Rx");
		Dictionary<string, ISpread<float>> FRegister = new Dictionary<string, ISpread<float>>();
		#endregion fields & pins

		[ImportingConstructor]
		public ReceiveHoldValueNode(IPluginHost host)
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

			SelectAddress();
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPackets.Channel != FPinInChannel[0])
				FPackets = new SRComms.Queue(FPinInChannel[0]);

			if (FPinInAddress.IsChanged)
			{
				ReassignRegister();
			}

			FPinOutOutput.SliceCount = FPinInAddress.SliceCount;
			for (int i = 0; i < FPinInAddress.SliceCount; i++)
				if (FPinInAddress[i] == "")
					FPinOutOutput[i].SliceCount = 0;
				else
					FPinOutOutput[i] = FRegister[FPinInAddress[i]];
		}

		void ReassignRegister()
		{
			for (int i=0; i<FPinInAddress.SliceCount; i++)
			{
				if (!FRegister.ContainsKey(FPinInAddress[i]))
					FRegister.Add(FPinInAddress[i], new Spread<float>(1));
			}

			bool matches;
			List<string> removes = new List<string>();
			foreach (var p in FRegister)
			{
				matches = false;
				for (int i=0; i<FPinInAddress.SliceCount; i++)
				{
					matches |= FPinInAddress[i] == p.Key;
				}
				if (!matches)
					removes.Add(p.Key);
			}

			foreach (var s in removes)
				FRegister.Remove(s);
		}

		void SelectAddress()
		{
			foreach (var p in FPackets)
			{
				if (FRegister.ContainsKey(p.Address))
				{
					int count = p.Values.Count;
					FRegister[p.Address].SliceCount = count;
					for (int i = 0; i < count; i++)
					{
						FRegister[p.Address][i] = p.Values[i].GetType() == typeof(float) ? (float)p.Values[i] : 0;
					}
				}
			}
		}
	}
}
