using UnityEngine;

public static class ColorHexTemplate
{
    public const string CT_HEX_FFFFFF = "#FFFFFF";  // Color(255, 255, 255)
    public const string CT_HEX_FF3333 = "#FF3333";  // Color(255, 51, 51)
    public const string CT_HEX_80FF80 = "#80FF80";  // Color(128, 255, 128)
    public const string CT_HEX_FF801A = "#FF801A";  // Color(255, 128, 26)
    public const string CT_HEX_FFFF99 = "#FFFF99";  // Color(255, 255, 153)
    public const string CT_HEX_ADD8E6 = "#ADD8E6";  // Color(173, 216, 230)
    public const string CT_HEX_FF6347 = "#FF6347";  // Color(255, 99, 71)
    public const string CT_HEX_FF4D4D = "#FF4D4D";  // Color(255, 77, 77)
    public const string CT_HEX_FF9696 = "#FF9696";  // Color(255, 150, 150)
    public const string CT_HEX_888888 = "#888888";

    public static Color CT_FF3333 => CT_HEX_FF3333.ToColor();
    public static Color CT_80FF80 => CT_HEX_80FF80.ToColor();
    public static Color CT_FF801A => CT_HEX_FF801A.ToColor();

    private static Color ToColor(this string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
}