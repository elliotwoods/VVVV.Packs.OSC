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
	[PluginInfo(Name = "Client", Category = "OSC", Help = "Send OSC over network", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class ClientNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInput;

		[Input("Remote host", IsSingle=true, StringType = StringType.IP, DefaultString="127.0.0.1")]
		IDiffSpread<string> FPinInRemote;

		[Input("Port", IsSingle = true, DefaultValue=4444)]
		IDiffSpread<int> FPinInPort;

		[Input("Enabled", IsSingle = true)]
		ISpread<bool> FPinInEnabled;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		OSCTransmitter FClient;

		Thread FThread;
		bool FRunning = false;
		
		Object FLockPackets = new Object();
		List<OSCPacket> FPacketList = new List<OSCPacket>();

		Object FLockStatus = new Object();
		String FStatus = "";
		#endregion fields & pins

		[ImportingConstructor]
		public ClientNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
			Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInEnabled[0] && !FRunning || FPinInPort.IsChanged && FRunning || FPinInRemote.IsChanged && FRunning)
				Open();
			else if (!FPinInEnabled[0] && FRunning)
				Close();

			if (FPinInput[0] != null && FRunning)
				lock (FLockPackets)
				{
					for (int i = 0; i < FPinInput.SliceCount; i++)
					{
						FPacketList.Add(FPinInput[i]);
					}
				}

			lock (FLockStatus)
			{
				if (FPinOutStatus[0] != FStatus)
					FPinOutStatus[0] = FStatus;
			}
		}

		void Open()
		{
			Close();
			FThread = new Thread(ThreadedFunction);
			FThread.Start();

		}

		void Close()
		{
			if (FRunning)
			{
				FRunning = false;
				FThread.Abort();

				FClient.Close();
				FClient = null;
			}
		}

		void ThreadedFunction()
		{
			try
			{
				FClient = new OSCTransmitter(FPinInRemote[0], FPinInPort[0]);
				FClient.Connect();

				FRunning = true;
				lock (FLockStatus)
					FStatus = "OK";
			}
			catch (Exception e)
			{
				lock (FLockStatus)
					FStatus = e.Message;

				if (FClient != null)
					FClient.Close();
				FRunning = false;
				return;
			}

			List<OSCPacket> copyList;
			while (FRunning)
			{
				lock (FLockPackets)
				{
					copyList = new List<OSCPacket>(FPacketList);
					FPacketList.Clear();
				}

				foreach (var p in copyList)
					FClient.Send(p);
			}
		}

	}
}
