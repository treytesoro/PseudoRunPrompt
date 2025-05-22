using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace runprompt
{
    public partial class Form1 : Form
    {
        static Form1 _instance;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifier keys (fsModifiers)
        public const int MOD_ALT = 0x0001;
        public const int MOD_CTRL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        // Hotkey ID (arbitrary value)
        public const int HOTKEY_ID = 9000;

        // Windows Message ID for Hotkey
        public const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);


        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;


        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
        out int pNumArgs);

        public Form1()
        {
            Form1._instance = this;

            _hookID = SetHook(_proc);

            UnregisterHotKey(this.Handle, HOTKEY_ID);
            InitializeComponent();
            this.Opacity = 0;
            PositionFormAtBottomLeft();

            List<string> mruList = MRUList.GetList();
            foreach (string item in mruList)
            {
                comboBox1.Items.Add(item);
            }

            if (mruList.Count > 0)
            {
                openFileDialog1.FileName = mruList.FirstOrDefault();
                comboBox1.Text = mruList.FirstOrDefault();
            }
            else
            {
                openFileDialog1.FileName = "";
            }

            // Register hotkey: Ctrl + Alt + R
            //bool registered = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CTRL | MOD_ALT, (int)Keys.Enter);
            bool registered = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CTRL | MOD_ALT, (int)Keys.Enter);

            if (registered)
            {
                //MessageBox.Show("Hotkey (Ctrl + Alt + B) registered successfully!", "Hotkey Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to register hotkey. It might already be in use.", "Hotkey Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";

            openFileDialog1.Filter = "Programs (*.exe, *.pif, *.com, *.bat, *.cmd)|*.exe;*.pif;*.com;*.bat;*.cmd";
            var results = openFileDialog1.ShowDialog();
            Debug.WriteLine(results);
            Debug.WriteLine(openFileDialog1.FileName);
            comboBox1.Text = openFileDialog1.FileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (comboBox1.Text.Trim() == "")
                return;

            Boolean addMRU = true;
            try
            {
                string[] splitline = CmdLineToArgvW.SplitArgs(comboBox1.Text);
                string[] args = splitline.Skip(1).ToArray();
                Process.Start(splitline.Take(1).Single(), String.Join(" ", args));
            }
            catch (Exception ex)
            {
                addMRU = false;
                MessageBox.Show($"Trey's app cannot find '{comboBox1.Text}'. Make sure you typed the name correctly, and then try again.", comboBox1.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (addMRU)
            {
                MRUList.Add(comboBox1.Text);
                comboBox1.Items.Clear();
                List<string> mruList = MRUList.GetList();
                foreach (string item in mruList)
                {
                    comboBox1.Items.Add(item);
                }
            }
            this.Close();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Listen for the WM_HOTKEY message
            if (m.Msg == WM_HOTKEY)
            {
                // Check if it's our hotkey
                if ((int)m.WParam == HOTKEY_ID)
                {
                    //MessageBox.Show("Hotkey (Ctrl + Alt + B) pressed!", "Hotkey Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Perform your desired action here
                    this.Show();
                    this.Focus();
                    this.TopMost = true; // Bring the form to the front
                    this.Opacity = 1;
                    this.ActiveControl = comboBox1;

                    List<string> mruList = MRUList.GetList();
                    foreach (string item in mruList)
                    {
                        comboBox1.Items.Add(item);
                    }

                    if (mruList.Count > 0)
                    {
                        comboBox1.SelectedItem = mruList.FirstOrDefault();
                        //comboBox1.Text = mruList.FirstOrDefault();
                    }
                    else
                    {
                        openFileDialog1.FileName = "";
                    }
                    SetForegroundWindow(this.Handle);
                    this.TopMost = false; // Remove topmost status
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Opacity = 0;
            e.Cancel = true;
            this.Hide();
            //UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ActiveControl = comboBox1;
            this.Hide();
        }

        private void PositionFormAtBottomLeft()
        {
            // Get the working area of the primary screen
            // The working area excludes the taskbar and any docked toolbars.
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

            // Calculate the X coordinate: 0 for the left edge
            int x = 0;

            // Calculate the Y coordinate: bottom of the working area - form height
            int y = workingArea.Bottom - this.Height;

            // Set the form's location
            this.Location = new Point(x, y);

            // It's good practice to set StartPosition to Manual when setting Location explicitly
            this.StartPosition = FormStartPosition.Manual;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Hide();
            this.Opacity = 1;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static bool winKeyDown = false;
        private static bool rKeyDown = false;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (key == Keys.LWin || key == Keys.RWin)
                    winKeyDown = true;
                if (key == Keys.R)
                    rKeyDown = true;

                if (winKeyDown && rKeyDown)
                {
                    Form1._instance.Show();
                    Form1._instance.Focus();
                    Form1._instance.TopMost = true; // Bring the form to the front
                    Form1._instance.Opacity = 1;
                    Form1._instance.ActiveControl = Form1._instance.comboBox1;

                    List<string> mruList = MRUList.GetList();
                    foreach (string item in mruList)
                    {
                        Form1._instance.comboBox1.Items.Add(item);
                    }

                    if (mruList.Count > 0)
                    {
                        Form1._instance.comboBox1.SelectedItem = mruList.FirstOrDefault();
                        //comboBox1.Text = mruList.FirstOrDefault();
                    }
                    else
                    {
                        Form1._instance.openFileDialog1.FileName = "";
                    }
                    SetForegroundWindow(Form1._instance.Handle);
                    Form1._instance.TopMost = false; // Remove topmost status
                    //MessageBox.Show("Win + R detected!");
                    // Reset state to prevent repeated alerts
                    winKeyDown = false;
                    rKeyDown = false;
                    return new IntPtr(1); // Suppress the key event
                }
            }
            else
            {
                // On key up, reset the flags
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                if (key == Keys.LWin || key == Keys.RWin)
                    winKeyDown = false;
                if (key == Keys.R)
                    rKeyDown = false;
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }

    internal static class CmdLineToArgvW
    {
        public static string[] SplitArgs(string unsplitArgumentLine)
        {
            int numberOfArgs;
            var ptrToSplitArgs = CommandLineToArgvW(unsplitArgumentLine, out numberOfArgs);
            // CommandLineToArgvW returns NULL upon failure.
            if (ptrToSplitArgs == IntPtr.Zero)
                throw new ArgumentException("Unable to split argument.", new Win32Exception());
            // Make sure the memory ptrToSplitArgs to is freed, even upon failure.
            try
            {
                var splitArgs = new string[numberOfArgs];
                // ptrToSplitArgs is an array of pointers to null terminated Unicode strings.
                // Copy each of these strings into our split argument array.
                for (var i = 0; i < numberOfArgs; i++)
                    splitArgs[i] = Marshal.PtrToStringUni(
                        Marshal.ReadIntPtr(ptrToSplitArgs, i * IntPtr.Size));
                return splitArgs;
            }
            finally
            {
                // Free memory obtained by CommandLineToArgW.
                LocalFree(ptrToSplitArgs);
            }
        }
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
            out int pNumArgs);
        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);
    }

    internal class MRUList
    {
        internal static void Add(string path)
        {
            const string mruKeyPath = @"Software\Sentara\AdminRunPrompt\MRU";
            const int maxItems = 10;

            using (RegistryKey mruKey = Registry.CurrentUser.CreateSubKey(mruKeyPath))
            {
                if (mruKey == null)
                    return;

                // Read existing MRU items
                List<string> items = new List<string>();
                for (int i = 0; ; i++)
                {
                    object value = mruKey.GetValue($"Item{i}");
                    if (value is string s)
                        items.Add(s);
                    else
                        break;
                }

                // Remove if already exists, then insert at top
                items.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
                items.Insert(0, path);

                // Keep only the most recent N items
                if (items.Count > maxItems)
                    items = items.Take(maxItems).ToList();

                // Clear existing values
                foreach (string name in mruKey.GetValueNames())
                {
                    mruKey.DeleteValue(name, false);
                }

                // Write updated MRU list
                for (int i = 0; i < items.Count; i++)
                {
                    mruKey.SetValue($"Item{i}", items[i]);
                }
            }
        }
        internal static void Clear()
        {
            const string mruKeyPath = @"Software\Sentara\AdminRunPrompt\MRU";
            using (RegistryKey mruKey = Registry.CurrentUser.OpenSubKey(mruKeyPath, writable: true))
            {
                if (mruKey == null)
                    return;

                foreach (string name in mruKey.GetValueNames())
                {
                    mruKey.DeleteValue(name, false);
                }
            }
        }
        internal static List<string> GetList()
        {
            const string mruKeyPath = @"Software\Sentara\AdminRunPrompt\MRU";
            using (RegistryKey mruKey = Registry.CurrentUser.OpenSubKey(mruKeyPath))
            {
                if (mruKey == null)
                    return new List<string>();
                List<string> items = new List<string>();
                for (int i = 0; ; i++)
                {
                    object value = mruKey.GetValue($"Item{i}");
                    if (value is string s)
                        items.Add(s);
                    else
                        break;
                }
                return items;
            }
        }
    }
}
