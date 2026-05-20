using System.Numerics;
using ImGuiNET;

namespace canmarket.src.GUI
{
    internal static class ImGuiTheme
    {
        private const int ColorCount = 21;
        private const int VarCount   = 7;

        public static void Push()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,  4f);
            ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding,   4f);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding,  5f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,  new Vector2(12f, 10f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,   new Vector2(6f,  3f));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,    new Vector2(8f,  6f));

            ImGui.PushStyleColor(ImGuiCol.WindowBg,          new Vector4(0.13f, 0.09f, 0.06f, 0.97f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg,           new Vector4(0.22f, 0.14f, 0.09f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive,     new Vector4(0.30f, 0.20f, 0.12f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.Button,            new Vector4(0.38f, 0.24f, 0.14f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered,     new Vector4(0.52f, 0.33f, 0.20f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,      new Vector4(0.28f, 0.18f, 0.10f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg,           new Vector4(0.18f, 0.12f, 0.08f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered,    new Vector4(0.25f, 0.17f, 0.10f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive,     new Vector4(0.30f, 0.20f, 0.13f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.SliderGrab,        new Vector4(0.82f, 0.63f, 0.23f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive,  new Vector4(0.95f, 0.75f, 0.30f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg,       new Vector4(0.10f, 0.07f, 0.04f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab,     new Vector4(0.40f, 0.26f, 0.15f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.Header,            new Vector4(0.35f, 0.22f, 0.13f, 0.80f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered,     new Vector4(0.45f, 0.29f, 0.17f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.PopupBg,           new Vector4(0.13f, 0.09f, 0.06f, 0.97f));
            ImGui.PushStyleColor(ImGuiCol.Separator,         new Vector4(0.52f, 0.36f, 0.20f, 0.80f));
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered,  new Vector4(0.70f, 0.50f, 0.28f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.Text,              new Vector4(0.95f, 0.88f, 0.75f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TextDisabled,      new Vector4(0.60f, 0.50f, 0.38f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.CheckMark,         new Vector4(0.47f, 0.88f, 0.18f, 1.00f));
        }

        public static void Pop()
        {
            ImGui.PopStyleColor(ColorCount);
            ImGui.PopStyleVar(VarCount);
        }

        public static readonly Vector4 ColorGold    = new(0.82f, 0.70f, 0.35f, 1f);
        public static readonly Vector4 ColorGreen   = new(0.50f, 1.00f, 0.50f, 1f);
        public static readonly Vector4 ColorYellow  = new(1.00f, 0.85f, 0.30f, 1f);
        public static readonly Vector4 ColorRed     = new(1.00f, 0.45f, 0.45f, 1f);
        public static readonly Vector4 ColorArrow   = new(0.60f, 0.50f, 0.38f, 1f);

        /// <summary>Picks stock label color: red=empty, yellow=below 30% of max, green=otherwise.</summary>
        public static Vector4 StockColor(int stock, int maxStock, int unlimitedSentinel)
        {
            if (stock == unlimitedSentinel) return ColorGreen;
            if (stock <= 0) return ColorRed;
            if (maxStock != unlimitedSentinel && maxStock > 0 && stock < maxStock * 0.3f) return ColorYellow;
            return ColorGreen;
        }

        public static void SectionHeader(string text)
        {
            ImGui.TextColored(ColorGold, text);
            ImGui.Separator();
            ImGui.Spacing();
        }

        /// <summary>Draws a centered arrow via DrawList primitives and advances cursor by its width.</summary>
        public static void DrawArrow(int slotSize)
        {
            const float arrowW = 22f;
            Vector2 pos = ImGui.GetCursorScreenPos();
            float cy  = pos.Y + slotSize * 0.5f;
            float x0  = pos.X + 3f;
            float x1  = x0 + 9f;
            uint  col = ImGui.GetColorU32(ColorArrow);
            var   dl  = ImGui.GetWindowDrawList();
            dl.AddLine(new Vector2(x0, cy), new Vector2(x1, cy), col, 1.5f);
            dl.AddTriangleFilled(
                new Vector2(x1,       cy - 4f),
                new Vector2(x1,       cy + 4f),
                new Vector2(x1 + 6f,  cy), col);
            ImGui.SetCursorScreenPos(new Vector2(pos.X + arrowW, pos.Y));
        }
    }
}
