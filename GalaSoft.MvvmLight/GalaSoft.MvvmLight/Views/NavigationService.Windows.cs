// ****************************************************************************
// <copyright file="NavigationService.Windows.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>02.10.2014</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

#if NETFX_CORE
namespace GalaSoft.MvvmLight.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Windows 10 UWP implementation of <see cref="INavigationService"/>.
    /// </summary>
    ////[ClassInfo(typeof(INavigationService))]
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// The key that is returned by the <see cref="CurrentPageKey"/> property
        /// when the current Page is the root page.
        /// </summary>
        public const string RootPageKey = "-- ROOT --";

        /// <summary>
        /// The key that is returned by the <see cref="CurrentPageKey"/> property
        /// when the current Page is not found.
        /// This can be the case when the navigation wasn't managed by this NavigationService,
        /// for example when it is directly triggered in the code behind, and the
        /// NavigationService was not configured for this page type.
        /// </summary>
        public const string UnknownPageKey = "-- UNKNOWN --";

        private readonly Dictionary<string, Type> pagesByKey = new Dictionary<string, Type>();

        private Frame currentFrame;

        /// <summary>
        /// Gets or sets the Frame that should be use for the navigation.
        /// If this is not set explicitly, then (Frame)Window.Current.Content is used.
        /// </summary>
        public Frame CurrentFrame
        {
            get => this.currentFrame ?? (this.currentFrame = (Frame)Window.Current.Content);
            set => this.currentFrame = value;
        }

        /// <summary>
        /// Gets a flag indicating if the CurrentFrame can navigate backwards.
        /// </summary>
        public bool CanGoBack => this.CurrentFrame.CanGoBack;

        /// <summary>
        /// Gets a flag indicating if the CurrentFrame can navigate forward.
        /// </summary>
        public bool CanGoForward => this.CurrentFrame.CanGoForward;

        /// <summary>
        /// Check if the CurrentFrame can navigate forward, and if yes, performs
        /// a forward navigation.
        /// </summary>
        public void GoForward()
        {
            if (this.CurrentFrame.CanGoForward)
            {
                this.CurrentFrame.GoForward();
            }
        }

        /// <summary>
        /// The key corresponding to the currently displayed page.
        /// </summary>
        public string CurrentPageKey
        {
            get
            {
                lock (this.pagesByKey)
                {
                    if (this.CurrentFrame.BackStackDepth == 0)
                    {
                        return RootPageKey;
                    }

                    if (this.CurrentFrame.Content == null)
                    {
                        return UnknownPageKey;
                    }

                    Type currentType = this.CurrentFrame.Content.GetType();

                    if (this.pagesByKey.All(p => p.Value != currentType))
                    {
                        return UnknownPageKey;
                    }

                    KeyValuePair<string, Type> item = this.pagesByKey.FirstOrDefault(
                        i => i.Value == currentType);

                    return item.Key;
                }
            }
        }

        /// <summary>
        /// If possible, discards the current page and displays the previous page
        /// on the navigation stack.
        /// </summary>
        public void GoBack()
        {
            if (this.CurrentFrame.CanGoBack)
            {
                this.CurrentFrame.GoBack();
            }
        }

        /// <summary>
        /// Displays a new page corresponding to the given key. 
        /// Make sure to call the <see cref="Configure"/>
        /// method first.
        /// </summary>
        /// <param name="pageKey">The key corresponding to the page
        /// that should be displayed.</param>
        /// <exception cref="ArgumentException">When this method is called for 
        /// a key that has not been configured earlier.</exception>
        public void NavigateTo(string pageKey)
        {
            this.NavigateTo(pageKey, null);
        }

        /// <summary>
        /// Displays a new page corresponding to the given key,
        /// and passes a parameter to the new page.
        /// Make sure to call the <see cref="Configure"/>
        /// method first.
        /// </summary>
        /// <param name="pageKey">The key corresponding to the page
        /// that should be displayed.</param>
        /// <param name="parameter">The parameter that should be passed
        /// to the new page.</param>
        /// <exception cref="ArgumentException">When this method is called for 
        /// a key that has not been configured earlier.</exception>
        public virtual void NavigateTo(string pageKey, object parameter)
        {
            lock (this.pagesByKey)
            {
                if (!this.pagesByKey.ContainsKey(pageKey))
                {
                    throw new ArgumentException(
                        string.Format(
                            "No such page: {0}. Did you forget to call NavigationService.Configure?",
                            pageKey),
                        "pageKey");
                }

                this.CurrentFrame.Navigate(this.pagesByKey[pageKey], parameter);
            }
        }

        /// <summary>
        /// Adds a key/page pair to the navigation service.
        /// </summary>
        /// <param name="key">The key that will be used later
        /// in the <see cref="NavigateTo(string)"/> or <see cref="NavigateTo(string, object)"/> methods.</param>
        /// <param name="pageType">The type of the page corresponding to the key.</param>
        public void Configure(string key, Type pageType)
        {
            lock (this.pagesByKey)
            {
                if (this.pagesByKey.ContainsKey(key))
                {
                    throw new ArgumentException("This key is already used: " + key);
                }

                if (this.pagesByKey.Any(p => p.Value == pageType))
                {
                    throw new ArgumentException(
                        "This type is already configured with key " + this.pagesByKey.First(p => p.Value == pageType).Key);
                }

                this.pagesByKey.Add(
                    key,
                    pageType);
            }
        }

        /// <summary>
        /// Gets the key corresponding to a given page type.
        /// </summary>
        /// <param name="page">The type of the page for which the key must be returned.</param>
        /// <returns>The key corresponding to the page type.</returns>
        public string GetKeyForPage(Type page)
        {
            lock (this.pagesByKey)
            {
                if (this.pagesByKey.ContainsValue(page))
                {
                    return this.pagesByKey.FirstOrDefault(p => p.Value == page).Key;
                }
                else
                {
                    throw new ArgumentException($"The page '{page.Name}' is unknown by the NavigationService");
                }
            }
        }
    }
}
#endif