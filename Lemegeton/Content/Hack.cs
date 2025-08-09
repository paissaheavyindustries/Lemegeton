using Dalamud.Bindings.ImGui;
using Lemegeton.Core;
using System;
using System.Numerics;
using GameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Vector3 = System.Numerics.Vector3;

namespace Lemegeton.Content
{

    #if !SANS_GOETIA

    public class Hack : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class Teleporter : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Hack;

            [AttributeOrderNumber(1000)]
            public System.Action GetCurrent { get; set; }
            [AttributeOrderNumber(1001)]
            public float X { get; set; }
            [AttributeOrderNumber(1002)]
            public float Y { get; set; }
            [AttributeOrderNumber(1003)]
            public float Z { get; set; }

            [AttributeOrderNumber(2000)]
            public int PlayersNearby { get; private set; } = 0;
            [AttributeOrderNumber(2001)]
            public bool AllowRiskyTeleport { get; set; } = false;
            [AttributeOrderNumber(2002)]
            public System.Action Teleport
            {
                get
                {
                    return _telepoAction != null && Enabled == true && (PlayersNearby == 0 || AllowRiskyTeleport == true) ? _telepoAction : null;
                }
                set
                {
                    _telepoAction = value;
                }
            }

            private System.Action _telepoAction = null;
            private DateTime _loaded;

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                ulong myid = _state.cs.LocalPlayer.GameObjectId;
                int plys = 0;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && go.GameObjectId != myid)
                    {
                        plys++;
                    }
                }
                PlayersNearby = plys;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Vector3 v1, v2, temp = new Vector3(X, Y, Z);
                float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 200.0);
                float bla = (float)Math.Abs(Math.Cos(time));
                float dist = 0.5f + bla;
                Vector4 col = new Vector4(1.0f - bla, 1.0f, 0.0f, 1.0f - (bla * 0.5f));
                Vector4 tcol = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Vector4 scol = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                for (int i = 0; i <= 24; i++)
                {
                    Vector3 mauw = _state.plug._ui.TranslateToScreen(
                        temp.X + (dist * Math.Sin((Math.PI / 12.0) * i)),
                        temp.Y,
                        temp.Z + (dist * Math.Cos((Math.PI / 12.0) * i))
                    );
                    draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                }
                draw.PathStroke(
                    ImGui.GetColorU32(col),
                    ImDrawFlags.None,
                    2.0f + ((1.0f - bla) * 4.0f)
                );
                v1 = _state.plug._ui.TranslateToScreen(temp.X + 3.0f, temp.Y, temp.Z);
                v2 = _state.plug._ui.TranslateToScreen(temp.X - 3.0f, temp.Y, temp.Z);
                draw.AddLine(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    ImGui.GetColorU32(col),
                    3.0f
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X + 2.0f, v1.Y + 2.0f), ImGui.GetColorU32(scol), "X+"
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X, v1.Y), ImGui.GetColorU32(tcol), "X+"
                );
                v1 = _state.plug._ui.TranslateToScreen(temp.X, temp.Y + 3.0f, temp.Z);
                v2 = _state.plug._ui.TranslateToScreen(temp.X, temp.Y - 3.0f, temp.Z);
                draw.AddLine(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    ImGui.GetColorU32(col),
                    3.0f
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X + 2.0f, v1.Y + 2.0f), ImGui.GetColorU32(scol), "Y+"
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X, v1.Y), ImGui.GetColorU32(tcol), "Y+"
                );
                v1 = _state.plug._ui.TranslateToScreen(temp.X, temp.Y, temp.Z + 3.0f);
                v2 = _state.plug._ui.TranslateToScreen(temp.X, temp.Y, temp.Z - 3.0f);
                draw.AddLine(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    ImGui.GetColorU32(col),
                    3.0f
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X + 2.0f, v1.Y + 2.0f), ImGui.GetColorU32(scol), "Z+"
                );
                draw.AddText(ImGui.GetFont(), 40.0f,
                    new Vector2(v1.X, v1.Y), ImGui.GetColorU32(tcol), "Z+"
                );
                return true;
            }

            private void GetCurrentPosition()
            {
                GameObject go = _state.cs.LocalPlayer as GameObject;
                X = go.Position.X;
                Y = go.Position.Y;
                Z = go.Position.Z;
            }

            private void PerformTeleport()
            {
                if (Active == false || _state.cfg.QuickToggleHacks == false || (PlayersNearby > 0 && AllowRiskyTeleport == false))
                {
                    return;
                }
                GameObject go = _state.cs.LocalPlayer as GameObject;
                unsafe
                {
                    GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                    gop->Position.X = X;
                    gop->Position.Y = Y;
                    gop->Position.Z = Z;
                }
            }

            public Teleporter(State state) : base(state)
            {
                Enabled = false;
                _loaded = DateTime.Now;
                GetCurrent = new System.Action(() => GetCurrentPosition());
                Teleport = new System.Action(() => PerformTeleport());
            }

        }        

        public Hack(State st) : base(st)
        {
            Enabled = false;
        }

    }

    #endif

}
