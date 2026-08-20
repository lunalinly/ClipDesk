using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("ClipDesk")]
[assembly: AssemblyDescription("Lightweight Windows clipboard manager")]
[assembly: AssemblyCompany("ClipDesk")]
[assembly: AssemblyProduct("ClipDesk")]
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

namespace ClipDeskNative {
  public class ClipItem {
    public string Id { get; set; }
    public string Text { get; set; }
    public string Title { get; set; }
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Used { get; set; }
    public string CategoryPath { get; set; }
  }
  public class AppSettings {
    public string StaffName { get; set; }
    public string WorkStart { get; set; }
    public string RestStart { get; set; }
    public string RestEnd { get; set; }
    public string WorkEnd { get; set; }
  }
  public class StoreData {
    public List<ClipItem> Items { get; set; }
    public AppSettings Settings { get; set; }
    public List<string> Categories { get; set; }
  }

  public class CleanButton : Button {
    protected override bool ShowFocusCues { get { return false; } }
  }

  public sealed class MainForm : Form {
#if PURPLE_THEME
    readonly Color Bg = Color.FromArgb(13, 9, 18);
    readonly Color Row = Color.FromArgb(34, 24, 44);
    readonly Color RowAlt = Color.FromArgb(25, 18, 34);
    readonly Color TextColor = Color.FromArgb(244, 238, 250);
    readonly Color Muted = Color.FromArgb(180, 163, 194);
    readonly Color Accent = Color.FromArgb(168, 85, 247);
    readonly Color AccentSoft = Color.FromArgb(67, 38, 91);
    readonly Color Surface = Color.FromArgb(24, 17, 32);
    readonly Color ButtonColor = Color.FromArgb(48, 33, 62);
    readonly Color ButtonHover = Color.FromArgb(72, 45, 94);
    readonly Color NavInactive = Color.FromArgb(41, 28, 53);
    readonly Color TopSurface = Color.FromArgb(11, 7, 15);
    readonly Color NavSurface = Color.FromArgb(18, 12, 25);
    readonly Color CategorySurface = Color.FromArgb(15, 10, 21);
    readonly Color AccentHover = Color.FromArgb(192, 132, 252);
    readonly Color AccentText = Color.FromArgb(224, 196, 255);
    readonly Color Divider = Color.FromArgb(61, 42, 75);
#else
    readonly Color Bg = Color.FromArgb(18, 19, 22);
    readonly Color Row = Color.FromArgb(31, 32, 36);
    readonly Color RowAlt = Color.FromArgb(25, 26, 29);
    readonly Color TextColor = Color.FromArgb(232, 232, 235);
    readonly Color Muted = Color.FromArgb(155, 158, 165);
    readonly Color Accent = Color.FromArgb(74, 144, 255);
    readonly Color AccentSoft = Color.FromArgb(28, 57, 94);
    readonly Color Surface = Color.FromArgb(23, 25, 30);
    readonly Color ButtonColor = Color.FromArgb(38, 41, 48);
    readonly Color ButtonHover = Color.FromArgb(53, 58, 68);
    readonly Color NavInactive = Color.FromArgb(31, 34, 41);
    readonly Color TopSurface = Color.FromArgb(12, 14, 18);
    readonly Color NavSurface = Color.FromArgb(17, 19, 24);
    readonly Color CategorySurface = Color.FromArgb(14, 16, 20);
    readonly Color AccentHover = Color.FromArgb(92, 160, 255);
    readonly Color AccentText = Color.FromArgb(180, 215, 255);
    readonly Color Divider = Color.FromArgb(46, 49, 57);
#endif
    readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipDesk");
    readonly string dataFile;
    readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
    readonly List<ClipItem> items = new List<ClipItem>();
    readonly List<ClipItem> shown = new List<ClipItem>();
    readonly List<string> categories = new List<string> { "開頭", "中間", "結尾", "未分類", "其他" };
    readonly ListBox clipList = new ListBox();
    readonly TreeView categoryTree = new TreeView();
    readonly Panel categoryPanel = new Panel();
    readonly TextBox searchBox = new TextBox();
    readonly Label countLabel = new Label();
    readonly Timer clipboardTimer = new Timer();
    readonly Timer focusTimer = new Timer();
    readonly CleanButton topButton = new CleanButton();
    readonly TabControl tabs = new TabControl();
    readonly ToolTip hoverTip = new ToolTip();
    int hoverClipIndex = -1;
    TreeNode hoverCategoryNode;
    Button clipsNavButton;
    Button attendanceNavButton;
    TextBox staffName;
    TextBox workStart, restStart, restEnd, workEnd;
    AppSettings settings = Defaults();
    string lastClipboard = "";
    string selectedCategory = "全部";
    bool rebuildingTree = false;
    IntPtr previousWindow = IntPtr.Zero;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern IntPtr GetWindow(IntPtr hWnd, uint command);
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);
    const uint GW_HWNDNEXT = 2;

    public MainForm() {
      dataFile = Path.Combine(dataDir, "data.json");
      Text = "ClipDesk";
      Width = 360;
      Height = 680;
      MinimumSize = new Size(300, 420);
      StartPosition = FormStartPosition.CenterScreen;
#if CUSTOM_CHROME
      FormBorderStyle = FormBorderStyle.None;
      Padding = new Padding(1);
      BackColor = AccentSoft;
#else
      BackColor = Bg;
#endif
      ForeColor = TextColor;
      Font = new Font("Microsoft JhengHei UI", 9F);
      AutoScaleMode = AutoScaleMode.Dpi;
      DoubleBuffered = true;
      serializer.MaxJsonLength = Int32.MaxValue;
      hoverTip.InitialDelay = 350;
      hoverTip.ReshowDelay = 100;
      hoverTip.AutoPopDelay = 30000;
      hoverTip.ShowAlways = true;
      hoverTip.BackColor = Surface;
      hoverTip.ForeColor = TextColor;
      BuildUi();
      LoadData();
      RebuildCategoryTree();
      RefreshList();
      Shown += delegate {
        if (Environment.GetEnvironmentVariable("CLIPDESK_NATIVE_SCREENSHOT_TAB") == "attendance") tabs.SelectedIndex = 1;
        UpdateNavigation();
        if (categoryTree.SelectedNode != null) categoryTree.SelectedNode.EnsureVisible();
        CaptureClipboard();
        string screenshotPath = Environment.GetEnvironmentVariable("CLIPDESK_NATIVE_SCREENSHOT");
        if (!String.IsNullOrWhiteSpace(screenshotPath)) {
          Timer screenshotTimer = new Timer();
          screenshotTimer.Interval = 450;
          screenshotTimer.Tick += delegate {
            screenshotTimer.Stop();
            using (Bitmap image = new Bitmap(ClientSize.Width, ClientSize.Height)) {
              DrawToBitmap(image, new Rectangle(Point.Empty, ClientSize));
              image.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            Close();
          };
          screenshotTimer.Start();
          return;
        }
        IntPtr fallback = GetWindow(Handle, GW_HWNDNEXT);
        if (fallback != IntPtr.Zero && fallback != Handle) previousWindow = fallback;
        focusTimer.Interval = 150;
        focusTimer.Tick += delegate { TrackForegroundWindow(); };
        focusTimer.Start();
        clipboardTimer.Interval = 650;
        clipboardTimer.Tick += delegate { CaptureClipboard(); };
        clipboardTimer.Start();
      };
      FormClosing += delegate { focusTimer.Stop(); clipboardTimer.Stop(); SaveData(); };
    }

    static AppSettings Defaults() {
      return new AppSettings { StaffName = "", WorkStart = "10:00", RestStart = "13:00", RestEnd = "14:00", WorkEnd = "19:00" };
    }

    Button FlatButton(string text, EventHandler click) {
      CleanButton b = new CleanButton();
      b.Text = text;
      b.Height = 30;
      b.AutoSize = false;
      b.FlatStyle = FlatStyle.Flat;
      b.FlatAppearance.BorderSize = 0;
      b.BackColor = ButtonColor;
      b.ForeColor = TextColor;
      b.Margin = new Padding(2);
      b.Padding = new Padding(6, 0, 6, 0);
      b.Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold);
      b.Width = Math.Max(36, TextRenderer.MeasureText(text, b.Font).Width + 22);
      b.Cursor = Cursors.Hand;
      b.TabStop = false;
      b.UseVisualStyleBackColor = false;
      b.MouseEnter += delegate { b.BackColor = ButtonHover; };
      b.MouseLeave += delegate { b.BackColor = ButtonColor; };
      b.Click += click;
      return b;
    }

