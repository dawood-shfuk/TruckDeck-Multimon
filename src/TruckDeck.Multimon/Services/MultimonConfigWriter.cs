using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public static class MultimonConfigWriter
    {
        static int _idCounter;

        public static string Generate(IList<ViewportDefinition> viewports)
        {
            if (viewports == null || viewports.Count == 0)
                throw new InvalidOperationException("At least one viewport is required.");

            _idCounter = 0;
            var rootId = NextId();
            var monitorIds = viewports.Select(_ => NextId()).ToList();
            var uiAnchor = NormalizedCoordCalculator.FindUiAnchor(viewports);

            var sb = new StringBuilder();
            sb.AppendLine("SiiNunit");
            sb.AppendLine("{");
            sb.AppendLine($"multimon_config : {rootId} {{");
            sb.AppendLine($" normalized_ui_x: {Format(uiAnchor.Normalized.X)}");
            sb.AppendLine($" normalized_ui_width: {Format(uiAnchor.Normalized.Width)}");
            sb.AppendLine($" monitors: {viewports.Count}");

            for (var i = 0; i < monitorIds.Count; i++)
                sb.AppendLine($" monitors[{i}]: {monitorIds[i]}");

            sb.AppendLine("}");

            for (var i = 0; i < viewports.Count; i++)
                WriteMonitorConfig(sb, monitorIds[i], viewports[i], MakeUniqueName(viewports, i));

            sb.AppendLine("}");
            return sb.ToString();
        }

        static string MakeUniqueName(IList<ViewportDefinition> viewports, int index)
        {
            var baseName = viewports[index].Name;
            var count = 0;
            for (var i = 0; i <= index; i++)
            {
                if (viewports[i].Name == baseName)
                    count++;
            }
            return count > 1 ? baseName + count : baseName;
        }

        static void WriteMonitorConfig(StringBuilder sb, string id, ViewportDefinition viewport, string uniqueName)
        {
            var n = viewport.Normalized;
            sb.AppendLine();
            sb.AppendLine($"monitor_config : {id} {{");
            sb.AppendLine($" name: {uniqueName}");
            sb.AppendLine($" normalized_x: {Format(n.X)}");
            sb.AppendLine($" normalized_y: {Format(n.Y)}");
            sb.AppendLine($" normalized_width: {Format(n.Width)}");
            sb.AppendLine($" normalized_height: {Format(n.Height)}");
            sb.AppendLine(" horizontal_fov_relative_offset: 0.000000");
            sb.AppendLine(" vertical_fov_relative_offset: 0.000000");
            sb.AppendLine($" heading_offset: {Format(viewport.HeadingOffset)}");
            sb.AppendLine($" pitch_offset: {Format(viewport.PitchOffset)}");
            sb.AppendLine($" roll_offset: {Format(viewport.RollOffset)}");
            sb.AppendLine(" camera_space_offset: (0.000000, 0.000000, 0.000000)");
            sb.AppendLine($" horizontal_fov_override: {Format(viewport.HorizontalFovOverride)}");
            sb.AppendLine($" vertical_fov_override: {Format(viewport.VerticalFovOverride)}");
            sb.AppendLine(" frustum_subrect_x: 0.000000");
            sb.AppendLine(" frustum_subrect_y: 0.000000");
            sb.AppendLine(" frustum_subrect_width: 1.000000");
            sb.AppendLine(" frustum_subrect_height: 1.000000");
            sb.AppendLine($" render_interior: {viewport.RenderInterior.ToString().ToLowerInvariant()}");
            sb.AppendLine($" render_exterior: {viewport.RenderExterior.ToString().ToLowerInvariant()}");
            sb.AppendLine("}");
        }

        static string NextId()
        {
            _idCounter++;
            return "_nameless.TDM." + _idCounter.ToString("0000", CultureInfo.InvariantCulture);
        }

        static string Format(float value) =>
            value.ToString("0.000000", CultureInfo.InvariantCulture);
    }
}
