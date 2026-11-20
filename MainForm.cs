using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RecipeWinFormsApp
{
    public partial class MainForm : Form
    {
        private readonly string _jsonFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recipes.json");

        private static readonly string[] WorkOptions =
        {
            "Drop",
            "StoreFront",
            "StoreRear"
        };

        private static readonly HashSet<string> WorkRecipeNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BlueColor_Work",
                "OtherColor_Work"
            };

        private Dictionary<string, List<Recipe>> _recipeDict =
            new Dictionary<string, List<Recipe>>(StringComparer.OrdinalIgnoreCase);

        private readonly BindingList<Recipe> _gridItems = new BindingList<Recipe>();

        public MainForm()
        {
            InitializeComponent();

            cmbRecipeKeys.DropDownStyle = ComboBoxStyle.DropDown;
            SetupGrid();
            ReloadAll(null);
        }

        private void SetupGrid()
        {
            dgvRecipe.AutoGenerateColumns = false;
            dgvRecipe.AllowUserToAddRows = false;
            dgvRecipe.Columns.Clear();

            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Recipe.RecipeName),
                HeaderText = "RecipeName",
                Name = "colRecipeName",
                Width = 150,
                ReadOnly = true
            });

            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Recipe.Value),
                HeaderText = "Value",
                Name = "colValue",
                Width = 140,
                ReadOnly = false
            });

            dgvRecipe.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Recipe.Desc),
                HeaderText = "Desc",
                Name = "colDesc",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            dgvRecipe.DataSource = _gridItems;

            dgvRecipe.RowsAdded += dgvRecipe_RowsAdded;
            dgvRecipe.CellValueChanged += dgvRecipe_CellValueChanged;
            dgvRecipe.EditingControlShowing += dgvRecipe_EditingControlShowing;
            dgvRecipe.CellValidating += dgvRecipe_CellValidating;
            dgvRecipe.DataError += dgvRecipe_DataError;
        }

        private bool IsWorkValueRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvRecipe.Rows.Count) return false;
            DataGridViewRow row = dgvRecipe.Rows[rowIndex];
            if (row.IsNewRow) return false;

            string recipeName = row.Cells["colRecipeName"].Value?.ToString()?.Trim() ?? string.Empty;
            return WorkRecipeNames.Contains(recipeName);
        }

        private void ApplyValueCellEditor(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvRecipe.Rows.Count) return;
            DataGridViewRow row = dgvRecipe.Rows[rowIndex];
            if (row.IsNewRow) return;

            string recipeName = row.Cells["colRecipeName"].Value?.ToString()?.Trim() ?? string.Empty;
            string currentValue = row.Cells["colValue"].Value?.ToString() ?? string.Empty;

            if (WorkRecipeNames.Contains(recipeName))
            {
                var comboCell = new DataGridViewComboBoxCell
                {
                    FlatStyle = FlatStyle.Flat
                };

                foreach (string opt in WorkOptions)
                {
                    comboCell.Items.Add(opt);
                }

                if (!string.IsNullOrEmpty(currentValue) && !comboCell.Items.Contains(currentValue))
                {
                    comboCell.Items.Add(currentValue);
                }

                comboCell.Value = string.IsNullOrWhiteSpace(currentValue) ? "Drop" : currentValue;
                row.Cells["colValue"] = comboCell;
            }
            else if (row.Cells["colValue"] is DataGridViewComboBoxCell)
            {
                var textCell = new DataGridViewTextBoxCell();
                textCell.Value = int.TryParse(currentValue, out int num) && num >= 0 && num <= 9999
                    ? num.ToString()
                    : "0";
                row.Cells["colValue"] = textCell;
            }
        }

        private void RefreshAllRowEditors()
        {
            for (int i = 0; i < dgvRecipe.Rows.Count; i++)
            {
                if (!dgvRecipe.Rows[i].IsNewRow)
                {
                    ApplyValueCellEditor(i);
                }
            }
        }

        private void dgvRecipe_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
            {
                if (i >= 0 && i < dgvRecipe.Rows.Count && !dgvRecipe.Rows[i].IsNewRow)
                {
                    ApplyValueCellEditor(i);
                }
            }
        }

        private void dgvRecipe_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvRecipe.Columns[e.ColumnIndex].Name == "colRecipeName")
            {
                ApplyValueCellEditor(e.RowIndex);
            }
        }

        private void dgvRecipe_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvRecipe.CurrentCell == null ||
                dgvRecipe.CurrentCell.ColumnIndex != dgvRecipe.Columns["colValue"].Index)
                return;

            if (IsWorkValueRow(dgvRecipe.CurrentCell.RowIndex))
                return;

            if (e.Control is TextBox tb)
            {
                tb.KeyPress -= NumericTextBox_KeyPress;
                tb.KeyPress += NumericTextBox_KeyPress;
            }
        }

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void dgvRecipe_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvRecipe.Rows[e.RowIndex].IsNewRow) return;
            if (dgvRecipe.Columns[e.ColumnIndex].Name != "colValue") return;
            if (IsWorkValueRow(e.RowIndex)) return;

            string input = e.FormattedValue?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(input)) return;

            if (!int.TryParse(input, out int value) || value < 0 || value > 9999)
            {
                e.Cancel = true;
                MessageBox.Show("숫자 항목의 Value는 0부터 9999 사이의 정수만 입력할 수 있습니다.",
                    "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void dgvRecipe_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private List<Recipe> CreateDefaultRecipes()
        {
            return new List<Recipe>
            {
                new Recipe { RecipeName = "PosXStart", Value = "0", Desc = "" },
                new Recipe { RecipeName = "PosXEnd", Value = "640", Desc = "" },
                new Recipe { RecipeName = "PosYStart", Value = "0", Desc = "" },
                new Recipe { RecipeName = "PosYEnd", Value = "480", Desc = "" },
                new Recipe { RecipeName = "Blue_Lower", Value = "104", Desc = "" },
                new Recipe { RecipeName = "Blue_Upper", Value = "130", Desc = "" },
                new Recipe { RecipeName = "BlueColor_Work", Value = "StoreFront", Desc = "Drop/StoreFront/StoreRear" },
                new Recipe { RecipeName = "OtherColor_Work", Value = "Drop", Desc = "Drop/StoreFront/StoreRear" },
                new Recipe { RecipeName = "Timeout", Value = "100", Desc = "" }
            };
        }

        private void ReloadAll(string targetKey)
        {
            _recipeDict = RecipeJsonStorage.LoadFromJson(_jsonFilePath);

            cmbRecipeKeys.SelectedIndexChanged -= cmbRecipeKeys_SelectedIndexChanged;
            cmbRecipeKeys.TextChanged -= cmbRecipeKeys_TextChanged;

            var keys = _recipeDict.Keys.OrderBy(k => k).ToList();
            cmbRecipeKeys.DataSource = null;
            cmbRecipeKeys.DataSource = keys;

            cmbRecipeKeys.SelectedIndexChanged += cmbRecipeKeys_SelectedIndexChanged;
            cmbRecipeKeys.TextChanged += cmbRecipeKeys_TextChanged;

            string activeKey = targetKey ?? keys.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(activeKey) &&
                _recipeDict.TryGetValue(activeKey, out List<Recipe> items))
            {
                cmbRecipeKeys.SelectedItem = keys.FirstOrDefault(
                    k => string.Equals(k, activeKey, StringComparison.OrdinalIgnoreCase));
                cmbRecipeKeys.Text = activeKey;
                BindGrid(items);
            }
            else
            {
                cmbRecipeKeys.Text = string.Empty;
                BindGrid(CreateDefaultRecipes());
            }

            RecipeService.UpdateCache(_recipeDict, cmbRecipeKeys.Text.Trim());
        }

        private void BindGrid(IEnumerable<Recipe> items)
        {
            _gridItems.Clear();
            foreach (var item in items)
            {
                _gridItems.Add(new Recipe
                {
                    RecipeName = item.RecipeName,
                    Value = item.Value,
                    Desc = item.Desc
                });
            }

            RefreshAllRowEditors();
        }

        private List<Recipe> ReadGrid()
        {
            dgvRecipe.EndEdit();
            return dgvRecipe.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => new Recipe
                {
                    RecipeName = r.Cells["colRecipeName"].Value?.ToString()?.Trim() ?? string.Empty,
                    Value = r.Cells["colValue"].Value?.ToString()?.Trim() ?? string.Empty,
                    Desc = r.Cells["colDesc"].Value?.ToString()?.Trim() ?? string.Empty
                })
                .Where(x => !(string.IsNullOrEmpty(x.RecipeName) &&
                              string.IsNullOrEmpty(x.Value) &&
                              string.IsNullOrEmpty(x.Desc)))
                .ToList();
        }

        // [저장] (신규 추가 및 기존 키 수정 통합)
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!dgvRecipe.EndEdit()) return;

            string key = cmbRecipeKeys.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("레시피 이름을 선택하거나 입력하세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<Recipe> items = ReadGrid();
            foreach (var item in items)
            {
                if (!WorkRecipeNames.Contains(item.RecipeName) && !string.IsNullOrEmpty(item.Value))
                {
                    if (!int.TryParse(item.Value, out int num) || num < 0 || num > 9999)
                    {
                        MessageBox.Show($"'{item.RecipeName}'의 Value는 0~9999 정수여야 합니다.",
                            "저장 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            _recipeDict[key] = items;
            RecipeJsonStorage.SaveToJson(_jsonFilePath, _recipeDict);
            ReloadAll(key);

            MessageBox.Show($"'{key}' 레시피가 저장되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ComboBox 선택 시 기존 그리드 표시
        private void cmbRecipeKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbRecipeKeys.SelectedItem == null) return;
            string key = cmbRecipeKeys.SelectedItem.ToString();
            if (_recipeDict.TryGetValue(key, out List<Recipe> items))
            {
                BindGrid(items);
                RecipeService.CurrentRecipeKey = key;
            }
        }

        // 새 레시피 이름 입력 시 기본값 템플릿 자동 입력
        private void cmbRecipeKeys_TextChanged(object sender, EventArgs e)
        {
            string currentText = cmbRecipeKeys.Text.Trim();
            if (string.IsNullOrWhiteSpace(currentText)) return;

            if (!_recipeDict.ContainsKey(currentText))
            {
                bool isAlreadyDefault = _gridItems.Count == 9 &&
                                        _gridItems.First().RecipeName == "PosXStart";
                if (!isAlreadyDefault)
                {
                    BindGrid(CreateDefaultRecipes());
                }
            }
        }

        // [삭제]
        private void btnDelete_Click(object sender, EventArgs e)
        {
            string key = cmbRecipeKeys.Text.Trim();
            if (string.IsNullOrWhiteSpace(key) || !_recipeDict.ContainsKey(key))
            {
                MessageBox.Show("삭제할 레시피를 선택하거나 입력하세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"'{key}' 레시피를 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _recipeDict.Remove(key);
            RecipeJsonStorage.SaveToJson(_jsonFilePath, _recipeDict);
            ReloadAll(null);
        }
    }
}