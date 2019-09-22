// ****************************************************************************
// <copyright file="ActivityBase.Android.cs" company="GalaSoft Laurent Bugnion">
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

#if __ANDROID__
namespace GalaSoft.MvvmLight.Views
{
    using Android.App;

    /// <summary>
    /// A base class for Activities that allow the <see cref="NavigationService"/>
    /// to keep track of the navigation journal.
    /// </summary>
    ////[ClassInfo(typeof(INavigationService))]
    public class ActivityBase : Activity
    {
        /// <summary>
        /// The activity that is currently in the foreground.
        /// </summary>
        public static ActivityBase CurrentActivity
        {
            get;
            private set;
        }

        internal string ActivityKey
        {
            get;
            private set;
        }

        internal static string NextPageKey
        {
            get;
            set;
        }

        /// <summary>
        /// If possible, discards the current page and displays the previous page
        /// on the navigation stack.
        /// </summary>
        public static void GoBack()
        {
            if (CurrentActivity != null)
            {
                CurrentActivity.OnBackPressed();
            }
        }

        /// <summary>
        /// Overrides <see cref="Activity.OnResume"/>. If you override
        /// this method in your own Activities, make sure to call
        /// base.OnResume to allow the <see cref="NavigationService"/>
        /// to work properly.
        /// </summary>
        protected override void OnResume()
        {
            CurrentActivity = this;

            if (string.IsNullOrEmpty(this.ActivityKey))
            {
                this.ActivityKey = NextPageKey;
                NextPageKey = null;
            }

            base.OnResume();
        }
    }
}
#endif