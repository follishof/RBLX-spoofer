using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RblxSpoofer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            WindowsPrincipal p = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!p.IsInRole(WindowsBuiltInRole.Administrator))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Application.ExecutablePath;
                    psi.UseShellExecute = true;
                    psi.Verb = "runas";
                    Process.Start(psi);
                }
                catch { }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    static class Theme
    {
        public static readonly Color Bg       = Color.FromArgb(14, 17, 27);
        public static readonly Color BgDeep    = Color.FromArgb(8, 9, 14);
        public static readonly Color Card      = Color.FromArgb(22, 26, 38);
        public static readonly Color CardEdge  = Color.FromArgb(38, 45, 62);
        public static readonly Color Accent    = Color.FromArgb(76, 141, 255);
        public static readonly Color AccentDeep= Color.FromArgb(43, 99, 240);
        public static readonly Color Cyan      = Color.FromArgb(70, 200, 255);
        public static readonly Color Green     = Color.FromArgb(53, 208, 138);
        public static readonly Color Yellow    = Color.FromArgb(245, 197, 66);
        public static readonly Color Text      = Color.FromArgb(231, 233, 240);
        public static readonly Color Muted     = Color.FromArgb(131, 138, 153);
        public static readonly Color Faint     = Color.FromArgb(86, 94, 112);

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Gradient wordmark: RBLX (blue->cyan) SPOOFER (white)
    class GradientHeader : Control
    {
        public GradientHeader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        static float WordWidth(string s, FontFamily fam, float em)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddString(s, fam, (int)FontStyle.Bold, em, new PointF(0, 0), StringFormat.GenericTypographic);
                return p.GetBounds().Width;
            }
        }

        static void DrawWord(Graphics g, string s, FontFamily fam, float em, float x, float y, Color c1, Color c2)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddString(s, fam, (int)FontStyle.Bold, em, new PointF(0, 0), StringFormat.GenericTypographic);
                RectangleF b = p.GetBounds();
                using (Matrix m = new Matrix())
                {
                    m.Translate(x - b.Left, y - b.Top);
                    p.Transform(m);
                }
                RectangleF nb = p.GetBounds();
                RectangleF gr = new RectangleF(nb.X - 1, nb.Y - 1, nb.Width + 2, nb.Height + 2);
                using (LinearGradientBrush br = new LinearGradientBrush(gr, c1, c2, LinearGradientMode.Horizontal))
                    g.FillPath(br, p);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using (FontFamily fam = new FontFamily("Segoe UI"))
            {
                float em = 30f * g.DpiY / 72f;
                float w1 = WordWidth("RBLX", fam, em);
                float w2 = WordWidth("SPOOFER", fam, em);
                float gap = em * 0.30f;
                float total = w1 + gap + w2;
                float x = (Width - total) / 2f;
                float y = 4f;

                DrawWord(g, "RBLX", fam, em, x, y, Theme.Accent, Theme.Cyan);
                DrawWord(g, "SPOOFER", fam, em, x + w1 + gap, y, Color.FromArgb(240, 242, 248), Color.FromArgb(200, 206, 220));

                // accent underline
                float uw = 46f;
                float ux = (Width - uw) / 2f;
                float uy = y + em + 6f;
                using (GraphicsPath up = Theme.Round(new Rectangle((int)ux, (int)uy, (int)uw, 3), 2))
                using (LinearGradientBrush ub = new LinearGradientBrush(new RectangleF(ux, uy, uw, 3), Theme.Accent, Theme.Cyan, LinearGradientMode.Horizontal))
                    g.FillPath(ub, up);
            }
        }
    }

    class Card : Panel
    {
        public int Radius = 14;
        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.Round(r, Radius))
            {
                using (SolidBrush b = new SolidBrush(Theme.Card))
                    e.Graphics.FillPath(b, path);
                using (Pen pen = new Pen(Theme.CardEdge, 1f))
                    e.Graphics.DrawPath(pen, path);
            }
        }
    }

    // small status pill
    class Pill : Control
    {
        public Color Fg = Theme.Green;
        public Color Fill = Color.FromArgb(38, 53, 208, 138);
        public Pill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
        }
        public void Set(string text, Color fg, Color fill)
        {
            Text = text; Fg = fg; Fill = fill; Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.Round(r, Height / 2))
            using (SolidBrush b = new SolidBrush(Fill))
                e.Graphics.FillPath(b, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, r, Fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    class AccentButton : Control
    {
        public int Radius = 12;
        bool hover, down;
        public AccentButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle full = new Rectangle(0, 0, Width - 1, Height - 1);

            // soft glow
            if (Enabled)
            {
                for (int i = 3; i >= 1; i--)
                {
                    Rectangle gr = new Rectangle(i, i, Width - 1 - i * 2, Height - 1 - i * 2);
                    using (GraphicsPath gp = Theme.Round(gr, Radius))
                    using (Pen pen = new Pen(Color.FromArgb(22, Theme.Accent), 2f))
                        g.DrawPath(pen, gp);
                }
            }

            Rectangle r = new Rectangle(2, 2, Width - 5, Height - 5);
            Color top, bot;
            if (!Enabled) { top = Color.FromArgb(48, 54, 68); bot = Color.FromArgb(38, 44, 56); }
            else if (down) { top = Theme.AccentDeep; bot = Color.FromArgb(30, 78, 200); }
            else if (hover) { top = Color.FromArgb(96, 160, 255); bot = Theme.Accent; }
            else { top = Theme.Accent; bot = Theme.AccentDeep; }

            using (GraphicsPath path = Theme.Round(r, Radius))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, LinearGradientMode.Vertical))
                    g.FillPath(b, path);
                // top sheen
                Rectangle sheen = new Rectangle(r.X, r.Y, r.Width, r.Height / 2);
                using (GraphicsPath sp = Theme.Round(new Rectangle(r.X, r.Y, r.Width, r.Height), Radius))
                {
                    Region old = g.Clip;
                    g.SetClip(sp);
                    using (LinearGradientBrush sb = new LinearGradientBrush(sheen, Color.FromArgb(40, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
                        g.FillRectangle(sb, sheen);
                    g.Clip = old;
                }
            }
            Color txt = Enabled ? Color.White : Theme.Muted;
            TextRenderer.DrawText(g, Text, Font, r, txt,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    class MainForm : Form
    {
        Label lblEthCur, lblWlanCur;
        Pill pillEth, pillWlan;
        Card resultCard;
        Label lblEthOld, lblWlanOld, lblEthNew, lblWlanNew;
        AccentButton btnSpoof;
        Label lblStatus;
        System.Windows.Forms.Timer dotTimer;
        int dotCount;
        List<RbxTarget> targets;
        Label lblSpoofNote;
        Label creditLabel;
        int resultCardBaseBottom;
        int baseClientH;

        const int PAD = 22;
        const int W = 440;

        public MainForm()
        {
            Text = "Roblox Spoofer";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            BuildUi();
        }

        void BuildUi()
        {
            SuspendLayout();
            Controls.Clear();

            int innerW = W - PAD * 2;
            int y = 24;

            // settings gear (top-right)
            Label gear = new Label();
            gear.Text = ((char)0xE713).ToString(); // MDL2 Settings gear
            gear.Font = new Font("Segoe MDL2 Assets", 12f);
            gear.ForeColor = Theme.Muted;
            gear.BackColor = Color.Transparent;
            gear.AutoSize = false;
            gear.Size = new Size(30, 30);
            gear.TextAlign = ContentAlignment.MiddleCenter;
            gear.Cursor = Cursors.Hand;
            gear.Location = new Point(W - 40, 12);
            gear.Click += new EventHandler(delegate(object s, EventArgs e) { OpenSettings(); });
            gear.MouseEnter += new EventHandler(delegate(object s, EventArgs e) { gear.ForeColor = Theme.Text; });
            gear.MouseLeave += new EventHandler(delegate(object s, EventArgs e) { gear.ForeColor = Theme.Muted; });
            Controls.Add(gear);
            gear.BringToFront();

            // header wordmark
            GradientHeader header = new GradientHeader();
            header.Location = new Point(0, y);
            header.Size = new Size(W, 52);
            Controls.Add(header);
            y += 62;

            Label disc = new Label();
            disc.Text = "Prevents cross-account game bans only.\r\nDoes NOT unban accounts or bypass game bans.";
            disc.Font = new Font("Segoe UI", 8.25f);
            disc.ForeColor = Theme.Faint;
            disc.BackColor = Color.Transparent;
            disc.TextAlign = ContentAlignment.MiddleCenter;
            disc.AutoSize = false;
            disc.Size = new Size(W, 32);
            disc.Location = new Point(0, y);
            Controls.Add(disc);
            y += 42;

            // adapter card
            Card infoCard = new Card();
            infoCard.Location = new Point(PAD, y);
            infoCard.Size = new Size(innerW, 108);
            Controls.Add(infoCard);

            Label curTitle = new Label();
            curTitle.Text = "NETWORK ADAPTERS";
            curTitle.Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
            curTitle.ForeColor = Theme.Faint;
            curTitle.BackColor = Theme.Card;
            curTitle.AutoSize = true;
            curTitle.Location = new Point(18, 14);
            infoCard.Controls.Add(curTitle);

            BuildAdapterRow(infoCard, "Ethernet", 40, out lblEthCur, out pillEth);
            BuildAdapterRow(infoCard, "WLAN", 72, out lblWlanCur, out pillWlan);
            y += 108 + 18;

            // targets card (detected Roblox installs / launchers + custom folders)
            targets = DetectTargets();
            List<RbxTarget> shown = new List<RbxTarget>();
            bool anyRoblox = false;
            foreach (RbxTarget t in targets)
            {
                if (t.Detected) anyRoblox = true;
                if (t.Detected || t.IsCustom) shown.Add(t);  // custom rows show even if missing, so they can be removed
            }

            int listCount = (shown.Count > 0 ? shown.Count : 1) + 1; // +1 for the "add folder" row
            int targCardH = 34 + listCount * 26 + 6;
            Card targCard = new Card();
            targCard.Location = new Point(PAD, y);
            targCard.Size = new Size(innerW, targCardH);
            Controls.Add(targCard);

            Label tTitle = new Label();
            tTitle.Text = "SPOOF TARGETS";
            tTitle.Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
            tTitle.ForeColor = Theme.Faint;
            tTitle.BackColor = Theme.Card;
            tTitle.AutoSize = true;
            tTitle.Location = new Point(18, 12);
            targCard.Controls.Add(tTitle);

            int ry = 32;
            if (shown.Count > 0)
            {
                foreach (RbxTarget t in shown)
                    BuildTargetRow(targCard, t, ry, out ry);
            }
            else
            {
                Label err = new Label();
                err.Text = ((char)0xEA39).ToString() + "  No Roblox install detected";
                err.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                err.ForeColor = Color.FromArgb(240, 90, 90);
                err.BackColor = Theme.Card;
                err.AutoSize = true;
                err.Location = new Point(18, ry + 1);
                targCard.Controls.Add(err);
                ry += 26;
            }

            // "add custom folder" row
            Label add = new Label();
            add.Text = "+  Add Roblox folder…";
            add.Font = new Font("Segoe UI", 9f);
            add.ForeColor = Theme.Accent;
            add.BackColor = Theme.Card;
            add.AutoSize = true;
            add.Cursor = Cursors.Hand;
            add.Location = new Point(18, ry + 1);
            add.Click += new EventHandler(delegate(object s, EventArgs e) { AddCustomFolder(); });
            targCard.Controls.Add(add);

            y += targCardH + 18;

            // spoof button
            btnSpoof = new AccentButton();
            btnSpoof.Text = "SPOOF NOW";
            btnSpoof.Location = new Point(PAD, y);
            btnSpoof.Size = new Size(innerW, 52);
            btnSpoof.Click += new EventHandler(OnSpoofClick);
            Controls.Add(btnSpoof);
            y += 52 + 12;

            lblStatus = CenteredLabel("", 9f, Theme.Accent, y, 18);
            Controls.Add(lblStatus);
            y += 24;

            // result card (compact by default; grows if there's an explanation note)
            resultCard = new Card();
            resultCard.Location = new Point(PAD, y);
            resultCard.Size = new Size(innerW, 178);
            resultCard.Visible = false;
            Controls.Add(resultCard);

            Label oldT = SectionLabel("BEFORE", Theme.Faint, 16);
            resultCard.Controls.Add(oldT);
            BuildResultRow(resultCard, "Ethernet", 38, out lblEthOld, Theme.Muted);
            BuildResultRow(resultCard, "WLAN", 62, out lblWlanOld, Theme.Muted);

            Panel divider = new Panel();
            divider.BackColor = Theme.CardEdge;
            divider.Size = new Size(innerW - 36, 1);
            divider.Location = new Point(18, 94);
            resultCard.Controls.Add(divider);

            Label newT = SectionLabel("AFTER", Theme.Green, 106);
            resultCard.Controls.Add(newT);
            BuildResultRow(resultCard, "Ethernet", 128, out lblEthNew, Theme.Green);
            BuildResultRow(resultCard, "WLAN", 152, out lblWlanNew, Theme.Green);

            Panel divider2 = new Panel();
            divider2.BackColor = Theme.CardEdge;
            divider2.Size = new Size(innerW - 36, 1);
            divider2.Location = new Point(18, 182);
            resultCard.Controls.Add(divider2);

            lblSpoofNote = new Label();
            lblSpoofNote.Font = new Font("Segoe UI", 8.25f);
            lblSpoofNote.ForeColor = Theme.Muted;
            lblSpoofNote.BackColor = Theme.Card;
            lblSpoofNote.AutoSize = false;
            lblSpoofNote.Size = new Size(innerW - 36, 64);
            lblSpoofNote.Location = new Point(18, 190);
            lblSpoofNote.Visible = false;
            resultCard.Controls.Add(lblSpoofNote);

            resultCardBaseBottom = y + 178;
            y += 178 + 16;

            creditLabel = BuildCredit(y);
            Controls.Add(creditLabel);
            y += 16 + PAD;

            baseClientH = y;
            ClientSize = new Size(W, y);
            ResumeLayout();
            RefreshCurrent();

            if (!anyRoblox)
            {
                lblStatus.ForeColor = Color.FromArgb(240, 90, 90);
                lblStatus.Text = "No Roblox found — only the MAC will be spoofed.";
            }
            else if (IsRobloxRunning())
            {
                lblStatus.ForeColor = Theme.Yellow;
                lblStatus.Text = "Roblox is open — it will be closed when you spoof.";
            }
        }

        void AddCustomFolder()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a Roblox install folder (one that has its own data / LocalStorage).";
                if (fbd.ShowDialog(this) != DialogResult.OK) return;
                string path = fbd.SelectedPath;
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

                bool ownStore = Directory.Exists(Path.Combine(path, "LocalStorage")) ||
                                File.Exists(Path.Combine(path, @"LocalStorage\RobloxCookies.dat")) ||
                                File.Exists(Path.Combine(path, "RobloxCookies.dat"));
                bool isBinaries = File.Exists(Path.Combine(path, "RobloxPlayerBeta.exe"));

                if (!ownStore && isBinaries)
                {
                    MessageBox.Show(
                        "That folder is a Roblox version that uses the main cookie store (%LOCALAPPDATA%\\Roblox).\n\n" +
                        "It's already covered by \"Roblox Player\" — there's nothing separate to patch there.",
                        "Shares the main cookie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (!ownStore && !isBinaries)
                {
                    MessageBox.Show("No Roblox data (LocalStorage / RobloxCookies.dat) was found in that folder.",
                        "Not a Roblox data folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Settings.AddCustomFolder(path);
                BuildUi();
            }
        }

        Label CenteredLabel(string text, float size, Color color, int y, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", size);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.AutoSize = false;
            l.Size = new Size(W, h);
            l.Location = new Point(0, y);
            return l;
        }

        // Clickable credit: "8832" -> Discord, "fantascript.xyz" -> site.
        Label BuildCredit(int y)
        {
            string txt = "by 8832  " + ((char)0x2022) + "  fantascript.xyz";
            LinkLabel l = new LinkLabel();
            l.Text = txt;
            l.Font = new Font("Segoe UI", 8.5f);
            l.AutoSize = false;
            l.Size = new Size(W, 16);
            l.Location = new Point(0, y);
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.BackColor = Color.Transparent;
            l.ForeColor = Theme.Faint;
            l.LinkColor = Theme.Muted;
            l.ActiveLinkColor = Theme.Accent;
            l.VisitedLinkColor = Theme.Muted;
            l.LinkBehavior = LinkBehavior.HoverUnderline;
            l.Links.Clear();
            l.Links.Add(txt.IndexOf("8832"), 4, "https://discord.gg/P9dEKq5Emd");
            l.Links.Add(txt.IndexOf("fantascript.xyz"), "fantascript.xyz".Length, "https://fantascript.xyz");
            l.LinkClicked += new LinkLabelLinkClickedEventHandler(OnCreditLink);
            return l;
        }

        void OnCreditLink(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open via explorer.exe so the URL launches in the user's normal (non-admin) browser.
            try { Process.Start("explorer.exe", e.Link.LinkData.ToString()); } catch { }
        }

        Label SectionLabel(string text, Color color, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
            l.ForeColor = color;
            l.BackColor = Theme.Card;
            l.AutoSize = true;
            l.Location = new Point(18, y);
            return l;
        }

        void BuildAdapterRow(Card parent, string name, int y, out Label val, out Pill pill)
        {
            bool wifi = name.IndexOf("WLAN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Wi", StringComparison.OrdinalIgnoreCase) >= 0;

            Label icon = new Label();
            icon.Text = wifi ? ((char)0xE701).ToString() : ((char)0xE839).ToString(); // MDL2 WiFi / Ethernet
            icon.Font = new Font("Segoe MDL2 Assets", 11f);
            icon.ForeColor = wifi ? Theme.Cyan : Theme.Accent;
            icon.BackColor = Theme.Card;
            icon.AutoSize = false;
            icon.Size = new Size(22, 22);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            icon.Location = new Point(16, y - 1);
            parent.Controls.Add(icon);

            Label n = new Label();
            n.Text = name;
            n.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            n.ForeColor = Theme.Text;
            n.BackColor = Theme.Card;
            n.AutoSize = true;
            n.Location = new Point(44, y);
            parent.Controls.Add(n);

            Label v = new Label();
            v.Text = "--";
            v.Font = new Font("Consolas", 10f);
            v.ForeColor = Theme.Muted;
            v.BackColor = Theme.Card;
            v.AutoSize = true;
            v.Location = new Point(120, y + 1);
            parent.Controls.Add(v);
            val = v;

            Pill pl = new Pill();
            pl.Size = new Size(82, 20);
            pl.Location = new Point(parent.Width - 82 - 18, y - 1);
            parent.Controls.Add(pl);
            pill = pl;
        }

        void BuildTargetRow(Card parent, RbxTarget t, int y, out int nextY)
        {
            MiniCheck chk = new MiniCheck();
            chk.Checked = t.Detected;      // detected default on; missing default off
            chk.Enabled = t.Detected;
            chk.Location = new Point(18, y);
            chk.Click += new EventHandler(delegate(object s, EventArgs e) { t.Selected = chk.Checked; });
            parent.Controls.Add(chk);
            t.Selected = t.Detected;

            Label n = new Label();
            n.Text = t.Name;
            n.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            n.ForeColor = t.Detected ? Theme.Text : Theme.Faint;
            n.BackColor = Theme.Card;
            n.AutoSize = true;
            n.Location = new Point(46, y + 1);
            parent.Controls.Add(n);

            int rightEdge = parent.Width - 18;

            // remove (x) for user-added custom folders
            if (t.IsCustom)
            {
                Label rm = new Label();
                rm.Text = ((char)0xE711).ToString(); // MDL2 Cancel/x
                rm.Font = new Font("Segoe MDL2 Assets", 8f);
                rm.ForeColor = Theme.Faint;
                rm.BackColor = Theme.Card;
                rm.AutoSize = false;
                rm.Size = new Size(18, 18);
                rm.TextAlign = ContentAlignment.MiddleCenter;
                rm.Cursor = Cursors.Hand;
                rm.Location = new Point(rightEdge - 18, y);
                string p = t.Path;
                rm.Click += new EventHandler(delegate(object s, EventArgs e) { Settings.RemoveCustomFolder(p); BuildUi(); });
                rm.MouseEnter += new EventHandler(delegate(object s, EventArgs e) { rm.ForeColor = Color.FromArgb(240, 90, 90); });
                rm.MouseLeave += new EventHandler(delegate(object s, EventArgs e) { rm.ForeColor = Theme.Faint; });
                parent.Controls.Add(rm);
                rightEdge -= 24;
            }

            Label tag = new Label();
            tag.Text = t.Detected ? (t.Note != null ? t.Note : "detected") : "missing";
            tag.Font = new Font("Segoe UI", 8f);
            tag.ForeColor = Theme.Faint;
            tag.BackColor = Theme.Card;
            tag.AutoSize = true;
            tag.Location = new Point(rightEdge - TextRenderer.MeasureText(tag.Text, tag.Font).Width, y + 3);
            parent.Controls.Add(tag);

            nextY = y + 26;
        }

        void BuildResultRow(Card parent, string name, int y, out Label val, Color color)
        {
            Label n = new Label();
            n.Text = name;
            n.Font = new Font("Segoe UI", 9.5f);
            n.ForeColor = Theme.Text;
            n.BackColor = Theme.Card;
            n.AutoSize = true;
            n.Location = new Point(18, y);
            parent.Controls.Add(n);

            Label v = new Label();
            v.Text = "--";
            v.Font = new Font("Consolas", 10f);
            v.ForeColor = color;
            v.BackColor = Theme.Card;
            v.AutoSize = true;
            v.Location = new Point(110, y);
            parent.Controls.Add(v);
            val = v;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle full = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            // When minimized the client area is 0x0 — gradient/path brushes throw on a zero rect.
            if (full.Width <= 0 || full.Height <= 0)
            {
                using (SolidBrush sb = new SolidBrush(Theme.Bg)) g.FillRectangle(sb, full);
                return;
            }
            using (LinearGradientBrush b = new LinearGradientBrush(full, Theme.Bg, Theme.BgDeep, LinearGradientMode.Vertical))
                g.FillRectangle(b, full);

            // soft accent glow behind the header
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath gp = new GraphicsPath())
            {
                int gw = 320, gh = 220;
                Rectangle gr = new Rectangle((ClientSize.Width - gw) / 2, -110, gw, gh);
                gp.AddEllipse(gr);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb(46, 76, 141, 255);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 76, 141, 255) };
                    g.FillPath(pgb, gp);
                }
            }
        }

        void RefreshCurrent()
        {
            lblEthCur.Text = GetMac(false);
            lblWlanCur.Text = GetMac(true);
            SetPill(pillEth, false);
            SetPill(pillWlan, true);
        }

        void SetPill(Pill pill, bool wireless)
        {
            if (!Exists(wireless))
                pill.Set("NONE", Theme.Faint, Color.FromArgb(30, 86, 94, 112));
            else if (IsUp(wireless))
                pill.Set("CONNECTED", Theme.Green, Color.FromArgb(40, 53, 208, 138));
            else
                pill.Set("OFFLINE", Theme.Muted, Color.FromArgb(34, 131, 138, 153));
        }

        void OnSpoofClick(object sender, EventArgs e)
        {
            // Warn before force-closing Roblox (needed to clear its locked data)
            if (IsRobloxRunning())
            {
                DialogResult r = MessageBox.Show(
                    "Roblox is currently open.\n\nIt must be closed so its data can be cleared. Close Roblox and continue?",
                    "Roblox is running", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (r != DialogResult.OK) return;
            }

            btnSpoof.Enabled = false;
            dotCount = 0;
            btnSpoof.Text = "SPOOFING.";
            StartDots();
            lblStatus.ForeColor = Theme.Accent;
            lblStatus.Text = "Randomizing MAC addresses and clearing Roblox data...";
            resultCard.Visible = false;

            string oldEth = GetMac(false);
            string oldWlan = GetMac(true);
            List<RbxTarget> chosen = targets;
            bool deep = Settings.DeepClean();

            Thread t = new Thread(delegate()
            {
                int ethRes = SpoofAdapter(false);
                int wlanRes = SpoofAdapter(true);
                RunClean(chosen, deep);
                string newEth = GetMac(false);
                string newWlan = GetMac(true);

                this.BeginInvoke((MethodInvoker)delegate()
                {
                    StopDots();
                    lblEthOld.Text = oldEth;
                    lblWlanOld.Text = oldWlan;
                    SetSpoofed(lblEthNew, ethRes, oldEth, newEth);
                    SetSpoofed(lblWlanNew, wlanRes, oldWlan, newWlan);

                    // Explain any adapter whose driver can't spoof (res == 2).
                    bool ethUnsup = (ethRes == 2);
                    bool wlanUnsup = (wlanRes == 2);
                    string who = ethUnsup && wlanUnsup ? "Both adapters" : (wlanUnsup ? "Your Wi-Fi" : (ethUnsup ? "Your Ethernet" : null));
                    ShowResultNote(who);

                    resultCard.Visible = true;
                    RefreshCurrent();
                    lblStatus.Text = "";
                    bool needsRestart = (ethRes == 1 && newEth == oldEth) ||
                                        (wlanRes == 1 && newWlan == oldWlan);
                    btnSpoof.Enabled = true;
                    btnSpoof.Text = "SPOOF AGAIN";
                    ShowCookiePrompt();
                });
            });
            t.IsBackground = true;
            t.Start();
        }

        // Grow the result card + window to show an explanation note, or keep it compact.
        void ShowResultNote(string who)
        {
            if (who == null)
            {
                lblSpoofNote.Visible = false;
                resultCard.Height = 178;
                creditLabel.Top = resultCard.Bottom + 16;
                ClientSize = new Size(W, baseClientH);
                return;
            }

            lblSpoofNote.Text = who + " couldn't be spoofed — this adapter's driver doesn't allow MAC " +
                "changes (common, not fixable in software). It rarely matters: Roblox tracks the cleared " +
                "cookie, not your MAC. Feel free to spoof and test in-game first.";
            lblSpoofNote.Visible = true;
            resultCard.Height = 262;
            creditLabel.Top = resultCard.Bottom + 16;
            ClientSize = new Size(W, creditLabel.Bottom + PAD);
        }

        void StartDots()
        {
            if (dotTimer == null)
            {
                dotTimer = new System.Windows.Forms.Timer();
                dotTimer.Interval = 400;
                dotTimer.Tick += new EventHandler(OnDotTick);
            }
            dotTimer.Start();
        }
        void StopDots() { if (dotTimer != null) dotTimer.Stop(); }
        void OnDotTick(object sender, EventArgs e)
        {
            dotCount = (dotCount % 3) + 1;
            btnSpoof.Text = "SPOOFING" + new string('.', dotCount);
        }

        void ShowCookiePrompt()
        {
            if (Settings.NotifDisabled()) return;
            using (CookiePromptForm f = new CookiePromptForm())
            {
                f.ShowDialog(this);
                if (f.DeleteForMe) RunCookieCleaner();
            }
        }

        void RunCookieCleaner()
        {
            Cursor = Cursors.WaitCursor;
            List<BrowserInfo> browsers = BrowserCookies.Detect();
            BrowserCookies.Scan(browsers);
            Cursor = Cursors.Default;

            if (browsers.Count == 0)
            {
                MessageBox.Show(this, "No supported browsers were found on this PC.",
                    "Nothing to clear", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool anyRoblox = false, anyRunning = false;
            foreach (BrowserInfo b in browsers)
            {
                if (b.RobloxCount > 0) anyRoblox = true;
                if (b.Running) anyRunning = true;
            }
            if (!anyRoblox && !anyRunning)
            {
                MessageBox.Show(this, "No Roblox cookies were found in any browser. You're clean.",
                    "All clear", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (BrowserSelectForm f = new BrowserSelectForm(browsers))
                f.ShowDialog(this);
        }

        void OpenSettings()
        {
            using (SettingsForm f = new SettingsForm())
                f.ShowDialog(this);
        }

        void SetSpoofed(Label lbl, int res, string oldMac, string newMac)
        {
            if (res == 0)
            {
                lbl.Text = "no adapter detected";
                lbl.ForeColor = Theme.Faint;
            }
            else if (newMac != oldMac)
            {
                lbl.Text = newMac;
                lbl.ForeColor = Theme.Green;
            }
            else if (res == 2)
            {
                // driver has no NetworkAddress param — it won't honor a spoof (see note below the results)
                lbl.Text = oldMac + "   unsupported";
                lbl.ForeColor = Theme.Faint;
            }
            else
            {
                // registry written & supported, but not applied live — takes effect on reboot
                lbl.Text = newMac + "  (restart PC)";
                lbl.ForeColor = Theme.Yellow;
            }
        }

        // ---------- Spoof logic ----------

        static readonly string[] VirtualHints = {
            "virtual", "vmware", "virtualbox", "hyper-v", "loopback", "tap-windows", "tap adapter",
            "vpn", "bluetooth", "wan miniport", "wi-fi direct", "npcap", "pseudo", "tunnel", "teredo",
            "hamachi", "zerotier", "wireguard", "openvpn", "radmin", "microsoft kernel debug"
        };

        static bool IsVirtual(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return false;
            string d = desc.ToLowerInvariant();
            foreach (string h in VirtualHints) if (d.IndexOf(h, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        // Find the primary physical adapter of the requested type (prefers a connected one).
        // Keyed by hardware type, not by name, so "Wi-Fi" / "WLAN" / "Ethernet 2" all resolve.
        static NetworkInterface Primary(bool wireless)
        {
            NetworkInterface best = null;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                NetworkInterfaceType t = ni.NetworkInterfaceType;
                bool typeOk = wireless
                    ? (t == NetworkInterfaceType.Wireless80211)
                    : (t == NetworkInterfaceType.Ethernet || t == NetworkInterfaceType.GigabitEthernet || t == NetworkInterfaceType.FastEthernetT);
                if (!typeOk) continue;
                if (IsVirtual(ni.Description)) continue;
                if (ni.GetPhysicalAddress().GetAddressBytes().Length != 6) continue;
                if (best == null) { best = ni; continue; }
                if (ni.OperationalStatus == OperationalStatus.Up && best.OperationalStatus != OperationalStatus.Up)
                    best = ni;
            }
            return best;
        }

        static bool Exists(bool wireless) { return Primary(wireless) != null; }
        static bool IsUp(bool wireless)
        {
            NetworkInterface ni = Primary(wireless);
            return ni != null && ni.OperationalStatus == OperationalStatus.Up;
        }

        static string NewRandomMac()
        {
            byte[] bytes = new byte[6];
            RandomNumberGenerator.Create().GetBytes(bytes);
            // clear multicast bit (bit0), set locally-administered bit (bit1) — required by most Wi-Fi drivers
            bytes[0] = (byte)((bytes[0] & 0xFC) | 0x02);
            string s = "";
            for (int i = 0; i < 6; i++) s += bytes[i].ToString("X2");
            return s;
        }

        static string GetMac(bool wireless)
        {
            NetworkInterface ni = Primary(wireless);
            if (ni == null) return "N/A";
            byte[] b = ni.GetPhysicalAddress().GetAddressBytes();
            if (b.Length == 0) return "N/A";
            string s = "";
            for (int i = 0; i < b.Length; i++)
            {
                if (i > 0) s += "-";
                s += b[i].ToString("X2");
            }
            return s;
        }

        // Result codes: 0 = no adapter, 1 = written & driver supports it, 2 = written but driver has no NetworkAddress param.
        static int SpoofAdapter(bool wireless)
        {
            NetworkInterface ni = Primary(wireless);
            if (ni == null) return 0;
            string desc = ni.Description;
            string name = ni.Name;
            string mac = NewRandomMac();

            bool wrote = false;
            bool hasParam = false;
            string classPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
            using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classPath))
            {
                if (classKey == null) return 0;
                foreach (string sub in classKey.GetSubKeyNames())
                {
                    if (sub.Length != 4) continue;
                    using (RegistryKey k = classKey.OpenSubKey(sub, true))
                    {
                        if (k == null) continue;
                        object dd = k.GetValue("DriverDesc");
                        if (dd != null && string.Equals(dd.ToString(), desc, StringComparison.OrdinalIgnoreCase))
                        {
                            k.SetValue("NetworkAddress", mac, RegistryValueKind.String);
                            wrote = true;
                            using (RegistryKey ndi = k.OpenSubKey(@"Ndi\params\NetworkAddress"))
                                hasParam = ndi != null;
                            break;
                        }
                    }
                }
            }
            if (!wrote) return 0;

            // Cycle the adapter so the new MAC takes effect (disable+enable = Restart-NetAdapter).
            string safe = name.Replace("'", "''");
            RunHidden("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Restart-NetAdapter -Name '" + safe + "' -Confirm:$false\"");
            Thread.Sleep(3500);
            return hasParam ? 1 : 2;
        }

        static bool IsRobloxRunning()
        {
            string[] procs = { "RobloxPlayerBeta", "RobloxStudioBeta", "RobloxCrashHandler" };
            foreach (string pn in procs)
                if (Process.GetProcessesByName(pn).Length > 0) return true;
            return false;
        }

        // Detect every Roblox install / launcher present on this machine.
        static List<RbxTarget> DetectTargets()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            List<RbxTarget> list = new List<RbxTarget>();

            // Standard Roblox Player (also the cookie store used by Bloxstrap / Fishstrap).
            string rblx = Path.Combine(local, "Roblox");
            RbxTarget player = new RbxTarget("Roblox Player", Directory.Exists(rblx));
            player.Cookies.Add(Path.Combine(rblx, @"LocalStorage\RobloxCookies.dat"));
            player.Cookies.Add(Path.Combine(rblx, "RobloxCookies.dat"));
            player.DeepDirs.Add(Path.Combine(rblx, "http"));
            player.DeepDirs.Add(Path.Combine(rblx, "RobloxBrowserCache"));
            player.DeepDirs.Add(Path.Combine(rblx, "logs"));
            list.Add(player);

            // Microsoft Store / UWP — a genuinely separate identity store.
            try
            {
                string pkgs = Path.Combine(local, "Packages");
                if (Directory.Exists(pkgs))
                    foreach (string dir in Directory.GetDirectories(pkgs, "*ROBLOX*"))
                    {
                        RbxTarget uwp = new RbxTarget("Roblox (Store)", true);
                        uwp.Cookies.Add(Path.Combine(dir, @"LocalState\LocalStorage\RobloxCookies.dat"));
                        uwp.DeepDirs.Add(Path.Combine(dir, @"AC\Cookies"));
                        uwp.DeepDirs.Add(Path.Combine(dir, @"AC\INetCache"));
                        list.Add(uwp);
                        break;
                    }
            }
            catch { }

            // Bloxstrap / Fishstrap launchers — share the Roblox cookie; only their caches are separate.
            AddLauncher(list, Path.Combine(local, "Bloxstrap"), "Bloxstrap");
            AddLauncher(list, Path.Combine(local, "Fishstrap"), "Fishstrap");

            // User-added custom install folders (portable / separate-data setups).
            foreach (string folder in Settings.CustomFolders())
            {
                if (string.IsNullOrEmpty(folder)) continue;
                bool present = Directory.Exists(folder);
                string label = System.IO.Path.GetFileName(folder.TrimEnd('\\', '/'));
                if (string.IsNullOrEmpty(label)) label = folder;
                RbxTarget c = new RbxTarget(label + "  (custom)", present);
                c.IsCustom = true;
                c.Path = folder;
                c.Cookies.Add(Path.Combine(folder, @"LocalStorage\RobloxCookies.dat"));
                c.Cookies.Add(Path.Combine(folder, "RobloxCookies.dat"));
                c.DeepDirs.Add(Path.Combine(folder, "http"));
                c.DeepDirs.Add(Path.Combine(folder, "logs"));
                list.Add(c);
            }

            return list;
        }

        static void AddLauncher(List<RbxTarget> list, string root, string name)
        {
            if (!Directory.Exists(root)) return;
            RbxTarget t = new RbxTarget(name, true);
            t.Note = "shares Roblox cookie";
            t.DeepDirs.Add(Path.Combine(root, "Logs"));
            t.DeepDirs.Add(Path.Combine(root, "LogsBackup"));
            list.Add(t);
        }

        // Clear cookie files (always) and caches (deep only) for the chosen targets, plus the shared registry tracker.
        static void RunClean(List<RbxTarget> targets, bool deep)
        {
            string[] procs = { "RobloxPlayerBeta", "RobloxStudioBeta", "RobloxCrashHandler" };
            bool killed = false;
            foreach (string pn in procs)
                foreach (Process pr in Process.GetProcessesByName(pn))
                    try { pr.Kill(); killed = true; } catch { }
            if (killed) Thread.Sleep(2000);

            foreach (RbxTarget t in targets)
            {
                if (!t.Detected || !t.Selected) continue;
                foreach (string c in t.Cookies) ClearCookieFile(c);
                if (deep) foreach (string d in t.DeepDirs) ClearDir(d);
            }

            // Shared browser-tracker cookies in the registry (not the "ROBLOX Corporation" app-registration key).
            try
            {
                using (RegistryKey sw = Registry.CurrentUser.OpenSubKey(@"Software\Roblox", true))
                {
                    if (sw != null)
                        foreach (string sub in sw.GetSubKeyNames())
                            if (sub.IndexOf("Browser", StringComparison.OrdinalIgnoreCase) >= 0)
                                try { sw.DeleteSubKeyTree(sub, false); } catch { }
                }
            }
            catch { }
        }

        static void ClearCookieFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.Create(path).Close();
            }
            catch { }
        }

        static void ClearDir(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                foreach (string f in Directory.GetFiles(path)) try { File.Delete(f); } catch { }
                foreach (string d in Directory.GetDirectories(path)) try { Directory.Delete(d, true); } catch { }
            }
            catch { }
        }

        static void RunHidden(string file, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = file;
                psi.Arguments = args;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process pr = Process.Start(psi);
                pr.WaitForExit(15000);
            }
            catch { }
        }
    }

    // Native Windows TaskDialog (with built-in "don't show again" checkbox)
    static class TaskDialogs
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct TASKDIALOGCONFIG
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public uint dwFlags;
            public uint dwCommonButtons;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszWindowTitle;
            public IntPtr hMainIcon;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszMainInstruction;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszContent;
            public uint cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszVerificationText;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszExpandedInformation;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszExpandedControlText;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszCollapsedControlText;
            public IntPtr hFooterIcon;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszFooter;
            public IntPtr pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int TaskDialogIndirect(ref TASKDIALOGCONFIG pTaskConfig,
            out int pnButton, out int pnRadioButton,
            [MarshalAs(UnmanagedType.Bool)] out bool pfVerificationFlagChecked);

        const uint TDCBF_OK_BUTTON = 0x0001;
        const uint TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
        const uint TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;
        // MAKEINTRESOURCE(-1) => warning icon
        static readonly IntPtr TD_WARNING_ICON = new IntPtr(65535);

        static readonly IntPtr TD_SHIELD_ICON = new IntPtr(65532); // success-ish shield

        public static void CookieReminder(IntPtr owner, bool needsRestart, out bool dontShowAgain)
        {
            dontShowAgain = false;

            string content = "Delete all roblox.com cookies in your browser if you were logged in " +
                             "with any accounts. Otherwise your old identity can be linked back to the new one.";
            if (needsRestart)
                content += "\n\nRestart your PC to finish applying the highlighted adapters.";

            TASKDIALOGCONFIG cfg = new TASKDIALOGCONFIG();
            cfg.cbSize = (uint)Marshal.SizeOf(typeof(TASKDIALOGCONFIG));
            cfg.hwndParent = owner;
            cfg.dwFlags = TDF_ALLOW_DIALOG_CANCELLATION | TDF_POSITION_RELATIVE_TO_WINDOW;
            cfg.dwCommonButtons = TDCBF_OK_BUTTON;
            cfg.pszWindowTitle = "Roblox Spoofer";
            cfg.hMainIcon = TD_WARNING_ICON;
            cfg.pszMainInstruction = "You are spoofed";
            cfg.pszContent = content;
            cfg.pszVerificationText = "Don't show this again";

            try
            {
                int button, radio;
                bool verified;
                int hr = TaskDialogIndirect(ref cfg, out button, out radio, out verified);
                if (hr == 0) { dontShowAgain = verified; return; }
            }
            catch { }

            // Fallback for older Windows without TaskDialog
            MessageBox.Show(content, "You are spoofed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // A Roblox install / launcher we can clean.
    class RbxTarget
    {
        public string Name;
        public string Note;
        public bool Detected;
        public bool Selected = true;
        public bool IsCustom;
        public string Path;
        public List<string> Cookies = new List<string>();
        public List<string> DeepDirs = new List<string>();
        public RbxTarget(string name, bool detected) { Name = name; Detected = detected; }
    }

    // Small square checkbox.
    class MiniCheck : Control
    {
        bool chk = true;
        public bool Checked { get { return chk; } set { chk = value; Invalidate(); } }
        public MiniCheck()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            Size = new Size(20, 20);
        }
        protected override void OnClick(EventArgs e) { chk = !chk; Invalidate(); base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 5))
            {
                if (chk)
                {
                    using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.White, 2f))
                    {
                        g.DrawLines(pen, new Point[] {
                            new Point(5, Height / 2), new Point(Width / 2 - 1, Height - 6), new Point(Width - 5, 5)
                        });
                    }
                }
                else
                {
                    using (Pen pen = new Pen(Theme.CardEdge, 1.5f)) g.DrawPath(pen, p);
                }
            }
        }
    }

    // ---- Persisted settings (registry) ----
    static class Settings
    {
        const string AppKey = @"Software\RblxSpoofer";
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunName = "RobloxSpoofer";

        public static bool NotifDisabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AppKey))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("HideCookieNotif");
                        return v != null && v.ToString() == "1";
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetNotifDisabled(bool disabled)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
                    if (k != null) k.SetValue("HideCookieNotif", disabled ? 1 : 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        public static string[] CustomFolders()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AppKey))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("CustomFolders");
                        if (v is string[]) return (string[])v;
                    }
                }
            }
            catch { }
            return new string[0];
        }

        public static void AddCustomFolder(string path)
        {
            try
            {
                List<string> list = new List<string>(CustomFolders());
                foreach (string s in list)
                    if (string.Equals(s, path, StringComparison.OrdinalIgnoreCase)) return;
                list.Add(path);
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
                    if (k != null) k.SetValue("CustomFolders", list.ToArray(), RegistryValueKind.MultiString);
            }
            catch { }
        }

        public static void RemoveCustomFolder(string path)
        {
            try
            {
                List<string> list = new List<string>();
                foreach (string s in CustomFolders())
                    if (!string.Equals(s, path, StringComparison.OrdinalIgnoreCase)) list.Add(s);
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
                    if (k != null) k.SetValue("CustomFolders", list.ToArray(), RegistryValueKind.MultiString);
            }
            catch { }
        }

        public static bool DeepClean()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AppKey))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("DeepClean");
                        return v != null && v.ToString() == "1";
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetDeepClean(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
                    if (k != null) k.SetValue("DeepClean", on ? 1 : 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        public static bool StartWithWindows()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    if (k != null) return k.GetValue(RunName) != null;
            }
            catch { }
            return false;
        }

        public static void SetStartWithWindows(bool enabled)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (k == null) return;
                    if (enabled) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
                    else if (k.GetValue(RunName) != null) k.DeleteValue(RunName);
                }
            }
            catch { }
        }
    }

    // ---- iOS-style toggle ----
    class ToggleSwitch : Control
    {
        bool on;
        public event EventHandler Toggled;
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            Size = new Size(46, 24);
        }
        public bool On
        {
            get { return on; }
            set { on = value; Invalidate(); }
        }
        protected override void OnClick(EventArgs e)
        {
            on = !on;
            Invalidate();
            if (Toggled != null) Toggled(this, EventArgs.Empty);
            base.OnClick(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new Rectangle(0, 0, Width - 1, Height - 1);
            Color trackColor = on ? Theme.Accent : Color.FromArgb(60, 66, 82);
            using (GraphicsPath p = Theme.Round(track, Height / 2))
            using (SolidBrush b = new SolidBrush(trackColor))
                g.FillPath(b, p);

            int d = Height - 8;
            int kx = on ? Width - d - 4 : 4;
            using (SolidBrush kb = new SolidBrush(Color.White))
                g.FillEllipse(kb, kx, 4, d, d);
        }
    }

    class SettingsForm : Form
    {
        public SettingsForm()
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            ClientSize = new Size(360, 230);
            Font = new Font("Segoe UI", 9f);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Label title = new Label();
            title.Text = "Settings";
            title.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(22, 18);
            Controls.Add(title);

            BuildToggleRow("Deep clean", "Also wipe cache & logs (slower, more thorough).", 58,
                Settings.DeepClean(),
                new EventHandler(delegate(object s, EventArgs e) { Settings.SetDeepClean(((ToggleSwitch)s).On); }));

            BuildToggleRow("Start with Windows", "Launch the spoofer when you sign in.", 112,
                Settings.StartWithWindows(),
                new EventHandler(delegate(object s, EventArgs e) { Settings.SetStartWithWindows(((ToggleSwitch)s).On); }));

            BuildToggleRow("Disable notifications", "Skip the reminder popup after spoofing.", 166,
                Settings.NotifDisabled(),
                new EventHandler(delegate(object s, EventArgs e) { Settings.SetNotifDisabled(((ToggleSwitch)s).On); }));
        }

        void BuildToggleRow(string title, string sub, int y, bool initial, EventHandler onToggle)
        {
            Label t = new Label();
            t.Text = title;
            t.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            t.ForeColor = Theme.Text;
            t.AutoSize = true;
            t.Location = new Point(22, y);
            Controls.Add(t);

            Label s = new Label();
            s.Text = sub;
            s.Font = new Font("Segoe UI", 8.25f);
            s.ForeColor = Theme.Muted;
            s.AutoSize = true;
            s.Location = new Point(22, y + 20);
            Controls.Add(s);

            ToggleSwitch tog = new ToggleSwitch();
            tog.On = initial;
            tog.Location = new Point(ClientSize.Width - 46 - 22, y + 4);
            tog.BackColor = Theme.Bg;
            tog.Toggled += onToggle;
            Controls.Add(tog);
        }
    }

    // ================= Browser cookie cleaner =================

    // Windows' built-in SQLite (winsqlite3.dll) — no bundled dependency.
    static class NativeSqlite
    {
        [DllImport("winsqlite3.dll", CharSet = CharSet.Unicode)]
        static extern int sqlite3_open16(string filename, out IntPtr db);
        [DllImport("winsqlite3.dll", CharSet = CharSet.Unicode)]
        static extern int sqlite3_prepare16_v2(IntPtr db, string sql, int nByte, out IntPtr stmt, IntPtr tail);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_step(IntPtr stmt);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_column_int(IntPtr stmt, int col);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_finalize(IntPtr stmt);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_close(IntPtr db);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_changes(IntPtr db);
        [DllImport("winsqlite3.dll")]
        static extern int sqlite3_busy_timeout(IntPtr db, int ms);

        // Count roblox cookies by querying a shared-read copy (works while the browser is open).
        public static int CountRoblox(string dbPath, string table, string col)
        {
            string tmp = null;
            try
            {
                tmp = CopyShared(dbPath);
                if (tmp == null) return -1;
                IntPtr db;
                if (sqlite3_open16(tmp, out db) != 0) return -1;
                int n = -1;
                IntPtr stmt;
                string sql = "SELECT COUNT(*) FROM " + table + " WHERE " + col + " LIKE '%roblox%'";
                if (sqlite3_prepare16_v2(db, sql, -1, out stmt, IntPtr.Zero) == 0)
                {
                    if (sqlite3_step(stmt) == 100) n = sqlite3_column_int(stmt, 0);
                    sqlite3_finalize(stmt);
                }
                sqlite3_close(db);
                return n;
            }
            catch { return -1; }
            finally { CleanupTemp(tmp); }
        }

        // Delete roblox cookies from the real DB (browser must be closed). Returns rows removed, or -1.
        public static int DeleteRoblox(string dbPath, string table, string col)
        {
            try
            {
                IntPtr db;
                if (sqlite3_open16(dbPath, out db) != 0) return -1;
                sqlite3_busy_timeout(db, 3000);
                int changed = -1;
                IntPtr stmt;
                string sql = "DELETE FROM " + table + " WHERE " + col + " LIKE '%roblox%'";
                if (sqlite3_prepare16_v2(db, sql, -1, out stmt, IntPtr.Zero) == 0)
                {
                    sqlite3_step(stmt);
                    sqlite3_finalize(stmt);
                    changed = sqlite3_changes(db);
                }
                sqlite3_close(db);
                return changed;
            }
            catch { return -1; }
        }

        static string CopyShared(string src)
        {
            if (!File.Exists(src)) return null;
            string tmp = Path.GetTempFileName();
            CopyOne(src, tmp);
            CopyOne(src + "-wal", tmp + "-wal");
            CopyOne(src + "-shm", tmp + "-shm");
            return tmp;
        }

        static void CopyOne(string src, string dst)
        {
            try
            {
                if (!File.Exists(src)) return;
                using (FileStream fin = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (FileStream fout = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
                    fin.CopyTo(fout);
            }
            catch { }
        }

        static void CleanupTemp(string tmp)
        {
            if (tmp == null) return;
            try { File.Delete(tmp); } catch { }
            try { File.Delete(tmp + "-wal"); } catch { }
            try { File.Delete(tmp + "-shm"); } catch { }
        }
    }

    class BrowserInfo
    {
        public string Name;
        public string Proc;         // process name to close / detect
        public string Table = "cookies";
        public string Col = "host_key";
        public List<string> Dbs = new List<string>();
        public bool Running;
        public int RobloxCount = -1;   // -1 = unknown (running/unreadable)
        public bool Selected;
    }

    static class BrowserCookies
    {
        public static List<BrowserInfo> Detect()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<BrowserInfo> list = new List<BrowserInfo>();

            AddChromium(list, "Google Chrome", Path.Combine(local, @"Google\Chrome\User Data"), "chrome");
            AddChromium(list, "Microsoft Edge", Path.Combine(local, @"Microsoft\Edge\User Data"), "msedge");
            AddChromium(list, "Brave", Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data"), "brave");
            AddChromium(list, "Vivaldi", Path.Combine(local, @"Vivaldi\User Data"), "vivaldi");
            AddChromium(list, "Opera", Path.Combine(appdata, @"Opera Software\Opera Stable"), "opera");
            AddChromium(list, "Opera GX", Path.Combine(appdata, @"Opera Software\Opera GX Stable"), "opera");
            AddFirefox(list, Path.Combine(appdata, @"Mozilla\Firefox\Profiles"), "firefox");

            return list;
        }

        static void AddChromium(List<BrowserInfo> list, string name, string root, string proc)
        {
            if (!Directory.Exists(root)) return;
            List<string> dbs = new List<string>();
            List<string> profiles = new List<string>();
            profiles.Add(root); // Opera keeps cookies at the root
            try
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string nm = Path.GetFileName(dir);
                    if (nm == "Default" || nm.StartsWith("Profile") || nm == "Guest Profile") profiles.Add(dir);
                }
            }
            catch { }
            foreach (string p in profiles)
            {
                string a = Path.Combine(p, @"Network\Cookies");
                string b = Path.Combine(p, "Cookies");
                if (File.Exists(a)) { if (!dbs.Contains(a)) dbs.Add(a); }
                else if (File.Exists(b)) { if (!dbs.Contains(b)) dbs.Add(b); }
            }
            if (dbs.Count == 0) return;
            BrowserInfo bi = new BrowserInfo();
            bi.Name = name; bi.Proc = proc; bi.Table = "cookies"; bi.Col = "host_key"; bi.Dbs = dbs;
            bi.Running = Process.GetProcessesByName(proc).Length > 0;
            list.Add(bi);
        }

        static void AddFirefox(List<BrowserInfo> list, string profilesRoot, string proc)
        {
            if (!Directory.Exists(profilesRoot)) return;
            List<string> dbs = new List<string>();
            try
            {
                foreach (string dir in Directory.GetDirectories(profilesRoot))
                {
                    string db = Path.Combine(dir, "cookies.sqlite");
                    if (File.Exists(db)) dbs.Add(db);
                }
            }
            catch { }
            if (dbs.Count == 0) return;
            BrowserInfo bi = new BrowserInfo();
            bi.Name = "Firefox"; bi.Proc = proc; bi.Table = "moz_cookies"; bi.Col = "host"; bi.Dbs = dbs;
            bi.Running = Process.GetProcessesByName(proc).Length > 0;
            list.Add(bi);
        }

        public static void Scan(List<BrowserInfo> list)
        {
            foreach (BrowserInfo b in list)
            {
                int total = 0; bool anyOk = false;
                foreach (string db in b.Dbs)
                {
                    int c = NativeSqlite.CountRoblox(db, b.Table, b.Col);
                    if (c >= 0) { total += c; anyOk = true; }
                }
                b.RobloxCount = anyOk ? total : -1;
            }
        }

        public static int Clear(BrowserInfo b)
        {
            int removed = 0;
            foreach (string db in b.Dbs)
            {
                int c = NativeSqlite.DeleteRoblox(db, b.Table, b.Col);
                if (c > 0) removed += c;
            }
            return removed;
        }

        public static void Close(BrowserInfo b)
        {
            foreach (Process p in Process.GetProcessesByName(b.Proc))
                try { p.Kill(); } catch { }
        }

        // Known browsers we can't clean — surfaced so the user handles them manually.
        public static List<string> DetectUnsupported()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<string> found = new List<string>();
            AddIf(found, "Yandex", Path.Combine(local, @"Yandex\YandexBrowser\User Data"));
            AddIf(found, "Pale Moon", Path.Combine(appdata, @"Moonchild Productions\Pale Moon\Profiles"));
            AddIf(found, "Waterfox", Path.Combine(appdata, @"Waterfox\Profiles"));
            AddIf(found, "LibreWolf", Path.Combine(appdata, @"librewolf\Profiles"));
            AddIf(found, "Maxthon", Path.Combine(appdata, "Maxthon"));
            return found;
        }

        static void AddIf(List<string> list, string name, string path)
        {
            try { if (Directory.Exists(path)) list.Add(name); } catch { }
        }
    }

    // "You still need to clear your browser cookies" prompt.
    class CookiePromptForm : Form
    {
        public bool DeleteForMe = false;

        public CookiePromptForm()
        {
            Text = "One step left";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg; ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            const int W = 400, M = 26;
            int inner = W - M * 2;

            Label title = new Label();
            title.Text = "Almost done";
            title.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(M, 26);
            Controls.Add(title);

            Label msg = new Label();
            msg.Text = "You still need to clear your Roblox browser cookies for the spoof to fully work.\r\n\r\nWant the spoofer to find and delete them for you?";
            msg.Font = new Font("Segoe UI", 9.75f);
            msg.ForeColor = Theme.Muted;
            msg.AutoSize = false;
            msg.Size = new Size(inner, 76);
            msg.Location = new Point(M, 62);
            Controls.Add(msg);

            AccentButton yes = new AccentButton();
            yes.Text = "DELETE THEM FOR ME";
            yes.Size = new Size(inner, 46);
            yes.Location = new Point(M, 150);
            yes.Click += new EventHandler(delegate(object s, EventArgs e) { DeleteForMe = true; DialogResult = DialogResult.OK; Close(); });
            Controls.Add(yes);

            Button no = new Button();
            no.Text = "I'll do it myself";
            no.Size = new Size(inner, 36);
            no.Location = new Point(M, 204);
            no.FlatStyle = FlatStyle.Flat;
            no.FlatAppearance.BorderColor = Theme.CardEdge;
            no.FlatAppearance.MouseOverBackColor = Theme.Card;
            no.ForeColor = Theme.Muted;
            no.BackColor = Theme.Bg;
            no.Font = new Font("Segoe UI", 9f);
            no.Cursor = Cursors.Hand;
            no.Click += new EventHandler(delegate(object s, EventArgs e) { DeleteForMe = false; DialogResult = DialogResult.Cancel; Close(); });
            Controls.Add(no);

            ClientSize = new Size(W, 264);
        }
    }

    // Browser selection + clear.
    class BrowserSelectForm : Form
    {
        List<BrowserInfo> browsers;
        List<MiniCheck> checks = new List<MiniCheck>();

        public BrowserSelectForm(List<BrowserInfo> list)
        {
            browsers = list;
            Text = "Clear Roblox cookies";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg; ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            const int W = 424;
            const int M = 24;
            int inner = W - M * 2;
            int y = 24;

            Label title = new Label();
            title.Text = "Clear Roblox cookies";
            title.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(M, y);
            Controls.Add(title);
            y += 32;

            Label sub = new Label();
            sub.Text = "Pick which browsers to clean. Running ones will be closed first.";
            sub.Font = new Font("Segoe UI", 8.75f);
            sub.ForeColor = Theme.Muted;
            sub.AutoSize = false;
            sub.Size = new Size(inner, 18);
            sub.Location = new Point(M, y);
            Controls.Add(sub);
            y += 30;

            // browser list card
            int listH = browsers.Count * 34 + 16;
            Card listCard = new Card();
            listCard.Location = new Point(M, y);
            listCard.Size = new Size(inner, listH);
            Controls.Add(listCard);

            int ry = 12;
            foreach (BrowserInfo b in browsers)
            {
                MiniCheck chk = new MiniCheck();
                chk.Checked = b.RobloxCount > 0 || (b.Running && b.RobloxCount < 0);
                chk.Location = new Point(16, ry + 3);
                checks.Add(chk);
                listCard.Controls.Add(chk);

                Label n = new Label();
                n.Text = b.Name;
                n.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                n.ForeColor = Theme.Text;
                n.BackColor = Theme.Card;
                n.AutoSize = true;
                n.Location = new Point(46, ry + 4);
                listCard.Controls.Add(n);

                string statusText; Color statusColor;
                if (b.Running) { statusText = "running — will close"; statusColor = Theme.Yellow; }
                else if (b.RobloxCount > 0) { statusText = b.RobloxCount + " Roblox cookie" + (b.RobloxCount == 1 ? "" : "s"); statusColor = Theme.Green; }
                else if (b.RobloxCount == 0) { statusText = "no Roblox cookies"; statusColor = Theme.Faint; }
                else { statusText = "couldn't read"; statusColor = Theme.Faint; }

                Label st = new Label();
                st.Text = statusText;
                st.Font = new Font("Segoe UI", 8.5f);
                st.ForeColor = statusColor;
                st.BackColor = Theme.Card;
                st.AutoSize = true;
                st.Location = new Point(inner - 16 - TextRenderer.MeasureText(statusText, st.Font).Width, ry + 5);
                listCard.Controls.Add(st);

                ry += 34;
            }
            y += listH + 14;

            // unsupported note
            List<string> unsup = BrowserCookies.DetectUnsupported();
            if (unsup.Count > 0)
            {
                Label u = new Label();
                u.Text = "Not supported — clear manually: " + string.Join(", ", unsup.ToArray());
                u.Font = new Font("Segoe UI", 8.25f);
                u.ForeColor = Theme.Yellow;
                u.AutoSize = false;
                u.Size = new Size(inner, 30);
                u.Location = new Point(M, y);
                Controls.Add(u);
                y += 34;
            }

            // primary action
            AccentButton clear = new AccentButton();
            clear.Text = "CLEAR SELECTED";
            clear.Size = new Size(inner, 44);
            clear.Location = new Point(M, y);
            clear.Click += new EventHandler(delegate(object s, EventArgs e) { DoClear(); });
            Controls.Add(clear);
            y += 52;

            // secondary row: Clear all | Cancel
            int half = (inner - 10) / 2;
            Button all = MakeFlat("Clear all", M, y, half);
            all.Click += new EventHandler(delegate(object s, EventArgs e)
            {
                foreach (MiniCheck c in checks) c.Checked = true;
                DoClear();
            });
            Controls.Add(all);

            Button cancel = MakeFlat("Cancel", M + half + 10, y, inner - half - 10);
            cancel.Click += new EventHandler(delegate(object s, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); });
            Controls.Add(cancel);
            y += 36;

            ClientSize = new Size(W, y + 20);
        }

        Button MakeFlat(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(w, 36);
            b.Location = new Point(x, y);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Theme.CardEdge;
            b.FlatAppearance.MouseOverBackColor = Theme.Card;
            b.ForeColor = Theme.Muted;
            b.BackColor = Theme.Bg;
            b.Font = new Font("Segoe UI", 9f);
            b.Cursor = Cursors.Hand;
            return b;
        }

        void DoClear()
        {
            List<BrowserInfo> chosen = new List<BrowserInfo>();
            for (int i = 0; i < browsers.Count; i++)
                if (checks[i].Checked) chosen.Add(browsers[i]);

            if (chosen.Count == 0)
            {
                MessageBox.Show(this, "Select at least one browser first.", "Nothing selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Warn before closing running browsers.
            List<string> toClose = new List<string>();
            foreach (BrowserInfo b in chosen) if (b.Running) toClose.Add(b.Name);
            if (toClose.Count > 0)
            {
                DialogResult r = MessageBox.Show(this,
                    "These browsers will be CLOSED so their cookies can be cleared:\n\n" +
                    "    " + string.Join(", ", toClose.ToArray()) + "\n\n" +
                    "Save any open work first. Continue?",
                    "Browsers will close", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (r != DialogResult.OK) return;
            }

            Cursor = Cursors.WaitCursor;
            bool closedAny = false;
            foreach (BrowserInfo b in chosen)
                if (b.Running) { BrowserCookies.Close(b); closedAny = true; }
            if (closedAny) System.Threading.Thread.Sleep(1800);

            int total = 0;
            List<string> done = new List<string>();
            foreach (BrowserInfo b in chosen)
            {
                int n = BrowserCookies.Clear(b);
                if (n > 0) { total += n; done.Add(b.Name + " (" + n + ")"); }
            }
            Cursor = Cursors.Default;

            if (total > 0)
                MessageBox.Show(this, "Cleared " + total + " Roblox cookie" + (total == 1 ? "" : "s") + " from:\n\n    " +
                    string.Join(", ", done.ToArray()), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(this, "No Roblox cookies were found to delete in the selected browsers.",
                    "Nothing to clear", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