    void UpdateNavigation() {
      if (clipsNavButton == null || attendanceNavButton == null) return;
      bool clipsActive = tabs.SelectedIndex == 0;
      clipsNavButton.BackColor = clipsActive ? Accent : NavInactive;
      clipsNavButton.ForeColor = Color.White;
      attendanceNavButton.BackColor = clipsActive ? NavInactive : Accent;
      attendanceNavButton.ForeColor = Color.White;
    }

    void UseDarkNativeTheme(Control control) {
      EventHandler apply = delegate { SetWindowTheme(control.Handle, "DarkMode_Explorer", null); };
      control.HandleCreated += apply;
      if (control.IsHandleCreated) apply(control, EventArgs.Empty);
    }

    void BuildUi() {
      FlowLayoutPanel top = new FlowLayoutPanel();
      top.Dock = DockStyle.Top;
      top.Height = 42;
      top.Padding = new Padding(5, 4, 2, 3);
      top.BackColor = TopSurface;
      top.WrapContents = false;
      top.Controls.Add(FlatButton("＋", delegate { EditItem(null); }));
      top.Controls.Add(FlatButton("編輯", delegate { EditSelected(); }));
      top.Controls.Add(FlatButton("刪除", delegate { DeleteSelected(); }));
      top.Controls.Add(FlatButton("分類", delegate { categoryPanel.Visible = !categoryPanel.Visible; }));
      topButton.Text = "置頂";
      topButton.Height = 30;
      topButton.AutoSize = false;
      topButton.Width = 58;
      topButton.FlatStyle = FlatStyle.Flat;
      topButton.FlatAppearance.BorderSize = 0;
      topButton.BackColor = ButtonColor;
      topButton.ForeColor = TextColor;
      topButton.Margin = new Padding(2);
      topButton.Padding = new Padding(6, 0, 6, 0);
      topButton.Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold);
      topButton.Width = Math.Max(58, TextRenderer.MeasureText(topButton.Text, topButton.Font).Width + 22);
      topButton.Cursor = Cursors.Hand;
      topButton.TabStop = false;
      topButton.UseVisualStyleBackColor = false;
      topButton.Click += delegate {
        TopMost = !TopMost;
        topButton.Text = TopMost ? "已置頂" : "置頂";
        topButton.Width = Math.Max(58, TextRenderer.MeasureText(topButton.Text, topButton.Font).Width + 22);
        topButton.BackColor = TopMost ? Accent : ButtonColor;
      };
      top.Controls.Add(topButton);

      FlowLayoutPanel nav = new FlowLayoutPanel();
      nav.Dock = DockStyle.Top;
      nav.Height = 38;
      nav.Padding = new Padding(6, 3, 4, 3);
      nav.WrapContents = false;
      nav.BackColor = NavSurface;
      clipsNavButton = FlatButton("剪貼簿", delegate { tabs.SelectedIndex = 0; UpdateNavigation(); });
      attendanceNavButton = FlatButton("出勤通知", delegate { tabs.SelectedIndex = 1; UpdateNavigation(); });
      clipsNavButton.MouseLeave += delegate { UpdateNavigation(); };
      attendanceNavButton.MouseLeave += delegate { UpdateNavigation(); };
      nav.Controls.Add(clipsNavButton);
      nav.Controls.Add(attendanceNavButton);
      ContextMenuStrip backupMenu = new ContextMenuStrip();
      backupMenu.Items.Add("匯出備份", null, delegate { ExportBackup(); });
      backupMenu.Items.Add("匯入備份", null, delegate { ImportBackup(); });
      Button backupButton = null;
      backupButton = FlatButton("備份", delegate { backupMenu.Show(backupButton, new Point(0, backupButton.Height)); });
      nav.Controls.Add(backupButton);

      Panel tabHost = new Panel();
      tabHost.Dock = DockStyle.Fill;
      tabHost.BackColor = Bg;
      tabHost.Padding = new Padding(0);
      tabs.Dock = DockStyle.None;
      tabs.Location = new Point(-4, -6);
      tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      tabHost.Resize += delegate { tabs.SetBounds(-4, -6, tabHost.ClientSize.Width + 8, tabHost.ClientSize.Height + 12); };
      tabs.Appearance = TabAppearance.FlatButtons;
      tabs.DrawMode = TabDrawMode.Normal;
      tabs.ItemSize = new Size(0, 1);
      tabs.SizeMode = TabSizeMode.Fixed;
      tabs.Padding = new Point(0, 0);

