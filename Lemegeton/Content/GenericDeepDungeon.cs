using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using Lemegeton.Core;
using System;
using System.Numerics;

namespace Lemegeton.Content
{

    public abstract class GenericDeepDungeon : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class DrawEnemies : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 ObjectColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool DrawLine { get; set; } = true;

            [AttributeOrderNumber(3000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(3002)]
            public Vector4 TextColor { get; set; } = new Vector4(1.0f, 0.8f, 0.8f, 1.0f);

            [AttributeOrderNumber(4000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(4001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Vector3 me = _state.cs.LocalPlayer.Position;
                Vector2 pt = new Vector2();
                me = _state.plug._ui.TranslateToScreen(me.X, me.Y, me.Z);
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                foreach (IGameObject go in _state.ot)
                {
                    if (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && go.SubKind == 5 && go.IsDead == false)
                    {
                        IBattleChara bc = (IBattleChara)go;
                        if (bc.CurrentHp > 0 && (_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile) != 0 && bc.ClassJob.RowId == 0)
                        {
                            double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                            Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                            pt.Y = temp.Y + 10.0f;
                            string name = IncludeDistance == true ? String.Format("{0} ({1:0})", go.Name.ToString(), dist) : String.Format("{0}", go.Name.ToString());
                            Vector2 sz = ImGui.CalcTextSize(name);
                            sz.X *= mul;
                            sz.Y *= mul;
                            pt.X = temp.X - (sz.X / 2.0f);
                            if (ShowNames == true)
                            {
                                if (ShowNameBg == true)
                                {
                                    draw.AddRectFilled(
                                        new Vector2(pt.X - 5, pt.Y - 5),
                                        new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                                        ImGui.GetColorU32(BgColor),
                                        1.0f
                                    );
                                }
                                draw.AddText(
                                    ImGui.GetFont(),
                                    20.0f,
                                    pt,
                                    ImGui.GetColorU32(TextColor),
                                    name
                                );
                            }
                            if (dist < 50.0f && DrawLine == true && (_state.GetStatusFlags(bc) & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.OffhandOut) == 0)
                            {
                                draw.AddLine(
                                    new Vector2(temp.X, temp.Y),
                                    new Vector2(me.X, me.Y),
                                    ImGui.GetColorU32(ObjectColor),
                                    dist < 20.0f ? 10.0f : (dist < 30.0f ? 5.0f : 2.0f)
                                );
                            }
                            draw.AddCircleFilled(
                                new Vector2(temp.X, temp.Y),
                                10.0f,
                                ImGui.GetColorU32(ObjectColor),
                                20
                            );
                        }
                    }
                }
                return true;
            }

            public DrawEnemies(State state) : base(state)
            {
            }

        }

        public class DrawPylons : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 ObjectColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public bool IncludeDistance { get; set; } = true;
            [AttributeOrderNumber(2002)]
            public Vector4 TextColor { get; set; } = new Vector4(0.8f, 1.0f, 0.8f, 1.0f);

            [AttributeOrderNumber(3000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Vector2 pt = new Vector2();
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                foreach (IGameObject go in _state.ot)
                {
                    // 2007187 dead return
                    // 2009506 hoh return
                    // 2009507 hoh passage
                    // 2013286 orthos return
                    // 2013287 orthos passage
                    if (
                        (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj)
                        &&
                        (
                            (go.DataId == 2007187 || go.DataId == 2007188)
                            ||
                            (go.DataId == 2009506 || go.DataId == 2009507)
                            ||
                            (go.DataId == 2013286 || go.DataId == 2013287)
                        )
                    )
                    {
                        double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                        Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                        pt.Y = temp.Y + 10.0f;
                        string name = IncludeDistance == true ? String.Format("{0} ({1:0})", go.Name.ToString(), dist) : String.Format("{0}", go.Name.ToString());
                        Vector2 sz = ImGui.CalcTextSize(name);
                        sz.X *= mul;
                        sz.Y *= mul;
                        pt.X = temp.X - (sz.X / 2.0f);
                        if (ShowNames == true)
                        {
                            if (ShowNameBg == true)
                            {
                                draw.AddRectFilled(
                                    new Vector2(pt.X - 5, pt.Y - 5),
                                    new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                                    ImGui.GetColorU32(BgColor),
                                    1.0f
                                );
                            }
                            draw.AddText(
                                ImGui.GetFont(),
                                20.0f,
                                pt,
                                ImGui.GetColorU32(TextColor),
                                name
                            );
                        }
                        draw.AddCircleFilled(
                            new Vector2(temp.X, temp.Y),
                            10.0f,
                            ImGui.GetColorU32(ObjectColor),
                            20
                        );
                    }
                }
                return true;
            }

            public DrawPylons(State state) : base(state)
            {
            }

        }

        public class DrawChests : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Drawing;

            [AttributeOrderNumber(1000)]
            public Vector4 GoldColor { get; set; } = new Vector4(0.9686f, 0.7608f, 0.0392f, 1.0f);
            [AttributeOrderNumber(1001)]
            public Vector4 SilverColor { get; set; } = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            [AttributeOrderNumber(1002)]
            public Vector4 BronzeColor { get; set; } = new Vector4(0.635f, 0.364f, 0.102f, 1.0f);
            [AttributeOrderNumber(1003)]
            public Vector4 BandedColor { get; set; } = new Vector4(0.455f, 0.714f, 0.455f, 1.0f);
            [AttributeOrderNumber(1004)]
            public Vector4 HoardColor { get; set; } = new Vector4(1.0f, 0.957f, 0.286f, 1.0f);

            [AttributeOrderNumber(2000)]
            public bool ShowNames { get; set; } = true;
            [AttributeOrderNumber(2001)]
            public bool IncludeDistance { get; set; } = true;

            [AttributeOrderNumber(3000)]
            public bool ShowNameBg { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public Vector4 BgColor { get; set; } = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);

            protected override bool ExecutionImplementation()
            {
                ImDrawListPtr draw;
                if (_state.StartDrawing(out draw) == false)
                {
                    return false;
                }
                Vector2 pt = new Vector2();
                float defSize = ImGui.GetFontSize();
                float mul = 20.0f / defSize;
                Vector4 itemcol = GoldColor;
                foreach (IGameObject go in _state.ot)
                {
                    if (
                        (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure)
                        ||
                        (go.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj && (go.DataId == 2007357 || go.DataId == 2007358 || go.DataId == 2007542 || go.DataId == 2007543))
                    )
                    {                        
                        switch (go.DataId)
                        {
                            case 2007358:
                                itemcol = GoldColor;
                                break;
                            case 2007357:
                                itemcol = SilverColor;
                                break;
                            case 2007543:
                                itemcol = BandedColor;
                                break;
                            case 2007542:
                                itemcol = HoardColor;
                                break;
                            default:
                                itemcol = BronzeColor;
                                break;
                        }
                        double dist = Vector3.Distance(_state.cs.LocalPlayer.Position, go.Position);
                        Vector3 temp = _state.plug._ui.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                        pt.Y = temp.Y + 10.0f;
                        string rname = go.DataId != 2007542 ? go.Name.ToString() : I18n.Translate("Content/DeepDungeon/AccursedHoard");
                        string name = IncludeDistance == true ? String.Format("{0} ({1:0})", rname, dist) : String.Format("{0}", rname);
                        Vector2 sz = ImGui.CalcTextSize(name);
                        sz.X *= mul;
                        sz.Y *= mul;
                        pt.X = temp.X - (sz.X / 2.0f);
                        if (ShowNames == true)
                        {
                            if (ShowNameBg == true)
                            {
                                draw.AddRectFilled(
                                    new Vector2(pt.X - 5, pt.Y - 5),
                                    new Vector2(pt.X + sz.X + 5, pt.Y + sz.Y + 5),
                                    ImGui.GetColorU32(BgColor),
                                    1.0f
                                );
                            }
                            draw.AddText(
                                ImGui.GetFont(),
                                20.0f,
                                pt,
                                ImGui.GetColorU32(itemcol),
                                name
                            );
                        }
                        draw.AddCircleFilled(
                            new Vector2(temp.X, temp.Y),
                            10.0f,
                            ImGui.GetColorU32(itemcol),
                            20
                        );
                    }
                }
                return true;
            }

            public DrawChests(State state) : base(state)
            {
            }

        }

        public GenericDeepDungeon(State st) : base(st)
        {
        }

    }

}
