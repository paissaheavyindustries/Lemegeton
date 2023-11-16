using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Lemegeton.Core.AutomarkerPrio;

namespace Lemegeton.Core
{

    internal class UserInterface
    {

        private Dictionary<MiscIconEnum, IDalamudTextureWrap> _misc = new Dictionary<MiscIconEnum, IDalamudTextureWrap>();
        private Dictionary<AutomarkerSigns.SignEnum, IDalamudTextureWrap> _signs = new Dictionary<AutomarkerSigns.SignEnum, IDalamudTextureWrap>();
        private Dictionary<AutomarkerPrio.PrioRoleEnum, IDalamudTextureWrap> _roles = new Dictionary<AutomarkerPrio.PrioRoleEnum, IDalamudTextureWrap>();
        private Dictionary<AutomarkerPrio.PrioTrinityEnum, IDalamudTextureWrap> _trinity = new Dictionary<AutomarkerPrio.PrioTrinityEnum, IDalamudTextureWrap>();
        private Dictionary<AutomarkerPrio.PrioJobEnum, IDalamudTextureWrap> _jobs = new Dictionary<AutomarkerPrio.PrioJobEnum, IDalamudTextureWrap>();
        private Dictionary<uint, IDalamudTextureWrap> _onDemand = new Dictionary<uint, IDalamudTextureWrap>();

        internal State _state;

        private DateTime _loaded = DateTime.Now;

        private object _dragObject = null;
        private bool _isDragging = false;
        private int _dragStartItem = 0;
        private int _dragEndItem = 0;

        internal enum MiscIconEnum
        {
            Lemegeton = 1,
            BlueDiamond = 2,
            PurpleDiamond = 3,
            RedDiamond = 4,
            Smiley = 5,
            RedCross = 6,
            Connected = 7,
            Disconnected = 8,
            Exclamation = 9,
            Number1 = 11,
            Number2 = 12,
            Number3 = 13,
            Number4 = 14,
            Number5 = 15,
            Number6 = 16,
            Number7 = 17,
            Number8 = 18,
            TimelineActive = 19,
            TimelineInactive = 20,
            ProfileActive = 21,
        }

