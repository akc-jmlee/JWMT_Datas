using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace JWMT_Datas;

/// <summary>패널 위 홀 분포와 Unit 번호를 그린다.</summary>
public sealed class UnitMapRenderer
{
    public float PanelWidth = 510000f;
    public float PanelHeight = 515000f;

    /// <summary>참이면 패널 크기 대신 데이터 범위에 맞춰 축을 잡는다.</summary>
    public bool AutoFit;

    /// <summary>1 이면 전체가 보이는 배율. 크면 확대.</summary>
    public float Zoom = 1f;

    /// <summary>화면 중앙에 놓을 데이터 좌표. null 이면 패널 중앙.</summary>
    public PointF? ViewCenter;

    // 화면 좌표 <-> 데이터 좌표 변환에 쓰라고 마지막 렌더 상태를 남긴다.
    public float LastScale { get; private set; } = 1f;
    public float LastFitScale { get; private set; } = 1f;
    public PointF LastOrigin { get; private set; }
    public Rectangle LastPlot { get; private set; }

    private static readonly Color HoleColor = Color.FromArgb(150, 150, 150);
    private static readonly Color LabelColor = Color.FromArgb(0, 0, 205);
    private static readonly Color PanelEdgeColor = Color.FromArgb(190, 60, 60);

    /// <summary>화면 픽셀을 데이터 좌표로 되돌린다.</summary>
    public PointF ToData(Point pixel) => new(
        (pixel.X - LastOrigin.X) / LastScale,
        (LastOrigin.Y - pixel.Y) / LastScale);