      TabPage clipPage = new TabPage("剪貼簿");
      clipPage.BackColor = Bg;
      clipPage.Padding = new Padding(4);
      clipList.Dock = DockStyle.Fill;
      clipList.BackColor = Bg;
      clipList.ForeColor = TextColor;
      clipList.BorderStyle = BorderStyle.None;
      clipList.DrawMode = DrawMode.OwnerDrawFixed;
      clipList.ItemHeight = 56;
      clipList.IntegralHeight = false;
      clipList.TabStop = false;
      clipList.DrawItem += DrawClip;
      clipList.SelectedIndexChanged += delegate { clipList.Invalidate(); };
      clipList.MouseMove += ShowClipTooltip;
      clipList.MouseLeave += delegate { hoverClipIndex = -1; hoverTip.Hide(clipList); };
      clipList.DoubleClick += delegate { PasteSelected(); };
      clipList.KeyDown += delegate(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Enter) { CopySelected(); e.Handled = true; }
        if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
      };
      ContextMenuStrip menu = new ContextMenuStrip();
      menu.Items.Add("複製", null, delegate { CopySelected(); });
      menu.Items.Add("貼上到上一個視窗", null, delegate { PasteSelected(); });
      menu.Items.Add("編輯", null, delegate { EditSelected(); });
      menu.Items.Add("釘選／取消釘選", null, delegate { TogglePin(); });
      menu.Items.Add(new ToolStripSeparator());
      menu.Items.Add("刪除", null, delegate { DeleteSelected(); });
      menu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e) { e.Cancel = clipList.SelectedIndex < 0; };
      clipList.ContextMenuStrip = menu;
      clipList.MouseDown += delegate(object sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Right) {
          int index = clipList.IndexFromPoint(e.Location);
          if (index >= 0) clipList.SelectedIndex = index;
        }
      };

      Panel searchPanel = new Panel();
      searchPanel.Dock = DockStyle.Bottom;
      searchPanel.Height = 44;
      searchPanel.BackColor = TopSurface;
      searchPanel.Padding = new Padding(7);
      searchBox.Dock = DockStyle.Fill;
      searchBox.BorderStyle = BorderStyle.FixedSingle;
      searchBox.BackColor = Surface;
      searchBox.ForeColor = TextColor;
      searchBox.Font = new Font(Font.FontFamily, 10F);
      searchBox.TextChanged += delegate { RefreshList(); };
      countLabel.Dock = DockStyle.Right;
      countLabel.Width = 42;
      countLabel.TextAlign = ContentAlignment.MiddleCenter;
      countLabel.ForeColor = Muted;
      searchPanel.Controls.Add(searchBox);
      searchPanel.Controls.Add(countLabel);

      categoryPanel.Dock = DockStyle.Top;
      categoryPanel.Height = 220;
      categoryPanel.BackColor = CategorySurface;
      categoryPanel.Padding = new Padding(5);
      categoryTree.Dock = DockStyle.Fill;
      categoryTree.BackColor = Surface;
      categoryTree.ForeColor = TextColor;
      categoryTree.BorderStyle = BorderStyle.None;
      categoryTree.DrawMode = TreeViewDrawMode.OwnerDrawAll;
      categoryTree.ItemHeight = 24;
      categoryTree.Indent = 18;
      categoryTree.ShowLines = false;
      categoryTree.ShowRootLines = true;
      categoryTree.ShowPlusMinus = false;
      categoryTree.DrawNode += DrawCategoryNode;
      categoryTree.HideSelection = false;
      categoryTree.FullRowSelect = true;
      categoryTree.TabStop = false;
      categoryTree.AfterSelect += delegate(object sender, TreeViewEventArgs e) {
        if (rebuildingTree) return;
        selectedCategory = e.Node.Tag == null ? "全部" : e.Node.Tag.ToString();
        RefreshList();
      };
      categoryTree.NodeMouseClick += delegate(object sender, TreeNodeMouseClickEventArgs e) {
        if (e.Button == MouseButtons.Right) categoryTree.SelectedNode = e.Node;
        if (e.Button == MouseButtons.Left && e.Node.Nodes.Count > 0) e.Node.Toggle();
      };
      categoryTree.MouseMove += ShowCategoryTooltip;
      categoryTree.MouseLeave += delegate { hoverCategoryNode = null; hoverTip.Hide(categoryTree); };
      ContextMenuStrip categoryMenu = new ContextMenuStrip();
      categoryMenu.Items.Add("新增子分類", null, delegate { AddCategory(); });
      categoryMenu.Items.Add("重新命名", null, delegate { RenameCategory(); });
      categoryMenu.Items.Add("刪除分類", null, delegate { DeleteCategory(); });
      categoryMenu.Items.Add(new ToolStripSeparator());
      categoryMenu.Items.Add("清空未分類", null, delegate { ClearUncategorized(); });
      categoryTree.ContextMenuStrip = categoryMenu;
      FlowLayoutPanel categoryActions = new FlowLayoutPanel();
      categoryActions.Dock = DockStyle.Bottom;
      categoryActions.Height = 70;
      categoryActions.Padding = new Padding(0, 3, 0, 0);
      categoryActions.WrapContents = true;
      categoryActions.Controls.Add(FlatButton("＋子分類", delegate { AddCategory(); }));
      categoryActions.Controls.Add(FlatButton("改名", delegate { RenameCategory(); }));
      categoryActions.Controls.Add(FlatButton("刪除分類", delegate { DeleteCategory(); }));
      categoryActions.Controls.Add(FlatButton("清空未分類", delegate { ClearUncategorized(); }));
      categoryPanel.Controls.Add(categoryTree);
      categoryPanel.Controls.Add(categoryActions);

      clipPage.Controls.Add(clipList);
      clipPage.Controls.Add(searchPanel);
      clipPage.Controls.Add(categoryPanel);

      TabPage attendancePage = new TabPage("出勤通知");
      attendancePage.BackColor = Bg;
      attendancePage.Padding = new Padding(8);
      BuildAttendance(attendancePage);
      tabs.TabPages.Add(clipPage);
      tabs.TabPages.Add(attendancePage);
      tabs.SelectedIndexChanged += delegate { UpdateNavigation(); };
      tabs.HandleCreated += delegate { tabs.SelectedIndex = 0; UpdateNavigation(); };
      UseDarkNativeTheme(clipList);
      UseDarkNativeTheme(categoryTree);
      UseDarkNativeTheme(tabs);
      UseDarkNativeTheme(searchBox);
      tabHost.Controls.Add(tabs);
      Controls.Add(tabHost);
      Controls.Add(nav);
      Controls.Add(top);
#if CUSTOM_CHROME
      BuildPersonalTitleBar();
#endif
      UpdateNavigation();
    }

#if CUSTOM_CHROME
    void DragPersonalWindow(object sender, MouseEventArgs e) {
      if (e.Button != MouseButtons.Left) return;
      ReleaseCapture();
      SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
    }

    CleanButton TitleBarButton(string text) {
      CleanButton button = new CleanButton();
      button.Text = text;
      button.Dock = DockStyle.Right;
      button.Width = 40;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderSize = 0;
      button.BackColor = TopSurface;
      button.ForeColor = TextColor;
      button.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
      button.TabStop = false;
      button.UseVisualStyleBackColor = false;
      button.Cursor = Cursors.Hand;
      button.MouseEnter += delegate { button.BackColor = ButtonHover; };
      button.MouseLeave += delegate { button.BackColor = TopSurface; };
      return button;
    }

    void BuildPersonalTitleBar() {
      Panel bar = new Panel();
      bar.Dock = DockStyle.Top;
      bar.Height = 32;
      bar.BackColor = TopSurface;
      bar.MouseDown += DragPersonalWindow;
      bar.Paint += delegate(object sender, PaintEventArgs e) {
        using (Pen pen = new Pen(Divider)) e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
      };

      Label title = new Label();
      title.Text = "◆  ClipDesk";
      title.Dock = DockStyle.Fill;
      title.Padding = new Padding(9, 0, 0, 0);
      title.TextAlign = ContentAlignment.MiddleLeft;
      title.ForeColor = AccentText;
      title.BackColor = TopSurface;
      title.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
      title.MouseDown += DragPersonalWindow;

      CleanButton minimize = TitleBarButton("—");
      minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
      CleanButton close = TitleBarButton("×");
      close.Font = new Font("Segoe UI", 13F, FontStyle.Regular);
      close.MouseEnter += delegate { close.BackColor = Accent; close.ForeColor = Color.White; };
      close.MouseLeave += delegate { close.BackColor = TopSurface; close.ForeColor = TextColor; };
      close.Click += delegate { Close(); };

      bar.Controls.Add(title);
      bar.Controls.Add(minimize);
      bar.Controls.Add(close);
      Controls.Add(bar);
    }

    protected override void WndProc(ref Message message) {
      base.WndProc(ref message);
      if (message.Msg != 0x0084 || message.Result != new IntPtr(1) || WindowState == FormWindowState.Maximized) return;
      Point point = PointToClient(Cursor.Position);
      int grip = 6;
      bool left = point.X <= grip;
      bool right = point.X >= ClientSize.Width - grip;
      bool top = point.Y <= grip;
      bool bottom = point.Y >= ClientSize.Height - grip;
      if (left && top) message.Result = new IntPtr(13);
      else if (right && top) message.Result = new IntPtr(14);
      else if (left && bottom) message.Result = new IntPtr(16);
      else if (right && bottom) message.Result = new IntPtr(17);
      else if (left) message.Result = new IntPtr(10);
      else if (right) message.Result = new IntPtr(11);
      else if (top) message.Result = new IntPtr(12);
      else if (bottom) message.Result = new IntPtr(15);
    }
