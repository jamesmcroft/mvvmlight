// ****************************************************************************
// <copyright file="ObservableTableViewSource.iOS.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>17.04.2016</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

#if __IOS__
namespace GalaSoft.MvvmLight.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using Foundation;
    using UIKit;

    /// <summary>
    /// A <see cref="UITableViewSource"/> that automatically updates the associated <see cref="UITableView"/> when its 
    /// data source changes. Note that the changes are only observed if the data source 
    /// implements <see cref="INotifyCollectionChanged"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the items in the data source.</typeparam>

    ////[ClassInfo(typeof(ObservableTableViewController<T>)]
    public class ObservableTableViewSource<TItem> : UITableViewSource, INotifyPropertyChanged
    {
        /// <summary>
        /// The <see cref="SelectedItem" /> property's name.
        /// </summary>
        public const string SelectedItemPropertyName = "SelectedItem";

        private readonly NSString defaultReuseId = new NSString("C");

        private readonly Thread mainThread;

        private IList<TItem> dataSource;

        private INotifyCollectionChanged notifier;

        private NSString reuseId;

        private TItem selectedItem;

        private UITableView view;

        /// <summary>
        /// When set, specifies which animation should be used when rows are added.
        /// </summary>
        public UITableViewRowAnimation AddAnimation
        {
            get;
            set;
        }

        /// <summary>
        /// A delegate to a method taking a <see cref="UITableViewCell"/>
        /// and setting its elements' properties according to the item
        /// passed as second parameter.
        /// </summary>
        public Action<UITableViewCell, TItem, NSIndexPath> BindCellDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// A delegate to a method creating or reusing a <see cref="UITableViewCell"/>.
        /// The cell will then be passed to the <see cref="BindCellDelegate"/>
        /// delegate to set the elements' properties. Note that this delegate is only
        /// used if you didn't register with a ReuseID using the UITableView.RegisterClassForCell method.
        /// </summary>
        public Func<NSString, UITableViewCell> CreateCellDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// The data source of this list controller.
        /// </summary>
        public IList<TItem> DataSource
        {
            get => this.dataSource;
            set
            {
                if (Equals(this.dataSource, value))
                {
                    return;
                }

                if (this.notifier != null)
                {
                    this.notifier.CollectionChanged -= this.HandleCollectionChanged;
                }

                this.dataSource = value;
                this.notifier = value as INotifyCollectionChanged;

                if (this.notifier != null)
                {
                    this.notifier.CollectionChanged += this.HandleCollectionChanged;
                }

                if (this.view != null)
                {
                    this.view.ReloadData();
                }
            }
        }

        /// <summary>
        /// When set, specifieds which animation should be used when a row is deleted.
        /// </summary>
        public UITableViewRowAnimation DeleteAnimation
        {
            get;
            set;
        }

        /// <summary>
        /// When set, returns the height of the view that will be used for the TableView's footer.
        /// </summary>
        /// <seealso cref="GetViewForFooterDelegate"/>
        public Func<nfloat> GetHeightForFooterDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// When set, returns the height of the view that will be used for the TableView's header.
        /// </summary>
        /// <seealso cref="GetViewForHeaderDelegate"/>
        public Func<nfloat> GetHeightForHeaderDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// When set, returns a view that can be used as the TableView's footer.
        /// </summary>
        /// <seealso cref="GetHeightForFooterDelegate"/>
        public Func<UIView> GetViewForFooterDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// When set, returns a view that can be used as the TableView's header.
        /// </summary>
        /// <seealso cref="GetHeightForHeaderDelegate"/>
        public Func<UIView> GetViewForHeaderDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// A reuse identifier for the TableView's cells.
        /// </summary>
        public string ReuseId
        {
            get => this.NsReuseId.ToString();

            set => this.reuseId = string.IsNullOrEmpty(value) ? null : new NSString(value);
        }

        /// <summary>
        /// Gets the UITableView's selected item. You can use one-way databinding on this property.
        /// </summary>
        public TItem SelectedItem
        {
            get => this.selectedItem;

            protected set
            {
                if (Equals(this.selectedItem, value))
                {
                    return;
                }

                this.selectedItem = value;
                this.RaisePropertyChanged(SelectedItemPropertyName);
                this.RaiseSelectionChanged();
            }
        }

        private NSString NsReuseId => this.reuseId ?? this.defaultReuseId;

        /// <summary>
        /// Constructs and initializes an instance of <see cref="ObservableTableViewSource{TItem}"/>
        /// </summary>
        public ObservableTableViewSource()
        {
            this.mainThread = Thread.CurrentThread;
            this.AddAnimation = UITableViewRowAnimation.Automatic;
            this.DeleteAnimation = UITableViewRowAnimation.Automatic;
        }

        /// <summary>
        /// Creates and returns a cell for the UITableView. Where needed, this method will
        /// optimize the reuse of cells for a better performance.
        /// </summary>
        /// <param name="view">The UITableView associated to this source.</param>
        /// <param name="indexPath">The NSIndexPath pointing to the item for which the cell must be returned.</param>
        /// <returns>The created and initialised <see cref="UITableViewCell"/>.</returns>
        public override UITableViewCell GetCell(UITableView view, NSIndexPath indexPath)
        {
            if (this.view == null)
            {
                this.view = view;
            }

            UITableViewCell cell;

            if (this.CreateCellDelegate != null)
            {
                cell = this.CreateCell(this.NsReuseId);
            }
            else
            {
                cell = view.DequeueReusableCell(this.NsReuseId) ?? this.CreateCell(this.NsReuseId);
            }

            try
            {
                IList<TItem> coll = this.dataSource;

                if (coll != null)
                {
                    TItem item = coll[indexPath.Row];
                    this.BindCell(cell, item, indexPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return cell;
        }

        /// <summary>
        /// When called, checks if the <see cref="GetHeightForFooterDelegate"/>has been set. 
        /// If yes, calls that delegate to get the TableView's footer height.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="section">The section index.</param>
        /// <returns>The footer's height.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override nfloat GetHeightForFooter(UITableView tableView, nint section)
        {
            return this.GetHeightForFooterDelegate?.Invoke() ?? 0;
        }

        /// <summary>
        /// When called, checks if the <see cref="GetHeightForHeaderDelegate"/>
        /// delegate has been set. If yes, calls that delegate to get the TableView's header height.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="section">The section index.</param>
        /// <returns>The header's height.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            return this.GetHeightForHeaderDelegate?.Invoke() ?? 0;
        }

        /// <summary>
        /// Gets the item selected by the NSIndexPath passed as parameter.
        /// </summary>
        /// <param name="indexPath">The NSIndexPath pointing to the desired item.</param>
        /// <returns>The item selected by the NSIndexPath passed as parameter.</returns>
        public TItem GetItem(NSIndexPath indexPath)
        {
            return this.dataSource[indexPath.Row];
        }

        /// <summary>
        /// When called, checks if the <see cref="GetViewForFooterDelegate"/>
        /// delegate has been set. If yes, calls that delegate to get the TableView's footer.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="section">The section index.</param>
        /// <returns>The UIView that should appear as the section's footer.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override UIView GetViewForFooter(UITableView tableView, nint section)
        {
            return this.GetViewForFooterDelegate != null ? this.GetViewForFooterDelegate() : base.GetViewForFooter(tableView, section);
        }

        /// <summary>
        /// When called, checks if the <see cref="GetViewForHeaderDelegate"/>
        /// delegate has been set. If yes, calls that delegate to get the TableView's header.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="section">The section index.</param>
        /// <returns>The UIView that should appear as the section's header.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            return this.GetViewForHeaderDelegate != null ? this.GetViewForHeaderDelegate() : base.GetViewForHeader(tableView, section);
        }

        /// <summary>
        /// Overrides the <see cref="UITableViewSource.NumberOfSections"/> method.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <returns>The number of sections of the UITableView.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        /// <summary>
        /// Overrides the <see cref="UITableViewSource.RowDeselected"/> method. When called, sets the
        /// <see cref="SelectedItem"/> property to null and raises the PropertyChanged and the SelectionChanged events.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="indexPath">The row's NSIndexPath.</param>
        public override void RowDeselected(UITableView tableView, NSIndexPath indexPath)
        {
            this.SelectedItem = default(TItem);
        }

        /// <summary>
        /// Overrides the <see cref="UITableViewSource.RowSelected"/> method. When called, sets the
        /// <see cref="SelectedItem"/> property and raises the PropertyChanged and the SelectionChanged events.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="indexPath">The row's NSIndexPath.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            TItem item = this.dataSource != null ? this.dataSource[indexPath.Row] : default(TItem);
            this.SelectedItem = item;
        }

        /// <summary>
        /// Overrides the <see cref="UITableViewSource.RowsInSection"/> method
        /// and returns the number of rows in the associated data source.
        /// </summary>
        /// <param name="tableView">The active TableView.</param>
        /// <param name="section">The active section.</param>
        /// <returns>The number of rows in the data source.</returns>
        /// <remarks>In the current implementation, only one section is supported.</remarks>
        public override nint RowsInSection(UITableView tableView, nint section)
        {
            if (this.view == null)
            {
                this.view = tableView;
            }

            return this.dataSource?.Count ?? 0;
        }

        /// <summary>
        /// Binds a <see cref="UITableViewCell"/> to an item's properties.
        /// If a <see cref="BindCellDelegate"/> is available, this delegate will be used.
        /// If not, a simple text will be shown.
        /// </summary>
        /// <param name="cell">The cell that will be prepared.</param>
        /// <param name="item">The item that should be used to set the cell up.</param>
        /// <param name="indexPath">The <see cref="NSIndexPath"/> for this cell.</param>
        protected virtual void BindCell(UITableViewCell cell, object item, NSIndexPath indexPath)
        {
            if (this.BindCellDelegate == null)
            {
                cell.TextLabel.Text = item.ToString();
            }
            else
            {
                this.BindCellDelegate(cell, (TItem)item, indexPath);
            }
        }

        /// <summary>
        /// Creates a <see cref="UITableViewCell"/> corresponding to the reuseId.
        /// If it is set, the <see cref="CreateCellDelegate"/> delegate will be used.
        /// </summary>
        /// <param name="reuseId">A reuse identifier for the cell.</param>
        /// <returns>The created cell.</returns>
        protected virtual UITableViewCell CreateCell(NSString reuseId)
        {
            return this.CreateCellDelegate == null ? new UITableViewCell(UITableViewCellStyle.Default, reuseId) : this.CreateCellDelegate(reuseId);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.view == null)
            {
                return;
            }

            Action act = () =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        int count = e.NewItems.Count;
                        var paths = new NSIndexPath[count];

                        for (int i = 0; i < count; i++)
                        {
                            paths[i] = NSIndexPath.FromRowSection(e.NewStartingIndex + i, 0);
                        }

                        this.view.InsertRows(paths, this.AddAnimation);
                    }

                        break;

                    case NotifyCollectionChangedAction.Remove:
                    {
                        int count = e.OldItems.Count;
                        var paths = new NSIndexPath[count];

                        for (int i = 0; i < count; i++)
                        {
                            NSIndexPath index = NSIndexPath.FromRowSection(e.OldStartingIndex + i, 0);
                            paths[i] = index;

                            object item = e.OldItems[i];

                            if (Equals(this.SelectedItem, item))
                            {
                                this.SelectedItem = default(TItem);
                            }
                        }

                        this.view.DeleteRows(paths, this.DeleteAnimation);
                    }

                        break;

                    default:
                        this.view.ReloadData();
                        break;
                }
            };

            bool isMainThread = Thread.CurrentThread == this.mainThread;

            if (isMainThread)
            {
                act();
            }
            else
            {
                NSOperationQueue.MainQueue.AddOperation(act);
                NSOperationQueue.MainQueue.WaitUntilAllOperationsAreFinished();
            }
        }

        private void RaiseSelectionChanged()
        {
            EventHandler handler = this.SelectionChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when a property of this instance changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when a new item gets selected in the list.
        /// </summary>
        public event EventHandler SelectionChanged;
    }
}
#endif