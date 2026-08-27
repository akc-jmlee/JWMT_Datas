using System.Text;

namespace JWMT_Datas
{
    internal static class Program
    {
        private static readonly string LogPath =
            Path.Combine(AppContext.BaseDirectory, "crash.log");

        [STAThread]
        static void Main()
        {
            // 잡히지 않은 예외로 그냥 사라지면 원인을 알 수 없다. 파일로 남기고 알려준다.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => Report(e.Exception, "UI 스레드");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Report(e.ExceptionObject as Exception, "백그라운드");

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        private static void Report(Exception? ex, string origin)
        {
            if (ex == null) return;

            string text = new StringBuilder()
                .AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + origin)
                .AppendLine(ex.ToString())
                .AppendLine(new string('-', 70))
                .ToString();

            try { File.AppendAllText(LogPath, text); } catch { /* 로그 실패로 또 죽지 않게 */ }

            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine +
                "자세한 내용은 아래 파일에 기록했습니다." + Environment.NewLine + LogPath,
                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
