using InputInterceptorNS;
using System.Diagnostics;
using System.Runtime.InteropServices;
using InputInterceptorNS;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

internal class Program
{
    static void Main(string[] args)
    {
        if (!InitializeDriver())
        {
            Console.WriteLine("Uh-oh, it seems you do not have InputInterceptor's driver installed, would you like to install it?");
            string userResponse = Console.ReadLine();

            if (userResponse != null && userResponse.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                InstallDriver();
            }
            else
            {
                Environment.Exit(0);
            }

            InstallDriver();
            return;
        }

        MouseHook mouseHook = new MouseHook();
        KeyboardHook keyboardHook = new KeyboardHook(KeyboardCallback);

        Bitmap bitmap = CaptureBitmap("RobloxPlayerBeta");
        bitmap.Save("testing.png", ImageFormat.Png);
        string ocrText = ExtractTextFromImage(bitmap);
        string[] sentences = {
            "Sometimes I have really deep thoughts about life and stuff",
            "Hey hivekin, can I bug you for a moment",
            "So, what's keeping you busy these days",
            "Me-wow, is that the latest Felinor fashion",
            "Wow, this breeze is great, right",
            "Some weather we're having these days, huh",
            "So, how's work",
            "You ever been to a Canor restaurant? The food's pretty howlright"
        };

        string matchedSentence = MatchWithSentences(ocrText, sentences);

        if (matchedSentence != null)
        {
            Console.WriteLine($"Matched Sentence: {matchedSentence} from {ocrText}");
        }
        else
        {
            Console.WriteLine($"No matching sentence found ({ocrText})");
        }

        Console.ReadKey();
        keyboardHook.Dispose();
        mouseHook.Dispose();
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
    private static extern int SetForegroundWindow(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

    static Bitmap CaptureBitmap(string processName)
    {
        Process proc;

        try
        {
            proc = Process.GetProcessesByName(processName)[0];
        }
        catch (IndexOutOfRangeException e)
        {
            return null;
        }

        // You need to focus on the application
        SetForegroundWindow(proc.MainWindowHandle);
        //ShowWindow(proc.MainWindowHandle, SW_RESTORE);

        // You need some amount of delay, but 1 second may be overkill
        Thread.Sleep(1000);

        Rect rect = new Rect();
        IntPtr error = GetWindowRect(proc.MainWindowHandle, ref rect);

        // Sometimes it gives errors
        while (error == (IntPtr)0)
        {
            error = GetWindowRect(proc.MainWindowHandle, ref rect);
        }

        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;

        // Define the middle section you want to capturedw
        int tertialWidth = width / 3; // One third of the total width
        int tertialHeight = height / 3; // One third of the total height
        int middleX = rect.left + width / 2 - tertialWidth / 2; // Center X coordinate
        int middleY = rect.top + height / 2 - tertialHeight / 2; // Center Y coordinate

        Bitmap bitmap = new Bitmap(tertialWidth, tertialHeight, PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(middleX, middleY, 0, 0, new Size(tertialWidth, tertialHeight), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    private static string ExtractTextFromImage(Bitmap image)
    {
        using (var engine = new TesseractEngine(@$"C:\Users\{Environment.UserName}\Downloads\tessdata_best-4.1.0", "eng", EngineMode.Default))
        {
            using (var pix = PixConverter.ToPix(image))
            {
                using (var page = engine.Process(pix))
                {
                    return page.GetText();
                }
            }
        }
    }

    private static string MatchWithSentences(string ocrText, string[] sentences)
    {
        // Normalize the OCR text
        string normalizedOcrText = NormalizeString(ocrText);

        foreach (string sentence in sentences)
        {
            // Normalize the sentence
            string normalizedSentence = NormalizeString(sentence);

            // Attempt to remove the known prefix using Regex
            string cleanedOcrText = Regex.Replace(normalizedOcrText, "^try some small talk on someone nearby\\.", "", RegexOptions.IgnoreCase);

            // Split the cleaned OCR text into words
            List<string> ocrWords = cleanedOcrText.Split(' ').ToList();

            // Split the sentence into words
            List<string> sentenceWords = normalizedSentence.Split(' ').ToList();

            // Find the common words
            IEnumerable<string> commonWords = ocrWords.Intersect(sentenceWords);

            // Set a threshold for the minimum number of matching words
            double thresholdPercentage = 0.3; //   70%
            int minMatchingWords = (int)(Math.Max(ocrWords.Count, sentenceWords.Count) * thresholdPercentage);

            // Check if the number of common words meets the threshold
            if (commonWords.Count() >= minMatchingWords)
            {
                return sentence;
            }
        }

        return null;
    }

    static string NormalizeString(string input)
    {
        // Convert to lowercase and remove punctuation
        return input.ToLower(CultureInfo.InvariantCulture).Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Replace("'", "");
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

    static void KeyboardCallback(ref KeyStroke keyStroke)
    {
        if (keyStroke.Code == KeyCode.F5 && keyStroke.State == KeyState.Down)
        {
            Console.WriteLine("F5 was pressed");
            //ScrollToFirstPersonAsync().ConfigureAwait(false);
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
}