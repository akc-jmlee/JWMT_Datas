using System.Globalization;

namespace JWMT_Datas
{
    public partial class Form1 : Form, IMessageFilter
    {
        private const float ZoomStep = 1.2f;
        private const float MinZoom = 1f;
        private const float MaxZoom = 500f;

        /// <summary>탭 하나가 리포트 묶음 하나. 뷰 상태는 탭마다 따로 기억한다.</summary>
        private sealed class MapDocument
        {
            public required ReportData Data { get; init; }
            public required string Key { get; init; }
            public float Zoom = 1f;
            public PointF? ViewCenter;
        }

        private readonly UnitMapRenderer renderer = new();
        private readonly List<MapDocument> docs = new();
        private CancellationTokenSource? loadCancel;

        // 크기를 끄는 동안에는 WM_SIZE 가 쉴 새 없이 들어온다. 120만 홀을 매번 다시
        // 그리면 멈춘 것처럼 보이고 재진입 위험도 커지므로, 잠시 멎은 뒤 한 번만 그린다.
        private readonly System.Windows.Forms.Timer resizeTimer = new() { Interval = 120 };
        private bool redrawing;

        private bool panning;
        private Point panStart;
        private PointF panCenterStart;

        private MapDocument? Current =>
            tabs.SelectedIndex >= 0 && tabs.SelectedIndex < docs.Count ? docs[tabs.SelectedIndex] : null;

        public Form1()
        {
            InitializeComponent();

            picMap.Resize += (_, _) => ScheduleRedraw();
            resizeTimer.Tick += (_, _) => { resizeTimer.Stop(); Redraw(); };
            picMap.MouseDown += picMap_MouseDown;
            picMap.MouseMove += picMap_MouseMove;
            picMap.MouseUp += picMap_MouseUp;
            picMap.MouseDoubleClick += (_, _) => ResetView();

            tabs.Deselected += tabs_Deselected;
            tabs.Selected += tabs_Selected;
            tabs.MouseUp += tabs_MouseUp;

            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
            Shown += (_, _) => TryAutoLoad();

            // PictureBox 는 포커스를 받지 못해 휠 메시지가 오지 않는다.
            // 커서가 지도 위에 있으면 메시지를 가로채 직접 처리한다.
            Application.AddMessageFilter(this);
            FormClosed += (_, _) =>
            {
                Application.RemoveMessageFilter(this);
                resizeTimer.Stop();
                resizeTimer.Dispose();
            };
        }

        #region 탭

        /// <summary>지도는 하나만 두고 선택된 탭으로 옮겨 붙인다(비트맵을 탭 수만큼 만들지 않는다).</summary>
        private void AttachMapToSelectedTab()
        {
            if (tabs.SelectedTab == null) return;
            if (picMap.Parent == tabs.SelectedTab) return;

            picMap.Parent?.Controls.Remove(picMap);
            tabs.SelectedTab.Controls.Add(picMap);
        }

        private void tabs_Deselected(object? sender, TabControlEventArgs e)
        {
            // 탭을 떠나기 전에 그 탭의 줌/위치를 보관한다.
            if (e.TabPageIndex >= 0 && e.TabPageIndex < docs.Count)
            {
                docs[e.TabPageIndex].Zoom = renderer.Zoom;
                docs[e.TabPageIndex].ViewCenter = renderer.ViewCenter;
            }
        }

        private void tabs_Selected(object? sender, TabControlEventArgs e)
        {
            AttachMapToSelectedTab();
            ApplyCurrentDocument();
        }

        private void ApplyCurrentDocument()
        {
            MapDocument? doc = Current;
            if (doc == null)
            {
                ClearMapImage();
                btnSave.Enabled = false;
                btnResetView.Enabled = false;
                lblStatus.Text = "리포트 파일을 고르거나 창에 끌어다 놓으세요. 여러 개를 한 번에 넣으면 탭으로 열립니다.";
                lblCursor.Text = "";
                return;
            }

            renderer.Zoom = doc.Zoom;
            renderer.ViewCenter = doc.ViewCenter;
            btnSave.Enabled = true;
            btnResetView.Enabled = true;
            ShowDocumentStatus(doc);
            Redraw();
        }

