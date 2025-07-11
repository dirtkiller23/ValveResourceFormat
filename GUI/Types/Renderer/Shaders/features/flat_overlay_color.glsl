#version 460

uniform vec4 g_vFlatOverlayColor;

void ApplyFlatOverlayColor(inout vec4 pixelColor)
{
    if (g_vFlatOverlayColor.a <= 0.0)
    {
        return;
    }

    float checkerBoard = mod(gl_FragCoord.x + gl_FragCoord.y, 2);
    pixelColor.rgb = mix(pixelColor.rgb, g_vFlatOverlayColor.rgb, g_vFlatOverlayColor.a * checkerBoard * 2.0);
}
