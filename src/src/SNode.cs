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
	class SRComms
	{
		public class Queue : HashSet<OSCPacket> 
		{
			private string FChannel;

			public Queue(string channel)
			{
				FChannel = channel;
			}

			public string Channel
			{
				get
				{
					return FChannel;
				}
			}
		}

		public static event EventHandler MessageSent;
		public static void OnMessageSent(Queue message)
		{
			if (MessageSent == null)
				return;
			MessageSent(message, EventArgs.Empty);
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "S", Category = "OSC", Help = "Send OSC packets across the graph", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class SNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInInput;

		[Input("Channel", IsSingle=true, DefaultString="Rx")]
		ISpread<string> FPinInChannel;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public SNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInInput.SliceCount > 0 && FPinInInput[0] != null)
			{
				Send(SpreadMax);
			}
		}

		void Send(int SliceCount)
		{
			SRComms.Queue q = new SRComms.Queue(FPinInChannel[0]);
			for (int i = 0; i < SliceCount; i++)
				q.Add(FPinInInput[0]);

			SRComms.OnMessageSent(q);
		}

	}
}