        private void ShowDocumentStatus(MapDocument doc)
        {
            ReportData d = doc.Data;
            lblStatus.Text =
                $"{d.Count:N0} 홀 | Unit {d.MinUnit}-{d.MaxUnit} | " +
                $"X {d.MinX:N0}~{d.MaxX:N0} | Y {d.MinY:N0}~{d.MaxY:N0} | {d.Files.Count}개 파일";
        }

        private void tabs_MouseUp(object? sender, MouseEventArgs e)
        {
            // 가운데 버튼으로 탭을 닫는다. 오른쪽 버튼은 그 탭을 고른 뒤 메뉴를 띄운다.
            for (int i = 0; i < tabs.TabCount; i++)
            {
                if (!tabs.GetTabRect(i).Contains(e.Location)) continue;
                if (e.Button == MouseButtons.Middle) CloseTab(i);
                else if (e.Button == MouseButtons.Right) tabs.SelectedIndex = i;
                return;
            }
        }

        private void menuCloseTab_Click(object? sender, EventArgs e) => CloseTab(tabs.SelectedIndex);

        private void menuCloseAll_Click(object? sender, EventArgs e)
        {
            picMap.Parent?.Controls.Remove(picMap);
            tabs.TabPages.Clear();
            docs.Clear();
            ApplyCurrentDocument();
        }

        private void CloseTab(int index)
        {
            if (index < 0 || index >= docs.Count) return;

            // 지도가 사라질 탭에 붙어 있으면 먼저 떼어낸다.
            if (picMap.Parent == tabs.TabPages[index]) picMap.Parent.Controls.Remove(picMap);

            docs.RemoveAt(index);
            tabs.TabPages.RemoveAt(index);

            AttachMapToSelectedTab();
            ApplyCurrentDocument();
        }

        #endregion

