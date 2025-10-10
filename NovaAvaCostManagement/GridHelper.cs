using System;
using System.Drawing;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public static class GridHelper
    {
        public static void SetupColumns(DataGridView dataGridView)
        {
            dataGridView.Columns.Clear();

            AddColumn(dataGridView, "colCostId", "Id", "Cost-ID", 80, true, true);
            AddColumn(dataGridView, "colName", "Name", "Name", 200, false, false);
            AddColumn(dataGridView, "colDescription", "Description", "Description", 150, false, false);
            AddColumn(dataGridView, "colSpecFilter", "SpecFilter", "DX.SPEC_filter", 100, false, false);
            AddColumn(dataGridView, "colSpecName", "SpecName", "DX.SPEC_Name", 150, false, false);
            AddColumn(dataGridView, "colSpecSize", "SpecSize", "DX.SPEC_Size", 100, false, false);
            AddColumn(dataGridView, "colSpecType", "SpecType", "DX.SPEC_Type", 120, false, false);
            AddColumn(dataGridView, "colSpecManufacturer", "SpecManufacturer", "DX.SPEC_Manufacturer", 150, false, false);
            AddColumn(dataGridView, "colSpecMaterial", "SpecMaterial", "DX.SPEC_Material", 120, false, false);
            AddColumn(dataGridView, "colLineId", "CalculationId", "Line-ID", 80, true, false);
            AddColumn(dataGridView, "colOrder", "Order", "Order", 70, true, false);
            AddColumn(dataGridView, "colIdent", "Ident", "GUID-Ident", 280, false, false);

            AddCatalogColumn(dataGridView, "colCatalog1", "Catalog 1", 180);
            AddCatalogColumn(dataGridView, "colCatalog2", "Catalog 2", 180);
            AddCatalogColumn(dataGridView, "colCatalog3", "Catalog 3", 180);
            AddCatalogColumn(dataGridView, "colCatalog4", "Catalog 4", 180);

            AddColumn(dataGridView, "colText", "Text", "Text", 200, false, false);

            var colLongText = new DataGridViewTextBoxColumn
            {
                Name = "colLongText",
                DataPropertyName = "LongText",
                HeaderText = "Longtext",
                Width = 300,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.False,
                    Font = new Font("Segoe UI", 8F)
                }
            };
            dataGridView.Columns.Add(colLongText);

            AddColumn(dataGridView, "colQuantity", "Qty", "Quantity", 100, false, false);
            AddColumn(dataGridView, "colQtyResult", "QtyResult", "Qty_result", 100, false, false);
            AddColumn(dataGridView, "colEinheit", "Qu", "Einheit", 80, false, false);
            AddColumn(dataGridView, "colPrice", "Up", "Price", 100, false, false);
            AddColumn(dataGridView, "colTotal", "UpResult", "Total", 120, true, false);
        }

        private static void AddColumn(DataGridView dataGridView, string name, string dataPropertyName,
            string headerText, int width, bool readOnly, bool frozen)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                ReadOnly = readOnly,
                Frozen = frozen
            };

            if (readOnly)
            {
                column.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            }

            dataGridView.Columns.Add(column);
        }

        private static void AddCatalogColumn(DataGridView dataGridView, string name, string headerText, int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(245, 245, 250)
                }
            };
            dataGridView.Columns.Add(column);
        }

        public static CostElement GetElementAtRow(DataGridView dataGridView, int rowIndex, ViewMode currentViewMode)
        {
            if (rowIndex < 0 || rowIndex >= dataGridView.Rows.Count)
                return null;

            var row = dataGridView.Rows[rowIndex];
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                    return wbsItem.Element;
            }
            else
            {
                if (boundItem is CostElement element)
                    return element;
            }

            return null;
        }

        public static CostElement GetElementFromRow(DataGridViewRow row, ViewMode currentViewMode)
        {
            var boundItem = row.DataBoundItem;

            if (currentViewMode == ViewMode.WbsHierarchy)
            {
                if (boundItem is WbsDisplayItem wbsItem)
                    return wbsItem.Element;
            }
            else
            {
                if (boundItem is CostElement element)
                    return element;
            }

            return null;
        }
    }
}