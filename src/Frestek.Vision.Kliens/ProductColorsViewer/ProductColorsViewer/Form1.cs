using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ProductColorsViewer
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://frestek.peacock-shilling.ts.net/")
        };

        // Ebbe a listába mentjük el az összes letöltött adatot, ebből fogunk szűrni
        private List<ProductColor> _allProducts = new List<ProductColor>();

        public Form1()
        {
            InitializeComponent();

            // 1. Megjelenés beállítása
            SetupModernUI();          // Az új, globális ablak- és gombdizájn
            SetupModernGridStyle();   // A táblázat stílusa

            // 2. Események bekötése
            dataGridView1.CellPainting += DataGridView1_CellPainting;
            btnLoad.Click += async (s, e) => await LoadDataAsync();
            Load += async (s, e) => await LoadDataAsync();

            // 3. ComboBox beállítása
            if (comboBox1 != null)
            {
                comboBox1.DropDownStyle = ComboBoxStyle.DropDownList; // Csak választani lehessen
                comboBox1.Items.Add("Ajánlott termék");
                comboBox1.Items.Add("Felhasználó");
                comboBox1.Items.Add("Észlelt szín");
                comboBox1.SelectedIndex = 0;
                comboBox1.SelectedIndexChanged += TextBox1_TextChanged;
            }

            // 4. Kereső TextBox bekötése
            if (textBox1 != null)
            {
                textBox1.TextChanged += TextBox1_TextChanged;
            }
        }

        // --- MODERN FELÜLET BEÁLLÍTÁSA ---
        private void SetupModernUI()
        {
            // Háttérszín (nagyon világos szürke, hogy a fehér táblázat kiemelkedjen)
            this.BackColor = System.Drawing.Color.FromArgb(245, 246, 248);
            dataGridView1.BackgroundColor = this.BackColor;

            // Frissítés gomb stílusa
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

            // ComboBox stílusa
            if (comboBox1 != null)
            {
                comboBox1.FlatStyle = FlatStyle.Flat;
                comboBox1.Font = new System.Drawing.Font("Segoe UI", 10F);
            }

            // TextBox stílusa és Vízjel (Placeholder) logikája
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

            // Főcím (Label) megkeresése és formázása
            Label lblTitle = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text != null && l.Text.Contains("Ajánláskezelő"));
            if (lblTitle != null)
            {
                lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
                lblTitle.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            }

            // Árfolyam kiírás (label2) formázása
            if (label2 != null)
            {
                label2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
                label2.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            }
        }

        // --- SZŰRÉS A TEXTBOX ÉS A COMBOBOX ALAPJÁN ---
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (_allProducts == null || _allProducts.Count == 0) return;

            string filterText = textBox1.Text.ToLower();

            // Ha a placeholder szöveg van benne, akkor úgy vesszük, mintha üres lenne
            if (filterText == "keresés...") filterText = "";

            if (string.IsNullOrWhiteSpace(filterText))
            {
                dataGridView1.DataSource = _allProducts;
            }
            else
            {
                string kivalasztottOszlop = comboBox1.SelectedItem?.ToString();

                var filteredList = _allProducts.Where(p =>
                {
                    if (kivalasztottOszlop == "Ajánlott termék")
                        return p.ProductName != null && p.ProductName.ToLower().Contains(filterText);
                    else if (kivalasztottOszlop == "Felhasználó")
                        return p.OrderId != null && p.OrderId.ToLower().Contains(filterText);
                    else if (kivalasztottOszlop == "Észlelt szín")
                        return p.Color != null && p.Color.ToLower().Contains(filterText);

                    return false;
                }).ToList();

                dataGridView1.DataSource = filteredList;
            }

            FormatGridColumns();
        }

        // --- OSZLOPOK FORMÁZÁSA ---
        private void FormatGridColumns()
        {
            if (dataGridView1.Columns.Count == 0) return;

            if (dataGridView1.Columns.Contains("Id")) dataGridView1.Columns["Id"].Visible = false;

            if (dataGridView1.Columns.Contains("Datum")) dataGridView1.Columns["Datum"].DisplayIndex = 0;
            if (dataGridView1.Columns.Contains("OrderId")) dataGridView1.Columns["OrderId"].DisplayIndex = 1;
            if (dataGridView1.Columns.Contains("Color")) dataGridView1.Columns["Color"].DisplayIndex = 2;
            if (dataGridView1.Columns.Contains("ProductName")) dataGridView1.Columns["ProductName"].DisplayIndex = 3;
            if (dataGridView1.Columns.Contains("ArHuf")) dataGridView1.Columns["ArHuf"].DisplayIndex = 4;
            if (dataGridView1.Columns.Contains("ArEur")) dataGridView1.Columns["ArEur"].DisplayIndex = 5;

            if (dataGridView1.Columns.Contains("Datum"))
            {
                dataGridView1.Columns["Datum"].HeaderText = "Dátum";
                dataGridView1.Columns["Datum"].DefaultCellStyle.Format = "yyyy.MM.dd";
            }
            if (dataGridView1.Columns.Contains("OrderId"))
                dataGridView1.Columns["OrderId"].HeaderText = "Felhasználó";

            if (dataGridView1.Columns.Contains("Color"))
                dataGridView1.Columns["Color"].HeaderText = "Észlelt szín";

            if (dataGridView1.Columns.Contains("ProductName"))
            {
                dataGridView1.Columns["ProductName"].HeaderText = "Ajánlott termék";
                dataGridView1.Columns["ProductName"].DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            }

            if (dataGridView1.Columns.Contains("ArHuf"))
            {
                dataGridView1.Columns["ArHuf"].HeaderText = "Ár (HUF)";
                dataGridView1.Columns["ArHuf"].DefaultCellStyle.Format = "#,##0 Ft";
                dataGridView1.Columns["ArHuf"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["ArHuf"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dataGridView1.Columns.Contains("ArEur"))
            {
                dataGridView1.Columns["ArEur"].HeaderText = "Ár (EUR)";
                dataGridView1.Columns["ArEur"].DefaultCellStyle.Format = "0.00 €";
                dataGridView1.Columns["ArEur"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["ArEur"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // 1. Alapértelmezetten minden oszlop legyen akkora, amekkora a tartalma
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // 2. Kivéve a "ProductName" (Ajánlott termék) oszlopot, az töltse ki a maradék helyet!
            if (dataGridView1.Columns.Contains("ProductName"))
            {
                dataGridView1.Columns["ProductName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            // Minden oszlop rendezésének kikapcsolása
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

        // --- ADATOK BETÖLTÉSE ---
        private async Task LoadDataAsync()
        {
            try
            {
                var json = await _http.GetStringAsync("DesktopModules/ProductColors/API/ProductColors/GetAll");
                _allProducts = JsonConvert.DeserializeObject<List<ProductColor>>(json);

                decimal currentEurRate = GetEurRateFromMNB();
                if (label2 != null) label2.Text = "1 EUR = " + currentEurRate.ToString("0.00") + " Ft";

                if (currentEurRate > 0)
                {
                    foreach (var item in _allProducts)
                    {
                        item.ArEur = Math.Round(item.ArHuf / currentEurRate, 2);
                    }
                }

                dataGridView1.DataSource = _allProducts;
                FormatGridColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a betöltés során: " + ex.Message);
            }
        }

        // --- DATAGRIDVIEW STÍLUS BEÁLLÍTÁSA ---
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

            var headerStyle = new DataGridViewCellStyle();
            headerStyle.SelectionBackColor = System.Drawing.Color.FromArgb(21, 76, 89);
            headerStyle.SelectionForeColor = System.Drawing.Color.White;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(21, 76, 89);
            headerStyle.ForeColor = System.Drawing.Color.White;
            headerStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyle.Padding = new Padding(8, 0, 0, 0);
            dataGridView1.ColumnHeadersDefaultCellStyle = headerStyle;

            var cellStyle = new DataGridViewCellStyle();
            cellStyle.BackColor = System.Drawing.Color.White;
            cellStyle.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            cellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            cellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(240, 245, 248);
            cellStyle.SelectionForeColor = System.Drawing.Color.Black;
            cellStyle.Padding = new Padding(8, 0, 0, 0);
            dataGridView1.DefaultCellStyle = cellStyle;

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
        }

        // --- SZÍNES NÉGYZET RAJZOLÁSA ---
        private void DataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "Color")
            {
                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                string colorName = e.Value?.ToString() ?? "";

                System.Drawing.Color boxColor = System.Drawing.Color.Transparent;
                if (colorName == "Piros") boxColor = System.Drawing.Color.FromArgb(230, 57, 70);
                else if (colorName == "Zöld") boxColor = System.Drawing.Color.FromArgb(42, 157, 143);
                else if (colorName == "Sárga") boxColor = System.Drawing.Color.FromArgb(244, 162, 97);
                else boxColor = System.Drawing.Color.LightGray;

                int boxSize = 24;
                int boxY = e.CellBounds.Y + (e.CellBounds.Height - boxSize) / 2;
                int boxX = e.CellBounds.X + 15;

                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(boxColor))
                {
                    e.Graphics.FillRectangle(brush, boxX, boxY, boxSize, boxSize);
                }

                using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.LightGray, 1))
                {
                    e.Graphics.DrawRectangle(pen, boxX, boxY, boxSize, boxSize);
                }

                using (System.Drawing.SolidBrush textBrush = new System.Drawing.SolidBrush(e.CellStyle.ForeColor))
                {
                    int textX = boxX + boxSize + 10;
                    int textY = e.CellBounds.Y + (e.CellBounds.Height - e.CellStyle.Font.Height) / 2;
                    e.Graphics.DrawString(colorName, e.CellStyle.Font, textBrush, textX, textY);
                }
            }
        }
    }

    // --- ADATMODELL ---
    public class ProductColor
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string OrderId { get; set; }
        public int ArHuf { get; set; }
        public decimal ArEur { get; set; }
        public DateTime Datum { get; set; }
    }
}