#endif

    bool IsFixedCategory(string path) {
      return path == "開頭" || path == "中間" || path == "結尾" || path == "未分類" || path == "其他";
    }

    string CleanCategoryName(string value) {
      return (value ?? "").Trim().Replace("/", "／").Replace("\\", "＼");
    }

    void RebuildCategoryTree() {
      rebuildingTree = true;
      string keep = selectedCategory;
      categoryTree.BeginUpdate();
      categoryTree.Nodes.Clear();
      TreeNode all = new TreeNode("全部");
      all.Tag = "全部";
      categoryTree.Nodes.Add(all);
      Dictionary<string, TreeNode> map = new Dictionary<string, TreeNode>();
      foreach (string root in new [] { "開頭", "中間", "結尾", "未分類", "其他" }) {
        if (!categories.Contains(root)) categories.Add(root);
        TreeNode node = new TreeNode(root);
        node.Tag = root;
        categoryTree.Nodes.Add(node);
        map[root] = node;
      }
      foreach (string path in categories.Distinct().OrderBy(x => x.Split('/').Length).ThenBy(x => x)) {
        if (IsFixedCategory(path)) continue;
        string[] parts = path.Split('/');
        if (parts.Length < 2 || !map.ContainsKey(parts[0])) continue;
        string built = parts[0];
        TreeNode parent = map[built];
        for (int i = 1; i < parts.Length; i++) {
          built += "/" + parts[i];
          TreeNode node;
          if (!map.TryGetValue(built, out node)) {
            node = new TreeNode(parts[i]);
            node.Tag = built;
            parent.Nodes.Add(node);
            map[built] = node;
          }
          parent = node;
        }
      }
      categoryTree.CollapseAll();
      TreeNode selected = FindCategoryNode(categoryTree.Nodes, keep);
      categoryTree.SelectedNode = selected ?? all;
      selectedCategory = categoryTree.SelectedNode.Tag.ToString();
      categoryTree.EndUpdate();
      rebuildingTree = false;
    }

    TreeNode FindCategoryNode(TreeNodeCollection nodes, string path) {
      foreach (TreeNode node in nodes) {
        if (node.Tag != null && node.Tag.ToString() == path) return node;
        TreeNode nested = FindCategoryNode(node.Nodes, path);
        if (nested != null) return nested;
      }
      return null;
    }

    string PromptText(string title, string label, string initial) {
      using (Form dialog = new Form()) {
        dialog.Text = title;
        dialog.Width = 310;
        dialog.Height = 145;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MinimizeBox = false;
        dialog.MaximizeBox = false;
        dialog.BackColor = Bg;
        dialog.ForeColor = TextColor;
        Label caption = new Label();
        caption.Text = label;
        caption.Dock = DockStyle.Top;
        caption.Height = 28;
        caption.Padding = new Padding(8, 8, 0, 0);
        TextBox input = new TextBox();
        input.Text = initial ?? "";
        input.Dock = DockStyle.Top;
        input.BackColor = Row;
        input.ForeColor = TextColor;
        FlowLayoutPanel buttons = new FlowLayoutPanel();
        buttons.Dock = DockStyle.Bottom;
        buttons.Height = 38;
        buttons.FlowDirection = FlowDirection.RightToLeft;
        Button ok = FlatButton("確定", delegate { dialog.DialogResult = DialogResult.OK; });
        Button cancel = FlatButton("取消", delegate { dialog.DialogResult = DialogResult.Cancel; });
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        Panel body = new Panel();
        body.Dock = DockStyle.Fill;
        body.Padding = new Padding(8);
        body.Controls.Add(input);
        dialog.Controls.Add(body);
        dialog.Controls.Add(caption);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        return CleanCategoryName(input.Text);
      }
    }

    void AddCategory() {
      string parent = selectedCategory;
      if (parent == "全部") {
        MessageBox.Show(this, "請先選擇一個主要分類或母分類。", "ClipDesk");
        return;
      }
      if (parent == "未分類") {
        MessageBox.Show(this, "「未分類」用來暫放剛複製的內容，不能建立子分類。請改用「其他」或其餘主要分類。", "ClipDesk");
        return;
      }
      string name = PromptText("新增子分類", "在「" + parent + "」下新增：", "");
      if (String.IsNullOrWhiteSpace(name)) return;
      string path = parent + "/" + name;
      if (categories.Contains(path)) {
        MessageBox.Show(this, "這個分類已經存在。", "ClipDesk");
        return;
      }
      categories.Add(path);
      selectedCategory = path;
      SaveData();
      RebuildCategoryTree();
      RefreshList();
    }

    void RenameCategory() {
      string oldPath = selectedCategory;
      if (oldPath == "全部" || IsFixedCategory(oldPath)) {
        MessageBox.Show(this, "固定主分類不能重新命名。", "ClipDesk");
        return;
      }
      string[] parts = oldPath.Split('/');
      string name = PromptText("重新命名", "新的分類名稱：", parts[parts.Length - 1]);
      if (String.IsNullOrWhiteSpace(name)) return;
      string parent = String.Join("/", parts.Take(parts.Length - 1).ToArray());
      string newPath = parent + "/" + name;
      if (newPath != oldPath && categories.Contains(newPath)) {
        MessageBox.Show(this, "這個分類已經存在。", "ClipDesk");
        return;
      }
      for (int i = 0; i < categories.Count; i++) {
        if (categories[i] == oldPath || categories[i].StartsWith(oldPath + "/"))
          categories[i] = newPath + categories[i].Substring(oldPath.Length);
      }
      foreach (ClipItem item in items) {
        string path = item.CategoryPath ?? "未分類";
        if (path == oldPath || path.StartsWith(oldPath + "/"))
          item.CategoryPath = newPath + path.Substring(oldPath.Length);
      }
      selectedCategory = newPath;
      SaveData();
      RebuildCategoryTree();
      RefreshList();
    }

    void ClearUncategorized() {
      int count = items.Count(x => String.IsNullOrWhiteSpace(x.CategoryPath) || x.CategoryPath == "未分類");
      if (count == 0) {
        MessageBox.Show(this, "目前沒有未分類內容。", "ClipDesk");
        return;
      }
      if (MessageBox.Show(this, "確定刪除全部 " + count + " 則未分類內容？其他分類不會受影響。", "ClipDesk", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
      items.RemoveAll(x => String.IsNullOrWhiteSpace(x.CategoryPath) || x.CategoryPath == "未分類");
      SaveData();
      RefreshList();
    }

    void DeleteCategory() {
      string path = selectedCategory;
      if (path == "全部" || IsFixedCategory(path)) {
        MessageBox.Show(this, "固定主分類不能刪除。", "ClipDesk");
        return;
      }
      if (MessageBox.Show(this, "會一起刪除所有下層分類，內容將移回主分類。確定繼續？", "ClipDesk", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
      string root = path.Split('/')[0];
      categories.RemoveAll(x => x == path || x.StartsWith(path + "/"));
      foreach (ClipItem item in items) {
        string itemPath = item.CategoryPath ?? "未分類";
        if (itemPath == path || itemPath.StartsWith(path + "/")) item.CategoryPath = root;
      }
      selectedCategory = root;
      SaveData();
      RebuildCategoryTree();
      RefreshList();
    }

    bool CategoryMatches(ClipItem item) {
      if (selectedCategory == "全部") return true;
      string path = String.IsNullOrWhiteSpace(item.CategoryPath) ? "未分類" : item.CategoryPath;
      return path == selectedCategory || path.StartsWith(selectedCategory + "/");
    }

    void BuildAttendance(Control parent) {
      Panel page = new Panel();
      page.Dock = DockStyle.Fill;
      page.AutoScroll = true;
      page.BackColor = Bg;
      TableLayoutPanel table = new TableLayoutPanel();
      table.Dock = DockStyle.Top;
      table.AutoSize = true;
      table.ColumnCount = 1;
      table.Padding = new Padding(12, 10, 12, 12);
      table.BackColor = Bg;

      Label heading = new Label();
      heading.Text = "今日出勤通知";
      heading.ForeColor = TextColor;
      heading.Font = new Font(Font.FontFamily, 15F, FontStyle.Bold);
      heading.AutoSize = true;
      heading.Margin = new Padding(0, 2, 0, 2);
      Label hint = new Label();
      hint.Text = "日期會自動帶入今天，休息時間可自行修改。";
      hint.ForeColor = Muted;
      hint.Font = new Font(Font.FontFamily, 8.5F);
      hint.AutoSize = true;
      hint.Margin = new Padding(0, 0, 0, 9);
      table.Controls.Add(heading);
      table.Controls.Add(hint);

      staffName = AddTextRow(table, "顯示名稱", "");
      workStart = AddTimeRow(table, "上班時間", "10:00");
      restStart = AddTimeRow(table, "休息開始", "13:00");
      restEnd = AddTimeRow(table, "休息結束", "14:00");
      workEnd = AddTimeRow(table, "下班時間", "19:00");

      Label actionLabel = new Label();
      actionLabel.Text = "快速複製";
      actionLabel.ForeColor = Muted;
      actionLabel.Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold);
      actionLabel.AutoSize = true;
      actionLabel.Margin = new Padding(0, 12, 0, 3);
      table.Controls.Add(actionLabel);

      FlowLayoutPanel actions = new FlowLayoutPanel();
      actions.Dock = DockStyle.Top;
      actions.AutoSize = false;
      actions.Height = 84;
      actions.WrapContents = true;
      actions.Padding = new Padding(0, 2, 0, 0);
      actions.Margin = new Padding(0);
      string[] labels = { "上班通知", "休息開始", "休息結束", "下班通知", "複製全部" };
      for (int i = 0; i < labels.Length; i++) {
        int action = i;
        Button button = FlatButton(labels[i], delegate { CopyAttendance(action); });
        button.Height = 34;
        button.Margin = new Padding(0, 0, 6, 6);
        if (i == labels.Length - 1) {
          button.BackColor = Accent;
          button.MouseEnter += delegate { button.BackColor = AccentHover; };
          button.MouseLeave += delegate { button.BackColor = Accent; };
        }
        actions.Controls.Add(button);
      }
      table.Controls.Add(actions);
      page.Controls.Add(table);
      parent.Controls.Add(page);
    }

    TextBox AddTextRow(TableLayoutPanel table, string label, string value) {
      Label caption = new Label();
      caption.Text = label;
      caption.ForeColor = Muted;
      caption.AutoSize = true;
      caption.Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold);
      caption.Margin = new Padding(0, 7, 0, 3);
      TextBox input = new TextBox();
      input.Text = value;
      input.Dock = DockStyle.Top;
      input.Height = 29;
      input.BackColor = Surface;
      input.ForeColor = TextColor;
      input.BorderStyle = BorderStyle.FixedSingle;
      input.Font = new Font(Font.FontFamily, 10F);
      input.Margin = new Padding(0, 0, 0, 2);
      table.Controls.Add(caption);
      table.Controls.Add(input);
      return input;
    }

    TextBox AddTimeRow(TableLayoutPanel table, string label, string value) {
      TextBox input = AddTextRow(table, label, value);
      input.MaxLength = 5;
      input.Width = 92;
      input.Dock = DockStyle.Top;
      input.LostFocus += delegate { input.Text = TimeValue(input); };
      return input;
    }

    string TimeValue(TextBox input) {
      string value = (input.Text ?? "").Trim();
      TimeSpan time;
      if (TimeSpan.TryParse(value, out time)) return DateTime.Today.Add(time).ToString("HH:mm");
      return value.Length == 0 ? "--:--" : value;
    }

    void DrawTab(object sender, DrawItemEventArgs e) {
      bool selected = e.Index == tabs.SelectedIndex;
      Rectangle rect = e.Bounds;
      using (SolidBrush bg = new SolidBrush(selected ? Surface : Bg)) e.Graphics.FillRectangle(bg, rect);
      Color color = selected ? TextColor : Muted;
      using (Font tabFont = new Font(Font.FontFamily, 9F, selected ? FontStyle.Bold : FontStyle.Regular))
        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabFont, rect, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
      if (selected) using (SolidBrush line = new SolidBrush(Accent)) e.Graphics.FillRectangle(line, rect.Left + 12, rect.Bottom - 3, rect.Width - 24, 3);
    }

    int CategoryNodeTextX(TreeNode node) {
      return 24 + Math.Max(0, node == null ? 0 : node.Level) * 18;
    }

    void DrawCategoryNode(object sender, DrawTreeNodeEventArgs e) {
      bool selected = (e.State & TreeNodeStates.Selected) != 0;
      Rectangle row = new Rectangle(0, e.Bounds.Y, Math.Max(1, categoryTree.ClientSize.Width), e.Bounds.Height);
      using (SolidBrush bg = new SolidBrush(selected ? AccentSoft : Surface)) e.Graphics.FillRectangle(bg, row);

      int level = Math.Max(0, e.Node.Level);
      int branchX = 6 + level * 18;
      int middleY = e.Bounds.Y + e.Bounds.Height / 2;
      using (Pen guide = new Pen(selected ? AccentText : Divider)) {
        for (int i = 0; i < level; i++) {
          int lineX = 12 + i * 18;
          e.Graphics.DrawLine(guide, lineX, e.Bounds.Top, lineX, e.Bounds.Bottom);
        }
        if (level > 0) e.Graphics.DrawLine(guide, branchX - 12, middleY, branchX + 5, middleY);
      }

      Rectangle glyph = new Rectangle(branchX, e.Bounds.Y + 5, 12, 12);
      if (e.Node.Nodes.Count > 0) {
        using (SolidBrush glyphBg = new SolidBrush(selected ? Accent : ButtonColor)) e.Graphics.FillRectangle(glyphBg, glyph);
        using (Font glyphFont = new Font("Segoe UI", 8F, FontStyle.Bold))
          TextRenderer.DrawText(e.Graphics, e.Node.IsExpanded ? "−" : "+", glyphFont, glyph, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
      } else if (level > 0) {
        Rectangle dot = new Rectangle(branchX + 4, middleY - 2, 4, 4);
        using (SolidBrush dotBrush = new SolidBrush(selected ? AccentText : Muted)) e.Graphics.FillRectangle(dotBrush, dot);
      }

      int textX = CategoryNodeTextX(e.Node);
      Color color = selected ? Color.White : TextColor;
      Rectangle textBounds = new Rectangle(textX, e.Bounds.Y, Math.Max(4, categoryTree.ClientSize.Width - textX - 8), e.Bounds.Height);
      using (Font nodeFont = new Font(Font.FontFamily, 9F, selected ? FontStyle.Bold : FontStyle.Regular))
        TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, textBounds, color,
          TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    string WrapTooltipText(string text, int width) {
      string normalized = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
      List<string> wrapped = new List<string>();
      foreach (string original in normalized.Split(new [] { '\n' }, StringSplitOptions.None)) {
        string line = original;
        if (line.Length == 0) { wrapped.Add(""); continue; }
        while (line.Length > width) {
          int cut = line.LastIndexOf(' ', width);
          if (cut < width / 2) cut = width;
          wrapped.Add(line.Substring(0, cut).TrimEnd());
          line = line.Substring(cut).TrimStart();
        }
        wrapped.Add(line);
      }
      return String.Join(Environment.NewLine, wrapped);
    }

    bool ClipTextIsTruncated(int index, out bool categoryTruncated) {
      categoryTruncated = false;
      if (index < 0 || index >= shown.Count || index >= clipList.Items.Count) return false;
      Rectangle bounds = clipList.GetItemRectangle(index);
      int availableWidth = Math.Max(4, bounds.Width - 36);
      int availableHeight = Math.Max(4, bounds.Height - 23);
      ClipItem item = shown[index];
      string value = (item.Text ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
      Size measured = TextRenderer.MeasureText(value, Font, new Size(availableWidth, Int32.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
      string path = String.IsNullOrWhiteSpace(item.CategoryPath) ? "未分類" : item.CategoryPath;
      string[] parts = path.Split(new [] { "/" }, StringSplitOptions.RemoveEmptyEntries);
      string category = (item.Pinned ? "◆  " : "") + (parts.Length == 0 ? "未分類" : parts[parts.Length - 1]);
      using (Font badge = new Font(Font.FontFamily, 8F, FontStyle.Bold))
        categoryTruncated = TextRenderer.MeasureText(category, badge, new Size(Int32.MaxValue, 16), TextFormatFlags.NoPadding).Width > availableWidth;
      return measured.Height > availableHeight || measured.Width > availableWidth;
    }

    void ShowClipTooltip(object sender, MouseEventArgs e) {
      int index = clipList.IndexFromPoint(e.Location);
      if (index == hoverClipIndex) return;
      hoverClipIndex = index;
      hoverTip.Hide(clipList);
      if (index < 0 || index >= shown.Count) return;
      bool categoryTruncated;
      bool textTruncated = ClipTextIsTruncated(index, out categoryTruncated);
      if (!textTruncated && !categoryTruncated) return;
      ClipItem item = shown[index];
      string tip = "";
      if (categoryTruncated) tip = "分類：" + (String.IsNullOrWhiteSpace(item.CategoryPath) ? "未分類" : item.CategoryPath);
      if (textTruncated) tip += (tip.Length > 0 ? Environment.NewLine + Environment.NewLine : "") + (item.Text ?? "");
      hoverTip.Show(WrapTooltipText(tip, 48), clipList, e.X + 14, e.Y + 18, 30000);
    }

    void ShowCategoryTooltip(object sender, MouseEventArgs e) {
      TreeNode node = categoryTree.GetNodeAt(e.Location);
      if (node == hoverCategoryNode) return;
      hoverCategoryNode = node;
      hoverTip.Hide(categoryTree);
      if (node == null) return;
      int availableWidth = Math.Max(1, categoryTree.ClientSize.Width - CategoryNodeTextX(node) - 8);
      int measuredWidth = TextRenderer.MeasureText(node.Text, categoryTree.Font, new Size(Int32.MaxValue, node.Bounds.Height), TextFormatFlags.NoPadding).Width;
      if (measuredWidth <= availableWidth) return;
      string fullPath = node.Tag == null ? node.Text : node.Tag.ToString();
      hoverTip.Show(WrapTooltipText(fullPath, 48), categoryTree, e.X + 14, e.Y + 18, 30000);
    }

    void DrawClip(object sender, DrawItemEventArgs e) {
      if (e.Index < 0 || e.Index >= shown.Count) return;
      ClipItem item = shown[e.Index];
      bool selected = (e.State & DrawItemState.Selected) != 0;
      Color background = selected ? AccentSoft : (e.Index % 2 == 0 ? Row : RowAlt);
      using (SolidBrush bg = new SolidBrush(background)) e.Graphics.FillRectangle(bg, e.Bounds);
      if (selected) using (SolidBrush bar = new SolidBrush(Accent)) e.Graphics.FillRectangle(bar, e.Bounds.Left, e.Bounds.Top, 3, e.Bounds.Height);

      Rectangle numberRect = new Rectangle(e.Bounds.X + 5, e.Bounds.Y + 5, 18, 18);
      using (Font small = new Font(Font.FontFamily, 8F, FontStyle.Bold))
        TextRenderer.DrawText(e.Graphics, (e.Index + 1).ToString(), small, numberRect, selected ? Color.White : Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);

      string path = String.IsNullOrWhiteSpace(item.CategoryPath) ? "未分類" : item.CategoryPath;
      string[] categoryParts = path.Split(new [] { "/" }, StringSplitOptions.RemoveEmptyEntries);
      string category = categoryParts.Length == 0 ? "未分類" : categoryParts[categoryParts.Length - 1];
      string categoryText = (item.Pinned ? "◆  " : "") + category;
      Rectangle categoryRect = new Rectangle(e.Bounds.X + 29, e.Bounds.Y + 3, Math.Max(4, e.Bounds.Width - 36), 16);
      using (Font badge = new Font(Font.FontFamily, 8F, FontStyle.Bold))
        TextRenderer.DrawText(e.Graphics, categoryText, badge, categoryRect, selected ? AccentText : Accent, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

      string value = (item.Text ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
      Rectangle textRect = new Rectangle(e.Bounds.X + 29, e.Bounds.Y + 20, Math.Max(4, e.Bounds.Width - 36), e.Bounds.Height - 23);
      Region oldClip = e.Graphics.Clip;
      e.Graphics.SetClip(textRect);
      TextRenderer.DrawText(e.Graphics, value, Font, textRect, TextColor, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
      e.Graphics.Clip = oldClip;
      oldClip.Dispose();
      using (Pen p = new Pen(Divider)) e.Graphics.DrawLine(p, e.Bounds.Left + 28, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    void RefreshList() {
      string q = (searchBox.Text ?? "").Trim();
      shown.Clear();
      IEnumerable<ClipItem> query = items.Where(CategoryMatches).OrderByDescending(x => x.Pinned).ThenByDescending(x => x.CreatedAt);
      if (q.Length > 0) query = query.Where(x => ((x.Title ?? "") + " " + (x.Text ?? "") + " " + (x.CategoryPath ?? "")).IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0);
      shown.AddRange(query);
      clipList.BeginUpdate();
      clipList.Items.Clear();
      foreach (ClipItem item in shown) clipList.Items.Add(item);
      clipList.EndUpdate();
      countLabel.Text = shown.Count.ToString();
      if (clipList.Items.Count > 0 && clipList.SelectedIndex < 0) clipList.SelectedIndex = 0;
    }

    ClipItem Selected() {
      int i = clipList.SelectedIndex;
      return i >= 0 && i < shown.Count ? shown[i] : null;
    }

    void CaptureClipboard() {
      try {
        if (!Clipboard.ContainsText()) return;
        string text = Clipboard.GetText().Trim();
        if (text.Length == 0 || text == lastClipboard) return;
        lastClipboard = text;
        ClipItem existing = items.FirstOrDefault(x => x.Text == text);
        if (existing != null) existing.CreatedAt = DateTime.Now;
        else items.Insert(0, new ClipItem { Id = Guid.NewGuid().ToString("N"), Text = text, Title = FirstLine(text), CategoryPath = "未分類", CreatedAt = DateTime.Now });
        SaveData();
        RefreshList();
      } catch { }
    }

    string FirstLine(string text) {
      string line = (text ?? "").Replace("\r\n", "\n").Split(new [] { "\n" }, StringSplitOptions.None)[0].Trim();
      return line.Length > 50 ? line.Substring(0, 50) : line;
    }

    void TrackForegroundWindow() {
      IntPtr foreground = GetForegroundWindow();
      if (foreground != IntPtr.Zero && foreground != Handle && IsWindow(foreground))
        previousWindow = foreground;
    }

    void PasteSelected() {
      ClipItem item = Selected();
      if (item == null || String.IsNullOrEmpty(item.Text)) return;

      IntPtr target = previousWindow;
      if (target == IntPtr.Zero || target == Handle || !IsWindow(target))
        target = GetWindow(Handle, GW_HWNDNEXT);

      CopySelected();

      if (target == IntPtr.Zero || target == Handle || !IsWindow(target)) {
        MessageBox.Show(this, "找不到上一個視窗，內容已複製到剪貼簿。", "ClipDesk");
        return;
      }

      Timer pasteTimer = new Timer();
      pasteTimer.Interval = 100;
      int phase = 0;
      pasteTimer.Tick += delegate {
        if (phase == 0) {
          SetForegroundWindow(target);
          phase = 1;
          return;
        }

        pasteTimer.Stop();
        try {
          SendKeys.SendWait("^v");
          previousWindow = target;
        } catch {
          MessageBox.Show(this, "無法自動貼上，內容已複製到剪貼簿。", "ClipDesk");
        }
        pasteTimer.Dispose();
      };
      pasteTimer.Start();
    }

    void CopySelected() {
      ClipItem item = Selected();
      if (item == null || String.IsNullOrEmpty(item.Text)) return;
      try {
        Clipboard.SetText(item.Text);
        lastClipboard = item.Text;
        item.Used++;
        item.CreatedAt = DateTime.Now;
        SaveData();
        RefreshList();
      } catch { }
    }

    void EditSelected() { EditItem(Selected()); }

    void EditItem(ClipItem item) {
      using (Form dialog = new Form()) {
        dialog.Text = item == null ? "新增內容" : "編輯內容";
        dialog.Width = Math.Max(290, Math.Min(440, Width));
        dialog.Height = 400;
        dialog.MinimizeBox = false;
        dialog.MaximizeBox = false;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.BackColor = Bg;
        dialog.ForeColor = TextColor;
        dialog.Font = Font;
        TextBox title = new TextBox();
        title.Dock = DockStyle.Top;
        title.Height = 28;
        title.Text = item == null ? "" : item.Title;
        title.BackColor = Row;
        title.ForeColor = TextColor;
        title.BorderStyle = BorderStyle.FixedSingle;
        ComboBox category = new ComboBox();
        category.Dock = DockStyle.Top;
        category.Height = 28;
        category.DropDownStyle = ComboBoxStyle.DropDownList;
        category.BackColor = Row;
        category.ForeColor = TextColor;
        foreach (string path in categories.Distinct().OrderBy(x => x)) category.Items.Add(path);
        string currentCategory = item == null || String.IsNullOrWhiteSpace(item.CategoryPath) ? "未分類" : item.CategoryPath;
        category.SelectedItem = currentCategory;
        if (category.SelectedIndex < 0) category.SelectedItem = "未分類";
        TextBox body = new TextBox();
        body.Dock = DockStyle.Fill;
        body.Multiline = true;
        body.ScrollBars = ScrollBars.Vertical;
        body.Text = item == null ? "" : item.Text;
        body.BackColor = Row;
        body.ForeColor = TextColor;
        body.BorderStyle = BorderStyle.FixedSingle;
        bool saveAsCopy = false;
        FlowLayoutPanel actions = new FlowLayoutPanel();
        actions.Dock = DockStyle.Bottom;
        actions.Height = 46;
        actions.Padding = new Padding(4, 5, 4, 3);
        actions.FlowDirection = FlowDirection.RightToLeft;
        Button save = FlatButton("儲存", delegate { dialog.DialogResult = DialogResult.OK; });
        Button saveCopy = FlatButton("另存一份", delegate { saveAsCopy = true; dialog.DialogResult = DialogResult.OK; });
        Button cancel = FlatButton("取消", delegate { dialog.DialogResult = DialogResult.Cancel; });
        save.BackColor = Accent;
        save.MouseEnter += delegate { save.BackColor = AccentHover; };
        save.MouseLeave += delegate { save.BackColor = Accent; };
        actions.Controls.Add(save);
        if (item != null) actions.Controls.Add(saveCopy);
        actions.Controls.Add(cancel);
        Panel content = new Panel();
        content.Dock = DockStyle.Fill;
        content.Padding = new Padding(8);
        content.Controls.Add(body);
        content.Controls.Add(category);
        content.Controls.Add(title);
        dialog.Controls.Add(content);
        dialog.Controls.Add(actions);
        dialog.AcceptButton = save;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        string text = body.Text.Trim();
        if (text.Length == 0) return;
        ClipItem target = item;
        if (target == null || saveAsCopy) {
          target = new ClipItem { Id = Guid.NewGuid().ToString("N"), CategoryPath = "未分類", CreatedAt = DateTime.Now };
          items.Insert(0, target);
        }
        target.Text = text;
        target.Title = String.IsNullOrWhiteSpace(title.Text) ? FirstLine(text) : title.Text.Trim();
        target.CategoryPath = category.SelectedItem == null ? "未分類" : category.SelectedItem.ToString();
        target.CreatedAt = DateTime.Now;
        SaveData();
        RefreshList();
      }
    }

    void DeleteSelected() {
      ClipItem item = Selected();
      if (item == null) return;
      if (MessageBox.Show(this, "確定刪除這則內容？", "ClipDesk", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
      items.Remove(item);
      SaveData();
      RefreshList();
    }

    void TogglePin() {
      ClipItem item = Selected();
      if (item == null) return;
      item.Pinned = !item.Pinned;
      SaveData();
      RefreshList();
    }

    string AttendanceText(int action) {
      string prefix = DateTime.Today.ToString("yyyy/MM/dd") + " " + (String.IsNullOrWhiteSpace(staffName.Text) ? "姓名" : staffName.Text.Trim());
      if (action == 0) return prefix + " " + TimeValue(workStart) + " 打卡上班";
      if (action == 1) return prefix + " " + TimeValue(restStart) + " 休息開始";
      if (action == 2) return prefix + " " + TimeValue(restEnd) + " 休息結束";
      return prefix + " " + TimeValue(workEnd) + " 打卡下班";
    }

    void CopyAttendance(int action) {
      try {
        string text = action == 4 ? String.Join(Environment.NewLine, new [] { AttendanceText(0), AttendanceText(1), AttendanceText(2), AttendanceText(3) }) : AttendanceText(action);
        Clipboard.SetText(text);
        lastClipboard = text;
        SaveData();
      } catch { }
    }

    string NormalizeImportedCategory(string value) {
      string path = (value ?? "").Trim().Trim('/');
      if (path.Length == 0) return "未分類";
      string[] rawParts = path.Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (rawParts.Length == 0) return "未分類";
      string root = rawParts[0].Trim();
      if (!new [] { "開頭", "中間", "結尾", "未分類", "其他" }.Contains(root)) return "未分類";
      if (root == "未分類" || rawParts.Length == 1) return root;
      List<string> parts = new List<string> { root };
      for (int i = 1; i < rawParts.Length; i++) {
        string part = CleanCategoryName(rawParts[i]);
        if (part.Length > 0) parts.Add(part);
      }
      return String.Join("/", parts);
    }

    void AddCategoryWithParents(string path) {
      string[] parts = (path ?? "").Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0) return;
      string built = parts[0];
      if (!categories.Contains(built)) categories.Add(built);
      for (int i = 1; i < parts.Length; i++) {
        built += "/" + parts[i];
        if (!categories.Contains(built)) categories.Add(built);
      }
    }

    void ExportBackup() {
      SaveData();
      using (SaveFileDialog dialog = new SaveFileDialog()) {
        dialog.Title = "匯出 ClipDesk 備份";
        dialog.Filter = "ClipDesk JSON 備份 (*.json)|*.json";
        dialog.FileName = "ClipDesk-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
        dialog.DefaultExt = "json";
        dialog.AddExtension = true;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try {
          StoreData backup = new StoreData { Items = items, Settings = settings, Categories = categories.Distinct().ToList() };
          File.WriteAllText(dialog.FileName, serializer.Serialize(backup));
          MessageBox.Show(this, "備份已匯出。", "ClipDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
          MessageBox.Show(this, "匯出失敗：" + ex.Message, "ClipDesk", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }

    void ImportBackup() {
      using (OpenFileDialog dialog = new OpenFileDialog()) {
        dialog.Title = "匯入 ClipDesk 備份";
        dialog.Filter = "ClipDesk JSON 備份 (*.json)|*.json|所有檔案 (*.*)|*.*";
        dialog.CheckFileExists = true;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try {
          FileInfo file = new FileInfo(dialog.FileName);
          if (file.Length > 50L * 1024L * 1024L) throw new InvalidDataException("備份檔超過 50 MB。請確認選擇了正確檔案。");
          StoreData data = serializer.Deserialize<StoreData>(File.ReadAllText(dialog.FileName));
          if (data == null || (data.Items == null && data.Settings == null && data.Categories == null))
            throw new InvalidDataException("這不是有效的 ClipDesk 備份檔。");
          DialogResult confirm = MessageBox.Show(this,
            "匯入會取代目前所有剪貼簿內容、分類與出勤設定。\n\n確定繼續嗎？",
            "匯入備份", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
          if (confirm != DialogResult.Yes) return;

          List<ClipItem> importedItems = new List<ClipItem>();
          foreach (ClipItem item in data.Items ?? new List<ClipItem>()) {
            if (item == null || String.IsNullOrEmpty(item.Text)) continue;
            item.Id = String.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id;
            item.Title = String.IsNullOrWhiteSpace(item.Title) ? FirstLine(item.Text) : item.Title;
            item.CategoryPath = NormalizeImportedCategory(item.CategoryPath);
            if (item.CreatedAt == DateTime.MinValue) item.CreatedAt = DateTime.Now;
            importedItems.Add(item);
          }

          items.Clear();
          items.AddRange(importedItems);
          categories.Clear();
          categories.AddRange(new [] { "開頭", "中間", "結尾", "未分類", "其他" });
          foreach (string path in data.Categories ?? new List<string>())
            AddCategoryWithParents(NormalizeImportedCategory(path));
          foreach (ClipItem item in items) AddCategoryWithParents(item.CategoryPath);
          settings = data.Settings ?? Defaults();
          staffName.Text = settings.StaffName ?? "";
          workStart.Text = settings.WorkStart ?? "10:00";
          restStart.Text = settings.RestStart ?? "13:00";
          restEnd.Text = settings.RestEnd ?? "14:00";
          workEnd.Text = settings.WorkEnd ?? "19:00";
          selectedCategory = "全部";
          searchBox.Clear();
          RebuildCategoryTree();
          RefreshList();
          try { lastClipboard = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : ""; } catch { lastClipboard = ""; }
          SaveData();
          MessageBox.Show(this, "匯入完成，共載入 " + items.Count + " 筆內容。", "ClipDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
          MessageBox.Show(this, "匯入失敗：" + ex.Message, "ClipDesk", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }

    void LoadData() {
      try {
        if (File.Exists(dataFile)) {
          StoreData data = serializer.Deserialize<StoreData>(File.ReadAllText(dataFile));
          if (data != null && data.Items != null) items.AddRange(data.Items.Where(x => x != null && !String.IsNullOrEmpty(x.Text)));
          if (data != null && data.Settings != null) settings = data.Settings;
          if (data != null && data.Categories != null) categories.AddRange(data.Categories);
        }
      } catch { }
      foreach (ClipItem item in items) if (String.IsNullOrWhiteSpace(item.CategoryPath)) item.CategoryPath = "未分類";
      categories.RemoveAll(x => String.IsNullOrWhiteSpace(x));
      List<string> uniqueCategories = categories.Distinct().ToList();
      categories.Clear();
      categories.AddRange(uniqueCategories);
      staffName.Text = settings.StaffName ?? "";
      workStart.Text = settings.WorkStart ?? "10:00";
      restStart.Text = settings.RestStart ?? "13:00";
      restEnd.Text = settings.RestEnd ?? "14:00";
      workEnd.Text = settings.WorkEnd ?? "19:00";
    }

    void SaveData() {
      try {
        settings.StaffName = staffName.Text;
        settings.WorkStart = TimeValue(workStart);
        settings.RestStart = TimeValue(restStart);
        settings.RestEnd = TimeValue(restEnd);
        settings.WorkEnd = TimeValue(workEnd);
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(dataFile, serializer.Serialize(new StoreData { Items = items, Settings = settings, Categories = categories.Distinct().ToList() }));
      } catch { }
    }
  }

  static class Program {
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new MainForm());
    }
  }
}
