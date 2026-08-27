using System.Globalization;

namespace JWMT_Datas
{
    public partial class Form1 : Form, IMessageFilter
    {
        private const float ZoomStep = 1.2f;
        private const float MinZoom = 1f;
        private const float MaxZoom = 500f;

        private readonly UnitMapRenderer renderer = new();
        private ReportData? data;
        private Bitmap? rendered;
        private CancellationTokenSource? loadCancel;

        private bool panning;
        private Point panStart;
        private PointF panCenterStart;

        public Form1()
        {
            InitializeComponent();

            picMap.Resize += (_, _) => Redraw();
            picMap.MouseDown += picMap_MouseDown;
            picMap.MouseMove += picMap_MouseMove;
            picMap.MouseUp += picMap_MouseUp;
            picMap.MouseDoubleClick += (_, _) => ResetView();

            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
            Shown += (_, _) => TryAutoLoad();

            // PictureBox 는 포커스를 받지 못해 휠 메시지가 오지 않는다.
            // 커서가 지도 위에 있으면 메시지를 가로채 직접 처리한다.
            Application.AddMessageFilter(this);
            FormClosed += (_, _) => Application.RemoveMessageFilter(this);
        }

        #region 휠 줌 / 패닝

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL || data == null) return false;

            long lParam = m.LParam.ToInt64();
            var screen = new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF));
            Point client = picMap.PointToClient(screen);
            if (!picMap.ClientRectangle.Contains(client)) return false;

            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            ZoomAt(client, delta > 0 ? ZoomStep : 1f / ZoomStep);
            return true;
        }

        /// <summary>커서 아래의 좌표가 제자리에 남도록 확대/축소한다.</summary>
        private void ZoomAt(Point client, float factor)
        {
            if (data == null || renderer.ViewCenter == null) return;

            float next = Math.Clamp(renderer.Zoom * factor, MinZoom, MaxZoom);
            if (Math.Abs(next - renderer.Zoom) < 0.0001f) return;

            float applied = next / renderer.Zoom;
            PointF anchor = renderer.ToData(client);
            PointF c = renderer.ViewCenter.Value;

            // 배율이 f 배가 될 때 앵커를 고정하려면 중심을 이렇게 옮겨야 한다.
            renderer.ViewCenter = new PointF(
                anchor.X - (anchor.X - c.X) / applied,
                anchor.Y - (anchor.Y - c.Y) / applied);
            renderer.Zoom = next;

            Redraw();
            UpdateCursorLabel(client);
        }

        private void picMap_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || data == null || renderer.ViewCenter == null) return;
            panning = true;
            panStart = e.Location;
            panCenterStart = renderer.ViewCenter.Value;
            picMap.Cursor = Cursors.SizeAll;
        }

        private void picMap_MouseMove(object? sender, MouseEventArgs e)
        {
            if (data == null) return;

            if (!panning)
            {
                UpdateCursorLabel(e.Location);
                return;
            }

            // 화면에서 끈 만큼 데이터 좌표를 반대로 옮긴다.
            float dx = (e.X - panStart.X) / renderer.LastScale;
            float dy = (e.Y - panStart.Y) / renderer.LastScale;
            renderer.ViewCenter = new PointF(panCenterStart.X - dx, panCenterStart.Y + dy);
            Redraw();
        }

        private void picMap_MouseUp(object? sender, MouseEventArgs e)
        {
            panning = false;
            picMap.Cursor = Cursors.Default;
        }

        private void UpdateCursorLabel(Point client)
        {
            PointF p = renderer.ToData(client);
            lblCursor.Text = $"X {p.X:N0}   Y {p.Y:N0}   |   {renderer.Zoom:0.##}x";
        }

        private void ResetView()
        {
            renderer.Zoom = 1f;
            renderer.ViewCenter = null;   // 다음 렌더에서 패널 중앙으로 다시 잡힌다
            Redraw();
        }

        private void btnResetView_Click(object? sender, EventArgs e) => ResetView();

        #endregion

        #region 파일 선택 / 읽기

        // 실행 파일 폴더에 리포트가 있으면 굳이 고르게 하지 않는다.
        private void TryAutoLoad()
        {
            var files = ReportData.FindReportCsv(AppContext.BaseDirectory);
            if (files.Count == 0) return;
            txtFile.Text = files[0];
            _ = LoadAsync(files);
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "리포트 파일 선택",
                // 파일명 규칙은 <날짜>_JWMT_Datas 로 고정이다.
                Filter = "JWMT 리포트|*_JWMT_Datas*.csv;*_JWMT_Datas*.xlsx|CSV 파일|*.csv|모든 파일|*.*",
                InitialDirectory = StartFolder()
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            txtFile.Text = dialog.FileName;
            LoadFromPath(dialog.FileName);
        }

        private void btnLoad_Click(object? sender, EventArgs e) => LoadFromPath(txtFile.Text);

        private void LoadFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "리포트 파일을 먼저 고르세요.", "파일 없음",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var files = ReportData.ResolveSet(path);
            if (files.Count == 0)
            {
                // xlsx 만 있는 경우가 흔해서 무엇이 필요한지 짚어준다.
                string folder = Directory.Exists(path) ? path : (Path.GetDirectoryName(path) ?? "");
                bool hasXlsx = Directory.Exists(folder) &&
                               Directory.GetFiles(folder, ReportData.NamePattern + ".xlsx").Length > 0;
                MessageBox.Show(this,
                    hasXlsx
                        ? "xlsx 만 있습니다. 리포트가 함께 만드는 _001, _002 … CSV 를 같은 폴더에 두세요."
                        : "'<날짜>_JWMT_Datas*.csv' 를 찾지 못했습니다.",
                    "파일 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _ = LoadAsync(files);
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped || dropped.Length == 0)
                return;

            // 파일이든 폴더든 ResolveSet 이 같은 묶음(_001, _002)을 찾아준다.
            txtFile.Text = dropped[0];
            LoadFromPath(dropped[0]);
        }

        private async Task LoadAsync(List<string> files)
        {
            loadCancel?.Cancel();
            loadCancel = new CancellationTokenSource();
            CancellationToken token = loadCancel.Token;

            SetBusy(true);
            lblStatus.Text = $"{files.Count}개 파일 읽는 중...";

            var reporter = new Progress<int>(p => progress.Value = Math.Clamp(p, 0, 100));
            try
            {
                ReportData loaded = await Task.Run(
                    () => ReportData.Load(files, reporter, token), token);

                data = loaded;
                lblStatus.Text =
                    $"{loaded.Count:N0} 홀 | Unit {loaded.MinUnit}-{loaded.MaxUnit} | " +
                    $"X {loaded.MinX:N0}~{loaded.MaxX:N0} | Y {loaded.MinY:N0}~{loaded.MaxY:N0} | " +
                    $"{files.Count}개 파일";
                btnSave.Enabled = true;
                btnResetView.Enabled = true;
                ResetView();
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "취소했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "읽기 실패: " + ex.Message;
                MessageBox.Show(this, ex.Message, "읽기 실패",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        #endregion

        // 축 기준이 바뀌면 이전 뷰 중심은 의미가 없으므로 전체 보기로 되돌린다.
        private void Redraw_Changed(object? sender, EventArgs e) => ResetView();

        private void Redraw()
        {
            if (data == null || picMap.Width < 10 || picMap.Height < 10) return;

            renderer.AutoFit = chkAutoFit.Checked;
            renderer.PanelWidth = ParseSize(txtPanelWidth.Text, 510000f);
            renderer.PanelHeight = ParseSize(txtPanelHeight.Text, 515000f);

            Bitmap next = renderer.Render(data, picMap.Width, picMap.Height);
            picMap.Image = next;

            rendered?.Dispose();
            rendered = next;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (data == null) return;

            using var dialog = new SaveFileDialog
            {
                Filter = "PNG 이미지|*.png",
                FileName = data.SourceName + "_UnitMap.png",
                InitialDirectory = StartFolder()
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // 화면 크기와 무관하게 인쇄/보고용 해상도로 다시 그린다(현재 줌 상태 유지).
                using Bitmap output = renderer.Render(data, 1600, 1600);
                output.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                lblStatus.Text = "저장 완료: " + dialog.FileName;
                Redraw();   // 저장용 렌더로 바뀐 뷰 상태를 화면 크기에 맞춰 되돌린다
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "저장 실패",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetBusy(bool busy)
        {
            btnLoad.Enabled = !busy;
            btnBrowse.Enabled = !busy;
            progress.Visible = busy;
            if (busy) progress.Value = 0;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private string StartFolder()
        {
            string path = txtFile.Text;
            if (Directory.Exists(path)) return path;
            string? folder = Path.GetDirectoryName(path);
            return Directory.Exists(folder) ? folder! : AppContext.BaseDirectory;
        }

        private static float ParseSize(string text, float fallback)
            => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0
                ? v
                : fallback;
    }
}
