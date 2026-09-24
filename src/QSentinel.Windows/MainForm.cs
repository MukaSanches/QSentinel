using System.Diagnostics;

namespace QSentinel;

public sealed class MainForm : Form
{
    private readonly AppSettings settings;
    private readonly bool startHidden;

    private readonly SystemMonitor monitor = new();
    private readonly OptimizationEngine optimizer;
    private readonly NetworkIntelligence network = new();

    private readonly System.Windows.Forms.Timer timer = new();
    private readonly NotifyIcon tray = new();
    private readonly DataGridView grid = new();

    private readonly Label cpuValue = new();
    private readonly Label ramValue = new();
    private readonly Label ioValue = new();
    private readonly Label optimizedValue = new();
    private readonly Label engineState = new();
    private readonly Label pressureLabel = new();

    private readonly PerformanceChart cpuChart = new()
    {
        ChartTitle = "CPU em tempo real",
        Suffix = "%",
        FixedMaximum = 100,
        Accent = Color.FromArgb(116, 103, 255)
    };

    private readonly PerformanceChart ramChart = new()
    {
        ChartTitle = "Memória física",
        Suffix = "%",
        FixedMaximum = 100,
        Accent = Color.FromArgb(55, 196, 154)
    };

    private readonly PerformanceChart ioChart = new()
    {
        ChartTitle = "Atividade de disco",
        Suffix = " MB/s",
        Accent = Color.FromArgb(70, 162, 255)
    };

    private MonitorSnapshot? latest;
    private bool exitRequested;

    private readonly Color Background =
        Color.FromArgb(8, 11, 18);

    private readonly Color Surface =
        Color.FromArgb(18, 24, 37);

    private readonly Color Surface2 =
        Color.FromArgb(25, 32, 47);

    private readonly Color Accent =
        Color.FromArgb(116, 103, 255);

    private readonly Color TextPrimary =
        Color.FromArgb(242, 245, 250);

    private readonly Color TextSecondary =
        Color.FromArgb(133, 146, 170);

