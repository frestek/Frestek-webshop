using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Windows.Forms.DataVisualization.Charting; // Diagramhoz szükséges!

namespace ProductColorsViewer
{
    public partial class Form1 : Form
    {
        // 1. A TE TAILSCALE SZERVERED CÍME
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://frestek.peacock-shilling.ts.net/")
        };

        private List<VisionLogEntry> _allLogs = new List<VisionLogEntry>();

        public Form1()
        {
            InitializeComponent();

            SetupModernUI();
            SetupModernGridStyle();

            dataGridView1.CellPainting += DataGridView1_CellPainting;
            btnLoad.Click += async (s, e) => await LoadDataAsync();
            Load += async (s, e) => await LoadDataAsync();

            // Szűrő ComboBox beállítása
            if (comboBox1 != null)
            {
                comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
                comboBox1.Items.Add("Ajánlott termék");
                comboBox1.Items.Add("Felhasználó");
                comboBox1.Items.Add("Észlelt szín");
                comboBox1.Items.Add("Dátum");
                comboBox1.SelectedIndex = 0;
                comboBox1.SelectedIndexChanged += TextBox1_TextChanged;
            }

            // Szűrő TextBox beállítása
            if (textBox1 != null)
            {
                textBox1.TextChanged += TextBox1_TextChanged;
            }
        }

        // --- MODERN FELÜLET BEÁLLÍTÁSA ---
        private void SetupModernUI()
        {
            this.BackColor = System.Drawing.Color.FromArgb(245, 246, 248);
            dataGridView1.BackgroundColor = this.BackColor;

            if (btnLoad != null)
            {
                btnLoad.FlatStyle = FlatStyle.Flat;
                btnLoad.FlatAppearance.BorderSize = 0;
                btnLoad.BackColor = System.Drawing.Color.FromArgb(21, 76, 89);
                btnLoad.ForeColor = System.Drawing.Color.White;
                btnLoad.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                btnLoad.Cursor = Cursors.Hand;
                btnLoad.Height = 40;
                btnLoad.Width = 120;
            }

            if (statbtn != null)
            {
                statbtn.FlatStyle = FlatStyle.Flat;
                statbtn.FlatAppearance.BorderSize = 0;
                statbtn.BackColor = System.Drawing.Color.FromArgb(21, 76, 89);
                statbtn.ForeColor = System.Drawing.Color.White;
                statbtn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                statbtn.Cursor = Cursors.Hand;
                statbtn.Height = 40;
                statbtn.Width = 120;
            }

            if (comboBox1 != null)
            {
                comboBox1.FlatStyle = FlatStyle.Flat;
                comboBox1.Font = new System.Drawing.Font("Segoe UI", 10F);
            }

            if (textBox1 != null)
            {
                textBox1.BorderStyle = BorderStyle.FixedSingle;
                textBox1.Font = new System.Drawing.Font("Segoe UI", 10F);
                textBox1.Text = "Keresés...";
                textBox1.ForeColor = System.Drawing.Color.Gray;

                textBox1.Enter += (s, e) => {
                    if (textBox1.Text == "Keresés...")
                    {
                        textBox1.Text = "";
                        textBox1.ForeColor = System.Drawing.Color.Black;
                    }
                };
                textBox1.Leave += (s, e) => {
                    if (string.IsNullOrWhiteSpace(textBox1.Text))
                    {
                        textBox1.Text = "Keresés...";
                        textBox1.ForeColor = System.Drawing.Color.Gray;
                    }
                };
            }

            Label lblTitle = this.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Text != null && l.Text.Contains("Ajánláskezelő"));
            if (lblTitle != null)
            {
                lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
                lblTitle.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            }

