#region usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.OSC;

using VVVV.Core.Logging;
using Newtonsoft.Json;
#endregion usings

namespace VVVV.Nodes.OSC.Splines
{
	#region PluginInfo
	[PluginInfo(Name = "Reader", Category = "OSC", Version = "LightBarrier", Help = "Read spline json", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class LightBarrierOSCReaderNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", IsSingle = true)]
		public IDiffSpread<string> FInFilename;

		[Input("Relaod", IsSingle = true)]
		public ISpread<bool> FInReload;

		[Input("Frame", IsSingle = true)]
		public ISpread<int> FInFrame;

		[Output("Output")]
		public ISpread<OSCMessage> FOutput;

		[Output("Frame Count", IsSingle = true)]
		public ISpread<int> FOutFrame;

		[Output("Status", IsSingle = true)]
		public ISpread<string> FOutStatus;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInFilename.IsChanged || FInReload[0])
			{
				try
				{
					var fileText = System.IO.File.ReadAllText(FInFilename[0]);
					var json = JsonConvert.DeserializeObject <Dictionary<string, List<string>>> (fileText);
					FOutStatus[0] = "OK";
				}
				catch (Exception e)
				{
					FOutStatus[0] = e.Message;
				}
			}
		}
	}
}