    public MainForm(
        AppSettings settings,
        bool startHidden)
    {
        this.settings = settings;
        this.startHidden = startHidden;
        optimizer = new OptimizationEngine(settings);

        Text = "QSentinel";
        ClientSize = new Size(1320, 820);
        MinimumSize = new Size(1050, 690);
        StartPosition = FormStartPosition.CenterScreen;

        BackColor = Background;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        BuildUi();
        BuildTray();

        timer.Interval =
            Math.Clamp(
                settings.RefreshMilliseconds,
                750,
                5000);

        timer.Tick +=
            (_, _) => RefreshDashboard();

        timer.Start();

        Shown += (_, _) =>
        {
            RefreshDashboard();

            if (startHidden)
            {
                BeginInvoke(() =>
                {
                    ShowInTaskbar = false;
                    Hide();
                });
            }
        };

        FormClosing += OnClosing;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        root.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                210));

        root.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        root.Controls.Add(
            BuildSidebar(),
            0,
            0);

        root.Controls.Add(
            BuildMain(),
            1,
            0);

        Controls.Add(root);
    }

    private Control BuildSidebar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 16, 25)
        };

        var brand = new Label
        {
            Text = "QSENTINEL",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 19),
            AutoSize = true,
            Location = new Point(23, 27)
        };

        var edition = new Label
        {
            Text = "SYSTEM ORCHESTRATOR  •  V1.4",
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI Semibold", 7.5f),
            AutoSize = true,
            Location = new Point(25, 64)
        };

        panel.Controls.Add(brand);
        panel.Controls.Add(edition);

        string[] items =
        {
            "◉  VISÃO GERAL",
            "▤  PROCESSOS",
            "◇  PROTEÇÃO",
            "⊘  BLOQUEIOS",
            "⌁  QUANTUM LAB"
        };

        int y = 126;

        foreach (string item in items)
        {
            bool selected =
                item.StartsWith("◉");

            var label = new Label
            {
                Text = item,
                Width = 180,
                Height = 42,
                Location = new Point(15, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                ForeColor =
                    selected
                        ? Color.White
                        : TextSecondary,
                BackColor =
                    selected
                        ? Color.FromArgb(29, 34, 53)
                        : Color.Transparent,
                Font = new Font("Segoe UI Semibold", 9)
            };

            panel.Controls.Add(label);
            y += 48;
        }

        var footer = new Label
        {
            Text =
                "● MOTOR RESIDENTE\n" +
                "Inicia automaticamente\n" +
                "com o Windows",
            ForeColor = Color.FromArgb(85, 205, 158),
            AutoSize = true,
            Location = new Point(24, 720),
            Anchor =
                AnchorStyles.Left |
                AnchorStyles.Bottom,
            Font = new Font("Segoe UI", 8.5f)
        };

        panel.Controls.Add(footer);

        return panel;
    }

    private Control BuildMain()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Background,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(22, 18, 22, 20)
        };

        main.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 82));

        main.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 110));

        main.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 225));

        main.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        main.Controls.Add(BuildHeader(), 0, 0);
        main.Controls.Add(BuildMetrics(), 0, 1);
        main.Controls.Add(BuildCharts(), 0, 2);
        main.Controls.Add(BuildProcessArea(), 0, 3);

        return main;
    }

    private Control BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Background
        };

        var title = new Label
        {
            Text = "Controle em tempo real",
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 21),
            Location = new Point(7, 4)
        };

        var subtitle = new Label
        {
            Text =
                "O motor adaptativo monitora CPU, memória, I/O e rede " +
                "e reduz o impacto de tarefas em segundo plano.",
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9),
            Location = new Point(9, 46)
        };

        engineState.AutoSize = true;
        engineState.Font =
            new Font("Segoe UI Semibold", 9.5f);

        pressureLabel.AutoSize = true;
        pressureLabel.ForeColor = TextSecondary;
        pressureLabel.Font =
            new Font("Segoe UI", 8.5f);

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(engineState);
        panel.Controls.Add(pressureLabel);

        panel.Resize += (_, _) =>
        {
            engineState.Location =
                new Point(
                    Math.Max(10, panel.Width - engineState.Width - 10),
                    9);

            pressureLabel.Location =
                new Point(
                    Math.Max(10, panel.Width - pressureLabel.Width - 10),
                    37);
        };

        UpdateEngineState();
        return panel;
    }

    private Control BuildMetrics()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty
        };

        for (int i = 0; i < 4; i++)
        {
            table.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));
        }

        table.Controls.Add(
            CreateMetricCard(
                "CPU",
                cpuValue,
                "0%"),
            0,
            0);

        table.Controls.Add(
            CreateMetricCard(
                "MEMÓRIA",
                ramValue,
                "0%"),
            1,
            0);

        table.Controls.Add(
            CreateMetricCard(
                "DISCO / I-O",
                ioValue,
                "0 MB/s"),
            2,
            0);

        table.Controls.Add(
            CreateMetricCard(
                "OTIMIZADOS",
                optimizedValue,
                "0"),
            3,
            0);

        return table;
    }

    private Panel CreateMetricCard(
        string caption,
        Label value,
        string initial)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = Surface,
            Padding = new Padding(18)
        };

        var title = new Label
        {
            Text = caption,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI Semibold", 8.5f),
            AutoSize = true,
            Location = new Point(17, 14)
        };

        value.Text = initial;
        value.ForeColor = TextPrimary;
        value.Font = new Font("Segoe UI Semibold", 23);
        value.AutoSize = true;
        value.Location = new Point(16, 39);

        card.Controls.Add(title);
        card.Controls.Add(value);

        return card;
    }

    private Control BuildCharts()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };

        for (int i = 0; i < 3; i++)
        {
            table.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    33.3333f));
        }

        table.Controls.Add(cpuChart, 0, 0);
        table.Controls.Add(ramChart, 1, 0);
        table.Controls.Add(ioChart, 2, 0);

        return table;
    }

    private Control BuildProcessArea()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Background,
            Padding = new Padding(6)
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Background
        };

        var title = new Label
        {
            Text = "PROCESSOS ATIVOS",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10.5f),
            AutoSize = true,
            Location = new Point(3, 16)
        };

        var optimize = CreateButton(
            "OTIMIZAR AGORA",
            Accent);

        var pause = CreateButton(
            settings.OptimizationEnabled
                ? "PAUSAR MOTOR"
                : "ATIVAR MOTOR",
            Surface2);

        var block = CreateButton(
            "BLOQUEAR APP",
            Surface2);

        optimize.Click += (_, _) =>
        {
            if (latest is null) return;

            optimizer.Tick(
                latest.Processes,
                latest.Metrics,
                force: true);

            RefreshDashboard();
        };

        pause.Click += (_, _) =>
        {
            settings.OptimizationEnabled =
                !settings.OptimizationEnabled;

            settings.Save();

            if (!settings.OptimizationEnabled)
                optimizer.RestoreAll();

            pause.Text =
                settings.OptimizationEnabled
                    ? "PAUSAR MOTOR"
                    : "ATIVAR MOTOR";

            UpdateEngineState();
        };

        block.Click += (_, _) =>
            BlockSelected();

        header.Controls.Add(title);
        header.Controls.Add(optimize);
        header.Controls.Add(pause);
        header.Controls.Add(block);

        header.Resize += (_, _) =>
        {
            block.Location =
                new Point(
                    header.Width - 128,
                    8);

            pause.Location =
                new Point(
                    header.Width - 266,
                    8);

            optimize.Location =
                new Point(
                    header.Width - 414,
                    8);
        };

        ConfigureGrid();

        panel.Controls.Add(grid);
        panel.Controls.Add(header);

        return panel;
    }

    private Button CreateButton(
        string text,
        Color back)
    {
        return new Button
        {
            Text = text,
            Width = 124,
            Height = 35,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 8),
            FlatAppearance =
            {
                BorderSize = 0
            }
        };
    }

    private void ConfigureGrid()
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(30, 39, 55);
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        grid.ColumnHeadersDefaultCellStyle.BackColor =
            Surface2;
        grid.ColumnHeadersDefaultCellStyle.ForeColor =
            TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI Semibold", 8);
        grid.ColumnHeadersHeight = 38;

        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(49, 45, 91);
        grid.DefaultCellStyle.SelectionForeColor =
            Color.White;
        grid.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);
        grid.RowTemplate.Height = 33;

        grid.Columns.Clear();

        grid.Columns.Add("name", "PROCESSO");
        grid.Columns["name"]!.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.Fill;

        grid.Columns.Add("cpu", "CPU");
        grid.Columns["cpu"]!.Width = 75;

        grid.Columns.Add("ram", "MEMÓRIA");
        grid.Columns["ram"]!.Width = 105;

        grid.Columns.Add("io", "I/O");
        grid.Columns["io"]!.Width = 90;

        grid.Columns.Add("pid", "PID");
        grid.Columns["pid"]!.Width = 78;

        grid.Columns.Add("status", "ESTADO");
        grid.Columns["status"]!.Width = 135;
    }

    private void RefreshDashboard()
    {
        try
        {
            latest = monitor.Capture();
            network.Tick();

            optimizer.Tick(
                latest.Processes,
                latest.Metrics);

            var metrics = latest.Metrics;

            cpuValue.Text =
                $"{metrics.CpuPercent:N0}%";

            ramValue.Text =
                $"{metrics.MemoryPercent:N0}%";

            ioValue.Text =
                $"{metrics.IoMbPerSecond:N1} MB/s";

            optimizedValue.Text =
                optimizer.OptimizedCount.ToString();

            cpuChart.AddPoint(metrics.CpuPercent);
            ramChart.AddPoint(metrics.MemoryPercent);
            ioChart.AddPoint(metrics.IoMbPerSecond);

            pressureLabel.Text =
                $"PRESSÃO {metrics.Pressure:N0}/100 • GARGALO {optimizer.DominantBottleneck} {optimizer.BottleneckConfidence}% • RAM LIVRE {optimizer.AvailableMemoryMb:N0} MB • COMMIT {optimizer.CommitPercent:N0}% • REDE {network.State} {network.HealthScore}/100 ↓{network.ReceiveMbps:N1} ↑{network.SendMbps:N1} Mbps • GW {network.GatewayLatencyMs:N0} ms • PERDA {network.PacketLossPercent:N1}%";

            grid.SuspendLayout();
            grid.Rows.Clear();

            foreach (var process in latest.Processes.Take(180))
            {
                string state =
                    optimizer.IsOptimized(process.Id)
                        ? "OTIMIZADO"
                        : process.Status;

                int row = grid.Rows.Add(
                    process.Name,
                    $"{process.CpuPercent:N1}%",
                    $"{process.MemoryMb:N0} MB",
                    $"{process.IoMbPerSecond:N1}",
                    process.Id,
                    state);

                grid.Rows[row].Tag = process;

                var cell =
                    grid.Rows[row].Cells[5];

                if (state == "OTIMIZADO")
                    cell.Style.ForeColor =
                        Color.FromArgb(91, 220, 165);
                else if (process.Protected)
                    cell.Style.ForeColor =
                        Color.FromArgb(95, 182, 255);
                else if (state == "ALTO IMPACTO")
                    cell.Style.ForeColor =
                        Color.FromArgb(255, 187, 86);
            }

            grid.ResumeLayout();
            UpdateEngineState();
        }
        catch
        {
            // Monitoramento nunca deve derrubar a interface.
        }
    }

    private void BlockSelected()
    {
        if (grid.SelectedRows.Count == 0)
            return;

        if (grid.SelectedRows[0].Tag is not ProcessInfo info)
            return;

        if (!SafetyEngine.CanOptimize(info))
        {
            MessageBox.Show(
                "Este processo está protegido ou está em uso.",
                "QSentinel Safety",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        var answer =
            MessageBox.Show(
                $"Bloquear \"{info.Name}\"?\n\n" +
                "O QSentinel passará a encerrar automaticamente " +
                "novas instâncias desse processo.",
                "Lista de bloqueio",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        if (!settings.IsBlocked(info.Name))
            settings.BlockedProcesses.Add(info.Name);

        settings.Save();

        try
        {
            using var process =
                Process.GetProcessById(info.Id);

            process.Kill(
                entireProcessTree: false);
        }
        catch { }

        RefreshDashboard();
    }

    private void UpdateEngineState()
    {
        bool enabled =
            settings.OptimizationEnabled;

        engineState.Text =
            enabled
                ? "●  MOTOR ADAPTATIVO ATIVO"
                : "●  MOTOR PAUSADO";

        engineState.ForeColor =
            enabled
                ? Color.FromArgb(91, 220, 165)
                : Color.FromArgb(255, 181, 80);
    }

    private void BuildTray()
    {
        tray.Icon = SystemIcons.Shield;
        tray.Text = "QSentinel — motor residente";
        tray.Visible = true;

        var menu = new ContextMenuStrip();

        menu.Items.Add(
            "Abrir QSentinel",
            null,
            (_, _) => RestoreWindow());

        var optimization =
            new ToolStripMenuItem(
                "Otimização automática")
            {
                Checked = settings.OptimizationEnabled,
                CheckOnClick = true
            };

        optimization.CheckedChanged +=
            (_, _) =>
            {
                settings.OptimizationEnabled =
                    optimization.Checked;

                settings.Save();

                if (!settings.OptimizationEnabled)
                    optimizer.RestoreAll();

                UpdateEngineState();
            };

        menu.Items.Add(optimization);

        var startup =
            new ToolStripMenuItem(
                "Iniciar com o Windows")
            {
                Checked = settings.AutoStart,
                CheckOnClick = true
            };

        startup.CheckedChanged +=
            (_, _) =>
            {
                settings.AutoStart =
                    startup.Checked;

                if (settings.AutoStart)
                    StartupManager.Enable();
                else
                    StartupManager.Disable();

                settings.Save();
            };

        menu.Items.Add(startup);

        menu.Items.Add(
            new ToolStripSeparator());

        menu.Items.Add(
            "Encerrar QSentinel",
            null,
            (_, _) =>
            {
                var answer =
                    MessageBox.Show(
                        "Encerrar o QSentinel agora?\n\n" +
                        "Se a inicialização automática estiver ativa, " +
                        "ele voltará no próximo login do Windows.",
                        "QSentinel",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                    return;

                exitRequested = true;
                optimizer.RestoreAll();
                tray.Visible = false;
                timer.Stop();
                Application.Exit();
            });

        tray.ContextMenuStrip = menu;

        tray.DoubleClick +=
            (_, _) => RestoreWindow();
    }

    private void RestoreWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if (!exitRequested &&
            e.CloseReason ==
            CloseReason.UserClosing)
        {
            e.Cancel = true;
            ShowInTaskbar = false;
            Hide();

            tray.ShowBalloonTip(
                1200,
                "QSentinel continua ativo",
                "A janela foi fechada, mas o motor continua trabalhando em segundo plano.",
                ToolTipIcon.Info);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            optimizer.RestoreAll();
            timer.Dispose();
            tray.Dispose();
        }

        base.Dispose(disposing);
    }
}
