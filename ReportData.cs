using System.Globalization;

namespace JWMT_Datas;

/// <summary>
/// JWMT 리포트 CSV 에서 플롯에 필요한 최소 컬럼(X, Y, Unit)만 스트리밍으로 읽는다.
/// 리포트는 100만 행에서 _001/_002 로 잘리고 한 파일이 300MB 를 넘기 때문에,
/// 전체를 문자열로 펼치지 않고 한 줄씩 읽으며 필요한 필드만 뽑는다.
/// </summary>
public sealed class ReportData
{
    public float[] X = Array.Empty<float>();
    public float[] Y = Array.Empty<float>();
    public int[] Unit = Array.Empty<int>();
    public int Count => X.Length;

    public string SourceName = string.Empty;
    public List<string> Files = new();

    public float MinX, MaxX, MinY, MaxY;
    public int MinUnit, MaxUnit;

    /// <summary>폴더에서 리포트 CSV 묶음을 찾는다. _001, _002 … 순서로 정렬한다.</summary>
    public static List<string> FindReportCsv(string folder, string? baseName = null)
    {
        if (!Directory.Exists(folder)) return new List<string>();

        string pattern = string.IsNullOrWhiteSpace(baseName)
            ? "*_JWMT_Datas*.csv"
            : Path.GetFileNameWithoutExtension(baseName) + "*.csv";

        return Directory.GetFiles(folder, pattern)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
    }

    public static ReportData Load(IEnumerable<string> files,
                                  IProgress<int>? progress = null,
                                  CancellationToken token = default)
    {
        var list = files.ToList();
        if (list.Count == 0) throw new InvalidOperationException("읽을 CSV 파일이 없습니다.");

        long totalBytes = list.Sum(f => new FileInfo(f).Length);
        long doneBytes = 0;
        int lastPercent = -1;

        // 120만 행 기준 X/Y/Unit 합쳐 약 12MB 라 메모리에 담아도 부담이 없다.
        var xs = new List<float>(1 << 21);
        var ys = new List<float>(1 << 21);
        var us = new List<int>(1 << 21);

        foreach (string file in list)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
                                              FileShare.ReadWrite, 1 << 20);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1 << 20);

            string? header = reader.ReadLine();
            if (header == null) continue;
            doneBytes += header.Length + 2;

            int ix = IndexOfColumn(header, "X");
            int iy = IndexOfColumn(header, "Y");
            int iu = IndexOfColumn(header, "Unit");
            if (ix < 0 || iy < 0)
                throw new InvalidDataException(
                    $"'{Path.GetFileName(file)}' 에서 X/Y 컬럼을 찾지 못했습니다. JWMT 리포트 CSV 가 맞는지 확인하세요.");

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                token.ThrowIfCancellationRequested();
                doneBytes += line.Length + 2;

                if (TryReadRow(line, ix, iy, iu, out float x, out float y, out int unit))
                {
                    xs.Add(x); ys.Add(y); us.Add(unit);
                }

                if (progress != null && (xs.Count & 0xFFFF) == 0)
                {
                    int percent = (int)(100L * doneBytes / Math.Max(1, totalBytes));
                    if (percent != lastPercent) { lastPercent = percent; progress.Report(percent); }
                }
            }
        }

        if (xs.Count == 0) throw new InvalidDataException("좌표 데이터를 한 행도 읽지 못했습니다.");

        var data = new ReportData
        {
            X = xs.ToArray(),
            Y = ys.ToArray(),
            Unit = us.ToArray(),
            Files = list,
            SourceName = Path.GetFileNameWithoutExtension(list[0])
        };
        data.ComputeBounds();
        progress?.Report(100);
        return data;
    }

    private void ComputeBounds()
    {
        MinX = MinY = float.MaxValue;
        MaxX = MaxY = float.MinValue;
        MinUnit = int.MaxValue; MaxUnit = int.MinValue;

        for (int i = 0; i < X.Length; i++)
        {
            if (X[i] < MinX) MinX = X[i];
            if (X[i] > MaxX) MaxX = X[i];
            if (Y[i] < MinY) MinY = Y[i];
            if (Y[i] > MaxY) MaxY = Y[i];
            if (Unit[i] > 0)
            {
                if (Unit[i] < MinUnit) MinUnit = Unit[i];
                if (Unit[i] > MaxUnit) MaxUnit = Unit[i];
            }
        }
        if (MinUnit == int.MaxValue) { MinUnit = 0; MaxUnit = 0; }
    }

    /// <summary>헤더에서 정확히 일치하는 컬럼 위치를 찾는다(X-1, X-2, B-X-2 와 구분해야 한다).</summary>
    private static int IndexOfColumn(string header, string name)
    {
        int field = 0, start = 0;
        for (int i = 0; i <= header.Length; i++)
        {
            if (i == header.Length || header[i] == ',')
            {
                var span = header.AsSpan(start, i - start).Trim().Trim('\uFEFF');
                if (span.Equals(name, StringComparison.OrdinalIgnoreCase)) return field;
                field++; start = i + 1;
            }
        }
        return -1;
    }

    /// <summary>한 줄에서 필요한 필드만 한 번의 순회로 뽑는다(Split 은 행마다 배열을 만들어 느리다).</summary>
    private static bool TryReadRow(string line, int ix, int iy, int iu,
                                   out float x, out float y, out int unit)
    {
        x = y = 0f; unit = 0;
        int need = Math.Max(ix, Math.Max(iy, iu));
        bool gotX = false, gotY = false;

        int field = 0, start = 0;
        for (int i = 0; i <= line.Length; i++)
        {
            if (i != line.Length && line[i] != ',') continue;

            if (field == ix)
                gotX = float.TryParse(line.AsSpan(start, i - start), NumberStyles.Float,
                                      CultureInfo.InvariantCulture, out x);
            else if (field == iy)
                gotY = float.TryParse(line.AsSpan(start, i - start), NumberStyles.Float,
                                      CultureInfo.InvariantCulture, out y);
            else if (field == iu)
                int.TryParse(line.AsSpan(start, i - start), NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out unit);

            field++; start = i + 1;
            if (field > need) break;
        }
        return gotX && gotY;
    }
}
