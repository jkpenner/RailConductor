using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

public static class TrackEditorDrawer
{
    public static void DrawTrack(Control overlay, PluginContext ctx)
    {
        foreach (var platform in ctx.TrackData.GetPlatforms())
        {
            DrawTrackPlatform(overlay, ctx, platform);
        }
        
        foreach (var link in ctx.TrackData.GetLinks())
        {
            DrawTrackLink(overlay, ctx, link);
        }

        foreach (var node in ctx.TrackData.GetNodes())
        {
            DrawTrackNode(overlay, ctx, node);
        }

        foreach (var signal in ctx.TrackData.GetSignals())
        {
            DrawTrackSignal(overlay, ctx, signal);
        }
    }

    public static Font GetFont() => ResourceLoader.Load<Font>(PluginSettings.FontPath);

    public static void DrawTrackPlatform(Control overlay, PluginContext ctx, PlatformData platform)
    {
        var scale = PluginUtility.GetZoom();

        var globalPosition = ctx.Track.ToGlobal(platform.Position);
        var center = PluginUtility.WorldToScreen(globalPosition);
        
        var size = platform.IsVertical 
            ? PluginSettings.PlatformVerticalSize 
            : PluginSettings.PlatformHorizontalSize;

        if (ctx.IsSelected(platform.Id))
        {
            var offset = new Vector2(2f, 2f);
            var selectedSize = (size + offset) * scale;
            overlay.DrawRect(new Rect2(center - (offset * 0.5f * scale), selectedSize), PluginSettings.SelectedColor);
        }

        var color = GetColor(ctx, platform.Id,
            PluginSettings.LinkNormalColor,
            PluginSettings.LinkHoverColor,
            PluginSettings.LinkDisabledColor);

        overlay.DrawRect(new Rect2(center, size * scale), color);

        // Draw the display name label
        var labelOffset = center - size * 0.5f;
        var labelSize = new Vector2(20f, 4f) * scale;
        
        overlay.DrawRect(new Rect2(center - labelOffset, labelSize), Colors.White);
        overlay.DrawString(GetFont(), 
            center - labelOffset,
            platform.DisplayName,
            alignment: HorizontalAlignment.Center,
            fontSize: (int)(4f * scale),
            modulate: Colors.Black,
            width: 60f);
    }

    public static void DrawTrackLink(Control overlay, PluginContext ctx, TrackLinkData link)
    {
        var nodeA = ctx.TrackData.GetNode(link.NodeAId);
        var nodeB = ctx.TrackData.GetNode(link.NodeBId);

        if (nodeA is null || nodeB is null)
        {
            return;
        }

        var scale = PluginUtility.GetZoom();

        var globalPosition1 = ctx.Track.ToGlobal(nodeA.Position);
        var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);

