﻿using System;
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
        public ListView.ColumnHeaderCollection Columns => this.ListView.Columns;

        public override Color ForeColor
        {
            get => this.ListView.ForeColor;
            set => this.ListView.ForeColor = value;
        }

        public ImageList Icons
        {
            get => this.ListView.SmallImageList;
            set => this.ListView.SmallImageList = value;
        }

        #endregion

        #region Data fields

        private readonly List<DataListViewRow> _rows = new();
        public IReadOnlyList<DataListViewRow> Rows => _rows;

        private readonly Dictionary<Type, DataListViewRowBinding> RowBindings = new();

        #endregion

        public DataListView()
        {
            InitializeComponent();
        }

        public DataListView AddRowBinding<TEntity>(Action<IDataListViewRowBinding<TEntity>> builder)
        {
            if (this.RowBindings.ContainsKey(typeof(TEntity)))
            {
                throw new ArgumentException($"{nameof(DataListView)} already has a binding for type {typeof(TEntity)}");
            }

            var binding = new DataListViewRowBinding<TEntity>();
            builder?.Invoke(binding);
            this.RowBindings.Add(typeof(TEntity), binding);

            return this;
        }

        public DataListViewRow AddRowFromValues(params object[] values)
        {
            var item = new DataListViewRow(this, values);

            this.ListView.Items.Add(item);
            this._rows.Add(item);

            return item;
        }

        public DataListViewRow<TEntity> AddRowFromEntity<TEntity>(TEntity entity)
        {
            if (!this.RowBindings.TryGetValue(typeof(TEntity), out var binding))
            {
                throw new ArgumentException($"{nameof(DataListView)} does not have a binding for type {typeof(TEntity)}");
            }

            var typedBinding = binding as DataListViewRowBinding<TEntity>;
            var item = new DataListViewRow<TEntity>(this, entity, typedBinding);

            this.ListView.Items.Add(item);
            this._rows.Add(item);

            return item;
        }

        public IEnumerable<DataListViewRow<TEntity>> GetRowsWithType<TEntity>()
        {
            return this.Rows
                .Select(row => row as DataListViewRow<TEntity>)
                .Where(row => row != null);
        }

        #region Nested class: DataListViewRow

        public class DataListViewRow : ListViewItem
        {
            public DataListView Parent { get; }

            private readonly List<DataListViewCell> _cells = new();
            public IReadOnlyList<DataListViewCell> Cells => _cells;

            public DataListViewRow(DataListView parent)
            {
                this.Parent = parent;
            }

            public DataListViewRow(DataListView parent, int numColumns)
                : this(parent)
            {
                this.FillCellsToIndex(numColumns - 1);
            }

            public DataListViewRow(DataListView parent, params object[] values)
                : this(parent)
            {
                this.SetCellValues(values);
            }

            public DataListViewRow SetCellValues(params object[] values)
            {
                if (values == null || values.Length == 0)
                {
                    // No values have been provided, clear out all existing values
                    foreach (var cell in this.Cells)
                    {
                        cell.Value = null;
                    }
                }
                else
                {
                    var maxIndex = Math.Max(this.Cells.Count, values.Length) - 1;

                    for (var i = 0; i <= maxIndex; i++)
                    {
                        if (i >= values.Length)
                        {
                            // A cell exists, but no value has been provided for it,
                            // so clear out the existing value
                            this.SetCellValue(i, null);
                        }
                        else
                        {
                            this.SetCellValue(i, values[i]);
                        }
                    }
                }

                return this;
            }

            public DataListViewRow SetCellValue(int cellIndex, object value)
            {
                var cell = this.GetCell(cellIndex);
                cell.Value = value;

                // ListView compatibility: Item [0] is actually just the row's Text
                if (cellIndex == 0)
                {
                    this.Text = cell.Text;
                }

                return this;
            }

            public DataListViewCell GetCell(int cellIndex)
            {
                this.FillCellsToIndex(cellIndex);

                return this.Cells[cellIndex];
            }

            #region Non-public methods

            private void FillCellsToIndex(int cellIndex)
            {
                if (cellIndex >= this.Cells.Count)
                {
                    for (var i = this.Cells.Count; i <= cellIndex; i++)
                    {
                        this.AddCell();
                    }
                }
            }

            private DataListViewCell AddCell()
            {
                var cell = new DataListViewCell(this);

                // ListView compatibility: Item [0] is simply the row's Text, but subsequent
                // items must be added as SubItems
                if (this.Cells.Count > 0)
                {
                    this.SubItems.Add(cell);
                }

                this._cells.Add(cell);

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
                this.Entity = entity;
                this.Binding = binding;
                this.RefreshValues();
            }

            public DataListViewRow<TEntity> RefreshValues()
            {
                var cellValues = this.Binding.ExtractValues(this.Entity).ToArray();
                this.SetCellValues(cellValues);
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
                get => this._value;
                set
                {
                    if (this._value == value) return;
                    this._value = value;
                    this.Text = value?.ToString();
                    this.ValueChanged?.Invoke(this, value);
                }
            }

            public event EventHandler<object> ValueChanged;

            public DataListViewCell(DataListViewRow parent)
            {
                this.Parent = parent;
            }

            public DataListViewCell(DataListViewRow parent, object value)
                : this(parent)
            {
                this.Value = value;
            }
        }

        #endregion

        #region Nested class: DataListViewRowBinding

        internal class DataListViewRowBinding
        {
            public Type EntityType { get; }

            public DataListViewRowBinding(Type entityType)
            {
                this.EntityType = entityType;
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
                    throw new ArgumentException($"{this.GetType()}.{nameof(AddCellBinding)} must have a cellIndex >= 0");
                }

                if (this.Extractors.ContainsKey(cellIndex))
                {
                    throw new ArgumentException($"{this.GetType()} already has a cell binding for column {cellIndex}");
                }

                this.Extractors.Add(cellIndex, extractor);
            }

            public IEnumerable<object> ExtractValues(TEntity entity)
            {
                return this.ExtractValues(entity, this.Extractors.Keys.Max());
            }

            public IEnumerable<object> ExtractValues(TEntity entity, int numValues)
            {
                for (var i = 0; i <= numValues; i++)
                {
                    object value = null;

                    if (this.Extractors.TryGetValue(i, out var extractor))
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
