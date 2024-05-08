using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Internal;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using static Lemegeton.Content.Overlays;
using static Lemegeton.Content.Overlays.DotTracker;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Lemegeton.Content
{

    public class VisualEnhancement : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class Castbar : Core.ContentItem
        {

            public class CastbarVisualsWidget : CustomPropertyInterface
            {

                public override void Deserialize(string data)
                {
                    string[] temp = data.Split(";");
                    foreach (string t in temp)
                    {
                        string[] item = t.Split("=", 2);
                        switch (item[0])
                        {
                            case "WarningThreshold": _cb._visualWarningThreshold = float.Parse(item[1]); break;
                            case "BarWidth": _cb._visualBarWidth = float.Parse(item[1]); break;
                            case "BarHeight": _cb._visualBarHeight = float.Parse(item[1]); break;
                            case "FontSize": _cb._visualFontSize = float.Parse(item[1]); break;
                            case "BarOffsetWorldX": _cb._visualBarOffsetWorldX = float.Parse(item[1]); break;
                            case "BarOffsetWorldY": _cb._visualBarOffsetWorldY = float.Parse(item[1]); break;
                            case "BarOffsetWorldZ": _cb._visualBarOffsetWorldZ = float.Parse(item[1]); break;
                            case "BarOffsetScreenX": _cb._visualBarOffsetScreenX = float.Parse(item[1]); break;
                            case "BarOffsetScreenY": _cb._visualBarOffsetScreenY = float.Parse(item[1]); break;
                        }
                    }
                }

                public override string Serialize()
                {
                    List<string> items = new List<string>();
                    items.Add(String.Format("WarningThreshold={0}", _cb._visualWarningThreshold));
                    items.Add(String.Format("BarWidth={0}", _cb._visualBarWidth));
                    items.Add(String.Format("BarHeight={0}", _cb._visualBarHeight));
                    items.Add(String.Format("FontSize={0}", _cb._visualFontSize));
                    items.Add(String.Format("BarOffsetWorldX={0}", _cb._visualBarOffsetWorldX));
                    items.Add(String.Format("BarOffsetWorldY={0}", _cb._visualBarOffsetWorldY));
                    items.Add(String.Format("BarOffsetWorldZ={0}", _cb._visualBarOffsetWorldZ));
                    items.Add(String.Format("BarOffsetScreenX={0}", _cb._visualBarOffsetScreenX));
                    items.Add(String.Format("BarOffsetScreenY={0}", _cb._visualBarOffsetScreenY));
                    return String.Join(";", items);
                }

                public override void RenderEditor(string path)
                {
                    Vector2 avail = ImGui.GetContentRegionAvail();
                    string proptr = I18n.Translate(path);
                    ImGui.Text(proptr);
                    ImGui.PushItemWidth(avail.X);
                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/WarningThreshold"));
                    float barr = _cb._visualWarningThreshold;
                    if (ImGui.DragFloat("##" + path + "/WarningThreshold", ref barr, 0.01f, 0.0f, 30.0f, "%.1f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _cb._visualWarningThreshold = barr;
                    }
                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/BarWidth"));
                    float barw = _cb._visualBarWidth;
                    if (ImGui.SliderFloat("##" + path + "/BarWidth", ref barw, 50.0f, 600.0f, "%.0f") == true)
                    {
                        _cb._visualBarWidth = barw;
                    }
                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/BarHeight"));
                    float barh = _cb._visualBarHeight;
                    if (ImGui.SliderFloat("##" + path + "/ItemHeight", ref barh, 10.0f, 60.0f, "%.0f") == true)
                    {
                        _cb._visualBarHeight = barh;
                    }
                    ImGui.Text(Environment.NewLine + I18n.Translate(path + "/FontSize"));
                    float fnts = _cb._visualFontSize;
                    if (ImGui.SliderFloat("##" + path + "/FontSize", ref fnts, 8.0f, 56.0f, "%.0f") == true)
                    {
                        _cb._visualFontSize = fnts;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate(path + "/BarOffsetWorld"));
                    float ofsworldx = _cb._visualBarOffsetWorldX;
                    if (ImGui.DragFloat("X##" + path + "/BarOffsetWorldX", ref ofsworldx, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _cb._visualBarOffsetWorldX = ofsworldx;
                    }
                    float ofsworldy = _cb._visualBarOffsetWorldY;
                    if (ImGui.DragFloat("Y##" + path + "/BarOffsetWorldY", ref ofsworldy, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _cb._visualBarOffsetWorldY = ofsworldy;
                    }
                    float ofsworldz = _cb._visualBarOffsetWorldZ;
                    if (ImGui.DragFloat("Z##" + path + "/BarOffsetWorldZ", ref ofsworldz, 0.01f, -5.0f, 5.0f, "%.2f", ImGuiSliderFlags.AlwaysClamp) == true)
                    { 
                        _cb._visualBarOffsetWorldZ = ofsworldz;
                    }
                    ImGui.TextWrapped(Environment.NewLine + I18n.Translate(path + "/BarOffsetScreen"));
                    float ofsscreenx = _cb._visualBarOffsetScreenX;
                    if (ImGui.DragFloat("X##" + path + "/BarOffsetScreenX", ref ofsscreenx, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _cb._visualBarOffsetScreenX = ofsscreenx;
                    }
                    float ofsscreeny = _cb._visualBarOffsetScreenY;
                    if (ImGui.DragFloat("Y##" + path + "/BarOffsetScreenY", ref ofsscreeny, 1.0f, -300.0f, 300.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp) == true)
                    {
                        _cb._visualBarOffsetScreenY = ofsscreeny;
                    }
                    ImGui.PopItemWidth();
                }

                private Castbar _cb;

                public CastbarVisualsWidget(Castbar cb)
                {
                    _cb = cb;
                }

            }

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 CastColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1001)]
            public Vector4 WarningColor { get; set; } = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            [AttributeOrderNumber(1002)]
            public Vector4 InterruptColor { get; set; } = new Vector4(1.0f, 0.0f, 1.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public CastbarVisualsWidget CastbarVisuals { get; set; }

            [AttributeOrderNumber(3000)]
            public bool OnlyCurrentTarget { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool OnlyTargettable { get; set; } = true;
            [AttributeOrderNumber(3002)]
            public bool ShowCastName { get; set; } = false;
            [AttributeOrderNumber(3003)]
            public bool ShowCastTime { get; set; } = false;

            internal float _visualBarHeight { get; set; } = 20.0f;
            internal float _visualBarWidth { get; set; } = 200.0f;
            internal float _visualWarningThreshold { get; set; } = 2.5f;
            internal float _visualFontSize { get; set; } = 16.0f;

            internal float _visualBarOffsetWorldX { get; set; } = 0.0f;
            internal float _visualBarOffsetWorldY { get; set; } = 2.0f;
            internal float _visualBarOffsetWorldZ { get; set; } = 0.0f;
            internal float _visualBarOffsetScreenX { get; set; } = 0.0f;
            internal float _visualBarOffsetScreenY { get; set; } = 0.0f;

            private void DrawCastBar(ImDrawListPtr draw, float x, float y, float width, float height, float timeRemaining, float timeMax, bool interruptable, string name)
            {
                float x2 = x + (width * timeRemaining / timeMax);
                float yt = y + (height * 0.3f);
                Vector4 maincol;
                if (timeRemaining > _visualWarningThreshold)
                {
                    maincol = CastColor;
                }
                else
                {
                    maincol = WarningColor;
                }
                Vector4 bg = new Vector4(0.0f, 0.0f, 0.0f, 0.5f);
                Vector4 shadow = new Vector4(
                    Math.Clamp(maincol.X * 0.5f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Y * 0.5f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Z * 0.5f, 0.0f, 1.0f),
                    maincol.W
                );
                Vector4 hilite = new Vector4(
                    Math.Clamp(maincol.X + 0.2f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Y + 0.2f, 0.0f, 1.0f),
                    Math.Clamp(maincol.Z + 0.2f, 0.0f, 1.0f),
                    maincol.W
                );
                draw.AddRectFilled(
                    new Vector2(x2, y),
                    new Vector2(x + width, y + height),
                    ImGui.GetColorU32(bg)
                );
                draw.AddRectFilledMultiColor(
                    new Vector2(x, y),
                    new Vector2(x2, yt),
                    ImGui.GetColorU32(maincol),
                    ImGui.GetColorU32(maincol),
                    ImGui.GetColorU32(hilite),
                    ImGui.GetColorU32(hilite)
                );
                draw.AddRectFilledMultiColor(
                    new Vector2(x, yt),
                    new Vector2(x2, y + height),
                    ImGui.GetColorU32(hilite),
                    ImGui.GetColorU32(hilite),
                    ImGui.GetColorU32(shadow),
                    ImGui.GetColorU32(shadow)
                );
                if (interruptable == true)
                {
                    uint col = ImGui.GetColorU32(InterruptColor);
                    float ang = (float)Math.Abs(Math.Sin(timeRemaining * 10.0f));
                    float ef = (float)Math.Round(ang * (_visualBarHeight / 10.0f));
                    float thicc = (float)Math.Round(Math.Clamp((_visualBarHeight / 15.0f), 2.0f, 10.0f));
                    draw.AddLine(new Vector2(x - ef, y - ef), new Vector2(x + width + ef, y - ef), col, thicc + (ang * thicc));
                    draw.AddLine(new Vector2(x - ef, y + height + ef), new Vector2(x + width + ef, y + height + ef), col, thicc + (ang * thicc));
                    draw.AddLine(new Vector2(x - ef, y - ef), new Vector2(x - ef, y + height + ef), col, thicc + (ang * thicc));
                    draw.AddLine(new Vector2(x + width + ef, y - ef), new Vector2(x + width + ef, y + height + ef), col, thicc + (ang * thicc));
                }
                if (ShowCastTime == true)
                {
                    string str = String.Format("{0:0.0}", timeRemaining);
                    Vector2 sz = ImGui.CalcTextSize(str);
                    float scale = _visualFontSize / sz.Y;
                    sz = new Vector2(sz.X * scale, sz.Y * scale);
                    draw.AddText(ImGui.GetFont(), _visualFontSize, new Vector2(x2 + 5.0f, y + (height / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), str);
                }
                if (name != "")
                {
                    Vector2 sz = ImGui.CalcTextSize(name);
                    float scale = _visualFontSize / sz.Y;
                    sz = new Vector2(sz.X * scale, sz.Y * scale);
                    draw.AddText(ImGui.GetFont(), _visualFontSize, new Vector2(x + (width / 2.0f) - (sz.X / 2.0f) + 1.0f, y + (height / 2.0f) - (sz.Y / 2.0f) + 1.0f), ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)), name);
                    draw.AddText(ImGui.GetFont(), _visualFontSize, new Vector2(x + (width / 2.0f) - (sz.X / 2.0f), y + (height / 2.0f) - (sz.Y / 2.0f)), ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), name);
                }
            }

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                PlayerCharacter c = _state.cs.LocalPlayer;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind != ObjectKind.BattleNpc)
                    {
                        continue;
                    }
                    if (OnlyCurrentTarget == true && (c.TargetObject == null || c.TargetObject.ObjectId != go.ObjectId))
                    {
                        continue;
                    }
                    if (go.SubKind == 2)
                    {
                        continue;
                    }
                    bool isTargettable;
                    unsafe
                    {
                        GameObjectPtr* gop = (GameObjectPtr*)go.Address;
                        isTargettable = gop->GetIsTargetable();
                    }                    
                    if (OnlyTargettable == true && isTargettable == false)
                    {
                        continue;
                    }
                    BattleChara bc = go as BattleChara;
                    if (bc.IsCasting == true && bc.CastActionId > 0)
                    {
                        Vector3 pos = _state.plug._ui.TranslateToScreen(
                            go.Position.X + _visualBarOffsetWorldX,
                            go.Position.Y + _visualBarOffsetWorldY,
                            go.Position.Z + _visualBarOffsetWorldZ
                        );
                        pos = new Vector3(pos.X + _visualBarOffsetScreenX, pos.Y + _visualBarOffsetScreenY, pos.Z);                        
                        DrawCastBar(draw, pos.X - (_visualBarWidth / 2.0f), pos.Y - _visualBarHeight, _visualBarWidth, _visualBarHeight, bc.TotalCastTime - bc.CurrentCastTime, bc.TotalCastTime, bc.IsCastInterruptible, 
                            ShowCastName == true ? _state.plug.GetActionName(bc.CastActionId) : ""
                        );
                    }
                }
                return true;
            }

            public Castbar(State state) : base(state)
            {
                Enabled = false;
                CastbarVisuals = new CastbarVisualsWidget(this);
            }

        }

        public class Hitbox : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 HitboxColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            [AttributeOrderNumber(1001)]
            public Vector4 CastColor { get; set; } = new Vector4(1.0f, 1.0f, 0.0f, 0.2f);

            [AttributeOrderNumber(2000)]
            public bool DrawOnEnemies { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public bool DrawOnPlayers { get; set; } = false;
            [AttributeOrderNumber(2002)]
            public bool OnlyCurrentTarget { get; set; } = true;
            [AttributeOrderNumber(2003)]
            public bool ShowCasts { get; set; } = true;

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                PlayerCharacter c = _state.cs.LocalPlayer;
                foreach (GameObject go in _state.ot)
                {
                    if (go.ObjectKind != ObjectKind.BattleNpc && go.ObjectKind != ObjectKind.Player)
                    {
                        continue;
                    }
                    if (OnlyCurrentTarget == true && (c.TargetObject == null || c.TargetObject.ObjectId != go.ObjectId))
                    {
                        continue;
                    }
                    if (go.ObjectKind == ObjectKind.BattleNpc && DrawOnEnemies == false)
                    {
                        continue;
                    }
                    if (go.ObjectKind == ObjectKind.Player && DrawOnPlayers == false)
                    {
                        continue;
                    }
                    if (go.SubKind == 2)
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
                    if (ShowCasts == true)
                    {
                        BattleChara bc = go as BattleChara;
                        if (bc.IsCasting == true && bc.CastActionId > 0)
                        {
                            float ex = go.HitboxRadius * bc.CurrentCastTime / bc.TotalCastTime;
                            for (int i = 0; i <= 48; i++)
                            {
                                Vector3 mauw = _state.plug._ui.TranslateToScreen(
                                    go.Position.X + (ex * Math.Sin((Math.PI / 24.0) * i)),
                                    go.Position.Y,
                                    go.Position.Z + (ex * Math.Cos((Math.PI / 24.0) * i))
                                );
                                draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                            }
                            draw.PathFillConvex(
                                ImGui.GetColorU32(CastColor)
                            );
                        }
                    }
                    for (int i = 0; i <= 48; i++)
                    {
                        Vector3 mauw = _state.plug._ui.TranslateToScreen(
                            go.Position.X + (go.HitboxRadius * Math.Sin((Math.PI / 24.0) * i)),
                            go.Position.Y,
                            go.Position.Z + (go.HitboxRadius * Math.Cos((Math.PI / 24.0) * i))
                        );
                        draw.PathLineTo(new Vector2(mauw.X, mauw.Y));
                    }
                    draw.PathStroke(
                        ImGui.GetColorU32(HitboxColor),
                        ImDrawFlags.None,
                        4.0f
                    );
                    Vector3 t3, t1 = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                    float ag = -1.0f * go.Rotation - (float)(Math.PI / 4.0f);
                    Vector3 t2 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * go.HitboxRadius), 
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * go.HitboxRadius)
                    );
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(HitboxColor),
                        4.0f
                    );
                    ag -= (float)(Math.PI / 2.0f);
                    t2 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * go.HitboxRadius),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * go.HitboxRadius)
                    );
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(HitboxColor),
                        4.0f
                    );
                    ag -= (float)(Math.PI / 2.0f);
                    t1 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f)),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f))
                    );
                    t2 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * go.HitboxRadius),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * go.HitboxRadius)
                    );
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(HitboxColor),
                        4.0f
                    );
                    ag -= (float)(Math.PI / 2.0f);
                    t1 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f)),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f))
                    );
                    t2 = _state.plug._ui.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * go.HitboxRadius),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * go.HitboxRadius)
                    );
                    draw.AddLine(
                        new Vector2(t1.X, t1.Y),
                        new Vector2(t2.X, t2.Y),
                        ImGui.GetColorU32(HitboxColor),
                        4.0f
                    );
                    ag = -1.0f * go.Rotation + (float)(Math.PI / 2.0f);
                    float dist = go.HitboxRadius / 10.0f;
                    dist = 0.2f;
                    float wid = dist * 2.0f;
                    float exag = ag + (float)Math.PI;
                    Vector2 point = new Vector2(
                        (float)(go.Position.X + (Math.Cos(ag) * go.HitboxRadius)),
                        (float)(go.Position.Z + (Math.Sin(ag) * go.HitboxRadius))
                    );
                    t1 = _state.plug._ui.TranslateToScreen(
                        point.X,
                        go.Position.Y,
                        point.Y
                    );
                    t2 = _state.plug._ui.TranslateToScreen(
                        point.X + (Math.Cos(exag + wid) * dist),
                        go.Position.Y,
                        point.Y + (Math.Sin(exag + wid) * dist)
                    );
                    t3 = _state.plug._ui.TranslateToScreen(
                        point.X + (Math.Cos(exag - wid) * dist),
                        go.Position.Y,
                        point.Y + (Math.Sin(exag - wid) * dist)
                    );
                    draw.PathLineTo(new Vector2(t1.X, t1.Y));
                    draw.PathLineTo(new Vector2(t2.X, t2.Y));
                    draw.PathLineTo(new Vector2(t3.X, t3.Y));
                    draw.PathLineTo(new Vector2(t1.X, t1.Y));
                    draw.PathFillConvex(
                        ImGui.GetColorU32(HitboxColor)
                    );
                }
                return true;
            }

            public Hitbox(State state) : base(state)
            {
                Enabled = false;
            }

        }

        public VisualEnhancement(State st) : base(st)
        {
        }

    }

}
