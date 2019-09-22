// ****************************************************************************
// <copyright file="CachingViewHolder.Android.cs" company="GalaSoft Laurent Bugnion">
// Copyright � GalaSoft Laurent Bugnion 2009-2016
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

#if __ANDROID__
namespace GalaSoft.MvvmLight.Helpers
{
    using System;
    using System.Collections.Generic;

    using Android.Runtime;
    using Android.Support.V7.Widget;
    using Android.Views;

    /// <summary>
    /// An extension of <see cref="RecyclerView.ViewHolder"/> optimized for usage with the
    /// <see cref="ObservableRecyclerAdapter{TItem, THolder}"/>. Provides additional features
    /// that can be used with the MVVM Light data binding system.
    /// </summary>
    ////[ClassInfo(typeof(ObservableAdapter<T>))]
    public class CachingViewHolder : RecyclerView.ViewHolder
    {
        private readonly Dictionary<object, Binding> bindings = new Dictionary<object, Binding>();
        private readonly Dictionary<int, View> cachedViews = new Dictionary<int, View>();
        private Action<int, View> clickCallback;

        /// <summary>
        /// A callback that will be executed when the corresponding item is clicked or tapped 
        /// by the user.
        /// </summary>
        public Action<int, View> ClickCallback
        {
            get => this.clickCallback;
            set
            {
                if (value == null)
                {
                    this.ItemView.Click -= this.OnViewClick;
                }
                else
                {
                    this.ItemView.Click += this.OnViewClick;
                }

                this.clickCallback = value;
            }
        }

        /// <summary>
        /// Initializes an instance of this class. In most cases this method
        /// is not used by the developer.
        /// </summary>
        /// <param name="javaReference"></param>
        /// <param name="transfer"></param>
        public CachingViewHolder(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        /// <param name="itemView">The view that this holder is attached to.</param>
        public CachingViewHolder(View itemView)
            : base(itemView)
        {
        }

        /// <summary>
        /// Detaches and removes a binding from this holder.
        /// Use this method (in case a view is getting recycled) to clean up
        /// existing bindings before attaching new ones. The binding that needs
        /// to be detached must have been added to the holder using the <see cref="SaveBinding"/> method.
        /// </summary>
        /// <param name="key">The key corresponding to the binding to detach and delete. This is the
        /// same key that was used in the <see cref="SaveBinding"/> method.</param>
        /// <returns>The binding that has been detached and deleted, in case further processing
        /// is necessary/</returns>
        public Binding DeleteBinding(object key)
        {
            lock (this.bindings)
            {
                if (this.bindings.ContainsKey(key))
                {
                    Binding binding = this.bindings[key];
                    binding.Detach();
                    this.bindings.Remove(key);
                    return binding;
                }

                return null;
            }
        }

        /// <summary>
        /// Explores the attached view and returns the UI element corresponding
        /// to the viewId. If no element is found with this ID, the method returns null.
        /// </summary>
        /// <typeparam name="TView">The type of the view to be returned.</typeparam>
        /// <param name="viewId">The ID of the subview that needs to be retrieved.</param>
        /// <returns>The sub view corresponding to the viewId, or null if no corresponding sub view is found.</returns>
        public TView FindCachedViewById<TView>(int viewId)
            where TView : View
        {
            if (this.cachedViews.ContainsKey(viewId))
            {
                return (TView)this.cachedViews[viewId];
            }

            TView view = this.ItemView.FindViewById<TView>(viewId);
            this.cachedViews.Add(viewId, view);
            return view;
        }

        /// <summary>
        /// Saves a binding between a element of the view and an element of the data item
        /// represented by the view.
        /// If the view is getting recycled, the binding should be deleted using the 
        /// <see cref="DeleteBinding"/> method.
        /// </summary>
        /// <param name="key">The key used to retriev the binding later. Typically
        /// the key is the view to which the binding is attached (if there is only
        /// one binding for this view).</param>
        /// <param name="binding">The binding to be saved.</param>
        public void SaveBinding(object key, Binding binding)
        {
            lock (this.bindings)
            {
                if (this.bindings.ContainsKey(key))
                {
                    this.bindings[key].Detach();
                    this.bindings[key] = binding;
                }
                else
                {
                    this.bindings.Add(key, binding);
                }
            }
        }

        private void OnViewClick(object sender, EventArgs e)
        {
            int position = this.AdapterPosition;

            if (position != RecyclerView.NoPosition)
            {
                this.clickCallback?.Invoke(position, this.ItemView);
            }
        }
    }
}
#endif