// ****************************************************************************
// <copyright file="PropertyChangedEventManager.Xamarin.cs" company="GalaSoft Laurent Bugnion">
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

#if __ANDROID__ || __IOS__
// ReSharper disable CheckNamespace
namespace System.Windows
{
    // ReSharper restore CheckNamespace
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Provides an implementation so that you can use the 
    /// "weak event listener" pattern to attach listeners
    /// for the <see cref="PropertyChanged" /> event. 
    /// </summary>

    ////[ClassInfo(typeof(Binding))]
    public class PropertyChangedEventManager
    {
        private static PropertyChangedEventManager manager;

        private static readonly object SyncLock = new object();

        private Dictionary<string, List<ListenerInfo>> list;

        /// <summary>
        /// Get the current instance of <see cref="PropertyChangedEventManager"/>
        /// </summary>
        private static PropertyChangedEventManager Instance => manager ?? (manager = new PropertyChangedEventManager());

        /// <summary>
        /// Adds the specified listener to the list of listeners on the specified source. 
        /// </summary>
        /// <param name="source">The object with the event.</param>
        /// <param name="listener">The object to add as a listener.</param>
        /// <param name="propertyName">The name of the property that exists on
        /// source upon which to listen for changes.</param>
        public static void AddListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            Instance.PrivateAddListener(source, listener, propertyName);
        }

        /// <summary>
        /// Removes the specified listener from the list of listeners on the 
        /// specified source. 
        /// </summary>
        /// <param name="listener">The object to remove as a listener.</param>
        public static void RemoveListener(IWeakEventListener listener)
        {
            Instance.PrivateRemoveListener(listener);
        }

        /// <summary>
        /// Private method to add the specified listener to the list of listeners 
        /// on the specified source. 
        /// </summary>
        /// <param name="source">The object with the event.</param>
        /// <param name="listener">The object to add as a listener.</param>
        /// <param name="propertyName">The name of the property that exists 
        /// on source upon which to listen for changes.</param>
        private void PrivateAddListener(INotifyPropertyChanged source, IWeakEventListener listener, string propertyName)
        {
            if (source == null)
            {
                return;
            }

            lock (SyncLock)
            {
                if (this.list == null)
                {
                    this.list = new Dictionary<string, List<ListenerInfo>>();
                }

                bool sourceExists = this.list.Any(
                    list => list.Value.Any(
                        entry => entry.InstanceReference != null && entry.InstanceReference.IsAlive
                                                                 && entry.InstanceReference.Target != null
                                                                 && entry.InstanceReference.Target.Equals(source)));

                if (this.list.ContainsKey(propertyName))
                {
                    this.list[propertyName].Add(new ListenerInfo(listener, source));
                }
                else
                {
                    var list = new List<ListenerInfo> { new ListenerInfo(listener, source) };
                    this.list.Add(propertyName, list);
                }

                if (!sourceExists)
                {
                    // Now, start listening to source
                    this.StartListening(source);
                }
            }
        }

        /// <summary>
        /// Private method to remove the specified listener from the list of listeners 
        /// on the specified source. 
        /// </summary>
        /// <param name="listener">The object to remove as a listener.</param>
        private void PrivateRemoveListener(IWeakEventListener listener)
        {
            if (this.list != null)
            {
                lock (SyncLock)
                {
                    string propertyName = null;
                    ListenerInfo toRemove = null;

                    foreach (KeyValuePair<string, List<ListenerInfo>> list in this.list)
                    {
                        foreach (ListenerInfo info in list.Value.Where(info => info.Listener == listener))
                        {
                            propertyName = list.Key;
                            toRemove = info;
                            break;
                        }

                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(propertyName))
                    {
                        return;
                    }

                    this.list[propertyName].Remove(toRemove);

                    if (this.list[propertyName].Count == 0)
                    {
                        this.list.Remove(propertyName);
                    }

                    bool checkInstance = this.list.Any(
                        l => l.Value.Any(
                            i => i.InstanceReference != null && i.InstanceReference.IsAlive
                                                             && i.InstanceReference.Target != null
                                                             && i.InstanceReference.Target.Equals(
                                                                 toRemove.InstanceReference.Target)));

                    if (!checkInstance)
                    {
                        this.StopListening((INotifyPropertyChanged)toRemove.InstanceReference.Target);
                    }
                }
            }
        }

        /// <summary>
        /// The method that handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">A <see cref="PropertyChangedEventArgs"/> that 
        /// contains the event data.</param>
        private void PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!this.list.ContainsKey(args.PropertyName))
            {
                return;
            }

            List<ListenerInfo> list = this.list[args.PropertyName];
            if (list == null)
            {
                return;
            }

            List<ListenerInfo> recipients = list.Where(
                    i => i.InstanceReference != null && i.InstanceReference.IsAlive
                                                     && i.InstanceReference.Target == sender && i.Listener != null)
                .ToList();

            // We have the listeners. Deal with them
            foreach (ListenerInfo item in recipients)
            {
                item.Listener.ReceiveWeakEvent(this.GetType(), sender, args);
            }
        }

        /// <summary>
        /// Begin listening for the <see cref="PropertyChanged"/> event on 
        /// the provided source.
        /// </summary>
        /// <param name="source">The object on which to start listening 
        /// for <see cref="PropertyChanged"/>.</param>
        private void StartListening(INotifyPropertyChanged source)
        {
            if (source != null)
            {
                source.PropertyChanged += this.PropertyChanged;
            }
        }

        /// <summary>
        /// Stop listening for the <see cref="PropertyChanged"/> event on the 
        /// provided source.
        /// </summary>
        /// <param name="source">The object on which to start listening for 
        /// <see cref="PropertyChanged"/>.</param>
        private void StopListening(INotifyPropertyChanged source)
        {
            if (source != null)
            {
                source.PropertyChanged -= this.PropertyChanged;
            }
        }

        private class ListenerInfo
        {
            public WeakReference InstanceReference
            {
                get;
                private set;
            }

            public IWeakEventListener Listener
            {
                get;
                private set;
            }

            public ListenerInfo(IWeakEventListener listener, INotifyPropertyChanged inpc)
            {
                this.Listener = listener;
                this.InstanceReference = new WeakReference(inpc);
            }
        }
    }
}
#endif