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

namespace VVVV.Packs.OSC.Splines
{
	#region PluginInfo
	[PluginInfo(Name = "Reader", Category = "OSC", Version = "LightBarrier", Help = "Read spline json", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class LightBarrierOSCReaderNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Filename", IsSingle = true, StringType = StringType.Filename)]
		public IDiffSpread<string> FInFilename;

		[Input("Relaod", IsSingle = true, IsBang = true)]
		public ISpread<bool> FInReload;

		[Input("Frame", IsSingle = true)]
		public IDiffSpread<int> FInFrame;

		[Output("Output")]
		public ISpread<OSCPacket> FOutput;

		[Output("Frame Count", IsSingle = true)]
		public ISpread<int> FOutFrameCount;

		[Output("Status", IsSingle = true)]
		public ISpread<string> FOutStatus;

		[Import()]
		public ILogger FLogger;

		SortedDictionary<int, List<OSCPacket>> FFrames = new SortedDictionary<int, List<OSCPacket>>();
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			bool needsSend = FInFrame.IsChanged;

			if (FInFilename.IsChanged || FInReload[0])
			{
				try
				{
					var fileText = System.IO.File.ReadAllText(FInFilename[0]);
					var json = JsonConvert.DeserializeObject <Dictionary<string, List<string>>> (fileText);

					this.FFrames.Clear();

					foreach (var frame in json)
					{
						int frameIndex = System.Convert.ToInt32(frame.Key);
						var packetList= new List<OSCPacket>();
						foreach(var encodedMessage in frame.Value)
						{
							var data = Convert.FromBase64String(encodedMessage);
							var packet = OSCPacket.Unpack(data);
							packetList.Add(packet);
						}
						this.FFrames.Add(frameIndex, packetList);
					}
					int firstIndex = this.FFrames.Keys.GetEnumerator().Current;
					int checkIndex = firstIndex; // get first
					bool trim = false;
					foreach(var frameIndex in this.FFrames.Keys)
					{
						if (frameIndex != checkIndex)
						{
							trim = true;
						}
						checkIndex++;
					}
					if (trim)
					{
						SortedDictionary<int, List<OSCPacket>> trimmedFrames = new SortedDictionary<int, List<OSCPacket>>();
						int i = 0;
						foreach(var frame in this.FFrames)
						{
							trimmedFrames.Add(i++, frame.Value);
						}
						this.FFrames = trimmedFrames;

						string exceptionMessage = "Encoded frames are not in order. Please resave. Missing " + checkIndex + ". Found :";
						foreach (var outFrameIndex in this.FFrames.Keys)
						{
							exceptionMessage += outFrameIndex.ToString() + ", ";
						}
						FOutStatus[0] = exceptionMessage;
					}
					else
					{
						FOutStatus[0] = "OK";
					}
					needsSend = true;
					FOutFrameCount[0] = this.FFrames.Count;
					FOutStatus[0] = "OK";
				}
				catch (Exception e)
				{
					FFrames.Clear();
					FOutStatus[0] = e.Message;
				}
			}

			int currentFrameIndex = FInFrame[0];
			if (FFrames.Count > 0)
			{
				currentFrameIndex = VMath.Zmod(currentFrameIndex, FFrames.Count);
			}
			needsSend &= FFrames.ContainsKey(currentFrameIndex);
			if (needsSend)
			{
				var currentFrame = this.FFrames[currentFrameIndex];
				FOutput.AssignFrom(currentFrame);
			} else {
				FOutput.SliceCount = 0;
			}
		}
	}
}

