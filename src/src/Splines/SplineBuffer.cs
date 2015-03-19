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
#endregion usings

namespace VVVV.Packs.OSC.Splines
{
	public class UserDataEntry
	{
		public string Name;
		public List<double> Values = new List<double>();
	}

    public class Spline
    {
        public class Property
        {
            public string Title;
            public List<double> Values = new List<double>();
        }
        public string Address;
		public Vector3D Position = new Vector3D();
		public List<Vector3D> Vertices = new List<Vector3D>();
		public RGBAColor Color = new RGBAColor(1.0, 1.0, 1.0, 1.0);
		public Dictionary<string, Spline> Children = new Dictionary<string, Spline>();
		public List<UserDataEntry> UserData = new List<UserDataEntry>();

        Spline inputBuffer;

		public Spline(bool hasInputBuffer)
		{
			if (hasInputBuffer)
			{
				inputBuffer = new Spline(false);
			}
		}
        public bool Process(List<string> addressList, ArrayList values, Matrix4x4 transform)
        {
            this.Address = addressList[0];

            var localAddress = new List<string>(addressList);
            localAddress.RemoveAt(0);

            if (localAddress.Count > 0)
            {
                switch (localAddress[0])
                {
                    case "begin":
                        this.inputBuffer = new Spline(false);
						this.inputBuffer.Address = this.Address;
                        break;
                    case "end":
                        this.Position = new Vector3D(this.inputBuffer.Position);
                        this.Vertices = new List<Vector3D>(this.inputBuffer.Vertices);
                        this.Color = new RGBAColor();
						this.Color.R = this.inputBuffer.Color.R;
						this.Color.G = this.inputBuffer.Color.G;
						this.Color.B = this.inputBuffer.Color.B;
						this.Color.A = this.inputBuffer.Color.A;
						this.UserData = new List<UserDataEntry>(inputBuffer.UserData);
                        this.Children = new Dictionary<string,Spline>(this.inputBuffer.Children);
						return true;
					case "position":
						if (values.Count >= 3)
						{
							for(int i=0; i<3; i++) {
								this.inputBuffer.Position[i] = (double) (float) values[i];
							}
							if (transform != null)
							{
								this.inputBuffer.Position = transform * this.inputBuffer.Position;
							}
						}
						break;
					case "displayColor":
						if (values.Count >= 3)
						{
							this.inputBuffer.Color.R = (double) (float) values[0];
							this.inputBuffer.Color.G = (double) (float) values[1];
							this.inputBuffer.Color.B = (double) (float) values[2];
						}
						break;
					case "childCount":
						break;
					case "userData":
						localAddress.RemoveAt(0);
						string propertyName = "";
						foreach (var level in localAddress)
						{
							if (propertyName != "")
							{
								propertyName += "/";
							}
							propertyName += level;
						}
						var userDataEntry = new UserDataEntry();
						userDataEntry.Name = propertyName;

						foreach (var value in values)
						{
							if (value is int)
							{
								userDataEntry.Values.Add((double)(int)value);
							}
							else if (value is float)
							{
								userDataEntry.Values.Add((double)(float)value);
							}
						}

						this.inputBuffer.UserData.Add(userDataEntry);
						break;
					case "spline":
						this.inputBuffer.Vertices.Clear();
						for (int i = 0;  i < values.Count / 3; i++)
						{
							var vertex = new Vector3D();
							for(int j=0; j<3; j++)
							{
								vertex[j] = (double)(float)values[i * 3 + j];
							}

							if (transform != null)
							{
								this.inputBuffer.Vertices.Add(transform * vertex);
							}
							else
							{
								this.inputBuffer.Vertices.Add(vertex);
							}
						}

						break;
					default:
						var childName = localAddress[0];
						if (!inputBuffer.Children.ContainsKey(childName)) {
							inputBuffer.Children.Add(childName, new Spline(true));
						}
						inputBuffer.Children[childName].Process(localAddress, values, transform);
						break;
                }
            }

			return false;
        }

		public void FlattenHeirarchy()
		{
			Dictionary<string, Spline> grandchildren = new Dictionary<string,Spline>();
			foreach(var child in this.Children) {
				child.Value.FlattenHeirarchy();
				foreach(var grandchild in child.Value.Children) {
					grandchildren.Add(child.Key + "/" + grandchild.Key, grandchild.Value);
				}
				child.Value.Children.Clear();
			}
			foreach(var grandchild in grandchildren)
			{
				this.Children.Add(grandchild.Key, grandchild.Value);
			}
		}
    }

    #region PluginInfo
    [PluginInfo(Name = "SplineBuffer", Category = "OSC", Version = "LightBarrier", Help = "Listen for splines", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo
    public class LightBarrierOSCBufferSplinesNode : IPluginEvaluate
    {
        class Buffer
        {
            public int Frame;
			public Spline Root = new Spline(true);

            /// Returns true if we got a full frame
            public bool Process(OSCPacket packet, Matrix4x4 transform)
            {
                switch (packet.Address)
                {
                    case "/frame":
                        if (packet.Values.Count >= 1)
                        {
                            this.Frame = (int)packet.Values[0];
                        }
                        break;
                    default:
                        //we're in it for the money!
                        var addressList = new List<string>(packet.Address.Split('/'));
                        var values = packet.Values;
						if (this.Root.Process(addressList, values, transform))
						{
							return true;
						}
                        break;
                }
                return false;
            }

			public void Clear()
			{
				this.Root = new Spline(true);
			}
        }

        #region fields & pins
        [Input("Input")]
        public ISpread<OSCPacket> FInput;

        [Input("Clear", IsBang = true, IsSingle = true)]
        public ISpread<bool> FInClear;

		[Input("Transform", IsSingle = true)]
		public ISpread<Matrix4x4> FInTransform;

        [Output("Output", IsSingle = true)]
        public ISpread<Spline> FOutput;

		[Output("Frame", IsSingle = true)]
		public ISpread<int> FOutFrame;

        [Import()]
        public ILogger FLogger;
        Buffer FBuffer = new Buffer();
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
			if (FInClear[0])
			{
				this.FBuffer.Clear();
			}

            FOutput.SliceCount = SpreadMax;
            foreach (var packet in FInput)
            {
                if (packet != null)
                {
                    if (FBuffer.Process(packet, FInTransform[0]))
                    {
						FOutput[0] = FBuffer.Root;
						FOutFrame[0] = FBuffer.Frame;
                    }
                }
            }
        }
    }
}
   
    