        var globalPosition2 = ctx.Track.ToGlobal(nodeB.Position);
        var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);

        if (ctx.IsSelected(link.Id))
        {
            overlay.DrawLine(screenPosition1, screenPosition2,
                PluginSettings.SelectedColor, (PluginSettings.LinkWidth + 2) * scale);
        }

        var color = GetColor(ctx, link.Id,
            PluginSettings.LinkNormalColor,
            PluginSettings.LinkHoverColor,
            PluginSettings.LinkDisabledColor);

        overlay.DrawLine(screenPosition1, screenPosition2, color, PluginSettings.LinkWidth * scale);

        var font = ResourceLoader.Load<Font>("res://addons/rail_conductor/fonts/default.tres");
        if (font is not null)
        {
            var center = screenPosition1.Lerp(screenPosition2, 0.5f);
            overlay.DrawRect(new Rect2(center - new Vector2(8f, 2f) * scale, new Vector2(16f, 4f) * scale),
                Colors.White);
            overlay.DrawString(font, center + new Vector2(-50f, (1.5f * scale)),
                link.Id[0..3].ToUpper(),
                alignment: HorizontalAlignment.Center,
                fontSize: (int)(4f * scale),
                modulate: Colors.Black,
                width: 100);
        }
    }

    public static void DrawTrackNode(Control overlay, PluginContext ctx, TrackNodeData node)
    {
        var scale = PluginUtility.GetZoom();
        var globalPosition = ctx.Track.ToGlobal(node.Position);
        var screenPosition = PluginUtility.WorldToScreen(globalPosition);

        if (ctx.IsSelected(node.Id))
        {
            overlay.DrawCircle(screenPosition, (PluginSettings.NodeRadius + 2) * scale,
                PluginSettings.SelectedColor);
        }

        var color = GetColor(ctx, node.Id,
            PluginSettings.NodeNormalColor,
            PluginSettings.NodeHoverColor,
            PluginSettings.NodeDisabledColor);

        overlay.DrawCircle(screenPosition, PluginSettings.NodeRadius * scale, color);

        var fillColor = GetColor(ctx, node.Id,
            PluginSettings.NodeFillNormalColor,
            PluginSettings.NodeFillHoverColor,
            PluginSettings.NodeFillDisabledColor);

        overlay.DrawCircle(screenPosition, (PluginSettings.NodeRadius - 2) * scale, fillColor);

        var font = ResourceLoader.Load<Font>("res://addons/rail_conductor/fonts/default.tres");
        if (font is not null)
        {
            overlay.DrawString(font, screenPosition + new Vector2(-50f, (1.5f * scale)),
                node.Id[0..3].ToUpper(),
                alignment: HorizontalAlignment.Center,
                fontSize: (int)(4f * scale),
                modulate: Colors.Black,
                width: 100);
        }

        if (node.NodeType != TrackNodeType.Switch && node.NodeType != TrackNodeType.Crossover)
        {
            return;
        }

        HashSet<string> drawn = [];
        for (var i = 0; i < node.PairedLinks.Count; i++)
        {
            var pair = node.PairedLinks[i];
            var linkA = ctx.TrackData.GetLink(pair.LinkAId);
            var linkB = ctx.TrackData.GetLink(pair.LinkBId);
            if (linkA is null || linkB is null)
            {
                continue;
            }

            var linkColor = i == 0 ? PluginSettings.SwitchPrimaryColor : PluginSettings.SwitchSecondaryColor;

            var linkANode = ctx.TrackData.GetNode(linkA.GetOtherNode(node.Id));
            if (linkANode is not null && drawn.Add(linkA.Id))
            {
                var direction = (linkANode.Position - node.Position).Normalized();
                var position = node.Position + direction * (PluginSettings.NodeRadius + 3);

                var linkGlobalPosition = ctx.Track.ToGlobal(position);
                var linkScreenPosition = PluginUtility.WorldToScreen(linkGlobalPosition);

                overlay.DrawCircle(linkScreenPosition, 2 * scale, linkColor);
            }

            var linkBNode = ctx.TrackData.GetNode(linkB.GetOtherNode(node.Id));
            if (linkBNode is not null && drawn.Add(linkB.Id))
            {
                var direction = (linkBNode.Position - node.Position).Normalized();
                var position = node.Position + direction * (PluginSettings.NodeRadius + 3);

                var linkGlobalPosition = ctx.Track.ToGlobal(position);
                var linkScreenPosition = PluginUtility.WorldToScreen(linkGlobalPosition);

                overlay.DrawCircle(linkScreenPosition, 2 * scale, linkColor);
            }
        }
    }

    public static void DrawTrackSignal(Control overlay, PluginContext ctx, SignalData signal)
    {
        var orientation = ctx.TrackData.GetSignalPosition(signal);
        if (orientation is null)
        {
            GD.PushWarning("Failed to get orientation of track signal.");
            return;
        }

        var scale = PluginUtility.GetZoom();

        var (position, angle) = orientation.Value;
        var signalGlobalPosition = ctx.Track.ToGlobal(position);
        var signalScreenPosition = PluginUtility.WorldToScreen(signalGlobalPosition);

        if (ctx.IsSelected(signal.Id))
        {
            overlay.DrawCircle(signalScreenPosition, 4f * scale, PluginSettings.SelectedColor);
            overlay.DrawArc(signalScreenPosition,
                5 * scale,
                angle + Mathf.DegToRad(140f),
                angle + Mathf.DegToRad(220f),
                6,
                PluginSettings.SelectedColor,
                2 * scale);
        }

        var color = GetColor(ctx, signal.Id,
            PluginSettings.SignalNormalColor,
            PluginSettings.SignalHoverColor,
            PluginSettings.SignalDisabledColor);

        overlay.DrawCircle(signalScreenPosition, 3f * scale, color);
        overlay.DrawArc(signalScreenPosition,
            4 * scale,
            angle + Mathf.DegToRad(150f),
            angle + Mathf.DegToRad(210f),
            6,
            color,
            2 * scale);
    }

    private static Color GetColor(PluginContext ctx, string id, Color normal, Color hover, Color disabled)
    {
        if (!ctx.IsSelectable(id))
        {
            return disabled;
        }

        return ctx.IsHovered(id) ? hover : normal;
    }
}