using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lemegeton.Core;
using System;
using System.Numerics;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObjectPtr = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Lemegeton.Content
{

    public class VisualEnhancement : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

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
                                Vector3 mauw = _state.plug.TranslateToScreen(
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
                        Vector3 mauw = _state.plug.TranslateToScreen(
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
                    Vector3 t3, t1 = _state.plug.TranslateToScreen(go.Position.X, go.Position.Y, go.Position.Z);
                    float ag = -1.0f * go.Rotation - (float)(Math.PI / 4.0f);
                    Vector3 t2 = _state.plug.TranslateToScreen(
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
                    t2 = _state.plug.TranslateToScreen(
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
                    t1 = _state.plug.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f)),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f))
                    );
                    t2 = _state.plug.TranslateToScreen(
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
                    t1 = _state.plug.TranslateToScreen(
                        go.Position.X + (Math.Cos(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f)),
                        go.Position.Y,
                        go.Position.Z + (Math.Sin(ag) * (go.HitboxRadius - go.HitboxRadius / 10.0f))
                    );
                    t2 = _state.plug.TranslateToScreen(
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
                    t1 = _state.plug.TranslateToScreen(
                        point.X,
                        go.Position.Y,
                        point.Y
                    );
                    t2 = _state.plug.TranslateToScreen(
                        point.X + (Math.Cos(exag + wid) * dist),
                        go.Position.Y,
                        point.Y + (Math.Sin(exag + wid) * dist)
                    );
                    t3 = _state.plug.TranslateToScreen(
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
