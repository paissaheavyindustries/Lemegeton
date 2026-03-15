using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Lemegeton.Core;
using System;
using System.Numerics;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Lemegeton.Content
{

    internal class PilgrimsTraverse : GenericDeepDungeon
    {

        private bool DungeonZoneOk = false;
        private bool BossZoneOk = false;

        private const int NameGrief = 14037;
        private const int NameEater = 14038;

        private DoubleTrouble _doubleTrouble;

        #region DoubleTrouble

        public class DoubleTrouble : Core.ContentItem
        {

            public ulong _idGrief = 0;
            public ulong _idEater = 0;

            public override FeaturesEnum Features => FeaturesEnum.Drawing;
            internal PilgrimsTraverse Parent;

            [AttributeOrderNumber(2000)]
            public Overlay Area { get; set; }

            [DebugOption]
            [AttributeOrderNumber(3000)]
            public System.Action Test { get; set; }

            public DoubleTrouble(State state) : base(state)
            {
                Enabled = false;
                Area = new Overlay();
                Area.Renderer += Render;
                Test = new System.Action(() => TestFunctionality());
            }

            public void TestFunctionality()
            {
                if (_idGrief > 0 || _idEater > 0)
                {
                    _idGrief = 0;
                    _idEater = 0;
                    return;
                }
                _state.InvokeZoneChange(1238);
                IGameObject me = _state.cs.LocalPlayer as IGameObject;
                _idGrief = me.GameObjectId;
                _idEater = me.GameObjectId;
                foreach (IGameObject go in _state.ot)
                {
                    if (go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && go.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                    {
                        continue;
                    }
                    if (go.GameObjectId == me.GameObjectId)
                    {
                        continue;
                    }
                    bool isTargettable;
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        isTargettable = gop->GetIsTargetable();
                    }
                    bool isHidden = (isTargettable == false);
                    if (isHidden == true)
                    {
                        continue;
                    }
                    Random r = new Random();
                    _idEater = go.GameObjectId;
                    Log(State.LogLevelEnum.Debug, null, "Testing from {0} to {1}", me, go);
                    return;
                }
                Log(State.LogLevelEnum.Debug, null, "Testing on self");
            }

            public override void Reset()
            {
                Log(State.LogLevelEnum.Debug, null, "Reset");
                _idGrief = 0;
                _idEater = 0;
            }

            protected void Render(ImDrawListPtr draw, bool configuring)
            {
                IGameObject goR = configuring == true ? _state.cs.LocalPlayer : _state.GetActorById(_idGrief);
                if (goR == null)
                {
                    return;
                }
                IGameObject goG = configuring == true ? _state.cs.LocalPlayer : _state.GetActorById(_idEater);
                if (goG == null)
                {
                    return;
                }
                ICharacter chR = (ICharacter)goR;
                ICharacter chG = (ICharacter)goG;
                ISharedImmediateTexture bws, tws;
                float x = Area.X;
                float y = Area.Y;
                float w = Area.Width;
                float h = Area.Height;
                int pad = Area.Padding;
                float hhp = (float)chR.CurrentHp / (float)chR.MaxHp * 100.0f;
                float nhp = (float)chG.CurrentHp / (float)chG.MaxHp * 100.0f;
                float dhp = Math.Abs(hhp - nhp);
                Vector4 hcol, ncol, wcol;
                hcol = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                ncol = hcol;
                wcol = hcol;
                float time = (float)(DateTime.Now - DateTime.Today).TotalMilliseconds / 200.0f;
                if (dhp > 14.0f)
                {
                    float ang = (float)Math.Abs(Math.Cos(time * 2.0f));
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 0.0f, 0.0f, ang);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 0.0f, 0.0f, ang);
                    }
                    wcol = new Vector4(1.0f, ang, 0.0f, 1.0f);
                }
                else if (dhp > 10.0f)
                {
                    float ang = (float)Math.Abs(Math.Cos(time));
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 0.5f, 0.0f, ang);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 0.5f, 0.0f, ang);
                    }
                    wcol = new Vector4(1.0f, 0.5f + (ang * 0.5f), 0.0f, 1.0f);
                }
                else if (dhp > 6.0f)
                {
                    if (hhp > nhp)
                    {
                        hcol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        ncol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                    }
                    wcol = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                }
                draw.AddRectFilled(
                    new Vector2(x, y),
                    new Vector2(x + w, y + h),
                    ImGui.GetColorU32(Area.BackgroundColor)
                );
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.LightDragon);
                IDalamudTextureWrap tw = tws.GetWrapOrEmpty();
                bws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.LightCircle);
                IDalamudTextureWrap bw = bws.GetWrapOrEmpty();
                draw.AddImage(
                    bw.Handle,
                    new Vector2(x + pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + tw.Width + pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(hcol)
                );
                draw.AddImage(
                    bw.Handle,
                    new Vector2(x + w - tw.Width - pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + w - pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    ImGui.GetColorU32(ncol)
                );
                draw.AddImage(
                    tw.Handle,
                    new Vector2(x + pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + tw.Width + pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 1.0f)
                );
                tws = _state.plug._ui.GetMiscIcon(UserInterface.MiscIconEnum.DarkDragon);
                tw = tws.GetWrapOrEmpty();
                draw.AddImage(
                    tw.Handle,
                    new Vector2(x + w - tw.Width - pad, y + (h / 2.0f) - (tw.Height / 2.0f)),
                    new Vector2(x + w - pad, y + (h / 2.0f) + (tw.Height / 2.0f)),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f)
                );
                float textSize = 24.0f, scale;
                string temp;
                Vector2 sz;
                temp = String.Format("{0:0.0} %", hhp);
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, -Area.Padding + y + Area.Height - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + (tw.Width / 2.0f) - (sz.X / 2.0f), -Area.Padding + y + Area.Height - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", nhp);
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + w - (tw.Width / 2.0f) - (sz.X / 2.0f) + 1.0f, -Area.Padding + y + Area.Height - sz.Y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + w - (tw.Width / 2.0f) - (sz.X / 2.0f), -Area.Padding + y + Area.Height - sz.Y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chR.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x + 1.0f, Area.Padding + y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(Area.Padding + x, Area.Padding + y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = chG.Name.ToString();
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + Area.Width - sz.X + 1.0f, Area.Padding + y + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(-Area.Padding + x + Area.Width - sz.X, Area.Padding + y), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), temp);
                temp = String.Format("{0:0.0} %", dhp);
                textSize = 40.0f;
                sz = ImGui.CalcTextSize(temp);
                scale = textSize / sz.Y;
                sz = new Vector2(sz.X * scale, sz.Y * scale);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f) + 1.0f, y + (h / 2.0f) - (sz.Y / 2.0f) + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), temp);
                draw.AddText(ImGui.GetFont(), textSize, new Vector2(x + (w / 2.0f) - (sz.X / 2.0f), y + (h / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(wcol), temp);
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_idGrief == 0 || _idEater == 0)
                {
                    return false;
                }                
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Render(draw, false);
                return true;
            }

        }

        #endregion

        protected override bool ExecutionImplementation()
        {
            if (DungeonZoneOk == true || BossZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
        }

        private void OnCombatantAdded(Dalamud.Game.ClientState.Objects.Types.IGameObject go)
        {
            if (go is ICharacter)
            {
                ICharacter ch = go as ICharacter;
                if (ch.MaxHp != 8910356 && ch.MaxHp != 19313403)
                {
                    return;
                }
                if (ch.NameId == NameGrief)
                {
                    Log(State.LogLevelEnum.Debug, null, "Grief found as {0:X8}", go.GameObjectId);
                    _doubleTrouble._idGrief = go.GameObjectId;
                }
                else if (ch.NameId == NameEater)
                {
                    Log(State.LogLevelEnum.Debug, null, "Eater found as {0:X8}", go.GameObjectId);
                    _doubleTrouble._idEater = go.GameObjectId;
                }
            }
        }

        public PilgrimsTraverse(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone >= 1281 && newZone <= 1290);
            bool bossZoneOk = (newZone == 1311 || newZone == 1333);
            if (newZoneOk == true && DungeonZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Dungeon content available");                
                _doubleTrouble = null;
                _doubleTrouble.Reset();
            }
            else if (bossZoneOk == true && BossZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Quantum content available");
                _doubleTrouble = (DoubleTrouble)Items["DoubleTrouble"];
                _state.OnCombatantAdded += OnCombatantAdded;
            }
            else if (newZoneOk == false && DungeonZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
                _state.OnCombatantAdded -= OnCombatantAdded;
                _doubleTrouble = null;
                _doubleTrouble.Reset();
            }
            DungeonZoneOk = newZoneOk;
            BossZoneOk = bossZoneOk;
        }

    }

}
