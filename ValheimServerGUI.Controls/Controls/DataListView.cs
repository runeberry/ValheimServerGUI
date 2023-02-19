using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class DataListView : UserControl
    {
        #region Designer fields

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor("System.Windows.Forms.Design.ColumnHeaderCollectionEditor", "System.Drawing.Design.UITypeEditor")]
        [Localizable(true)]
        [MergableProperty(false)]
        public ListView.ColumnHeaderCollection Columns => ListView.Columns;

        public override Color ForeColor
        {
            get => ListView.ForeColor;
            set => ListView.ForeColor = value;
        }

        public ImageList Icons
        {
            get => ListView.SmallImageList;
            set => ListView.SmallImageList = value;
        }

        #endregion

        #region Data fields

        private readonly List<DataListViewRow> _rows = new();
        public IReadOnlyList<DataListViewRow> Rows => _rows;

        private readonly ConcurrentDictionary<Type, DataListViewRowBinding> RowBindings = new();

        public bool IsRowSelected => ListView.SelectedItems.Count > 0;

        public event EventHandler SelectionChanged;

        public int? SortColumn { get; protected set; }

        public bool SortDescending { get; protected set; }

        #endregion

        public DataListView()
        {
            InitializeComponent();

            ListView.ItemSelectionChanged += ListView_ItemSelectionChanged;
            ListView.ColumnClick += ListView_ColumnClick;
        }

        private void ListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var descending = false;
            if (SortColumn == e.Column) descending = !SortDescending;

            OrderByColumn(e.Column, descending);
        }

        public DataListView AddRowBinding<TEntity>(Action<IDataListViewRowBinding<TEntity>> builder)
        {
            var binding = new DataListViewRowBinding<TEntity>();
            if (RowBindings.TryAdd(typeof(TEntity), binding))
            {
                builder?.Invoke(binding);
            };

            return this;
        }

        #region Read methods

        public IEnumerable<DataListViewRow<TEntity>> GetRowsWithType<TEntity>()
        {
            return Rows
                .Select(row => row as DataListViewRow<TEntity>)
                .Where(row => row != null);
        }

        public DataListViewRow GetSelectedRow()
        {
            if (!IsRowSelected) return null;

            return ListView.SelectedItems[0] as DataListViewRow;
        }

        public DataListViewRow<TEntity> GetSelectedRow<TEntity>()
        {
            if (!IsRowSelected) return null;

            return ListView.SelectedItems[0] as DataListViewRow<TEntity>;
        }

        public bool TryGetSelectedRow(out DataListViewRow row)
        {
            row = GetSelectedRow();
            return row != null;
        }

        public bool TryGetSelectedRow<TEntity>(out DataListViewRow<TEntity> row)
        {
            row = GetSelectedRow<TEntity>();
            return row != null;
        }

        #endregion

        #region Write methods

        public DataListViewRow AddRowFromValues(params object[] values)
        {
            var item = new DataListViewRow(this, values);

            ListView.Items.Add(item);
            _rows.Add(item);

            // Reapply the sort when data is added
            if (SortColumn.HasValue) OrderByColumn(SortColumn.Value, SortDescending);

            return item;
        }

        public DataListViewRow<TEntity> AddRowFromEntity<TEntity>(TEntity entity)
        {
            if (!RowBindings.TryGetValue(typeof(TEntity), out var binding))
            {
                throw new ArgumentException($"{nameof(DataListView)} does not have a binding for type {typeof(TEntity)}");
            }

            var typedBinding = binding as DataListViewRowBinding<TEntity>;
            var item = new DataListViewRow<TEntity>(this, entity, typedBinding);

            ListView.Items.Add(item);
            _rows.Add(item);

            // Reapply the sort when data is added
            if (SortColumn.HasValue) OrderByColumn(SortColumn.Value, SortDescending);

            return item;
        }

        public bool RemoveRow(DataListViewRow row)
        {
            if (row == null) return false;

            var result = _rows.Remove(row);

            if (result)
            {
                ListView.Items.Remove(row);
            }

            return result;
        }

        public bool RemoveSelectedRow()
        {
            return RemoveRow(GetSelectedRow());
        }

        public bool RemoveRows(IEnumerable<DataListViewRow> rows)
        {
            if (rows == null) return false;

            var anyRemoved = false;

            foreach (var row in rows)
            {
                var result = RemoveRow(row);
                anyRemoved = anyRemoved || result;
            }

            return anyRemoved;
        }

        public bool RemoveRowsWhere(Func<DataListViewRow, bool> condition)
        {
            return RemoveRows(Rows.Where(condition).ToList());
        }

        public bool RemoveRowsWhere<TEntity>(Func<DataListViewRow<TEntity>, bool> condition)
        {
            return RemoveRows(GetRowsWithType<TEntity>().Where(condition).ToList());
        }

        #endregion

        #region Sort methods

        public void OrderByColumn(int colIndex, bool descending = false)
        {
            if (colIndex >= Columns.Count) return;

            var rowsCopy = descending
                ? _rows.OrderByDescending(r => r.GetCell(colIndex)?.Value).ToArray()
                : _rows.OrderBy(r => r.GetCell(colIndex)?.Value).ToArray();

            SortColumn = colIndex;
            SortDescending = descending;

            ListView.Items.Clear();
            _rows.Clear();

            ListView.Items.AddRange(rowsCopy);
            _rows.AddRange(rowsCopy);
        }

        #endregion

        #region Nested class: DataListViewRow

        public class DataListViewRow : ListViewItem
        {
            public DataListView Parent { get; }

            private readonly List<DataListViewCell> _cells = new();
            public IReadOnlyList<DataListViewCell> Cells => _cells;

            public DataListViewRow(DataListView parent)
            {
                Parent = parent;
            }

            public DataListViewRow(DataListView parent, int numColumns)
                : this(parent)
            {
                FillCellsToIndex(numColumns - 1);
            }

            public DataListViewRow(DataListView parent, params object[] values)
                : this(parent)
            {
                SetCellValues(values);
            }

            public DataListViewRow SetCellValues(params object[] values)
            {
                if (values == null || values.Length == 0)
                {
                    // No values have been provided, clear out all existing values
                    foreach (var cell in Cells)
                    {
                        cell.Value = null;
                    }
                }
                else
                {
                    var maxIndex = Math.Max(Cells.Count, values.Length) - 1;

                    for (var i = 0; i <= maxIndex; i++)
                    {
                        if (i >= values.Length)
                        {
                            // A cell exists, but no value has been provided for it,
                            // so clear out the existing value
                            SetCellValue(i, null);
                        }
                        else
                        {
                            SetCellValue(i, values[i]);
                        }
                    }
                }

                return this;
            }

            public DataListViewRow SetCellValue(int cellIndex, object value)
            {
                var cell = GetCell(cellIndex);
                cell.Value = value;

                // ListView compatibility: Item [0] is actually just the row's Text
                if (cellIndex == 0)
                {
                    Text = cell.Text;
                }

                return this;
            }

            public DataListViewCell GetCell(int cellIndex)
            {
                FillCellsToIndex(cellIndex);

                return Cells[cellIndex];
            }

            #region Non-public methods

            private void FillCellsToIndex(int cellIndex)
            {
                if (cellIndex >= Cells.Count)
                {
                    for (var i = Cells.Count; i <= cellIndex; i++)
                    {
                        AddCell();
                    }
                }
            }

            private DataListViewCell AddCell()
            {
                var cell = new DataListViewCell(this);

                // ListView compatibility: Item [0] is simply the row's Text, but subsequent
                // items must be added as SubItems
                if (Cells.Count > 0)
                {
                    SubItems.Add(cell);
                }

                _cells.Add(cell);

                return cell;
            }

            #endregion
        }

        public class DataListViewRow<TEntity> : DataListViewRow
        {
            public TEntity Entity { get; private set; }

            private DataListViewRowBinding<TEntity> Binding { get; }

            internal DataListViewRow(DataListView parent, TEntity entity, DataListViewRowBinding<TEntity> binding)
                : base(parent)
            {
                Entity = entity;
                Binding = binding;
                RefreshValues();
            }

            public DataListViewRow<TEntity> RefreshValues()
            {
                var cellValues = Binding.ExtractValues(Entity).ToArray();
                SetCellValues(cellValues);
                return this;
            }
        }

        #endregion

        #region Nested class: DataListViewCell

        public class DataListViewCell : ListViewItem.ListViewSubItem
        {
            public DataListViewRow Parent { get; }

            private object _value;
            public object Value
            {
                get => _value;
                set
                {
                    if (_value == value) return;
                    _value = value;
                    Text = value?.ToString();
                    ValueChanged?.Invoke(this, value);
                }
            }

            public event EventHandler<object> ValueChanged;

            public DataListViewCell(DataListViewRow parent)
            {
                Parent = parent;
            }

            public DataListViewCell(DataListViewRow parent, object value)
                : this(parent)
            {
                Value = value;
            }
        }

        #endregion

        #region Nested class: DataListViewRowBinding

        internal class DataListViewRowBinding
        {
            public Type EntityType { get; }

            public DataListViewRowBinding(Type entityType)
            {
                EntityType = entityType;
            }
        }

        internal class DataListViewRowBinding<TEntity> : DataListViewRowBinding, IDataListViewRowBinding<TEntity>
        {
            private readonly Dictionary<int, Func<TEntity, object>> Extractors = new();

            public DataListViewRowBinding() : base(typeof(TEntity)) { }

            public void AddCellBinding(int cellIndex, Func<TEntity, object> extractor)
            {
                if (cellIndex < 0)
                {
                    throw new ArgumentException($"{GetType()}.{nameof(AddCellBinding)} must have a cellIndex >= 0");
                }

                if (Extractors.ContainsKey(cellIndex))
                {
                    throw new ArgumentException($"{GetType()} already has a cell binding for column {cellIndex}");
                }

                Extractors.Add(cellIndex, extractor);
            }

            public IEnumerable<object> ExtractValues(TEntity entity)
            {
                return ExtractValues(entity, Extractors.Keys.Max());
            }

            public IEnumerable<object> ExtractValues(TEntity entity, int numValues)
            {
                for (var i = 0; i <= numValues; i++)
                {
                    object value = null;

                    if (Extractors.TryGetValue(i, out var extractor))
                    {
                        try
                        {
                            value = extractor(entity);
                        }
                        catch
                        {
                            // Since this is a UI element, suppress all errors that might
                            // occur in custom extraction code
                        }
                    }

                    yield return value;
                }
            }
        }

        public interface IDataListViewRowBinding<TEntity>
        {
            void AddCellBinding(int cellIndex, Func<TEntity, object> extractor);
        }

        #endregion
    }
}
