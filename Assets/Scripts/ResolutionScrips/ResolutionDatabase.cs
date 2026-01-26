using System.Collections.Generic;
using UnityEngine;

public static class ResolutionDatabase
{
    public enum AspectGroup { Native, _16x9, _16x10, _21x9, _32x9, _3x2, _4x3, _5x4 }

    public struct Res
    {
        public int w, h; public string label; public AspectGroup group;
        public Res(int w, int h, AspectGroup g, string label = null)
        { this.w = w; this.h = h; this.group = g; this.label = label ?? $"{w}¡¿{h}"; }
    }

    public static readonly List<Res> All = new()
    {
        // 16:9
        new Res( 960,  540, AspectGroup._16x9),
        new Res(1280,  720, AspectGroup._16x9),
        new Res(1366,  768, AspectGroup._16x9),
        new Res(1600,  900, AspectGroup._16x9),
        new Res(1920, 1080, AspectGroup._16x9),
        new Res(2560, 1440, AspectGroup._16x9),
        new Res(3200, 1800, AspectGroup._16x9),
        new Res(3840, 2160, AspectGroup._16x9),
        new Res(5120, 2880, AspectGroup._16x9),

        // 16:10
        new Res(1280,  800, AspectGroup._16x10),
        new Res(1440,  900, AspectGroup._16x10),
        new Res(1680, 1050, AspectGroup._16x10),
        new Res(1920, 1200, AspectGroup._16x10),
        new Res(2560, 1600, AspectGroup._16x10),
        new Res(2880, 1800, AspectGroup._16x10),
        new Res(3840, 2400, AspectGroup._16x10),
        new Res(5120, 3200, AspectGroup._16x10),

        // 21:9
        new Res(2560, 1080, AspectGroup._21x9),
        new Res(3440, 1440, AspectGroup._21x9),
        new Res(3840, 1600, AspectGroup._21x9),
        new Res(5120, 2160, AspectGroup._21x9),

        // 32:9
        new Res(3840, 1080, AspectGroup._32x9),
        new Res(5120, 1440, AspectGroup._32x9),
        new Res(7680, 2160, AspectGroup._32x9),

        // 3:2
        new Res(2160, 1440, AspectGroup._3x2),
        new Res(3000, 2000, AspectGroup._3x2),
        new Res(3240, 2160, AspectGroup._3x2),

        // 4:3
        new Res(1024,  768, AspectGroup._4x3),
        new Res(1280,  960, AspectGroup._4x3),
        new Res(1600, 1200, AspectGroup._4x3),

        // 5:4
        new Res(1280, 1024, AspectGroup._5x4),
        new Res(2560, 2048, AspectGroup._5x4),
    };

    public static AspectGroup GuessGroupFromWH(int w, int h)
    {
        float a = (float)w / h;
        if (Mathf.Abs(a - 16f / 9f) < 0.02f) return AspectGroup._16x9;
        if (Mathf.Abs(a - 16f / 10f) < 0.02f) return AspectGroup._16x10;
        if (Mathf.Abs(a - 21f / 9f) < 0.02f) return AspectGroup._21x9;
        if (Mathf.Abs(a - 32f / 9f) < 0.02f) return AspectGroup._32x9;
        if (Mathf.Abs(a - 3f / 2f) < 0.02f) return AspectGroup._3x2;
        if (Mathf.Abs(a - 4f / 3f) < 0.02f) return AspectGroup._4x3;
        if (Mathf.Abs(a - 5f / 4f) < 0.02f) return AspectGroup._5x4;
        return AspectGroup.Native;
    }
}
