﻿#region usings
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

	enum Filter {Matches, Contains, Starts, Ends};

	#region PluginInfo
	[PluginInfo(Name = "Sift", Category = "OSC", Help = "Sift packets for an address", Tags = "")]
	#endregion PluginInfo
	public class SiftNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<OSCPacket> FPinInput;

		[Input("Filter address")]
		ISpread<string> FPinInAddress;

		[Input("Mode", IsSingle = true)]
		ISpread<Filter> FPinInFilter;

		[Output("Output")]
		ISpread<OSCPacket> FPinOutput;
		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public SiftNode(IPluginHost host)
		{
		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{

			FPinOutput.SliceCount = 0;
			if (FPinInput.SliceCount > 0 && FPinInput[0] != null)
				for (int i = 0; i < FPinInput.SliceCount; i++)
				{
					bool matches = true;
					for (int j=0; j< FPinInAddress.SliceCount; j++)
					{
						switch(FPinInFilter[0])
						{
							case Filter.Matches:
								if (FPinInput[i].Address == FPinInAddress[j])
									matches = true;
								break;

							case Filter.Contains:
								if (FPinInput[i].Address.Contains(FPinInAddress[j]))
									matches = true;
								break;

							case Filter.Starts:
								if (FPinInput[i].Address.StartsWith(FPinInAddress[j]))
									matches = true;
								break;

							case Filter.Ends:
								if (FPinInput[i].Address.EndsWith(FPinInAddress[j]))
									matches = true;
								break;
						}
					}
					if (matches)
						FPinOutput.Add<OSCPacket>(FPinInput[i]);
				}

			if (FPinOutput.SliceCount == 0)
			{
				FPinOutput.SliceCount = 1;
				FPinOutput[0] = null;
			}
		}

	}
}
