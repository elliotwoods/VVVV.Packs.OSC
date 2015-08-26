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
	public abstract class ReceiveNode<T> : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInInput;

		[Input("Address")]
		ISpread<string> FPinInAddress;

		[Input("Mode", IsSingle=true)]
		ISpread<Filter.Type> FPinInFilter;

		[Output("Output")]
		ISpread<ISpread<T>> FPinOutOutput;

		[Output("Address")]
		ISpread<string> FPinOutAddress;

		[Output("OnReceive")]
		ISpread<bool> FPinOutOnReceive;

		[Import]
		ILogger FLogger;
		#endregion fields & pins

		[ImportingConstructor]
		public ReceiveNode()
		{
		}

		bool WrongAddress(OSCPacket p)
		{
			bool matches = false;
			for (int j=0; j< FPinInAddress.SliceCount; j++)
			{
				matches |= Filter.Test(p.Address, FPinInAddress[j], FPinInFilter[0]);
			}
			return !matches;
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			var filteredMessages = new List<OSCPacket>();
			foreach (var message in FPinInInput)
			{
				if (message == null) {
					continue;
					}
				if (!WrongAddress(message))
				{
					filteredMessages.Add(message);
					if (filteredMessages.Count > 3)
					{
						break;
					}
				}
			}

			int count = filteredMessages.Count;
			FPinOutOutput.SliceCount = count;
			FPinOutAddress.SliceCount = count;

			if (count > 0)
			{
				int i = 0;
				foreach (var p in filteredMessages)
				{
					FPinOutOutput[i].SliceCount = p.Values.Count;
					FPinOutAddress[i] = p.Address;

					for (int j = 0; j < p.Values.Count; j++)
					{
						FPinOutOutput[i][j] = p.Values[j].GetType() == typeof(T) ? (T)p.Values[j] : GetDefault();
					}
					i++;
				}
			}
			else
			{
				FPinOutOutput.SliceCount = 0;
				FPinOutAddress.SliceCount = 0;
			}
			FPinOutOnReceive[0] = count > 0;
		}

		protected abstract T GetDefault();
	}


	#region PluginInfo
	[PluginInfo(Name = "Receive", Category = "OSC", Version = "value", Help = "Receive OSC packets from across the graph as floats", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveValueNode : ReceiveNode<float>
	{
		protected override float GetDefault()
		{
			return 0.0f;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Receive", Category = "OSC", Version = "value, int", Help = "Receive OSC packets from across the graph as ints", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveValueIntNode : ReceiveNode<int>
	{
		protected override int GetDefault()
		{
			return 0;
		}
	}


	#region PluginInfo
	[PluginInfo(Name = "Receive", Category = "OSC", Version = "string", Help = "Receive OSC packets from across the graph as floats", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceiveStringNode : ReceiveNode<string>
	{
		protected override string GetDefault()
		{
			return "";
		}
	}
}