    public Bitmap Render(ReportData data, int width, int height)
    {
        width = Math.Max(400, width);
        height = Math.Max(400, height);

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // 왼쪽은 눈금 숫자, 위쪽은 제목 두 줄이 들어갈 자리다.
            var plot = new Rectangle(90, 66, width - 120, height - 128);
            LastPlot = plot;
            if (plot.Width < 50 || plot.Height < 50) return bmp;

            float axisMaxX = AutoFit ? NiceCeiling(data.MaxX) : PanelWidth;
            float axisMaxY = AutoFit ? NiceCeiling(data.MaxY) : PanelHeight;
            if (axisMaxX <= 0) axisMaxX = 1;
            if (axisMaxY <= 0) axisMaxY = 1;

            // 가로/세로 배율을 같게 두어야 패널 비율이 왜곡되지 않는다.
            // 딱 맞추면 판넬 테두리가 축 프레임에 겹쳐 안 보이므로 살짝 여백을 둔다.
            float fitScale = Math.Min(plot.Width / axisMaxX, plot.Height / axisMaxY) * 0.95f;
            float scale = fitScale * Math.Max(0.05f, Zoom);
            // 한 번 그린 뒤에는 중심이 확정돼 있어야 줌/패닝 계산이 단순해진다.
            ViewCenter ??= new PointF(axisMaxX / 2f, axisMaxY / 2f);
            PointF center = ViewCenter.Value;

            // 뷰 중심이 플롯 영역 한가운데에 오도록 원점을 잡는다.
            float originX = plot.Left + plot.Width / 2f - center.X * scale;
            float originY = plot.Top + plot.Height / 2f + center.Y * scale;

            LastFitScale = fitScale;
            LastScale = scale;
            LastOrigin = new PointF(originX, originY);

            DrawTitle(g, data, width);
            DrawHoles(bmp, data, originX, originY, scale, plot, scale / fitScale);
            DrawPanelOutline(g, plot, scale, originX, originY);
            DrawAxes(g, plot, scale, originX, originY);
            DrawUnitLabels(g, data, originX, originY, scale, plot);
        }
        return bmp;
    }

    private static void DrawTitle(Graphics g, ReportData data, int width)
    {
        using var titleFont = new Font("Malgun Gothic", 10.5f);
        using var subFont = new Font("Malgun Gothic", 10f);
        using var brush = new SolidBrush(Color.Black);
        using var fmt = new StringFormat { Alignment = StringAlignment.Center };

        string title = data.SourceName + " (Unit Info Map)";
        string sub = data.MaxUnit > 0
            ? "Unit " + data.MinUnit + "-" + data.MaxUnit
            : data.Count.ToString("N0") + " holes";

        g.DrawString(title, titleFont, brush, new RectangleF(0, 12, width, 22), fmt);
        g.DrawString(sub, subFont, brush, new RectangleF(0, 36, width, 22), fmt);
    }

    /// <summary>
    /// 홀이 120만 개까지 나오므로 Graphics 로 하나씩 찍으면 수십 초가 걸린다.
    /// 비트맵 메모리에 직접 써서 점 개수와 무관하게 한 번에 끝낸다.
    /// </summary>
    private static void DrawHoles(Bitmap bmp, ReportData data, float originX, float originY,
                                  float scale, Rectangle plot, float zoom)
    {
        // 확대하면 점이 흩어져 안 보이므로 굵기를 조금 키운다.
        int dot = zoom >= 8f ? 3 : zoom >= 3f ? 2 : 1;

        var bits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
        try
        {
            int argb = HoleColor.ToArgb();
            unsafe
            {
                byte* basePtr = (byte*)bits.Scan0;
                for (int i = 0; i < data.X.Length; i++)
                {
                    // 화면 Y 는 아래로 증가하므로 뒤집어 찍는다(데이터는 좌하단 원점).
                    int px = (int)(originX + data.X[i] * scale);
                    int py = (int)(originY - data.Y[i] * scale);
                    if (px < plot.Left || px + dot > plot.Right ||
                        py < plot.Top || py + dot > plot.Bottom) continue;

                    for (int dy = 0; dy < dot; dy++)
                    {
                        int* row = (int*)(basePtr + (py + dy) * bits.Stride);
                        for (int dx = 0; dx < dot; dx++) row[px + dx] = argb;
                    }
                }
            }
        }
        finally
        {
            bmp.UnlockBits(bits);
        }
    }

    /// <summary>
    /// 실제 판넬 경계를 데이터 좌표 (0,0)~(PanelWidth, PanelHeight) 로 그린다.
    /// 축 프레임은 보이는 영역일 뿐이라 판넬 가장자리와 다르다. 유닛 배열이 판넬
    /// 어디에 놓였는지(여백이 고른지) 보려면 이 선이 있어야 한다.
    /// </summary>
    private void DrawPanelOutline(Graphics g, Rectangle plot, float scale,
                                  float originX, float originY)
    {
        if (PanelWidth <= 0 || PanelHeight <= 0) return;

        float left = originX;
        float right = originX + PanelWidth * scale;
        float bottom = originY;
        float top = originY - PanelHeight * scale;

        var state = g.Save();
        try
        {
            // 확대하면 판넬 모서리가 축 밖으로 나가므로 플롯 영역 안에서만 그린다.
            g.SetClip(plot);
            using var pen = new Pen(PanelEdgeColor, 1.6f);
            g.DrawRectangle(pen, left, top, right - left, bottom - top);

            // 전체가 보일 때만 치수를 적는다(확대 중에는 방해가 된다).
            if (left >= plot.Left && right <= plot.Right && top >= plot.Top && bottom <= plot.Bottom)
            {
                using var font = new Font("Segoe UI", 8f);
                using var brush = new SolidBrush(PanelEdgeColor);
                string text = $"Panel {PanelWidth:N0} x {PanelHeight:N0}";
                g.DrawString(text, font, brush, left + 4, top + 3);
            }
        }
        finally
        {
            g.Restore(state);
        }
    }

    private static void DrawAxes(Graphics g, Rectangle plot, float scale,
                                 float originX, float originY)
    {
        using var framePen = new Pen(Color.Black, 1f);
        using var font = new Font("Segoe UI", 8.5f);
        using var labelFont = new Font("Segoe UI", 9.5f);
        using var brush = new SolidBrush(Color.Black);
        using var right = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        using var center = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawRectangle(framePen, plot);

        // 줌/패닝 후에는 보이는 구간만 눈금을 찍어야 한다.
        float visMinX = (plot.Left - originX) / scale;
        float visMaxX = (plot.Right - originX) / scale;
        float visMinY = (originY - plot.Bottom) / scale;
        float visMaxY = (originY - plot.Top) / scale;

        float stepX = NiceStep(visMaxX - visMinX);
        for (float v = (float)(Math.Ceiling(visMinX / stepX) * stepX); v <= visMaxX; v += stepX)
        {
            float px = originX + v * scale;
            g.DrawLine(framePen, px, plot.Bottom, px, plot.Bottom + 4);
            g.DrawString(((long)Math.Round(v)).ToString(), font, brush,
                         new RectangleF(px - 60, plot.Bottom + 6, 120, 16), center);
        }

        float stepY = NiceStep(visMaxY - visMinY);
        for (float v = (float)(Math.Ceiling(visMinY / stepY) * stepY); v <= visMaxY; v += stepY)
        {
            float py = originY - v * scale;
            g.DrawLine(framePen, plot.Left - 4, py, plot.Left, py);
            g.DrawString(((long)Math.Round(v)).ToString(), font, brush,
                         new RectangleF(plot.Left - 84, py - 8, 78, 16), right);
        }

        g.DrawString("X Coordinate", labelFont, brush,
                     new RectangleF(plot.Left, plot.Bottom + 28, plot.Width, 18), center);

        var state = g.Save();
        g.TranslateTransform(22, plot.Top + plot.Height / 2f);
        g.RotateTransform(-90);
        g.DrawString("Y Coordinate", labelFont, brush, new RectangleF(-60, -9, 120, 18), center);
        g.Restore(state);
    }

    /// <summary>Unit 번호를 각 Unit 홀 분포의 중앙에 찍는다.</summary>
    private static void DrawUnitLabels(Graphics g, ReportData data,
                                       float originX, float originY, float scale, Rectangle plot)
    {
        if (data.MaxUnit <= 0) return;

        int n = data.MaxUnit + 1;
        var sumX = new double[n];
        var sumY = new double[n];
        var count = new long[n];

        for (int i = 0; i < data.X.Length; i++)
        {
            int u = data.Unit[i];
            if (u <= 0 || u >= n) continue;
            sumX[u] += data.X[i];
            sumY[u] += data.Y[i];
            count[u]++;
        }

        using var font = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var brush = new SolidBrush(LabelColor);
        using var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (int u = 1; u < n; u++)
        {
            if (count[u] == 0) continue;
            float cx = originX + (float)(sumX[u] / count[u]) * scale;
            float cy = originY - (float)(sumY[u] / count[u]) * scale;
            if (cx < plot.Left || cx > plot.Right || cy < plot.Top || cy > plot.Bottom) continue;
            g.DrawString("U" + u, font, brush, new RectangleF(cx - 40, cy - 11, 80, 22), fmt);
        }
    }

    private static float NiceStep(float range)
    {
        if (range <= 0) return 1;
        double raw = range / 5.0;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double step = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
        return (float)Math.Max(1, step * mag);
    }

    private static float NiceCeiling(float value)
    {
        if (value <= 0) return 1;
        float step = NiceStep(value);
        return (float)(Math.Ceiling(value / step) * step);
    }
}
