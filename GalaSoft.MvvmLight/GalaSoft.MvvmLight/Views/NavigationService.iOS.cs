// ****************************************************************************
// <copyright file="NavigationService.iOS.cs" company="GalaSoft Laurent Bugnion">
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
    using System.Reflection;

    using UIKit;

    /// <summary>
    /// Xamarin iOS implementation of <see cref="INavigationService"/>.
    /// This implementation can be used in Xamarin iOS applications (not Xamarin Forms).
    /// </summary>
    /// <remarks>For this navigation service to work properly, it should be initialized
    /// using the <see cref="Initialize"/> method, with the application's
    /// <see cref="UINavigationController"/>.</remarks>

    ////[ClassInfo(typeof(INavigationService))]
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// The key that is returned by the <see cref="CurrentPageKey"/> property
        /// when the current UIViewController is the root controller.
        /// </summary>
        public const string RootPageKey = "-- ROOT --";

        /// <summary>
        /// The key that is returned by the <see cref="CurrentPageKey"/> property
        /// when the current UIViewController is not found.
        /// This can be the case when the navigation wasn't managed by this NavigationService,
        /// for example when it is directly triggered in the Storyboard.
        /// </summary>
        public const string UnknownPageKey = "-- UNKNOWN --";

        private readonly Dictionary<string, TypeActionOrKey> pagesByKey = new Dictionary<string, TypeActionOrKey>();

        private readonly Dictionary<WeakReference, object> parametersByController =
            new Dictionary<WeakReference, object>();

        /// <summary>
        /// The key corresponding to the currently displayed page.
        /// </summary>
        public string CurrentPageKey
        {
            get
            {
                lock (this.pagesByKey)
                {
                    switch (this.NavigationController.ViewControllers.Length)
                    {
                        case 0:
                            return UnknownPageKey;
                        case 1:
                            return RootPageKey;
                    }

                    UIViewController topController = this.NavigationController.ViewControllers.Last();

                    TypeActionOrKey item = this.pagesByKey.Values.FirstOrDefault(i => i.ControllerType == topController.GetType());

                    if (item == null)
                    {
                        return UnknownPageKey;
                    }

                    KeyValuePair<string, TypeActionOrKey> pair = this.pagesByKey.First(i => i.Value == item);
                    return pair.Key;
                }
            }
        }

        /// <summary>
        /// Gets the NavigationController that was passed in the <see cref="Initialize"/> method.
        /// </summary>
        public UINavigationController NavigationController
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds a key/page pair to the navigation service.
        /// This method will create a new controller on demand, using
        /// reflection. You can use <see cref="Configure(string, Func{object,UIViewController})"/>
        /// if you need more fine-grained control over the controller's creation.
        /// </summary>
        /// <param name="key">The key that will be used later
        /// in the <see cref="NavigateTo(string)"/> or <see cref="NavigateTo(string, object)"/> methods.</param>
        /// <param name="controllerType">The type of the controller corresponding to the key.</param>
        public void Configure(string key, Type controllerType)
        {
            var item = new TypeActionOrKey { ControllerType = controllerType };

            this.SaveConfigurationItem(key, item);
        }

        /// <summary>
        /// Adds a key/page pair to the navigation service.
        /// This method allows the caller to have fine grained control over the controller's
        /// creation.
        /// </summary>
        /// <param name="key">The key that will be used later
        /// in the <see cref="NavigateTo(string)"/> or <see cref="NavigateTo(string, object)"/> methods.</param>
        /// <param name="createAction">A Func returning the controller corresponding
        /// to the given key.</param>
        public void Configure(string key, Func<object, UIViewController> createAction)
        {
            var item = new TypeActionOrKey { CreateControllerAction = createAction };

            this.SaveConfigurationItem(key, item);
        }

        /// <summary>
        /// Adds a key/page pair to the navigation service.
        /// This method should be used when working with Storyboard for the UI.
        /// </summary>
        /// <param name="key">The key that will be used later
        /// in the <see cref="NavigateTo(string)"/> or <see cref="NavigateTo(string, object)"/> methods.</param>
        /// <param name="storyboardId">The idea of the UIViewController
        /// in the Storyboard. Use the storyboardIdentifier/restorationIdentifier property
        /// in the *.storyboard document.</param>
        public void Configure(string key, string storyboardId)
        {
            var item = new TypeActionOrKey { StoryboardControllerKey = storyboardId };

            this.SaveConfigurationItem(key, item);
        }

        /// <summary>
        /// Allows a caller to get the navigation parameter corresponding 
        /// to the Intent parameter.
        /// </summary>
        /// <param name="controller">The <see cref="UIViewController"/> that was navigated to.</param>
        /// <returns>The navigation parameter. If no parameter is found,
        /// returns null.</returns>
        public object GetAndRemoveParameter(UIViewController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(
                    nameof(controller),
                    "This method must be called with a valid UIViewController");
            }

            lock (this.parametersByController)
            {
                object value = null;
                WeakReference key = null;

                foreach (KeyValuePair<WeakReference, object> pair in this.parametersByController.Where(pair => Equals(pair.Key.Target, controller)))
                {
                    key = pair.Key;
                    value = pair.Value;
                    break;
                }

                if (key != null)
                {
                    this.parametersByController.Remove(key);
                }

                return value;
            }
        }

        /// <summary>
        /// If possible, discards the current page and displays the previous page
        /// on the navigation stack.
        /// </summary>
        public void GoBack()
        {
            this.NavigationController.PopViewController(true);
        }

        /// <summary>
        /// Initialized the navigation service. This method must be called
        /// before the <see cref="NavigateTo(string)"/> or
        /// <see cref="NavigateTo(string, object)"/> methods are called.
        /// </summary>
        /// <param name="navigation">The application's navigation controller.</param>
        public void Initialize(UINavigationController navigation)
        {
            this.NavigationController = navigation;
        }

        /// <summary>
        /// Displays a new page corresponding to the given key. 
        /// Make sure to call the <see cref="Configure(string, Type)"/>
        /// or <see cref="Configure(string, Func{object,UIViewController})"/>
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
        /// and passes a parameter to the new page's constructor.
        /// Make sure to call the <see cref="Configure(string, Type)"/>
        /// or <see cref="Configure(string, Func{object,UIViewController})"/>
        /// method first.
        /// </summary>
        /// <param name="pageKey">The key corresponding to the page
        /// that should be displayed.</param>
        /// <param name="parameter">The parameter that should be passed
        /// to the new page's constructor.</param>
        /// <exception cref="ArgumentException">When this method is called for 
        /// a key that has not been configured earlier.</exception>
        /// <exception cref="InvalidOperationException">When this method is called for 
        /// a page that doesn't have a suitable constructor (i.e.
        /// a constructor with a parameter corresponding to the
        /// navigation parameter's type).</exception>
        public void NavigateTo(string pageKey, object parameter)
        {
            lock (this.pagesByKey)
            {
                Exception creationException = null;
                bool done = false;
                TypeActionOrKey item;

                if (this.pagesByKey.ContainsKey(pageKey))
                {
                    item = this.pagesByKey[pageKey];
                }
                else
                {
                    item = new TypeActionOrKey { StoryboardControllerKey = pageKey };
                }

                UIViewController controller = null;

                if (item.CreateControllerAction != null)
                {
                    try
                    {
                        controller = item.CreateControllerAction(parameter);
                        done = true;
                    }
                    catch (Exception ex)
                    {
                        creationException = ex;
                    }
                }

                if (!string.IsNullOrEmpty(item.StoryboardControllerKey))
                {
                    if (this.NavigationController == null)
                    {
                        throw new InvalidOperationException(
                            "Unable to navigate: Did you forget to call NavigationService.Initialize?");
                    }

                    if (this.NavigationController.Storyboard == null)
                    {
                        throw new InvalidOperationException(
                            "Unable to navigate: No storyboard found. You need to call NavigationService.Configure!");
                    }

                    try
                    {
                        controller =
                            this.NavigationController.Storyboard.InstantiateViewController(
                                item.StoryboardControllerKey);
                        done = true;
                    }
                    catch (Exception ex)
                    {
                        creationException = ex;
                    }

                    if (parameter != null)
                    {
                        // Save parameter in list for later
                        this.parametersByController.Add(new WeakReference(controller), parameter);
                    }
                }

                if (!done && item.ControllerType != null)
                {
                    try
                    {
                        controller = this.MakeController(item.ControllerType, parameter);

                        if (controller == null)
                        {
                            throw new InvalidOperationException("No suitable constructor found for page " + pageKey);
                        }
                    }
                    catch (Exception ex)
                    {
                        creationException = ex;
                    }
                }

                if (controller == null)
                {
                    throw new InvalidOperationException(
                        $"Unable to create a controller for key {pageKey}",
                        creationException);
                }

                if (item.ControllerType == null)
                {
                    item.ControllerType = controller.GetType();
                }

                this.NavigationController.PushViewController(controller, true);
            }
        }

        private UIViewController MakeController(Type controllerType, object parameter)
        {
            ConstructorInfo constructor;
            object[] parameters;

            if (parameter == null)
            {
                constructor = controllerType.GetTypeInfo().DeclaredConstructors
                    .FirstOrDefault(c => !c.GetParameters().Any());

                parameters = new object[] { };
            }
            else
            {
                constructor = controllerType.GetTypeInfo().DeclaredConstructors.FirstOrDefault(
                    c =>
                    {
                        ParameterInfo[] p = c.GetParameters();
                        return p.Length == 1 && p[0].ParameterType == parameter.GetType();
                    });

                parameters = new[] { parameter };
            }

            if (constructor == null)
            {
                return null;
            }

            var controller = constructor.Invoke(parameters) as UIViewController;
            return controller;
        }

        private void SaveConfigurationItem(string key, TypeActionOrKey item)
        {
            lock (this.pagesByKey)
            {
                if (this.pagesByKey.ContainsKey(key))
                {
                    this.pagesByKey[key] = item;
                }
                else
                {
                    this.pagesByKey.Add(key, item);
                }
            }
        }

        private class TypeActionOrKey
        {
            public Type ControllerType;

            public Func<object, UIViewController> CreateControllerAction;

            public string StoryboardControllerKey;
        }
    }
}
#endif