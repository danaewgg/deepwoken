using InputInterceptorNS;
using System.Diagnostics;
using System.Runtime.InteropServices;
using InputInterceptorNS;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace CharismaTrainer
{
    internal class Program
    {

        static void MouseCallback(ref MouseStroke mouseStroke)
        {
            Console.WriteLine($"{mouseStroke.X} {mouseStroke.Y} {mouseStroke.Flags} {mouseStroke.State} {mouseStroke.Information}"); // Mouse XY coordinates are raw
                                                                                                                                     // Invert mouse X
                                                                                                                                     //mouseStroke.X = -mouseStroke.X;
                                                                                                                                     // Invert mouse Y                                                                                                          //mouseStroke.Y = -mouseStroke.Y;
        }

        static void KeyboardCallback(ref KeyStroke keyStroke)
        {
            if (keyStroke.Code == KeyCode.F5 && keyStroke.State == KeyState.Down)
            {
                Console.WriteLine("F5 was pressed");
                ScrollToFirstPersonAsync().ConfigureAwait(false);
                //keyboardHook.SimulateInput("hello");

            }

            //Console.WriteLine($"{keyStroke.Code} {keyStroke.State} {keyStroke.Information}");

            // Button swap
            //keyStroke.Code = keyStroke.Code switch {
            //    KeyCode.A => KeyCode.B,
            //    KeyCode.B => KeyCode.A,
            //    _ => keyStroke.Code,
            //};
        }

        private static async Task ScrollToFirstPersonAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                mouseHook.SimulateScrollUp();
                await Task.Delay(10); // Use Task.Delay instead of Thread.Sleep to not block keyboard input while it's running
            }
            await Task.Delay(5000);
            for (int i = 0; i < 10; i++)
            {
                mouseHook.SimulateScrollDown();
                await Task.Delay(10);
            }
        }

        static Boolean InitializeDriver()
        {
            if (InputInterceptor.CheckDriverInstalled())
            {
                Console.WriteLine("Input interceptor seems to be installed.");
                if (InputInterceptor.Initialize())
                {
                    Console.WriteLine("Input interceptor successfully initialized.");
                    return true;
                }
            }
            Console.WriteLine("Input interceptor initialization failed.");
            return false;
        }

        static void InstallDriver()
        {
            Console.WriteLine("Input interceptor not installed.");
            if (InputInterceptor.CheckAdministratorRights())
            {
                Console.WriteLine("Installing...");
                if (InputInterceptor.InstallDriver())
                {
                    Console.WriteLine("Done! Restart your computer.");
                }
                else
                {
                    Console.WriteLine("Something... gone... wrong... :(");
                }
            }
            else
            {
                Console.WriteLine("Restart program with administrator rights so it will be installed.");
            }
        }
                [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        static MouseHook mouseHook;
        static KeyboardHook keyboardHook;
        static void Main(string[] args)
        {
            if (InitializeDriver())
            {
                mouseHook = new MouseHook();
                keyboardHook = new KeyboardHook(KeyboardCallback);
                var yessuh = CaptureApplicationYE("RobloxPlayerBeta");
                yessuh.Save("test7.png", ImageFormat.Png);
                Console.WriteLine("Hooks enabled. Press any key to release.");
                using (var engine = new TesseractEngine(@"C:\Users\Danail\Downloads\tessdata_best-4.1.0", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile("test7.png")) //Pix.LoadFromMemory(ImageToByte2(CaptureApplicationYE("RobloxPlayerBeta")))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                            Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

                            Console.WriteLine("Text (GetText): \r\n{0}", text);
                            Console.WriteLine("Text (iterator):");
                        }
                    }
                }
                // Example usage:
                // Get the screen width and height
                int screenWidth = Console.LargestWindowWidth;
                int screenHeight = Console.LargestWindowHeight;

                // Calculate the center position
                int centerX = GetSystemMetrics(0);
                int centerY = GetSystemMetrics(1);
                var centerPoint = GetWindowCenter("RobloxPlayerBeta");
                Console.WriteLine($"Center of the window is at ({centerX / 2}, {centerY / 2})");
                mouseHook.SetCursorPosition(centerX / 2, centerY / 2);
                Console.ReadKey();
                keyboardHook.Dispose();
                mouseHook.Dispose();
            }
            else
            {
                InstallDriver();
            }

            Console.WriteLine("End of program. Press any key.");
            Console.ReadKey();
        }

        public static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        private static Point GetWindowCenter(string procName)
        {
            var proc = Process.GetProcessesByName(procName).FirstOrDefault();
            if (proc == null) return Point.Empty;

            var rect = new Rect();
            GetWindowRect(proc.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            // Check if the window is minimized (iconic)
            bool isMinimized = IsIconic(proc.MainWindowHandle);

            // Define the middle section you want to capture
            int middleWidth = width / 3; // One third of the total width
            int middleHeight = height / 3; // One third of the total height
            int middleX = rect.left + width / 2 - middleWidth / 2; // Center X coordinate
            int middleY = rect.top + height / 2 - middleHeight / 2; // Center Y coordinate

            // Translate the client coordinates to screen coordinates
            Point screenMiddlePoint = new Point(middleX, middleY);
            ClientToScreen(proc.MainWindowHandle, ref screenMiddlePoint);

            return screenMiddlePoint;
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_STYLE = -16;
        private const uint WS_MAXIMIZE = 0x01000000;

        static Bitmap CaptureApplicationYE(string procName)
        {
            Process proc;

            // ... existing code to get the process and window rectangle ...
            // Cater for cases when the process can't be located.
            try
            {
                proc = Process.GetProcessesByName(procName)[0];
            }
            catch (IndexOutOfRangeException e)
            {
                return null;
            }

            // You need to focus on the application
            SetForegroundWindow(proc.MainWindowHandle);
            ShowWindow(proc.MainWindowHandle, SW_RESTORE);

            // You need some amount of delay, but  1 second may be overkill
            Thread.Sleep(1000);

            Rect rect = new Rect();
            IntPtr error = GetWindowRect(proc.MainWindowHandle, ref rect);

            // Sometimes it gives error.
            while (error == (IntPtr)0)
            {
                error = GetWindowRect(proc.MainWindowHandle, ref rect);
            }

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            // Define the middle section you want to capture
            int middleWidth = width / 3; // One third of the total width
            int middleHeight = height / 3; // One third of the total height
            int middleX = rect.left + width / 2 - middleWidth / 2; // Center X coordinate
            int middleY = rect.top + height / 2 - middleHeight / 2; // Center Y coordinate

            Bitmap bmp = new Bitmap(middleWidth, middleHeight, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                // Capture the middle portion of the window
                graphics.CopyFromScreen(middleX, middleY, 0, 0, new Size(middleWidth, middleHeight), CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }
    }
}