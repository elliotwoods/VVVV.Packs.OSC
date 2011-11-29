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
	public abstract class ReceiveNode<T> : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Channel", IsSingle = true, DefaultString = "Rx")]
		ISpread<string> FPinInChannel;

		[Input("Address")]
		ISpread<string> FPinInAddress;

		[Input("Mode", IsSingle=true)]
		ISpread<Filter> FPinInFilter;

		[Output("Output")]
		ISpread<ISpread<T>> FPinOutOutput;

		[Output("Address")]
		ISpread<string> FPinOutAddress;

		[Output("OnReceive")]
		ISpread<bool> FPinOutOnReceive;

		[Import]
		ILogger FLogger;

		object FLockPackets = new object();
		SRComms.Queue FPackets = new SRComms.Queue("Rx");
		#endregion fields & pins

		[ImportingConstructor]
		public ReceiveNode()
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

			FPackets.RemoveWhere(WrongAddress);
		}

		bool WrongAddress(OSCPacket p)
		{
			bool matches = false;
			for (int j=0; j< FPinInAddress.SliceCount; j++)
			{
				switch(FPinInFilter[0])
				{
					case Filter.Matches:
						matches |= p.Address == FPinInAddress[j];
						break;

					case Filter.Contains:
						matches |= p.Address.Contains(FPinInAddress[j]);
						break;

					case Filter.Starts:
						matches |= p.Address.StartsWith(FPinInAddress[j]);
						break;

					case Filter.Ends:
						matches |= p.Address.EndsWith(FPinInAddress[j]);
						break;

					case Filter.All:
						matches = true;
						break;
				}
			}
			return !matches;
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
				FPinOutAddress.SliceCount = count;

				if (count > 0)
				{
					int i = 0;
					foreach (var p in FPackets)
					{
						FPinOutOutput[i].SliceCount = p.Values.Count;
						FPinOutAddress[i] = p.Address;

						for (int j = 0; j < p.Values.Count; j++)
						{
							FPinOutOutput[i][j] = p.Values[j].GetType() == typeof(T) ? (T)p.Values[j] : GetDefault();
						}
						i++;
					}
					FPackets.Clear();
				}
				else
				{
					FPinOutOutput.SliceCount = 0;
					FPinOutAddress.SliceCount = 0;
				}
				FPinOutOnReceive[0] = count > 0;
			}
		}

		protected abstract T GetDefault();
	}


	#region PluginInfo
	[PluginInfo(Name = "Receive", Category = "OSC", Version = "Value", Help = "Receive OSC packets from across the graph as floats", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveValueNode : ReceiveNode<float>
	{
		protected override float GetDefault()
		{
			return 0.0f;
		}
	}


	#region PluginInfo
	[PluginInfo(Name = "Receive", Category = "OSC", Version = "String", Help = "Receive OSC packets from across the graph as floats", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveStringNode : ReceiveNode<string>
	{
		protected override string GetDefault()
		{
			return "";
		}
	}
}
