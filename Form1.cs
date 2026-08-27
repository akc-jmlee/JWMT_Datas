using System.Globalization;

namespace JWMT_Datas
{
    public partial class Form1 : Form
    {
        private readonly UnitMapRenderer renderer = new();
        private ReportData? data;
        private Bitmap? rendered;
        private CancellationTokenSource? loadCancel;

        public Form1()
        {
            InitializeComponent();

            // 기본값은 실행 파일이 있는 폴더. 여기에 리포트 CSV 를 넣으면 바로 잡힌다.
            txtFolder.Text = AppContext.BaseDirectory;

            picMap.Resize += (_, _) => Redraw();
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
            Shown += (_, _) => TryAutoLoad();
        }

        // 폴더에 이미 리포트가 있으면 굳이 누르게 하지 않는다.
        private void TryAutoLoad()
        {
            var files = ReportData.FindReportCsv(txtFolder.Text);
            if (files.Count > 0) _ = LoadAsync(files);
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = SafeFolder(txtFolder.Text) };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtFolder.Text = dialog.SelectedPath;
                TryAutoLoad();
            }
        }

        private void btnLoad_Click(object? sender, EventArgs e)
        {
            var files = ReportData.FindReportCsv(txtFolder.Text);
            if (files.Count == 0)
            {
                // xlsx 만 있는 경우가 흔해서 무엇을 넣어야 하는지 짚어준다.
                bool hasXlsx = Directory.Exists(txtFolder.Text) &&
                               Directory.GetFiles(txtFolder.Text, "*_JWMT_Datas*.xlsx").Length > 0;
                MessageBox.Show(this,
                    hasXlsx
                        ? "xlsx 만 있습니다. 리포트가 함께 만드는 _001, _002 … CSV 파일을 넣어주세요."
                        : "폴더에서 '*_JWMT_Datas*.csv' 를 찾지 못했습니다.",
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

            // 폴더를 떨어뜨리면 그 폴더, 파일이면 그 파일과 같은 묶음(_001, _002)을 함께 읽는다.
            string first = dropped[0];
            if (Directory.Exists(first))
            {
                txtFolder.Text = first;
                TryAutoLoad();
                return;
            }

            string folder = Path.GetDirectoryName(first) ?? "";
            txtFolder.Text = folder;

            string baseName = Path.GetFileNameWithoutExtension(first);
            int split = baseName.LastIndexOf("_0", StringComparison.Ordinal);
            if (split > 0) baseName = baseName.Substring(0, split);

            var files = ReportData.FindReportCsv(folder, baseName);
            if (files.Count == 0) files = ReportData.FindReportCsv(folder);
            if (files.Count > 0) _ = LoadAsync(files);
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
                    string.Join(", ", files.Select(Path.GetFileName));
                btnSave.Enabled = true;
                Redraw();
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

        private void Redraw_Changed(object? sender, EventArgs e) => Redraw();

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
                InitialDirectory = SafeFolder(txtFolder.Text)
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // 화면 크기와 무관하게 인쇄/보고용으로 쓸 만한 해상도로 다시 그린다.
                using Bitmap output = renderer.Render(data, 1600, 1600);
                output.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                lblStatus.Text = "저장 완료: " + dialog.FileName;
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

        private static string SafeFolder(string path)
            => Directory.Exists(path) ? path : AppContext.BaseDirectory;

        private static float ParseSize(string text, float fallback)
            => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0
                ? v
                : fallback;
    }
}
