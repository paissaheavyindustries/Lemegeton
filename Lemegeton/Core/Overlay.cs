using Dalamud.Utility;
using ImGuiNET;
using System.Numerics;

namespace Lemegeton.Core
{

    internal class Overlay
    {

        public int X { get; set; } = 100;
        public int Y { get; set; } = 100;

        public int _Width = 300;
        public int Width
        {
            get { return _Width; }
            set
            {
                _Width = value;
                if (_Width < 50)
                {
                    _Width = 50;
                }
            }
        }

        public int _Height = 300;
        public int Height
        {
            get { return _Height; }
            set
            {
                _Height = value;
                if (_Height < 50)
                {
                    _Height = 50;
                }
            }
        }

        public int _Padding = 5;
        public int Padding
        {
            get { return _Padding; }
            set
            {
                _Padding = value;
                if (_Padding < 0)
                {
                    _Padding = 0;
                }
                if (_Padding > 10)
                {
                    _Padding = 10;
                }
            }
        }

        public Vector4 BackgroundColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.5f);

        public delegate void OverlayRenderDelegate(ImDrawListPtr draw, bool configuring);
        public OverlayRenderDelegate Renderer;

        public Overlay()
        {
        }

        public string Serialize()
        {
            return string.Format("{0};{1};{2};{3};{4};{5}", X, Y, _Width, _Height, _Padding, ImGui.GetColorU32(BackgroundColor));
        }

        public void Deserialize(string data)
        {
            string[] boop = data.Split(';');
            for (int i = 0; i < boop.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        {
                            if (int.TryParse(boop[i], out int tenp) == true)
                            {
                                X = tenp;
                            }
                        }
                        break;
                    case 1:
                        {
                            if (int.TryParse(boop[i], out int tenp) == true)
                            {
                                Y = tenp;
                            }
                        }
                        break;
                    case 2:
                        {
                            if (int.TryParse(boop[i], out int tenp) == true)
                            {
                                Width = tenp;
                            }
                        }
                        break;
                    case 3:
                        {
                            if (int.TryParse(boop[i], out int tenp) == true)
                            {
                                Height = tenp;
                            }
                        }
                        break;
                    case 4:
                        {
                            if (int.TryParse(boop[i], out int tenp) == true)
                            {
                                Padding = tenp;
                            }
                        }
                        break;
                    case 5:
                        {
                            if (uint.TryParse(boop[i], out uint tenp) == true)
                            {
                                BackgroundColor = ImGui.ColorConvertU32ToFloat4(tenp);
                            }
                        }
                        break;
                }
            }
        }

    }

}
