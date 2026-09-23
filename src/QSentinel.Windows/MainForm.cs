using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace QSentinel;

public sealed class MainForm : Form
{
    private readonly SystemMonitor monitor = new();
    private readonly DataGridView grid = new();
    private readonly Label ramValue = new();
    private readonly Label processValue = new();
    private readonly Label protectedValue = new();
    private readonly Label savedValue = new();
    private readonly Label modeLabel = new();
    private readonly NotifyIcon tray = new();
    private readonly System.Windows.Forms.Timer timer = new();

    private List<ProcessInfo> snapshot = new();
    private long savedBytes;

    private readonly Color Background = Color.FromArgb(9, 12, 20);
    private readonly Color Surface = Color.FromArgb(17, 22, 34);
    private readonly Color Surface2 = Color.FromArgb(23, 30, 45);
    private readonly Color Accent = Color.FromArgb(111, 92, 255);
    private readonly Color TextPrimary = Color.FromArgb(238, 241, 248);
    private readonly Color TextSecondary = Color.FromArgb(145, 154, 177);

    public MainForm()
    {
        Text = "QSentinel";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(950, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10);
        DoubleBuffered = true;

        BuildUi();
        BuildTray();

        timer.Interval = 2000;
        timer.Tick += (_, _) => RefreshProcesses();
        timer.Start();

        Shown += (_, _) => RefreshProcesses();
        FormClosing += OnClosing;
    }

    private void BuildUi()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = Background,
            Padding = new Padding(28, 18, 28, 8)
        };

        var title = new Label
        {
            Text = "QSENTINEL",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 23),
            ForeColor = TextPrimary,
            Location = new Point(28, 18)
        };

        var subtitle = new Label
        {
            Text = "Adaptive Windows Resource Orchestrator",
            AutoSize = true,
            Font = new Font("Segoe UI", 9),
            ForeColor = TextSecondary,
            Location = new Point(31, 57)
        };

        modeLabel.Text = "●  OBSERVATION MODE";
        modeLabel.AutoSize = true;
        modeLabel.ForeColor = Color.FromArgb(91, 220, 165);
        modeLabel.Font = new Font("Segoe UI Semibold", 10);
        modeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        modeLabel.Location = new Point(930, 35);

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(modeLabel);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            Padding = new Padding(24, 8, 24, 8),
            BackColor = Background,
            WrapContents = false
        };

        cards.Controls.Add(CreateCard("MEMÓRIA", ramValue, "0%"));
        cards.Controls.Add(CreateCard("PROCESSOS", processValue, "0"));
        cards.Controls.Add(CreateCard("PROTEGIDOS", protectedValue, "0"));
        cards.Controls.Add(CreateCard("MEMÓRIA LIBERADA", savedValue, "0 MB"));

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            Padding = new Padding(28, 10, 28, 8),
            BackColor = Background
        };

        var refresh = CreateButton("Atualizar agora");
        refresh.Location = new Point(28, 10);
        refresh.Click += (_, _) => RefreshProcesses();

        var terminate = CreateButton("Encerrar selecionado");
        terminate.Location = new Point(180, 10);
        terminate.Width = 185;
        terminate.Click += (_, _) => TerminateSelected();

        toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(terminate);

        ConfigureGrid();

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 4, 28, 28),
            BackColor = Background
        };

        gridHost.Controls.Add(grid);

        Controls.Add(gridHost);
        Controls.Add(toolbar);
        Controls.Add(cards);
        Controls.Add(header);
    }

    private Panel CreateCard(string caption, Label value, string initial)
    {
        var panel = new Panel
        {
            Width = 260,
            Height = 96,
            Margin = new Padding(4, 0, 12, 0),
            BackColor = Surface,
            Padding = new Padding(18)
        };

        var name = new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI Semibold", 9),
            Location = new Point(18, 15)
        };

        value.Text = initial;
        value.AutoSize = true;
        value.ForeColor = TextPrimary;
        value.Font = new Font("Segoe UI Semibold", 22);
        value.Location = new Point(17, 43);

        panel.Controls.Add(name);
        panel.Controls.Add(value);

        return panel;
    }

    private Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 140,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface2,
            ForeColor = TextPrimary,
            Cursor = Cursors.Hand,
            FlatAppearance =
            {
                BorderColor = Color.FromArgb(49, 58, 78),
                BorderSize = 1
            }
        };
    }

    private void ConfigureGrid()
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(34, 41, 58);
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoGenerateColumns = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Surface2;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI Semibold", 9);
        grid.ColumnHeadersHeight = 42;

        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(46, 42, 86);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        grid.RowTemplate.Height = 38;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "PROCESSO",
            DataPropertyName = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "PID",
            DataPropertyName = "Id",
            Width = 90
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "MEMÓRIA",
            Name = "Memory",
            Width = 130
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ESTADO",
            DataPropertyName = "Status",
            Width = 160
        });
    }

    private void RefreshProcesses()
    {
        try
        {
            snapshot = monitor.Snapshot();

            grid.Rows.Clear();

            foreach (var p in snapshot)
            {
                int index = grid.Rows.Add(
                    p.Name,
                    p.Id,
                    $"{p.MemoryMb:N0} MB",
                    p.Status);

                grid.Rows[index].Tag = p;

                if (p.Protected)
                    grid.Rows[index].Cells[3].Style.ForeColor =
                        Color.FromArgb(91, 220, 165);
                else if (p.Status == "ALTO USO")
                    grid.Rows[index].Cells[3].Style.ForeColor =
                        Color.FromArgb(255, 194, 92);
            }

            ramValue.Text = $"{SystemMonitor.MemoryUsagePercent():N0}%";
            processValue.Text = snapshot.Count.ToString();
            protectedValue.Text =
                snapshot.Count(x => x.Protected).ToString();

            savedValue.Text =
                $"{savedBytes / 1024d / 1024d:N0} MB";
        }
        catch
        {
            // Monitoring must never crash the UI.
        }
    }

    private void TerminateSelected()
    {
        if (grid.SelectedRows.Count == 0)
            return;

        if (grid.SelectedRows[0].Tag is not ProcessInfo info)
            return;

        if (!SafetyEngine.MayTerminate(info))
        {
            MessageBox.Show(
                "Esse processo está protegido pelo Safety Engine.",
                "QSentinel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"Encerrar {info.Name} (PID {info.Id})?\n\n" +
            "Nesta versão o encerramento manual exige confirmação.",
            "QSentinel Safety",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            using var process = Process.GetProcessById(info.Id);

            long memory = 0;

            try
            {
                memory = process.WorkingSet64;
            }
            catch { }

            process.Kill(false);
            process.WaitForExit(3000);

            savedBytes += memory;

            RefreshProcesses();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível encerrar o processo.\n\n" + ex.Message,
                "QSentinel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BuildTray()
    {
        tray.Icon = SystemIcons.Shield;
        tray.Text = "QSentinel";
        tray.Visible = true;

        var menu = new ContextMenuStrip();

        menu.Items.Add(
            "Abrir QSentinel",
            null,
            (_, _) => RestoreWindow());

        menu.Items.Add(
            "Atualizar",
            null,
            (_, _) => RefreshProcesses());

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(
            "Sair",
            null,
            (_, _) =>
            {
                tray.Visible = false;
                timer.Stop();
                Application.Exit();
            });

        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => RestoreWindow();
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();

            tray.ShowBalloonTip(
                1500,
                "QSentinel",
                "O monitor continua funcionando em segundo plano.",
                ToolTipIcon.Info);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            tray.Dispose();
        }

        base.Dispose(disposing);
    }
}