        internal void LoadTextures()
        {
            _misc[MiscIconEnum.Lemegeton] = GetTexture(33237);
            _misc[MiscIconEnum.BlueDiamond] = GetTexture(63937);
            _misc[MiscIconEnum.PurpleDiamond] = GetTexture(63939);
            _misc[MiscIconEnum.RedDiamond] = GetTexture(63938);
            _misc[MiscIconEnum.Smiley] = GetTexture(61551);
            _misc[MiscIconEnum.RedCross] = GetTexture(61552);
            _misc[MiscIconEnum.Connected] = GetTexture(61555);
            _misc[MiscIconEnum.Disconnected] = GetTexture(61553);
            _misc[MiscIconEnum.Exclamation] = GetTexture(5);
            _misc[MiscIconEnum.Number1] = GetTexture(66162);
            _misc[MiscIconEnum.Number2] = GetTexture(66163);
            _misc[MiscIconEnum.Number3] = GetTexture(66164);
            _misc[MiscIconEnum.Number4] = GetTexture(66165);
            _misc[MiscIconEnum.Number5] = GetTexture(66166);
            _misc[MiscIconEnum.Number6] = GetTexture(66167);
            _misc[MiscIconEnum.Number7] = GetTexture(66168);
            _misc[MiscIconEnum.Number8] = GetTexture(66169);
            _misc[MiscIconEnum.TimelineActive] = GetTexture(61806);
            _misc[MiscIconEnum.TimelineInactive] = GetTexture(61807);
            _misc[MiscIconEnum.ProfileActive] = GetTexture(61803);
            _signs[AutomarkerSigns.SignEnum.Attack1] = GetTexture(61201);
            _signs[AutomarkerSigns.SignEnum.Attack2] = GetTexture(61202);
            _signs[AutomarkerSigns.SignEnum.Attack3] = GetTexture(61203);
            _signs[AutomarkerSigns.SignEnum.Attack4] = GetTexture(61204);
            _signs[AutomarkerSigns.SignEnum.Attack5] = GetTexture(61205);
            _signs[AutomarkerSigns.SignEnum.Attack6] = GetTexture(61206);
            _signs[AutomarkerSigns.SignEnum.Attack7] = GetTexture(61207);
            _signs[AutomarkerSigns.SignEnum.Attack8] = GetTexture(61208);
            _signs[AutomarkerSigns.SignEnum.Bind1] = GetTexture(61211);
            _signs[AutomarkerSigns.SignEnum.Bind2] = GetTexture(61212);
            _signs[AutomarkerSigns.SignEnum.Bind3] = GetTexture(61213);
            _signs[AutomarkerSigns.SignEnum.Ignore1] = GetTexture(61221);
            _signs[AutomarkerSigns.SignEnum.Ignore2] = GetTexture(61222);
            _signs[AutomarkerSigns.SignEnum.Square] = GetTexture(61231);
            _signs[AutomarkerSigns.SignEnum.Circle] = GetTexture(61232);
            _signs[AutomarkerSigns.SignEnum.Plus] = GetTexture(61233);
            _signs[AutomarkerSigns.SignEnum.Triangle] = GetTexture(61234);
            _signs[AutomarkerSigns.SignEnum.AttackNext] = _signs[AutomarkerSigns.SignEnum.Attack1];
            _signs[AutomarkerSigns.SignEnum.BindNext] = _signs[AutomarkerSigns.SignEnum.Bind1];
            _signs[AutomarkerSigns.SignEnum.IgnoreNext] = _signs[AutomarkerSigns.SignEnum.Ignore1];
            _trinity[AutomarkerPrio.PrioTrinityEnum.Tank] = GetTexture(62581);
            _trinity[AutomarkerPrio.PrioTrinityEnum.Healer] = GetTexture(62582);
            _trinity[AutomarkerPrio.PrioTrinityEnum.DPS] = GetTexture(62583);
            _roles[AutomarkerPrio.PrioRoleEnum.Tank] = GetTexture(62581);
            _roles[AutomarkerPrio.PrioRoleEnum.Healer] = GetTexture(62582);
            _roles[AutomarkerPrio.PrioRoleEnum.Melee] = GetTexture(62584);
            _roles[AutomarkerPrio.PrioRoleEnum.Ranged] = GetTexture(62586);
            _roles[AutomarkerPrio.PrioRoleEnum.Caster] = GetTexture(62587);
            _jobs[AutomarkerPrio.PrioJobEnum.PLD] = GetTexture(62119);
            _jobs[AutomarkerPrio.PrioJobEnum.WAR] = GetTexture(62121);
            _jobs[AutomarkerPrio.PrioJobEnum.DRK] = GetTexture(62132);
            _jobs[AutomarkerPrio.PrioJobEnum.GNB] = GetTexture(62137);
            _jobs[AutomarkerPrio.PrioJobEnum.WHM] = GetTexture(62124);
            _jobs[AutomarkerPrio.PrioJobEnum.SCH] = GetTexture(62128);
            _jobs[AutomarkerPrio.PrioJobEnum.AST] = GetTexture(62133);
            _jobs[AutomarkerPrio.PrioJobEnum.SGE] = GetTexture(62140);
            _jobs[AutomarkerPrio.PrioJobEnum.MNK] = GetTexture(62120);
            _jobs[AutomarkerPrio.PrioJobEnum.DRG] = GetTexture(62122);
            _jobs[AutomarkerPrio.PrioJobEnum.NIN] = GetTexture(62130);
            _jobs[AutomarkerPrio.PrioJobEnum.SAM] = GetTexture(62134);
            _jobs[AutomarkerPrio.PrioJobEnum.RPR] = GetTexture(62139);
            _jobs[AutomarkerPrio.PrioJobEnum.BRD] = GetTexture(62123);
            _jobs[AutomarkerPrio.PrioJobEnum.MCH] = GetTexture(62131);
            _jobs[AutomarkerPrio.PrioJobEnum.DNC] = GetTexture(62138);
            _jobs[AutomarkerPrio.PrioJobEnum.BLM] = GetTexture(62125);
            _jobs[AutomarkerPrio.PrioJobEnum.SMN] = GetTexture(62127);
            _jobs[AutomarkerPrio.PrioJobEnum.RDM] = GetTexture(62135);
            _jobs[AutomarkerPrio.PrioJobEnum.BLU] = GetTexture(62136);
        }