        #region 휠 줌 / 패닝

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL || Current == null) return false;

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
            if (Current == null || renderer.ViewCenter == null) return;

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
            if (e.Button != MouseButtons.Left || Current == null || renderer.ViewCenter == null) return;
            panning = true;
            panStart = e.Location;
            panCenterStart = renderer.ViewCenter.Value;
            picMap.Cursor = Cursors.SizeAll;
        }

        private void picMap_MouseMove(object? sender, MouseEventArgs e)
        {
            if (Current == null) return;

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
            if (files.Count == 0) { ApplyCurrentDocument(); return; }
            txtFile.Text = files[0];
            _ = OpenPathsAsync(new[] { files[0] });
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "리포트 파일 선택 (여러 개 선택 가능)",
                // 파일명 규칙은 <날짜>_JWMT_Datas 로 고정이다.
                Filter = "JWMT 리포트|*_JWMT_Datas*.csv;*_JWMT_Datas*.xlsx|CSV 파일|*.csv|모든 파일|*.*",
                InitialDirectory = StartFolder(),
                Multiselect = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            txtFile.Text = dialog.FileNames[0];
            _ = OpenPathsAsync(dialog.FileNames);
        }

        private void btnLoad_Click(object? sender, EventArgs e)
            => _ = OpenPathsAsync(new[] { txtFile.Text });

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped || dropped.Length == 0)
                return;

            txtFile.Text = dropped[0];
            _ = OpenPathsAsync(dropped);
        }

        /// <summary>
        /// 넣은 경로들을 묶음 단위로 정리해 탭으로 연다. _001 과 _002 를 함께 넣어도
        /// 같은 묶음이면 탭 하나로 합쳐지고, 이미 열려 있으면 그 탭을 고른다.
        /// </summary>
        private async Task OpenPathsAsync(IEnumerable<string> paths)
        {
            var sets = new List<(string Key, List<string> Files)>();
            var seen = new HashSet<string>();

            foreach (string path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                string key = ReportData.SetKey(path);
                if (!seen.Add(key)) continue;

                var files = ReportData.ResolveSet(path);
                if (files.Count > 0) sets.Add((key, files));
            }

            if (sets.Count == 0)
            {
                WarnNothingFound(paths.FirstOrDefault() ?? "");
                return;
            }

            foreach (var set in sets)
            {
                int existing = docs.FindIndex(d => d.Key == set.Key);
                if (existing >= 0) { tabs.SelectedIndex = existing; continue; }
                await LoadAsync(set.Key, set.Files);
            }
        }

        private void WarnNothingFound(string path)
        {
            string folder = Directory.Exists(path) ? path : (Path.GetDirectoryName(path) ?? "");
            bool hasXlsx = Directory.Exists(folder) &&
                           Directory.GetFiles(folder, ReportData.NamePattern + ".xlsx").Length > 0;
            MessageBox.Show(this,
                hasXlsx
                    ? "xlsx 만 있습니다. 리포트가 함께 만드는 _001, _002 … CSV 를 같은 폴더에 두세요."
                    : "'<날짜>_JWMT_Datas*.csv' 를 찾지 못했습니다.",
                "파일 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task LoadAsync(string key, List<string> files)
        {
            loadCancel?.Cancel();
            loadCancel = new CancellationTokenSource();
            CancellationToken token = loadCancel.Token;

            SetBusy(true);
            lblStatus.Text = $"{ReportData.BaseName(files[0])} 읽는 중... ({files.Count}개 파일)";

            var reporter = new Progress<int>(p => progress.Value = Math.Clamp(p, 0, 100));
            try
            {
                ReportData loaded = await Task.Run(
                    () => ReportData.Load(files, reporter, token), token);

                docs.Add(new MapDocument { Data = loaded, Key = key });
                tabs.TabPages.Add(new TabPage(loaded.SourceName) { ToolTipText = files[0] });
                tabs.SelectedIndex = tabs.TabCount - 1;

                // 첫 탭은 Selected 이벤트가 오지 않는 경우가 있어 직접 붙여준다.
                AttachMapToSelectedTab();
                renderer.Zoom = 1f;
                renderer.ViewCenter = null;
                ApplyCurrentDocument();
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

        private void ClearMapImage()
        {
            Image? previous = picMap.Image;
            picMap.Image = null;
            previous?.Dispose();
        }

        private void ScheduleRedraw()
        {
            resizeTimer.Stop();
            resizeTimer.Start();
        }

        private void Redraw()
        {
            // 레이아웃은 동기라 Image 대입이 Resize 를 다시 부를 수 있다. 그대로 두면
            // 안쪽 호출이 만든 이미지를 바깥 호출이 dispose 해 화면이 죽은 비트맵을
            // 가리키게 된다(그리기 시점에 '매개 변수가 잘못되었습니다' 로 튕긴다).
            if (redrawing) return;

            MapDocument? doc = Current;
            if (doc == null || picMap.Width < 10 || picMap.Height < 10) return;

            redrawing = true;
            try
            {
                renderer.AutoFit = chkAutoFit.Checked;
                renderer.PanelWidth = ParseSize(txtPanelWidth.Text, 510000f);
                renderer.PanelHeight = ParseSize(txtPanelHeight.Text, 515000f);

                Bitmap next = renderer.Render(doc.Data, picMap.Width, picMap.Height);

                // 화면이 들고 있는 것만 진실로 보고 교체한다(별도 필드는 어긋날 수 있다).
                Image? previous = picMap.Image;
                picMap.Image = next;
                if (!ReferenceEquals(previous, next)) previous?.Dispose();

                // 렌더가 확정한 뷰 상태를 탭에 되돌려 둔다(탭 전환 시 그대로 복원된다).
                doc.Zoom = renderer.Zoom;
                doc.ViewCenter = renderer.ViewCenter;
            }
            finally
            {
                redrawing = false;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            MapDocument? doc = Current;
            if (doc == null) return;

            using var dialog = new SaveFileDialog
            {
                Filter = "PNG 이미지|*.png",
                FileName = doc.Data.SourceName + "_UnitMap.png",
                InitialDirectory = StartFolder()
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // 화면 크기와 무관하게 인쇄/보고용 해상도로 다시 그린다(현재 줌 상태 유지).
                using Bitmap output = renderer.Render(doc.Data, 1600, 1600);
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