            if (label2 != null)
            {
                label2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
                label2.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            }
        }

        // --- SZŰRÉS LOGIKÁJA ---
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (_allLogs == null || _allLogs.Count == 0) return;

            string filterText = textBox1.Text.ToLower();

            if (filterText == "keresés...") filterText = "";

            if (string.IsNullOrWhiteSpace(filterText))
            {
                dataGridView1.DataSource = _allLogs;
            }
            else
            {
                string kivalasztottOszlop = comboBox1.SelectedItem?.ToString();

                var filteredList = _allLogs.Where(p =>
                {
                    if (kivalasztottOszlop == "Ajánlott termék")
                        return p.RecommendedProductName != null &&
                               p.RecommendedProductName.ToLower().Contains(filterText);

                    else if (kivalasztottOszlop == "Felhasználó")
                        return p.UserName != null &&
                               p.UserName.ToLower().Contains(filterText);

                    else if (kivalasztottOszlop == "Észlelt szín")
                        return p.DetectedHex != null &&
                               p.DetectedHex.ToLower().Contains(filterText);

                    else if (kivalasztottOszlop == "Dátum")
                        return p.CreatedDate.ToString("yyyy.MM.dd").Contains(filterText);

                    return false;
                }).ToList();

                dataGridView1.DataSource = filteredList;
            }

            FormatGridColumns();
        }

        // --- DATAGRIDVIEW OSZLOPOK FORMÁZÁSA ÉS SORRENDJE ---
        private void FormatGridColumns()
        {
            if (dataGridView1.Columns.Count == 0) return;

            // Felesleges technikai oszlopok elrejtése
            if (dataGridView1.Columns.Contains("LogId")) dataGridView1.Columns["LogId"].Visible = false;
            if (dataGridView1.Columns.Contains("ModuleId")) dataGridView1.Columns["ModuleId"].Visible = false;
            if (dataGridView1.Columns.Contains("ColorGroup")) dataGridView1.Columns["ColorGroup"].Visible = false;

            // Oszlopok sorrendje
            if (dataGridView1.Columns.Contains("CreatedDate")) dataGridView1.Columns["CreatedDate"].DisplayIndex = 0;
            if (dataGridView1.Columns.Contains("UserName")) dataGridView1.Columns["UserName"].DisplayIndex = 1;
            if (dataGridView1.Columns.Contains("DetectedHex")) dataGridView1.Columns["DetectedHex"].DisplayIndex = 2;
            if (dataGridView1.Columns.Contains("RecommendedProductName")) dataGridView1.Columns["RecommendedProductName"].DisplayIndex = 3;
            if (dataGridView1.Columns.Contains("PriceHUF")) dataGridView1.Columns["PriceHUF"].DisplayIndex = 4;
            if (dataGridView1.Columns.Contains("ArEur")) dataGridView1.Columns["ArEur"].DisplayIndex = 5;

            // Oszlopok fejléce és megjelenése
            if (dataGridView1.Columns.Contains("CreatedDate"))
            {
                dataGridView1.Columns["CreatedDate"].HeaderText = "Dátum";
                dataGridView1.Columns["CreatedDate"].DefaultCellStyle.Format = "yyyy.MM.dd HH:mm";
            }

            if (dataGridView1.Columns.Contains("UserName"))
                dataGridView1.Columns["UserName"].HeaderText = "Felhasználó";

            if (dataGridView1.Columns.Contains("DetectedHex"))
            {
                dataGridView1.Columns["DetectedHex"].HeaderText = "Észlelt szín";
                // Észlelt szín oszlop fixálása szélesebbre
                dataGridView1.Columns["DetectedHex"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns["DetectedHex"].Width = 140;
            }

            if (dataGridView1.Columns.Contains("RecommendedProductName"))
            {
                dataGridView1.Columns["RecommendedProductName"].HeaderText = "Ajánlott krétafesték";
                dataGridView1.Columns["RecommendedProductName"].DefaultCellStyle.Font =
                    new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            }

            if (dataGridView1.Columns.Contains("PriceHUF"))
            {
                dataGridView1.Columns["PriceHUF"].HeaderText = "Ár (HUF)";
                dataGridView1.Columns["PriceHUF"].DefaultCellStyle.Format = "#,##0 Ft";
                dataGridView1.Columns["PriceHUF"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["PriceHUF"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dataGridView1.Columns.Contains("ArEur"))
            {
                dataGridView1.Columns["ArEur"].HeaderText = "Ár (EUR)";
                dataGridView1.Columns["ArEur"].DefaultCellStyle.Format = "€ 0.00";
                dataGridView1.Columns["ArEur"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["ArEur"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Szélességek automatikus beállítása (a DetectedHex-et ez nem bántja az AutoSizeMode.None miatt)
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            if (dataGridView1.Columns.Contains("RecommendedProductName"))
            {
                dataGridView1.Columns["RecommendedProductName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            // Rendezettség kikapcsolása
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        // --- MNB ÁRFOLYAM LEKÉRDEZÉS ---
        private decimal GetEurRateFromMNB()
        {
            try
            {
                var mnbService = new MnbService.MNBArfolyamServiceSoapClient();
                var request = new MnbService.GetCurrentExchangeRatesRequestBody();
                var response = mnbService.GetCurrentExchangeRates(request);

                string xmlResult = response.GetCurrentExchangeRatesResult;
                var xml = new XmlDocument();
                xml.LoadXml(xmlResult);

                var eurNode = xml.SelectSingleNode("//Rate[@curr='EUR']");
                if (eurNode != null)
                {
                    string rateStr = eurNode.InnerText.Replace(',', '.');
                    return decimal.Parse(rateStr, System.Globalization.CultureInfo.InvariantCulture);
                }
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nem sikerült lekérni az MNB árfolyamot: " + ex.Message);
                return 0;
            }
        }

        // --- ADATOK BETÖLTÉSE AZ API-BÓL ---
        private async Task LoadDataAsync()
        {
            try
            {
                // A DNN RouteMapper beállítása alapján felépített URL
                var json = await _http.GetStringAsync("DesktopModules/FrestekVision/API/Vision/GetLogs");

                _allLogs = JsonConvert.DeserializeObject<List<VisionLogEntry>>(json);

                // MNB számítás
                decimal currentEurRate = GetEurRateFromMNB();

                if (label2 != null)
                    label2.Text = "1 EUR = " + currentEurRate.ToString("0.00") + " Ft";

                if (currentEurRate > 0)
                {
                    foreach (var item in _allLogs)
                    {
                        item.ArEur = Math.Round(item.PriceHUF / currentEurRate, 2);
                    }
                }

                // UI Frissítése
                dataGridView1.DataSource = _allLogs;
                FormatGridColumns();

                // Diagram frissítése a betöltött adatokból
                DrawColorGroupChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a betöltés során: " + ex.Message);
            }
        }

        // --- DIAGRAM RAJZOLÁSA A PANEL1-RE ---
        private void DrawColorGroupChart()
        {
            // Csoportosítjuk az adatokat Színcsoport szerint és megszámoljuk őket
            var stats = _allLogs
                .Where(x => !string.IsNullOrWhiteSpace(x.ColorGroup))
                .GroupBy(x => x.ColorGroup)
                .Select(g => new { Group = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (stats.Count == 0) return;

            // Panel letisztítása
            panel1.Controls.Clear();

            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;
            chart.BackColor = System.Drawing.Color.White;

            // Rajzterület beállítása
            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.BackColor = System.Drawing.Color.Transparent;

            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10);
            chartArea.AxisX.Interval = 1;

            chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(230, 230, 230);
            chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chartArea.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 10);

            chart.ChartAreas.Add(chartArea);

            // Adatsor létrehozása
            Series series = new Series("Színcsoportok");
            series.ChartType = SeriesChartType.Column;
            series.Color = System.Drawing.Color.FromArgb(21, 76, 89);

            series.IsValueShownAsLabel = true;
            series.Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold);
            series.LabelForeColor = System.Drawing.Color.FromArgb(244, 103, 29);

            foreach (var item in stats)
            {
                series.Points.AddXY(item.Group, item.Count);
            }
            chart.Series.Add(series);

            Title title = new Title("Színcsoportok eloszlása", Docking.Top,
                new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                System.Drawing.Color.FromArgb(50, 50, 50));
            chart.Titles.Add(title);

            panel1.Controls.Add(chart);
        }

        // --- DATAGRIDVIEW ALAP STÍLUSOK ---
        private void SetupModernGridStyle()
        {
            dataGridView1.BackgroundColor = System.Drawing.Color.White;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.Height = 60;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.GridColor = System.Drawing.Color.FromArgb(235, 238, 240);

            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersHeight = 50;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            var headerStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = System.Drawing.Color.FromArgb(21, 76, 89),
                SelectionForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.FromArgb(21, 76, 89),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            dataGridView1.ColumnHeadersDefaultCellStyle = headerStyle;

            var cellStyle = new DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 50),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular),
                SelectionBackColor = System.Drawing.Color.FromArgb(240, 245, 248),
                SelectionForeColor = System.Drawing.Color.Black,
                Padding = new Padding(8, 0, 0, 0)
            };
            dataGridView1.DefaultCellStyle = cellStyle;

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
        }

        // --- SZÍNES NÉGYZET RAJZOLÁSA (HEX KÓD ALAPJÁN) ---
        private void DataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "DetectedHex")
            {
                // Magas minőségű grafika (éles vonalak)
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                string hexCode = e.Value?.ToString() ?? "#FFFFFF";
                if (!hexCode.StartsWith("#")) hexCode = "#" + hexCode; // Biztosíték, ha lemaradna a hash mark

                System.Drawing.Color boxColor;
                try
                {
                    boxColor = System.Drawing.ColorTranslator.FromHtml(hexCode);
                }
                catch
                {
                    boxColor = System.Drawing.Color.LightGray; // Alapértelmezett szín, ha érvénytelen a HEX
                }

                int boxSize = 24;
                int boxY = e.CellBounds.Y + (e.CellBounds.Height - boxSize) / 2;
                int boxX = e.CellBounds.X + 15;

                using (var brush = new System.Drawing.SolidBrush(boxColor))
                    e.Graphics.FillRectangle(brush, boxX, boxY, boxSize, boxSize);

                using (var pen = new System.Drawing.Pen(System.Drawing.Color.LightGray, 1))
                    e.Graphics.DrawRectangle(pen, boxX, boxY, boxSize, boxSize);

                using (var textBrush = new System.Drawing.SolidBrush(e.CellStyle.ForeColor))
                {
                    int textX = boxX + boxSize + 10;
                    int textY = e.CellBounds.Y + (e.CellBounds.Height - e.CellStyle.Font.Height) / 2;
                    e.Graphics.DrawString(hexCode, e.CellStyle.Font, textBrush, textX, textY);
                }
            }
        }

        // Statisztika panel megnyitó/bezáró
        private void statbtn_Click(object sender, EventArgs e)
        {
            if (panel1.Visible)
            {
                panel1.Visible = false;
            }
            else
            {
                panel1.Visible = true;
            }
        }
    }

    // --- AZ ADATMODELL ---
    // Pontosan megegyezik az API VisionLog objektumával
    public class VisionLogEntry
    {
        public int LogId { get; set; }
        public int ModuleId { get; set; }
        public string DetectedHex { get; set; }
        public string RecommendedProductName { get; set; }
        public decimal PriceHUF { get; set; }
        public string ColorGroup { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; }

        // Ez csak a kliensben létezik, mi számoljuk ki!
        public decimal ArEur { get; set; }
    }
}