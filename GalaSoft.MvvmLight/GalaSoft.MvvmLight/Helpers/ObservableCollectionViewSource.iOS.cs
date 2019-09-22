// ****************************************************************************
// <copyright file="ObservableCollectionViewSource.iOS.cs" company="GalaSoft Laurent Bugnion">
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
    /// A <see cref="UICollectionViewSource"/> that automatically updates the associated <see cref="UICollectionView"/> when its 
    /// data source changes. Note that the changes are only observed if the data source 
    /// implements <see cref="INotifyCollectionChanged"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the items in the data source.</typeparam>
    /// <typeparam name="TCell">The type of the <see cref="UICollectionViewCell"/> used in the CollectionView.
    /// This can either be UICollectionViewCell or a derived type.</typeparam>
    ////[ClassInfo(typeof(ObservableTableViewController<T>)]
    public class ObservableCollectionViewSource<TItem, TCell> : UICollectionViewSource, INotifyPropertyChanged
        where TCell : UICollectionViewCell
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

        private UICollectionView view;

        /// <summary>
        /// A delegate to a method taking a <see cref="UICollectionViewCell"/>
        /// and setting its elements' properties according to the item
        /// passed as second parameter.
        /// </summary>
        public Action<TCell, TItem, NSIndexPath> BindCellDelegate
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

                this.view?.ReloadData();
            }
        }

        /// <summary>
        /// A delegate to a method returning a <see cref="UICollectionReusableView"/>
        /// and used to set supplementary views on the UICollectionView.
        /// </summary>
        public Func<NSString, NSIndexPath, UICollectionReusableView> GetSupplementaryViewDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// A reuse identifier for the UICollectionView's cells.
        /// </summary>
        public string ReuseId
        {
            get => this.NsReuseId.ToString();
            set => this.reuseId = string.IsNullOrEmpty(value) ? null : new NSString(value);
        }

        /// <summary>
        /// Gets the UICollectionView's selected item. You can use one-way databinding on this property.
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
        /// Creates and initializes a new instance of <see cref="ObservableCollectionViewSource{TItem, TCell}"/>
        /// </summary>
        public ObservableCollectionViewSource()
        {
            this.mainThread = Thread.CurrentThread;
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.GetCell"/> method.
        /// Creates and returns a cell for the UICollectionView. Where needed, this method will
        /// optimize the reuse of cells for a better performance.
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <param name="indexPath">The NSIndexPath pointing to the item for which the cell must be returned.</param>
        /// <returns>The created and initialised <see cref="UICollectionViewCell"/>.</returns>
        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = (TCell)collectionView.DequeueReusableCell(this.NsReuseId, indexPath);

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
        /// Gets the item selected by the NSIndexPath passed as parameter.
        /// </summary>
        /// <param name="indexPath">The NSIndexPath pointing to the desired item.</param>
        /// <returns>The item selected by the NSIndexPath passed as parameter.</returns>
        public TItem GetItem(NSIndexPath indexPath)
        {
            return this.dataSource[indexPath.Row];
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.GetItemsCount"/> method.
        /// Gets the number of items in the data source.
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <param name="section">The section for which the count is needed. In the current
        /// implementation, only one section is supported.</param>
        /// <returns>The number of items in the data source.</returns>
        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            this.SetView(collectionView);
            return this.dataSource.Count;
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.GetViewForSupplementaryElement"/> method.
        /// When called, checks if the <see cref="GetSupplementaryViewDelegate"/>
        /// delegate has been set. If yes, calls that delegate to get a supplementary view for the UICollectionView.
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <param name="elementKind">The kind of supplementary element.</param>
        /// <param name="indexPath">The NSIndexPath pointing to the element.</param>
        /// <returns>A supplementary view for the UICollectionView.</returns>
        public override UICollectionReusableView GetViewForSupplementaryElement(
            UICollectionView collectionView,
            NSString elementKind,
            NSIndexPath indexPath)
        {
            if (this.GetSupplementaryViewDelegate == null)
            {
                throw new InvalidOperationException(
                    "GetViewForSupplementaryElement was called but no GetSupplementaryViewDelegate was found");
            }

            UICollectionReusableView view = this.GetSupplementaryViewDelegate(elementKind, indexPath);
            return view;
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.ItemDeselected"/> method.
        /// Called when an item is deselected in the UICollectionView.
        /// <remark>If you subclass ObservableCollectionViewSource, you may override this method
        /// but you may NOT call base.ItemDeselected(...) in your overriden method, as this causes an exception
        /// in iOS. Because of this, you must take care of resetting the <see cref="SelectedItem"/> property 
        /// yourself by calling SelectedItem = default(TItem);</remark>
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <param name="indexPath">The NSIndexPath pointing to the element.</param>
        public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            this.SelectedItem = default(TItem);
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.ItemSelected"/> method.
        /// Called when an item is selected in the UICollectionView.
        /// <remark>If you subclass ObservableCollectionViewSource, you may override this method
        /// but you may NOT call base.ItemSelected(...) in your overriden method, as this causes an exception
        /// in iOS. Because of this, you must take care of setting the <see cref="SelectedItem"/> property 
        /// yourself by calling var item = GetItem(indexPath); SelectedItem = item;</remark>
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <param name="indexPath">The NSIndexPath pointing to the element.</param>
        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            TItem item = this.dataSource[indexPath.Row];
            this.SelectedItem = item;
        }

        /// <summary>
        /// Overrides the <see cref="UICollectionViewSource.NumberOfSections"/> method.
        /// The number of sections in this UICollectionView. In the current implementation,
        /// only one section is supported.
        /// </summary>
        /// <param name="collectionView">The UICollectionView associated to this source.</param>
        /// <returns></returns>
        public override nint NumberOfSections(UICollectionView collectionView)
        {
            this.SetView(collectionView);
            return 1;
        }

        /// <summary>
        /// Sets a <see cref="UICollectionViewCell"/>'s elements according to an item's properties.
        /// If a <see cref="BindCellDelegate"/> is available, this delegate will be used.
        /// If not, a simple text will be shown.
        /// </summary>
        /// <param name="cell">The cell that will be prepared.</param>
        /// <param name="item">The item that should be used to set the cell up.</param>
        /// <param name="indexPath">The <see cref="NSIndexPath"/> for this cell.</param>
        protected virtual void BindCell(UICollectionViewCell cell, object item, NSIndexPath indexPath)
        {
            if (this.BindCellDelegate == null)
            {
                throw new InvalidOperationException("BindCell was called but no BindCellDelegate was found");
            }

            this.BindCellDelegate((TCell)cell, (TItem)item, indexPath);
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

                        this.view.InsertItems(paths);
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

                        this.view.DeleteItems(paths);
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

        private void SetView(UICollectionView collectionView)
        {
            if (this.view != null)
            {
                return;
            }

            this.view = collectionView;
            this.view.RegisterClassForCell(typeof(TCell), this.NsReuseId);
        }

        /// <summary>
        /// Occurs when a property of this instance changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when a new item gets selected in the UICollectionView.
        /// </summary>
        public event EventHandler SelectionChanged;
    }
}
#endif