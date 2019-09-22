// ****************************************************************************
// <copyright file="DialogService.iOS.cs" company="GalaSoft Laurent Bugnion">
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

#if __IOS__
namespace GalaSoft.MvvmLight.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using UIKit;

    /// <summary>
    /// An implementation of <see cref="IDialogService"/> allowing
    /// to display simple dialogs to the user. Note that this class
    /// uses the built in UIAlertController which may or may not
    /// be sufficient for your needs. Using this class is easy
    /// but feel free to develop your own IDialogService implementation
    /// if needed.
    /// </summary>

    ////[ClassInfo(typeof(IDialogService))]
    public class DialogService : IDialogService
    {
        private readonly Stack<UIWindow> windows = new Stack<UIWindow>();

        /// <summary>
        /// Displays information about an error.
        /// </summary>
        /// <param name="message">The message to be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <param name="buttonText">The text shown in the only button
        /// in the dialog box. If left null, the text "OK" will be used.</param>
        /// <param name="afterHideCallback">A callback that should be executed after
        /// the dialog box is closed by the user.</param>
        /// <returns>A Task allowing this async method to be awaited.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task ShowError(string message, string title, string buttonText, Action afterHideCallback)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(
                title,
                message,
                buttonText ?? "OK",
                null,
                () =>
                {
                    afterHideCallback?.Invoke();
                    tcs.SetResult(true);
                },
                null);

            return tcs.Task;
        }

        /// <summary>
        /// Displays information about an error.
        /// </summary>
        /// <param name="error">The exception of which the message must be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <param name="buttonText">The text shown in the only button
        /// in the dialog box. If left null, the text "OK" will be used.</param>
        /// <param name="afterHideCallback">A callback that should be executed after
        /// the dialog box is closed by the user.</param>
        /// <returns>A Task allowing this async method to be awaited.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task ShowError(Exception error, string title, string buttonText, Action afterHideCallback)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(
                title,
                error.Message,
                buttonText ?? "OK",
                null,
                () =>
                {
                    afterHideCallback?.Invoke();
                    tcs.SetResult(true);
                },
                null);

            return tcs.Task;
        }

        /// <summary>
        /// Displays information to the user. The dialog box will have only
        /// one button with the text "OK".
        /// </summary>
        /// <param name="message">The message to be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <returns>A Task allowing this async method to be awaited.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task ShowMessage(string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(title, message, "OK", null, () => tcs.SetResult(true), null);

            return tcs.Task;
        }

        /// <summary>
        /// Displays information to the user. The dialog box will have only
        /// one button.
        /// </summary>
        /// <param name="message">The message to be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <param name="buttonText">The text shown in the only button
        /// in the dialog box. If left null, the text "OK" will be used.</param>
        /// <param name="afterHideCallback">A callback that should be executed after
        /// the dialog box is closed by the user.</param>
        /// <returns>A Task allowing this async method to be awaited.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task ShowMessage(string message, string title, string buttonText, Action afterHideCallback)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(
                title,
                message,
                buttonText ?? "OK",
                null,
                () =>
                {
                    afterHideCallback?.Invoke();
                    tcs.SetResult(true);
                },
                null);

            return tcs.Task;
        }

        /// <summary>
        /// Displays information to the user. The dialog box will have two
        /// buttons.
        /// </summary>
        /// <param name="message">The message to be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <param name="buttonConfirmText">The text shown in the "confirm" button
        /// in the dialog box. If left null, the text "OK" will be used.</param>
        /// <param name="buttonCancelText">The text shown in the "cancel" button
        /// in the dialog box. If left null, the text "Cancel" will be used.</param>
        /// <param name="afterHideCallback">A callback that should be executed after
        /// the dialog box is closed by the user. The callback method will get a boolean
        /// parameter indicating if the "confirm" button (true) or the "cancel" button
        /// (false) was pressed by the user.</param>
        /// <returns>A Task allowing this async method to be awaited. The task will return
        /// true or false depending on the dialog result.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task<bool> ShowMessage(
            string message,
            string title,
            string buttonConfirmText,
            string buttonCancelText,
            Action<bool> afterHideCallback)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(
                title,
                message,
                buttonCancelText ?? "Cancel",
                buttonConfirmText ?? "OK",
                () =>
                {
                    afterHideCallback?.Invoke(false);
                    tcs.SetResult(false);
                },
                () =>
                {
                    afterHideCallback?.Invoke(true);
                    tcs.SetResult(true);
                });

            return tcs.Task;
        }

        /// <summary>
        /// Displays information to the user in a simple dialog box. The dialog box will have only
        /// one button with the text "OK". This method should be used for debugging purposes.
        /// </summary>
        /// <param name="message">The message to be shown to the user.</param>
        /// <param name="title">The title of the dialog box. This may be null.</param>
        /// <returns>A Task allowing this async method to be awaited.</returns>
        /// <remarks>Displaying dialogs in iOS is synchronous. As such,
        /// this method will be executed synchronously even though it can be awaited
        /// for cross-platform compatibility purposes.</remarks>
        public Task ShowMessageBox(string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Show(title, message, "OK", null, () => tcs.SetResult(true), null);

            return tcs.Task;
        }

        private void Show(
            string title,
            string message,
            string cancelButtonText,
            string otherButtonText,
            Action cancelButtonAction,
            Action otherButtonAction)
        {
            UIAlertController alertController = this.CreateAlertController(
                title,
                message,
                cancelButtonText,
                otherButtonText,
                cancelButtonAction,
                otherButtonAction);

            UIWindow window = this.CreateWindow();
            this.windows.Push(window);

            window.MakeKeyAndVisible();
            window.RootViewController.PresentViewController(alertController, true, null);
        }

        private UIAlertController CreateAlertController(
            string title,
            string message,
            string cancelButtonText,
            string otherButtonText,
            Action cancelButtonAction,
            Action confirmButtonAction)
        {
            UIAlertController alertController = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            if (!string.IsNullOrEmpty(cancelButtonText))
            {
                alertController.AddAction(
                    UIAlertAction.Create(
                        cancelButtonText,
                        UIAlertActionStyle.Cancel,
                        (action) =>
                        {
                            cancelButtonAction?.Invoke();
                            this.PopWindow();
                        }));
            }

            if (!string.IsNullOrEmpty(otherButtonText))
            {
                alertController.AddAction(
                    UIAlertAction.Create(
                        otherButtonText,
                        UIAlertActionStyle.Default,
                        (action) =>
                        {
                            confirmButtonAction?.Invoke();
                            this.PopWindow();
                        }));
            }

            return alertController;
        }

        private UIWindow CreateWindow()
        {
            var window = new UIWindow(UIScreen.MainScreen.Bounds)
                         {
                             RootViewController = new UIViewController(),
                             TintColor = UIApplication.SharedApplication.KeyWindow?.TintColor,
                             WindowLevel = (UIApplication.SharedApplication.Windows.LastOrDefault()?.WindowLevel ?? 0)
                                           + 1
                         };

            return window;
        }

        private void PopWindow()
        {
            UIWindow window = this.windows.Pop();
            window.Hidden = true;
        }
    }
}
#endif