using System.Globalization;

namespace SalsaNOW.Core.Shell;

public static class AccentColor
{
    public static bool TryParse(string? value, out int abgr)
    {
        abgr = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string hex = value!.Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            return false;

        uint red = (uint)(rgb >> 16) & 0xFF;
        uint green = (uint)(rgb >> 8) & 0xFF;
        uint blue = (uint)rgb & 0xFF;
        abgr = unchecked((int)(0xFF000000u | (blue << 16) | (green << 8) | red));
        return true;
    }

    public static byte[] BuildPalette(int abgr)
    {
        byte red = (byte)(abgr & 0xFF);
        byte green = (byte)((abgr >> 8) & 0xFF);
        byte blue = (byte)((abgr >> 16) & 0xFF);

        var palette = new byte[32];
        for (int i = 0; i < 8; i++)
        {
            palette[i * 4] = red;
            palette[i * 4 + 1] = green;
            palette[i * 4 + 2] = blue;
            palette[i * 4 + 3] = 0xFF;
        }

        return palette;
    }
}
