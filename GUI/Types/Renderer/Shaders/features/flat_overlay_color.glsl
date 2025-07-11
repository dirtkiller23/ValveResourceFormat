#version 460

uniform vec4 g_vFlatOverlayColor;

void ApplyFlatOverlayColor(inout vec4 pixelColor, in MaterialProperties_t mat)
{
    if (g_vFlatOverlayColor.a <= 0.0)
    {
        return;
    }

    float checkerBoard = mod(gl_FragCoord.x + gl_FragCoord.y, 2);
    float rimBoost = pow2(1.0 - dot(mat.Normal, mat.ViewDir))  * 0.5;
    pixelColor.rgb = mix(pixelColor.rgb, g_vFlatOverlayColor.rgb, g_vFlatOverlayColor.a * checkerBoard + rimBoost);
}
