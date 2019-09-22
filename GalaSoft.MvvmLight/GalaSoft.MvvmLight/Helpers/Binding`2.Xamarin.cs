// ****************************************************************************
// <copyright file="Binding`2.Xamarin.cs" company="GalaSoft Laurent Bugnion">
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
namespace GalaSoft.MvvmLight.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Windows;

    /// <summary>
    /// Creates a binding between two properties. If the source implements INotifyPropertyChanged, the source property raises the PropertyChanged event
    /// and the BindingMode is OneWay or TwoWay, the target property will be synchronized with the source property. If
    /// the target implements INotifyPropertyChanged, the target property raises the PropertyChanged event and the BindingMode is
    /// TwoWay, the source property will also be synchronized with the target property.
    /// </summary>
    /// <typeparam name="TSource">The type of the source property that is being databound.</typeparam>
    /// <typeparam name="TTarget">The type of the target property that is being databound. If the source type
    /// is not the same as the target type, an automatic conversion will be attempted. However only
    /// simple types can be converted. For more complex conversions, use the <see cref="ConvertSourceToTarget"/>
    /// and <see cref="ConvertTargetToSource"/> methods to define custom converters.</typeparam>
    ////[ClassInfo(typeof(Binding))]
    public partial class Binding<TSource, TTarget> : Binding
    {
        private readonly SimpleConverter converter = new SimpleConverter();

        private readonly List<IWeakEventListener> listeners = new List<IWeakEventListener>();

        private readonly Dictionary<string, DelegateInfo> sourceHandlers = new Dictionary<string, DelegateInfo>();

        private readonly Expression<Func<TSource>> sourcePropertyExpression;

        private readonly string sourcePropertyName;

        private readonly Dictionary<string, DelegateInfo> targetHandlers = new Dictionary<string, DelegateInfo>();

        private readonly Expression<Func<TTarget>> targetPropertyExpression;

        private readonly string targetPropertyName;

        private bool isFallbackValueActive;

        private WeakAction onSourceUpdate;

        private WeakReference propertySource;

        private WeakReference propertyTarget;

        private bool resolveTopField;

        private bool settingSourceToTarget;

        private bool settingTargetToSource;

        private PropertyInfo sourceProperty;

        private PropertyInfo targetProperty;

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to return a value. This can happen if one of the
        /// items on the Path (except the source property itself) is null, or if the Converter throws an exception.
        /// </summary>
        public TSource FallbackValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets of sets the value used when the source property is null (or equals to default(TSource)).
        /// </summary>
        public TSource TargetNullValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current value of the binding.
        /// </summary>
        public TTarget Value
        {
            get
            {
                if (this.propertySource == null || !this.propertySource.IsAlive)
                {
                    return default(TTarget);
                }

                Type type = this.propertySource.Target.GetType();
                PropertyInfo property = type.GetProperty(this.sourcePropertyName);
                return (TTarget)property.GetValue(this.propertySource.Target, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the Binding class for which the source and target properties
        /// are located in different objects.
        /// </summary>
        /// <param name="source">The source of the binding. If this object implements INotifyPropertyChanged and the
        /// BindingMode is OneWay or TwoWay, the target will be notified of changes to the target property.</param>
        /// <param name="sourcePropertyName">The name of the source property for the binding.</param>
        /// <param name="target">The target of the binding. If this object implements INotifyPropertyChanged and the
        /// BindingMode is TwoWay, the source will be notified of changes to the source property.</param>
        /// <param name="targetPropertyName">The name of the target property for the binding.</param>
        /// <param name="mode">The mode of the binding. OneTime means that the target property will be set once (when the binding is
        /// created) but that subsequent changes will be ignored. OneWay means that the target property will be set, and
        /// if the PropertyChanged event is raised by the source, the target property will be updated. TwoWay means that the source
        /// property will also be updated if the target raises the PropertyChanged event. Default means OneWay if only the source
        /// implements INPC, and TwoWay if both the source and the target implement INPC.</param>
        /// <param name="fallbackValue">Tthe value to use when the binding is unable to return a value. This can happen if one of the
        /// items on the Path (except the source property itself) is null, or if the Converter throws an exception.</param>
        /// <param name="targetNullValue">The value to use when the binding is unable to return a value. This can happen if one of the
        /// items on the Path (except the source property itself) is null, or if the Converter throws an exception.</param>
        public Binding(
            object source,
            string sourcePropertyName,
            object target = null,
            string targetPropertyName = null,
            BindingMode mode = BindingMode.Default,
            TSource fallbackValue = default(TSource),
            TSource targetNullValue = default(TSource))
        {
            this.Mode = mode;
            this.FallbackValue = fallbackValue;
            this.TargetNullValue = targetNullValue;

            this.TopSource = new WeakReference(source);
            this.propertySource = new WeakReference(source);
            this.sourcePropertyName = sourcePropertyName;

            if (target == null)
            {
                this.TopTarget = this.TopSource;
                this.propertyTarget = this.propertySource;
            }
            else
            {
                this.TopTarget = new WeakReference(target);
                this.propertyTarget = new WeakReference(target);
            }

            this.targetPropertyName = targetPropertyName;
            this.Attach();
        }

        /// <summary>
        /// Initializes a new instance of the Binding class for which the source and target properties
        /// are located in different objects.
        /// </summary>
        /// <param name="source">The source of the binding. If this object implements INotifyPropertyChanged and the
        /// BindingMode is OneWay or TwoWay, the target will be notified of changes to the target property.</param>
        /// <param name="sourcePropertyExpression">An expression pointing to the source property. It can be
        /// a simple expression "() => [source].MyProperty" or a composed expression "() => [source].SomeObject.SomeOtherObject.SomeProperty".</param>
        /// <param name="target">The target of the binding. If this object implements INotifyPropertyChanged and the
        /// BindingMode is TwoWay, the source will be notified of changes to the source property.</param>
        /// <param name="targetPropertyExpression">An expression pointing to the target property. It can be
        /// a simple expression "() => [target].MyProperty" or a composed expression "() => [target].SomeObject.SomeOtherObject.SomeProperty".</param>
        /// <param name="mode">The mode of the binding. OneTime means that the target property will be set once (when the binding is
        /// created) but that subsequent changes will be ignored. OneWay means that the target property will be set, and
        /// if the PropertyChanged event is raised by the source, the target property will be updated. TwoWay means that the source
        /// property will also be updated if the target raises the PropertyChanged event. Default means OneWay if only the source
        /// implements INPC, and TwoWay if both the source and the target implement INPC.</param>
        /// <param name="fallbackValue">Tthe value to use when the binding is unable to return a value. This can happen if one of the
        /// items on the Path (except the source property itself) is null, or if the Converter throws an exception.</param>
        /// <param name="targetNullValue">The value to use when the binding is unable to return a value. This can happen if one of the
        /// items on the Path (except the source property itself) is null, or if the Converter throws an exception.</param>
        public Binding(
            object source,
            Expression<Func<TSource>> sourcePropertyExpression,
            object target = null,
            Expression<Func<TTarget>> targetPropertyExpression = null,
            BindingMode mode = BindingMode.Default,
            TSource fallbackValue = default(TSource),
            TSource targetNullValue = default(TSource))
            : this(
                source,
                sourcePropertyExpression,
                null,
                target,
                targetPropertyExpression,
                mode,
                fallbackValue,
                targetNullValue)
        {
        }

        internal Binding(
            object source,
            Expression<Func<TSource>> sourcePropertyExpression,
            bool? resolveTopField,
            object target = null,
            Expression<Func<TTarget>> targetPropertyExpression = null,
            BindingMode mode = BindingMode.Default,
            TSource fallbackValue = default(TSource),
            TSource targetNullValue = default(TSource))
        {
            this.Mode = mode;
            this.FallbackValue = fallbackValue;
            this.TargetNullValue = targetNullValue;

            this.TopSource = new WeakReference(source);
            this.sourcePropertyExpression = sourcePropertyExpression;
            this.sourcePropertyName = GetPropertyName(sourcePropertyExpression);

            this.TopTarget = target == null ? this.TopSource : new WeakReference(target);
            this.targetPropertyExpression = targetPropertyExpression;
            this.targetPropertyName = GetPropertyName(targetPropertyExpression);

            this.Attach(
                this.TopSource.Target,
                this.TopTarget.Target,
                mode,
                resolveTopField ?? target == null && targetPropertyExpression != null);
        }

        /// <summary>
        /// Defines a custom conversion method for a binding. To be used when the
        /// binding's source property is of a different type than the binding's
        /// target property, and the conversion cannot be done automatically (simple
        /// values).
        /// </summary>
        /// <param name="convert">A func that will be called with the source
        /// property's value, and will return the target property's value.
        ///  IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <returns>The Binding instance.</returns>
        public Binding<TSource, TTarget> ConvertSourceToTarget(Func<TSource, TTarget> convert)
        {
            this.converter.SetConvert(convert);
            this.ForceUpdateValueFromSourceToTarget();
            return this;
        }

        /// <summary>
        /// Defines a custom conversion method for a two-way binding. To be used when the
        /// binding's target property is of a different type than the binding's
        /// source property, and the conversion cannot be done automatically (simple
        /// values).
        /// </summary>
        /// <param name="convertBack">A func that will be called with the source
        /// property's value, and will return the target property's value.
        ///  IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <returns>The Binding instance.</returns>
        /// <remarks>This method is inactive on OneTime or OneWay bindings.</remarks>
        public Binding<TSource, TTarget> ConvertTargetToSource(Func<TTarget, TSource> convertBack)
        {
            this.converter.SetConvertBack(convertBack);
            return this;
        }

        /// <summary>
        /// Instructs the Binding instance to stop listening to value changes and to
        /// remove all listeners.
        /// </summary>
        public override void Detach()
        {
            foreach (IWeakEventListener listener in this.listeners)
            {
                PropertyChangedEventManager.RemoveListener(listener);
            }

            this.listeners.Clear();

            this.DetachSourceHandlers();
            this.DetachTargetHandlers();
        }

        /// <summary>
        /// Forces the Binding's value to be reevaluated. The target value will
        /// be set to the source value.
        /// </summary>
        public override void ForceUpdateValueFromSourceToTarget()
        {
            if (this.onSourceUpdate == null && (this.propertySource == null || !this.propertySource.IsAlive
                                                                            || this.propertySource.Target == null))
            {
                return;
            }

            if (this.targetProperty != null)
            {
                try
                {
                    TTarget value = this.GetSourceValue();
                    object targetValue = this.targetProperty.GetValue(this.propertyTarget.Target);

                    if (!Equals(value, targetValue))
                    {
                        this.settingSourceToTarget = true;
                        this.SetTargetValue(value);
                        this.settingSourceToTarget = false;
                    }
                }
                catch
                {
                    if (!Equals(this.FallbackValue, default(TSource)))
                    {
                        this.settingSourceToTarget = true;
                        this.targetProperty.SetValue(this.propertyTarget.Target, this.FallbackValue, null);
                        this.settingSourceToTarget = false;
                    }
                }
            }

            if (this.onSourceUpdate != null && this.onSourceUpdate.IsAlive)
            {
                this.onSourceUpdate.Execute();
            }

            this.RaiseValueChanged();
        }

        /// <summary>
        /// Forces the Binding's value to be reevaluated. The source value will
        /// be set to the target value.
        /// </summary>
        public override void ForceUpdateValueFromTargetToSource()
        {
            if (this.propertyTarget == null || !this.propertyTarget.IsAlive || this.propertyTarget.Target == null
                || this.propertySource == null || !this.propertySource.IsAlive || this.propertySource.Target == null)
            {
                return;
            }

            if (this.targetProperty != null)
            {
                TSource value = this.GetTargetValue();
                object sourceValue = this.sourceProperty.GetValue(this.propertySource.Target);

                if (!Equals(value, sourceValue))
                {
                    this.settingTargetToSource = true;
                    this.SetSourceValue(value);
                    this.settingTargetToSource = false;
                }
            }

            this.RaiseValueChanged();
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound source object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <param name="eventName">The name of the event that should be observed
        /// to update the binding's value.</param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime binding. Such bindings cannot be updated. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        /// <exception cref="ArgumentNullException">When the eventName parameter is null
        /// or is an empty string.</exception>
        /// <exception cref="ArgumentException">When the requested event does not exist on the
        /// source control.</exception>
        public Binding<TSource, TTarget> ObserveSourceEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return this;
            }

            if (this.Mode == BindingMode.OneTime)
            {
                throw new InvalidOperationException("This method cannot be used with OneTime bindings");
            }

            if (this.onSourceUpdate == null && (this.propertySource == null || !this.propertySource.IsAlive
                                                                            || this.propertySource.Target == null))
            {
                throw new InvalidOperationException("Source is not ready");
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            Type type = this.propertySource.Target.GetType();
            EventInfo ev = type.GetEvent(eventName);
            if (ev == null)
            {
                throw new ArgumentException("Event not found: " + eventName, nameof(eventName));
            }

            EventHandler handler = this.HandleSourceEvent;

            DelegateInfo defaultHandlerInfo = this.sourceHandlers.Values.FirstOrDefault(i => i.IsDefault);

            if (defaultHandlerInfo != null)
            {
                this.DetachSourceHandlers();
            }

            var info = new DelegateInfo { Delegate = handler };

            if (this.sourceHandlers.ContainsKey(eventName))
            {
                this.sourceHandlers[eventName] = info;
            }
            else
            {
                this.sourceHandlers.Add(eventName, info);
            }

            ev.AddEventHandler(this.propertySource.Target, handler);

            return this;
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound source object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <remarks>Use this method when the event requires a specific EventArgs type
        /// instead of the standard EventHandler.</remarks>
        /// <typeparam name="TEventArgs">The type of the EventArgs used by this control's event.</typeparam>
        /// <param name="eventName">The name of the event that should be observed
        /// to update the binding's value.</param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime binding. Such bindings cannot be updated. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        /// <exception cref="ArgumentNullException">When the eventName parameter is null
        /// or is an empty string.</exception>
        /// <exception cref="ArgumentException">When the requested event does not exist on the
        /// source control.</exception>
        public Binding<TSource, TTarget> ObserveSourceEvent<TEventArgs>(string eventName)
            where TEventArgs : EventArgs
        {
            if (string.IsNullOrEmpty(eventName) || this.sourceHandlers.ContainsKey(eventName))
            {
                return this;
            }

            if (this.Mode == BindingMode.OneTime)
            {
                throw new InvalidOperationException("This method cannot be used with OneTime bindings");
            }

            if (this.onSourceUpdate == null && (this.propertySource == null || !this.propertySource.IsAlive
                                                                            || this.propertySource.Target == null))
            {
                throw new InvalidOperationException("Source is not ready");
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            Type type = this.propertySource.Target.GetType();
            EventInfo ev = type.GetEvent(eventName);
            if (ev == null)
            {
                throw new ArgumentException("Event not found: " + eventName, nameof(eventName));
            }

            EventHandler<TEventArgs> handler = this.HandleSourceEvent;

            DelegateInfo defaultHandlerInfo = this.sourceHandlers.Values.FirstOrDefault(i => i.IsDefault);

            if (defaultHandlerInfo != null)
            {
                this.DetachSourceHandlers();
            }

            var info = new DelegateInfo { Delegate = handler };

            if (this.sourceHandlers.ContainsKey(eventName))
            {
                this.sourceHandlers[eventName] = info;
            }
            else
            {
                this.sourceHandlers.Add(eventName, info);
            }

            ev.AddEventHandler(this.propertySource.Target, handler);

            return this;
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound target object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <param name="eventName">The name of the event that should be observed
        /// to update the binding's value.</param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime or a OneWay binding. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        /// <exception cref="ArgumentNullException">When the eventName parameter is null
        /// or is an empty string.</exception>
        /// <exception cref="ArgumentException">When the requested event does not exist on the
        /// target control.</exception>
        public Binding<TSource, TTarget> ObserveTargetEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return this;
            }

            if (this.Mode == BindingMode.OneTime || this.Mode == BindingMode.OneWay)
            {
                throw new InvalidOperationException("This method cannot be used with OneTime or OneWay bindings");
            }

            if (this.onSourceUpdate != null)
            {
                throw new InvalidOperationException("Cannot use SetTargetEvent with onSourceUpdate");
            }

            if (this.propertyTarget == null || !this.propertyTarget.IsAlive || this.propertyTarget.Target == null)
            {
                throw new InvalidOperationException("Target is not ready");
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            Type type = this.propertyTarget.Target.GetType();

            EventInfo ev = type.GetEvent(eventName);
            if (ev == null)
            {
                throw new ArgumentException("Event not found: " + eventName, nameof(eventName));
            }

            EventHandler handler = this.HandleTargetEvent;

            DelegateInfo defaultHandlerInfo = this.targetHandlers.Values.FirstOrDefault(i => i.IsDefault);

            if (defaultHandlerInfo != null)
            {
                this.DetachTargetHandlers();
            }

            var info = new DelegateInfo { Delegate = handler };

            if (this.targetHandlers.ContainsKey(eventName))
            {
                this.targetHandlers[eventName] = info;
            }
            else
            {
                this.targetHandlers.Add(eventName, info);
            }

            ev.AddEventHandler(this.propertyTarget.Target, handler);

            return this;
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound target object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <remarks>Use this method when the event requires a specific EventArgs type
        /// instead of the standard EventHandler.</remarks>
        /// <typeparam name="TEventArgs">The type of the EventArgs used by this control's event.</typeparam>
        /// <param name="eventName">The name of the event that should be observed
        /// to update the binding's value.</param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime or OneWay binding. This exception can
        /// also be thrown when the target object is null or has already been
        /// garbage collected before this method is called.</exception>
        /// <exception cref="ArgumentNullException">When the eventName parameter is null
        /// or is an empty string.</exception>
        /// <exception cref="ArgumentException">When the requested event does not exist on the
        /// target control.</exception>
        public Binding<TSource, TTarget> ObserveTargetEvent<TEventArgs>(string eventName)
            where TEventArgs : EventArgs
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return this;
            }

            if (this.Mode == BindingMode.OneTime || this.Mode == BindingMode.OneWay)
            {
                throw new InvalidOperationException("This method cannot be used with OneTime or OneWay bindings");
            }

            if (this.onSourceUpdate != null)
            {
                throw new InvalidOperationException("Cannot use SetTargetEvent with onSourceUpdate");
            }

            if (this.propertyTarget == null || !this.propertyTarget.IsAlive || this.propertyTarget.Target == null)
            {
                throw new InvalidOperationException("Target is not ready");
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            Type type = this.propertyTarget.Target.GetType();

            EventInfo ev = type.GetEvent(eventName);
            if (ev == null)
            {
                throw new ArgumentException("Event not found: " + eventName, nameof(eventName));
            }

            EventHandler<TEventArgs> handler = this.HandleTargetEvent;

            DelegateInfo defaultHandlerInfo = this.targetHandlers.Values.FirstOrDefault(i => i.IsDefault);

            if (defaultHandlerInfo != null)
            {
                this.DetachTargetHandlers();
            }

            var info = new DelegateInfo { Delegate = handler };

            if (this.targetHandlers.ContainsKey(eventName))
            {
                this.targetHandlers[eventName] = info;
            }
            else
            {
                this.targetHandlers.Add(eventName, info);
            }

            ev.AddEventHandler(this.propertyTarget.Target, handler);

            return this;
        }

        /// <summary>
        /// Defines an action that will be executed every time that the binding value
        /// changes.
        /// </summary>
        /// <param name="callback">The action that will be executed when the binding changes.
        /// IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When WhenSourceChanges is called on
        /// a binding which already has a target property set.</exception>
        public Binding<TSource, TTarget> WhenSourceChanges(Action callback)
        {
            if (this.targetPropertyExpression != null)
            {
                throw new InvalidOperationException(
                    "You cannot set both the targetPropertyExpression and call WhenSourceChanges");
            }

            this.onSourceUpdate = new WeakAction(callback);

            if (this.onSourceUpdate.IsAlive)
            {
                this.onSourceUpdate.Execute();
            }

            return this;
        }

        private void Attach(object source, object target, BindingMode mode)
        {
            this.Attach(source, target, mode, this.resolveTopField);
        }

        private void Attach(object source, object target, BindingMode mode, bool resolveTopField)
        {
            this.resolveTopField = resolveTopField;

            IList<PropertyAndName> sourceChain = GetPropertyChain(
                source,
                null,
                this.sourcePropertyExpression.Body as MemberExpression,
                this.sourcePropertyName,
                resolveTopField);

            PropertyAndName lastSourceInChain = sourceChain.Last();
            sourceChain.Remove(lastSourceInChain);

            this.propertySource = new WeakReference(lastSourceInChain.Instance);

            if (mode != BindingMode.OneTime)
            {
                foreach (PropertyAndName instance in sourceChain)
                {
                    if (!(instance.Instance is INotifyPropertyChanged inpc))
                    {
                        continue;
                    }

                    var listener = new ObjectSwappedEventListener(this, inpc);
                    this.listeners.Add(listener);
                    PropertyChangedEventManager.AddListener(inpc, listener, instance.Name);
                }
            }

            if (target != null && this.targetPropertyExpression != null && this.targetPropertyName != null)
            {
                IList<PropertyAndName> targetChain = GetPropertyChain(
                    target,
                    null,
                    this.targetPropertyExpression.Body as MemberExpression,
                    this.targetPropertyName,
                    resolveTopField);

                PropertyAndName lastTargetInChain = targetChain.Last();
                targetChain.Remove(lastTargetInChain);

                this.propertyTarget = new WeakReference(lastTargetInChain.Instance);

                if (mode != BindingMode.OneTime)
                {
                    foreach (PropertyAndName instance in targetChain)
                    {
                        if (!(instance.Instance is INotifyPropertyChanged inpc))
                        {
                            continue;
                        }

                        var listener = new ObjectSwappedEventListener(this, inpc);
                        this.listeners.Add(listener);
                        PropertyChangedEventManager.AddListener(inpc, listener, instance.Name);
                    }
                }
            }

            this.isFallbackValueActive = false;

            if (sourceChain.Any(r => r.Instance == null))
            {
                this.isFallbackValueActive = true;
            }
            else
            {
                if (lastSourceInChain.Instance == null)
                {
                    this.isFallbackValueActive = true;
                }
            }

            this.Attach();
        }

        private void Attach()
        {
            if (this.propertyTarget != null && this.propertyTarget.IsAlive && this.propertyTarget.Target != null
                && !string.IsNullOrEmpty(this.targetPropertyName))
            {
                Type targetType = this.propertyTarget.Target.GetType();
                this.targetProperty = targetType.GetProperty(this.targetPropertyName);

                if (this.targetProperty == null)
                {
                    throw new InvalidOperationException("Property not found: " + this.targetPropertyName);
                }
            }

            if (this.propertySource == null || !this.propertySource.IsAlive || this.propertySource.Target == null)
            {
                this.SetSpecialValues();
                return;
            }

            Type sourceType = this.propertySource.Target.GetType();
            this.sourceProperty = sourceType.GetProperty(this.sourcePropertyName);

            if (this.sourceProperty == null)
            {
                throw new InvalidOperationException("Property not found: " + this.sourcePropertyName);
            }

            // OneTime binding
            if (this.CanBeConverted(this.sourceProperty, this.targetProperty))
            {
                TTarget value = this.GetSourceValue();

                if (this.targetProperty != null && this.propertyTarget != null && this.propertyTarget.IsAlive
                    && this.propertyTarget.Target != null)
                {
                    this.settingSourceToTarget = true;
                    this.SetTargetValue(value);
                    this.settingSourceToTarget = false;
                }

                if (this.onSourceUpdate != null && this.onSourceUpdate.IsAlive)
                {
                    this.onSourceUpdate.Execute();
                }
            }

            if (this.Mode == BindingMode.OneTime)
            {
                return;
            }

            // Check OneWay binding

            if (this.propertySource.Target is INotifyPropertyChanged inpc)
            {
                var listener = new PropertyChangedEventListener(this, inpc, true);

                this.listeners.Add(listener);
                PropertyChangedEventManager.AddListener(inpc, listener, this.sourcePropertyName);
            }
            else
            {
                this.CheckControlSource();
            }

            if (this.Mode == BindingMode.OneWay || this.Mode == BindingMode.Default)
            {
                return;
            }

            // Check TwoWay binding
            if (this.onSourceUpdate != null || this.propertyTarget == null || !this.propertyTarget.IsAlive
                || this.propertyTarget.Target == null)
            {
                return;
            }

            if (this.propertyTarget.Target is INotifyPropertyChanged inpc2)
            {
                var listener = new PropertyChangedEventListener(this, inpc2, false);

                this.listeners.Add(listener);
                PropertyChangedEventManager.AddListener(inpc2, listener, this.targetPropertyName);
            }
            else
            {
                this.CheckControlTarget();
            }
        }

        private bool CanBeConverted(PropertyInfo sourceProperty, PropertyInfo targetProperty)
        {
            if (targetProperty == null)
            {
                return true;
            }

            Type sourceType = sourceProperty.PropertyType;
            Type targetType = targetProperty.PropertyType;

            return sourceType == targetType || (this.IsValueType(sourceType) && this.IsValueType(targetType));
        }

        private void DetachSourceHandlers()
        {
            if (this.propertySource == null || !this.propertySource.IsAlive || this.propertySource.Target == null)
            {
                return;
            }

            foreach (string eventName in this.sourceHandlers.Keys)
            {
                Type type = this.propertySource.Target.GetType();
                EventInfo ev = type.GetEvent(eventName);
                if (ev == null)
                {
                    return;
                }

                ev.RemoveEventHandler(this.propertySource.Target, this.sourceHandlers[eventName].Delegate);
            }

            this.sourceHandlers.Clear();
        }

        private void DetachTargetHandlers()
        {
            if (this.propertySource == null || !this.propertySource.IsAlive || this.propertySource.Target == null)
            {
                return;
            }

            foreach (string eventName in this.targetHandlers.Keys)
            {
                Type type = this.propertyTarget.Target.GetType();
                EventInfo ev = type.GetEvent(eventName);
                if (ev == null)
                {
                    return;
                }

                ev.RemoveEventHandler(this.propertyTarget.Target, this.targetHandlers[eventName].Delegate);
            }

            this.targetHandlers.Clear();
        }

        private static IList<PropertyAndName> GetPropertyChain(
            object topInstance,
            IList<PropertyAndName> instances,
            MemberExpression expression,
            string propertyName,
            bool resolveTopField,
            bool top = true)
        {
            if (instances == null)
            {
                instances = new List<PropertyAndName>();
            }

            if (!(expression.Expression is MemberExpression ex))
            {
                if (top)
                {
                    instances.Add(new PropertyAndName { Instance = topInstance, Name = propertyName });
                }

                return instances;
            }

            IList<PropertyAndName> list = GetPropertyChain(
                topInstance,
                instances,
                ex,
                propertyName,
                resolveTopField,
                false);

            if (list.Count == 0)
            {
                list.Add(new PropertyAndName { Instance = topInstance, });
            }

            if (top && list.Count > 0 && list.First().Instance != topInstance)
            {
                list.Insert(0, new PropertyAndName { Instance = topInstance });
            }

            PropertyAndName lastInstance = list.Last();

            if (lastInstance.Instance == null)
            {
                return list;
            }

            var prop = ex.Member as PropertyInfo;

            if (prop != null)
            {
                try
                {
                    object newInstance = prop.GetMethod.Invoke(lastInstance.Instance, new object[] { });

                    lastInstance.Name = prop.Name;

                    list.Add(new PropertyAndName { Instance = newInstance, });
                }
                catch (TargetException)
                {
                }
            }
            else
            {
                if (lastInstance.Instance == topInstance && resolveTopField)
                {
                    var field = ex.Member as FieldInfo;
                    if (field != null)
                    {
                        try
                        {
                            object newInstance = field.GetValue(lastInstance.Instance);

                            lastInstance.Name = field.Name;

                            list.Add(new PropertyAndName { Instance = newInstance, });
                        }
                        catch (ArgumentException)
                        {
                            throw new InvalidOperationException(
                                "Are you trying to use SetBinding with a local variable? "
                                + "Try to use new Binding instead");
                        }
                    }
                }
            }

            if (top)
            {
                list.Last().Name = propertyName;
            }

            return list;
        }

        private static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                return null;
            }

            if (!(propertyExpression.Body is MemberExpression body))
            {
                throw new ArgumentException("Invalid argument", nameof(propertyExpression));
            }

            var property = body.Member as PropertyInfo;

            if (property == null)
            {
                throw new ArgumentException("Argument is not a property", nameof(propertyExpression));
            }

            return property.Name;
        }

        private TTarget GetSourceValue()
        {
            if (this.sourceProperty == null)
            {
                return default(TTarget);
            }

            var sourceValue = (TSource)this.sourceProperty.GetValue(this.propertySource.Target, null);

            try
            {
                return this.converter.Convert(sourceValue);
            }
            catch (Exception)
            {
                if (!Equals(this.FallbackValue, default(TSource)))
                {
                    return this.converter.Convert(this.FallbackValue);
                }

                var targetValue = (TTarget)this.targetProperty.GetValue(this.propertyTarget.Target, null);
                return targetValue;
            }
        }

        private TSource GetTargetValue()
        {
            var targetValue = (TTarget)this.targetProperty.GetValue(this.propertyTarget.Target, null);

            try
            {
                return this.converter.ConvertBack(targetValue);
            }
            catch (Exception)
            {
                var sourceValue = (TSource)this.sourceProperty.GetValue(this.propertySource.Target, null);
                return sourceValue;
            }
        }

        private void HandleSourceEvent<TEventArgs>(object sender, TEventArgs args)
        {
            if (this.propertyTarget != null && this.propertyTarget.IsAlive && this.propertyTarget.Target != null
                && this.propertySource != null && this.propertySource.IsAlive && this.propertySource.Target != null
                && !this.settingTargetToSource)
            {
                TTarget valueLocal = this.GetSourceValue();
                object targetValue = this.targetProperty.GetValue(this.propertyTarget.Target, null);

                if (Equals(valueLocal, targetValue))
                {
                    return;
                }

                if (this.targetProperty != null)
                {
                    this.settingSourceToTarget = true;
                    this.SetTargetValue(valueLocal);
                    this.settingSourceToTarget = false;
                }
            }

            this.onSourceUpdate?.Execute();

            this.RaiseValueChanged();
        }

        private void HandleTargetEvent<TEventArgs>(object source, TEventArgs args)
        {
            if (this.propertyTarget != null && this.propertyTarget.IsAlive && this.propertyTarget.Target != null
                && this.propertySource != null && this.propertySource.IsAlive && this.propertySource.Target != null
                && !this.settingSourceToTarget)
            {
                TSource valueLocal = this.GetTargetValue();
                object sourceValue = this.sourceProperty.GetValue(this.propertySource.Target, null);

                if (Equals(valueLocal, sourceValue))
                {
                    return;
                }

                this.settingTargetToSource = true;
                this.SetSourceValue(valueLocal);
                this.settingTargetToSource = false;
            }

            this.RaiseValueChanged();
        }

        private bool IsSourceDefaultValue()
        {
            if (this.sourceProperty == null)
            {
                return true;
            }

            var sourceValue = (TSource)this.sourceProperty.GetValue(this.propertySource.Target, null);
            return Equals(default(TSource), sourceValue);
        }

        private bool IsValueType(Type type)
        {
            return type.IsPrimitive || type == typeof(string);
        }

        private void RaiseValueChanged()
        {
            EventHandler handler = this.ValueChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        private void SetSourceValue(TSource value)
        {
            this.sourceProperty.SetValue(this.propertySource.Target, value, null);
        }

        private bool SetSpecialValues()
        {
            if (this.isFallbackValueActive)
            {
                Type type = typeof(TTarget);
                object castedValue = Convert.ChangeType(this.FallbackValue, type);
                this.targetProperty.SetValue(this.propertyTarget.Target, castedValue, null);
                return true;
            }

            if (Equals(default(TTarget), this.TargetNullValue) || !this.IsSourceDefaultValue())
            {
                return false;
            }

            this.targetProperty.SetValue(
                this.propertyTarget.Target,
                this.converter.Convert(this.TargetNullValue),
                null);

            return true;
        }

        private void SetTargetValue(TTarget value)
        {
            if (!this.SetSpecialValues())
            {
                this.targetProperty.SetValue(this.propertyTarget.Target, value, null);
            }
        }

        /// <summary>
        /// Occurs when the value of the databound property changes.
        /// </summary>
        public override event EventHandler ValueChanged;

        internal class ObjectSwappedEventListener : IWeakEventListener
        {
            private readonly WeakReference bindingReference;

            public WeakReference InstanceReference
            {
                get;
            }

            public ObjectSwappedEventListener(Binding<TSource, TTarget> binding, INotifyPropertyChanged instance)
            {
                this.bindingReference = new WeakReference(binding);
                this.InstanceReference = new WeakReference(instance);
            }

            public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
            {
                if (this.InstanceReference.Target != sender || !(e is PropertyChangedEventArgs propArgs)
                                                            || this.bindingReference == null
                                                            || !this.bindingReference.IsAlive
                                                            || this.bindingReference.Target == null)
                {
                    return false;
                }

                var binding = ((Binding<TSource, TTarget>)this.bindingReference.Target);

                binding.Detach();

                binding.Attach(binding.Source, binding.Target, binding.Mode);

                return true;

            }
        }

        internal class PropertyAndName
        {
            public object Instance;

            public string Name;
        }

        internal class PropertyChangedEventListener : IWeakEventListener
        {
            private readonly WeakReference bindingReference;

            private readonly bool updateFromSourceToTarget;

            /// <summary>
            /// Gets a reference to the instance that this listener listens to.
            /// </summary>
            public WeakReference InstanceReference
            {
                get;
            }

            public PropertyChangedEventListener(
                Binding<TSource, TTarget> binding,
                INotifyPropertyChanged instance,
                bool updateFromSourceToTarget)
            {
                this.updateFromSourceToTarget = updateFromSourceToTarget;
                this.bindingReference = new WeakReference(binding);
                this.InstanceReference = new WeakReference(instance);
            }

            public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
            {
                if (this.bindingReference == null || !this.bindingReference.IsAlive
                                                  || this.bindingReference.Target == null)
                {
                    return false;
                }

                var binding = (Binding<TSource, TTarget>)this.bindingReference.Target;

                if (this.updateFromSourceToTarget)
                {
                    if (binding.propertySource == null || !binding.propertySource.IsAlive
                                                       || sender != binding.propertySource.Target)
                    {
                        return true;
                    }

                    if (!binding.settingTargetToSource)
                    {
                        binding.ForceUpdateValueFromSourceToTarget();
                    }

                    binding.settingTargetToSource = false;
                }
                else
                {
                    if (binding.propertyTarget == null || !binding.propertyTarget.IsAlive
                                                       || sender != binding.propertyTarget.Target)
                    {
                        return true;
                    }

                    if (!binding.settingSourceToTarget)
                    {
                        binding.ForceUpdateValueFromTargetToSource();
                    }

                    binding.settingSourceToTarget = false;
                }

                return true;
            }
        }

        private class DelegateInfo
        {
            public Delegate Delegate;

            public bool IsDefault;
        }

        private class SimpleConverter
        {
            private WeakFunc<TSource, TTarget> convert;

            private WeakFunc<TTarget, TSource> convertBack;

            public TTarget Convert(TSource value)
            {
                if (this.convert != null && this.convert.IsAlive)
                {
                    return this.convert.Execute(value);
                }

                try
                {
                    return (TTarget)System.Convert.ChangeType(value, typeof(TTarget));
                }
                catch (Exception)
                {
                    return default(TTarget);
                }
            }

            public TSource ConvertBack(TTarget value)
            {
                if (this.convertBack != null && this.convertBack.IsAlive)
                {
                    return this.convertBack.Execute(value);
                }

                try
                {
                    return (TSource)System.Convert.ChangeType(value, typeof(TSource));
                }
                catch (Exception)
                {
                    return default(TSource);
                }
            }

            public void SetConvert(Func<TSource, TTarget> convert)
            {
                this.convert = new WeakFunc<TSource, TTarget>(convert);
            }

            public void SetConvertBack(Func<TTarget, TSource> convertBack)
            {
                this.convertBack = new WeakFunc<TTarget, TSource>(convertBack);
            }
        }
    }
}
#endif