        internal void UnloadTextures()
        {
            foreach (KeyValuePair<AutomarkerSigns.SignEnum, IDalamudTextureWrap> kp in _signs)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _signs.Clear();
            foreach (KeyValuePair<MiscIconEnum, IDalamudTextureWrap> kp in _misc)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _misc.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioTrinityEnum, IDalamudTextureWrap> kp in _trinity)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _trinity.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioRoleEnum, IDalamudTextureWrap> kp in _roles)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _roles.Clear();
            foreach (KeyValuePair<AutomarkerPrio.PrioJobEnum, IDalamudTextureWrap> kp in _jobs)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _jobs.Clear();
            foreach (KeyValuePair<uint, IDalamudTextureWrap> kp in _onDemand)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _onDemand.Clear();
        }

        internal IDalamudTextureWrap GetOnDemandIcon(uint iconId)
        {
            if (_onDemand.ContainsKey(iconId) == false)
            {
                _onDemand[iconId] = GetTexture(iconId);
            }
            return _onDemand[iconId];
        }

        internal IDalamudTextureWrap GetMiscIcon(MiscIconEnum icon)
        {
            if (_misc.ContainsKey(icon) == true)
            {
                return _misc[icon];
            }
            return null;
        }

        internal IDalamudTextureWrap GetSignIcon(AutomarkerSigns.SignEnum icon)
        {
            if (_signs.ContainsKey(icon) == true)
            {
                return _signs[icon];
            }
            return null;
        }

        internal Vector3 TranslateToScreen(double x, double y, double z)
        {
            Vector2 tenp;
            _state.gg.WorldToScreen(
                new Vector3((float)x, (float)y, (float)z),
                out tenp
            );
            return new Vector3(tenp.X, tenp.Y, (float)z);
        }

        internal IDalamudTextureWrap? GetTexture(uint id)
        {
            return _state.tp.GetIcon(id, Dalamud.Plugin.Services.ITextureProvider.IconFlags.None);
        }

        internal static ImGuiMouseButton ImageButton(IDalamudTextureWrap tw, bool enabled, string tooltip)
        {
            if (enabled == true)
            {
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height));
            }
            else
            {
                ImGui.Image(tw.ImGuiHandle, new Vector2(tw.Width, tw.Height), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 0.25f));
            }
            if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip);
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left) == true)
            {
                return ImGuiMouseButton.Left;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) == true)
            {
                return ImGuiMouseButton.Right;
            }
            return (ImGuiMouseButton)(-1);
        }

        internal IDalamudTextureWrap GetJobIcon(uint jobId)
        {
            AutomarkerPrio.PrioJobEnum job = (AutomarkerPrio.PrioJobEnum)jobId;
            if (_jobs.ContainsKey(job) == true)
            {
                return _jobs[job];
            }
            return null;
        }

        private static bool JobSelected(ulong bitmap, PrioJobEnum job)
        {
            return ((bitmap >> (int)job) & 0x1UL) != 0;
        }

        private static ulong SetJob(ulong bitmap, PrioJobEnum job, bool set)
        {
            if (set == true)
            {
                return bitmap | (0x1UL << (int)job);
            }
            else
            {
                return bitmap & (~(0x1UL << (int)job));
            }
        }

        internal ulong RenderJobSelector(ulong bitmap, bool allowLimited)
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            float wid = ImGui.GetWindowWidth();
            List<PrioJobEnum> jobList = new List<PrioJobEnum>();
            jobList.AddRange(new PrioJobEnum[] {
                PrioJobEnum.PLD, PrioJobEnum.WAR, PrioJobEnum.DRK, PrioJobEnum.GNB,
                PrioJobEnum.WHM, PrioJobEnum.SCH, PrioJobEnum.AST, PrioJobEnum.SGE,
                PrioJobEnum.MNK, PrioJobEnum.DRG, PrioJobEnum.NIN, PrioJobEnum.SAM, PrioJobEnum.RPR,
                PrioJobEnum.BRD, PrioJobEnum.MCH, PrioJobEnum.DNC,
                PrioJobEnum.BLM, PrioJobEnum.SMN, PrioJobEnum.RDM,
            });
            if (allowLimited == true)
            {
                jobList.Add(PrioJobEnum.BLU);
            }
            int numjobs = jobList.Count;
            foreach (AutomarkerPrio.PrioJobEnum p in jobList)
            {
                bool selected = JobSelected(bitmap, p);
                float curx = ImGui.GetCursorPosX() + _jobs[p].Width + style.ItemSpacing.X;
                ImGuiMouseButton btn = ImageButton(_jobs[p], selected, I18n.Translate("Job/" + p));
                if (btn == ImGuiMouseButton.Left)
                {
                    bitmap ^= 0x1UL << (int)p;
                }
                if (btn == ImGuiMouseButton.Right)
                {
                    PrioRoleEnum r = JobToRole((uint)p);
                    switch (r)
                    {
                        case PrioRoleEnum.Tank:
                            bitmap = SetJob(bitmap, PrioJobEnum.PLD, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.WAR, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.DRK, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.GNB, selected == false);
                            break;
                        case PrioRoleEnum.Healer:
                            bitmap = SetJob(bitmap, PrioJobEnum.WHM, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.SCH, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.AST, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.SGE, selected == false);
                            break;
                        case PrioRoleEnum.Melee:
                            bitmap = SetJob(bitmap, PrioJobEnum.MNK, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.DRG, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.NIN, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.SAM, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.RPR, selected == false);
                            break;
                        case PrioRoleEnum.Ranged:
                            bitmap = SetJob(bitmap, PrioJobEnum.BRD, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.MCH, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.DNC, selected == false);
                            break;
                        case PrioRoleEnum.Caster:
                            bitmap = SetJob(bitmap, PrioJobEnum.BLM, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.SMN, selected == false);
                            bitmap = SetJob(bitmap, PrioJobEnum.RDM, selected == false);
                            if (allowLimited == true)
                            {
                                bitmap = SetJob(bitmap, PrioJobEnum.BLU, selected == false);
                            }
                            break;
                    }
                }
                if (curx + _jobs[p].Width + style.ItemSpacing.X < wid && numjobs > 1)
                {
                    ImGui.SameLine();
                }
                numjobs--;
            }
            return bitmap;
        }

        internal static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            string ico = icon.ToIconString();
            if (ImGui.Button(ico + "##" + icon + "/" + tooltip) == true)
            {
                ImGui.PopFont();
                return true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip);
                ImGui.EndTooltip();
            }
            return false;
        }

        private string TranslateOrderableItem<T>(T item)
        {
            if (item is PrioRoleEnum)
            {
                PrioRoleEnum tmp = (PrioRoleEnum)Convert.ChangeType(item, typeof(PrioRoleEnum));
                return I18n.Translate("Role/" + tmp.ToString());
            }
            if (item is PrioTrinityEnum)
            {
                PrioTrinityEnum tmp = (PrioTrinityEnum)Convert.ChangeType(item, typeof(PrioTrinityEnum));
                return I18n.Translate("Trinity/" + tmp.ToString());
            }
            if (item is PrioJobEnum)
            {
                PrioJobEnum tmp = (PrioJobEnum)Convert.ChangeType(item, typeof(PrioJobEnum));
                return I18n.Translate("Job/" + tmp.ToString());
            }
            if (item is int)
            {
                return I18n.Translate("Automarker/PrioType/PartyMember");
            }
            if (item is string)
            {
                return item.ToString();
            }
            return "???";
        }

        private IDalamudTextureWrap? RetrieveOrderableIcon<T>(T item)
        {
            if (item is PrioRoleEnum)
            {
                PrioRoleEnum tmp = (PrioRoleEnum)Convert.ChangeType(item, typeof(PrioRoleEnum));
                return _roles[tmp];
            }
            if (item is PrioTrinityEnum)
            {
                PrioTrinityEnum tmp = (PrioTrinityEnum)Convert.ChangeType(item, typeof(PrioTrinityEnum));
                return _trinity[tmp];
            }
            if (item is PrioJobEnum)
            {
                PrioJobEnum tmp = (PrioJobEnum)Convert.ChangeType(item, typeof(PrioJobEnum));
                return _jobs[tmp];
            }
            if (item is int)
            {
                int tmp = (int)Convert.ChangeType(item, typeof(int));
                return _misc[(MiscIconEnum)10 + tmp];
            }
            if (item is string)
            {
                return null;
            }
            return null;
        }

        private void RenderOrderableItem<T>(T item, int index, float x, float y)
        {
            Vector2 btnsize = new Vector2(160, 50);
            Vector2 icosize = new Vector2(30, 30);
            Vector2 pt = ImGui.GetCursorPos();
            string text = TranslateOrderableItem(item);
            IDalamudTextureWrap? icon = RetrieveOrderableIcon(item);
            float icospace = 0.0f;
            if (icon != null)
            {
                ImGui.SetCursorPos(new Vector2(x + (btnsize.X - icosize.X) - 5.0f, y + (btnsize.Y / 2.0f) - (icosize.Y / 2.0f)));
                ImGui.Image(icon.ImGuiHandle, icosize);
                icospace = icosize.X;
            }
            ImGui.SetCursorPos(new Vector2(x + 5.0f, y + (btnsize.Y / 2.0f) - ImGui.GetFontSize() / 2.0f));
            ImGui.Text((index + 1).ToString());
            ImGui.SetCursorPos(new Vector2(x + (btnsize.X - icospace - 10.0f) - ImGui.CalcTextSize(text).X, y + (btnsize.Y / 2.0f) - (ImGui.GetFontSize() / 2.0f)));
            ImGui.Text(text);
            ImGui.SetCursorPos(pt);
        }

        internal void RenderOrderableList<T>(List<T> items)
        {
            Vector2 maxsize = ImGui.GetContentRegionAvail();
            Vector2 btnsize = new Vector2(160, 50);
            Vector2 margin = new Vector2(10, 10);
            Vector2 icosize = new Vector2(30, 30);
            float x = 0.0f, y = 0.0f;
            Vector2 curpos = ImGui.GetCursorPos();
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
            int perRow = (int)Math.Floor(maxsize.X / (btnsize.X + margin.X));
            perRow = Math.Clamp(perRow, 1, items.Count > 0 ? items.Count : 1);
            int itemx = 1, itemy = 1;
            bool isStillDragging = false;
            Vector2 screenpos = ImGui.GetCursorScreenPos();
            float time = (float)((DateTime.Now - _loaded).TotalMilliseconds / 300.0);
            Vector4 hilite = new Vector4(1.0f, 1.0f, 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)), 0.5f + 0.5f * (float)Math.Abs(Math.Cos(time)));
            for (int i = 0; i < items.Count; i++)
            {
                T p = items[i];
                string temp = "##OrderableList_" + i;
                Vector2 curItem = new Vector2(curpos.X + x, curpos.Y + y);
                ImGui.SetCursorPos(curItem);
                ImGui.Selectable(temp, true, ImGuiSelectableFlags.None, btnsize);
                x += btnsize.X + margin.X;
                if (ImGui.IsItemHovered() == true && ImGui.IsItemActive() == false)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18n.Translate("Misc/DragToReorderPrio"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemActive() == true)
                {
                    isStillDragging = true;
                    if (_isDragging == false)
                    {
                        _isDragging = true;
                        _dragObject = items;
                        _dragStartItem = i;
                    }
                    ImDrawListPtr dl = ImGui.GetWindowDrawList();
                    if (ImGui.IsItemHovered() == false)
                    {
                        Vector2 mpos = ImGui.GetMousePos();
                        int xpos = (int)Math.Floor((mpos.X - screenpos.X) / (btnsize.X + margin.X));
                        int ypos = (int)Math.Floor((mpos.Y - screenpos.Y) / (btnsize.Y + margin.Y));
                        xpos = Math.Clamp(xpos, 0, perRow - 1);
                        ypos = Math.Clamp(ypos, 0, (int)Math.Ceiling((float)items.Count / perRow) - 1);
                        int xtpos = (int)Math.Floor((mpos.X - screenpos.X + (btnsize.X / 2.0f)) / (btnsize.X + margin.X));
                        int jpos, ipos = (ypos * perRow) + xpos;
                        ipos = Math.Clamp(ipos, 0, items.Count);
                        int lim = perRow * (int)Math.Floor((float)ipos / perRow);
                        lim = items.Count - lim;
                        xpos = Math.Clamp(xpos, 0, lim - 1);
                        ipos = (ypos * perRow) + xpos;
                        if (i != ipos)
                        {
                            jpos = (xtpos > xpos) ? 1 : -1;
                            float cpx = (xpos * (btnsize.X + margin.X) + screenpos.X);
                            float cpy = (ypos * (btnsize.Y + margin.Y) + screenpos.Y);
                            if (jpos < 0)
                            {
                                dl.AddLine(new Vector2(cpx, cpy), new Vector2(cpx, cpy + btnsize.Y), ImGui.GetColorU32(hilite), 3.0f);
                                dl.AddTriangleFilled(
                                    new Vector2(cpx, cpy),
                                    new Vector2(cpx + 10.0f, cpy),
                                    new Vector2(cpx, cpy + 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                dl.AddTriangleFilled(
                                    new Vector2(cpx, cpy + btnsize.Y),
                                    new Vector2(cpx + 10.0f, cpy + btnsize.Y),
                                    new Vector2(cpx, cpy + btnsize.Y - 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                _dragEndItem = ipos;
                            }
                            else
                            {
                                dl.AddLine(new Vector2(cpx + btnsize.X, cpy), new Vector2(cpx + btnsize.X, cpy + btnsize.Y), ImGui.GetColorU32(hilite), 3.0f);
                                dl.AddTriangleFilled(
                                    new Vector2(cpx + btnsize.X, cpy),
                                    new Vector2(cpx + btnsize.X - 10.0f, cpy),
                                    new Vector2(cpx + btnsize.X, cpy + 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                dl.AddTriangleFilled(
                                    new Vector2(cpx + btnsize.X, cpy + btnsize.Y),
                                    new Vector2(cpx + btnsize.X - 10.0f, cpy + btnsize.Y),
                                    new Vector2(cpx + btnsize.X, cpy + btnsize.Y - 10.0f),
                                    ImGui.GetColorU32(hilite)
                                );
                                _dragEndItem = ipos + 1;
                            }
                        }
                    }
                    else
                    {
                        _dragEndItem = _dragStartItem;
                    }
                    {
                        Vector2 cpos = Vector2.Add(ImGui.GetWindowPos(), curItem);
                        cpos = Vector2.Subtract(cpos, new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()));
                        dl.AddRect(cpos, new Vector2(cpos.X + btnsize.X, cpos.Y + btnsize.Y), ImGui.GetColorU32(hilite), 0.0f, ImDrawFlags.None, 2.0f);
                    }
                }
                itemx++;
                if (itemx > perRow)
                {
                    x = 0.0f;
                    y += btnsize.Y + margin.Y;
                    itemy++;
                    itemx = 1;
                }
                else if (i < items.Count - 1)
                {
                    ImGui.SameLine();
                }
                RenderOrderableItem(p, i, curItem.X, curItem.Y);
            }
            ImGui.PopStyleVar();
            if (isStillDragging == false && _dragObject == items)
            {
                _dragEndItem = Math.Clamp(_dragEndItem, 0, items.Count);
                _isDragging = false;
                if (_dragStartItem != _dragEndItem)
                {
                    var tmp = items[_dragStartItem];
                    items.Remove(tmp);
                    items.Insert(_dragEndItem >= _dragStartItem ? _dragEndItem - 1 : _dragEndItem, tmp);
                }
                _dragStartItem = 0;
                _dragEndItem = 0;
                _dragObject = null;
            }
        }

        internal static void KeepWindowInSight()
        {
            Vector2 pt = ImGui.GetWindowPos();
            Vector2 szy = ImGui.GetWindowSize();
            bool moved = false;
            Vector2 szx = ImGui.GetIO().DisplaySize;
            if (szy.X > szx.X || szy.Y > szx.Y)
            {
                szy.X = Math.Min(szy.X, szx.X);
                szy.Y = Math.Min(szy.Y, szx.Y);
                ImGui.SetWindowSize(szy);
            }
            if (pt.X < 0)
            {
                pt.X += (0.0f - pt.X) / 5.0f;
                moved = true;
            }
            if (pt.Y < 0)
            {
                pt.Y += (0.0f - pt.Y) / 5.0f;
                moved = true;
            }
            if (pt.X + szy.X > szx.X)
            {
                pt.X -= ((pt.X + szy.X) - szx.X) / 5.0f;
                moved = true;
            }
            if (pt.Y + szy.Y > szx.Y)
            {
                pt.Y -= ((pt.Y + szy.Y) - szx.Y) / 5.0f;
                moved = true;
            }
            if (moved == true)
            {
                ImGui.SetWindowPos(pt);
            }
        }

        internal void RenderWarning(string text)
        {
            Vector2 tenp = ImGui.GetCursorPos();
            IDalamudTextureWrap tw = GetMiscIcon(UserInterface.MiscIconEnum.Exclamation);
            ImGui.Image(
                tw.ImGuiHandle, new Vector2(tw.Width, tw.Height)
            );
            Vector2 anp1 = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(tenp.X + tw.Width + 10, tenp.Y));
            ImGui.TextWrapped(text);
            Vector2 anp2 = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(tenp.X, Math.Max(anp1.Y, anp2.Y)));
        }

    }

}
