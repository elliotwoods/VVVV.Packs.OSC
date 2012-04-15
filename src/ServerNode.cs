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
	[PluginInfo(Name = "Server", Category = "OSC", Help = "Listen for OSC", Tags = "")]
	#endregion PluginInfo
	public class ServerNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Port", IsSingle = true, DefaultValue=4444)]
		IDiffSpread<int> FPinInPort;

		[Input("Enabled", IsSingle = true)]
		ISpread<bool> FPinInEnabled;

		[Output("Output")]
		ISpread<OSCPacket> FPinOutOutput;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		OSCReceiver FServer;

		Thread FThread;
		bool FRunning = false;
		
		Object FLockList = new Object();
		List<OSCPacket> FPacketList = new List<OSCPacket>();

		#endregion fields & pins

		[ImportingConstructor]
		public ServerNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
			Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInEnabled[0] && !FRunning || FPinInPort.IsChanged && FRunning)
				Open();
			else if (!FPinInEnabled[0] && FRunning)
				Close();

			Output();
		}

		void Open()
		{
			Close();
			try
			{
				FServer = new OSCReceiver(FPinInPort[0]);
				FServer.Connect();

				FRunning = true;
				FThread = new Thread(ThreadedFunction);
				FThread.Start();

				FPinOutStatus[0] = "OK";
			}
			catch (Exception e)
			{
				Close();
				FPinOutStatus[0] = e.Message;
			}
		}

		void Close()
		{
			if (FRunning)
			{
				FRunning = false;
				FThread.Abort();

				FServer.Close();
				FServer = null;
			}
		}

		void Output()
		{
			if (!FRunning)
				return;

			lock (FPacketList)
			{
				int count = FPacketList.Count;
				if (count > 0)
				{
					FPinOutOutput.SliceCount = count;
					for (int i = 0; i < count; i++)
						FPinOutOutput[i] = FPacketList[i];
				}
				else
				{
					FPinOutOutput.SliceCount = 1;
					FPinOutOutput[0] = null;
				}

				FPacketList.Clear();
			}
		}

		void ThreadedFunction()
		{
			OSCPacket p;

			while (FRunning)
			{
				p = FServer.Receive();

				lock (FLockList)
					FPacketList.Add(p);
			}
		}

	}
}
