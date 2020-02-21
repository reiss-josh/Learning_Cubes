using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Density
{
    private static float freq = 3.0f;

    public static float Sample(Vector3 ws)
    {
        float density = -ws.y;
        density += Noise.Perlin3D(ws, freq);
        density += Noise.Perlin3D(ws * 2, freq) * 0.5f;
        return density;
    }
}
