// ****************************************************************************
// <copyright file="Binding`2.Android.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>22.01.2016</date>
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
    using Android.Text;
    using Android.Views;
    using Android.Widget;

    // Partial class for Android only.
    public partial class Binding<TSource, TTarget>
    {
        /// <summary>
        /// Define that the binding should be evaluated when the bound control's source property changes. 
        /// Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to specify that the binding must be updated when the property changes.
        /// </summary>
        /// <remarks>This method should only be used with the following items:
        /// <para>- an EditText control and its Text property (TextChanged event).</para>
        /// <para>- a CompoundButton control and its Checked property (CheckedChange event).</para>
        /// </remarks>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime binding. Such bindings cannot be updated. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        public Binding<TSource, TTarget> ObserveSourceEvent()
        {
            return this.ObserveSourceEvent(UpdateTriggerMode.PropertyChanged);
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound source object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <param name="mode">Defines the binding's update mode. Use 
        /// <see cref="UpdateTriggerMode.LostFocus"/> to update the binding when
        /// the source control loses the focus. You can also use
        /// <see cref="UpdateTriggerMode.PropertyChanged"/> to update the binding
        /// when the source control's property changes.
        /// The PropertyChanged mode should only be used with the following items:
        /// <para>- an EditText control and its Text property (TextChanged event).</para>
        /// <para>- a CompoundButton control and its Checked property (CheckedChange event).</para>
        /// </param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime binding. Such bindings cannot be updated. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        public Binding<TSource, TTarget> ObserveSourceEvent(UpdateTriggerMode mode)
        {
            switch (mode)
            {
                case UpdateTriggerMode.LostFocus:
                    return this.ObserveSourceEvent<View.FocusChangeEventArgs>("FocusChange");

                case UpdateTriggerMode.PropertyChanged:
                    return this.CheckControlSource();
            }

            return this;
        }

        /// <summary>
        /// Define that the binding should be evaluated when the bound control's target property changes. 
        /// Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to specify that the binding must be updated when the property changes.
        /// </summary>
        /// <remarks>This method should only be used with the following items:
        /// <para>- an EditText control and its Text property (TextChanged event).</para>
        /// <para>- a CompoundButton control and its Checked property (CheckedChange event).</para>
        /// </remarks>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime or a OneWay binding. This exception can
        /// also be thrown when the target object is null or has already been
        /// garbage collected before this method is called.</exception>
        public Binding<TSource, TTarget> ObserveTargetEvent()
        {
            return this.ObserveTargetEvent(UpdateTriggerMode.PropertyChanged);
        }

        /// <summary>
        /// Define when the binding should be evaluated when the bound target object
        /// is a control. Because Xamarin controls are not DependencyObjects, the
        /// bound property will not automatically update the binding attached to it. Instead,
        /// use this method to define which of the control's events should be observed.
        /// </summary>
        /// <param name="mode">Defines the binding's update mode. Use 
        /// <see cref="UpdateTriggerMode.LostFocus"/> to update the binding when
        /// the source control loses the focus. You can also use
        /// <see cref="UpdateTriggerMode.PropertyChanged"/> to update the binding
        /// when the source control's property changes.
        /// The PropertyChanged mode should only be used with the following items:
        /// <para>- an EditText control and its Text property (TextChanged event).</para>
        /// <para>- a CompoundButton control and its Checked property (CheckedChange event).</para>
        /// </param>
        /// <returns>The Binding instance.</returns>
        /// <exception cref="InvalidOperationException">When this method is called
        /// on a OneTime or a OneWay binding. This exception can
        /// also be thrown when the source object is null or has already been
        /// garbage collected before this method is called.</exception>
        public Binding<TSource, TTarget> ObserveTargetEvent(UpdateTriggerMode mode)
        {
            switch (mode)
            {
                case UpdateTriggerMode.LostFocus:
                    return this.ObserveTargetEvent<View.FocusChangeEventArgs>("FocusChange");

                case UpdateTriggerMode.PropertyChanged:
                    return this.CheckControlTarget();
            }

            return this;
        }

        private Binding<TSource, TTarget> CheckControlSource()
        {
            if (this.propertySource.Target is EditText)
            {
                Binding<TSource, TTarget> binding = this.ObserveSourceEvent<TextChangedEventArgs>("TextChanged");
                binding.sourceHandlers["TextChanged"].IsDefault = true;
                return binding;
            }

            if (this.propertySource.Target is CompoundButton)
            {
                Binding<TSource, TTarget> binding = this.ObserveSourceEvent<CompoundButton.CheckedChangeEventArgs>("CheckedChange");
                binding.sourceHandlers["CheckedChange"].IsDefault = true;
                return binding;
            }

            return this;
        }

        private Binding<TSource, TTarget> CheckControlTarget()
        {
            if (this.Mode != BindingMode.TwoWay)
            {
                return this;
            }

            if (this.propertyTarget.Target is EditText)
            {
                Binding<TSource, TTarget> binding = this.ObserveTargetEvent<TextChangedEventArgs>("TextChanged");
                binding.targetHandlers["TextChanged"].IsDefault = true;
                return binding;
            }

            if (this.propertyTarget.Target is CompoundButton)
            {
                Binding<TSource, TTarget> binding = this.ObserveTargetEvent<CompoundButton.CheckedChangeEventArgs>("CheckedChange");
                binding.targetHandlers["CheckedChange"].IsDefault = true;
                return binding;
            }

            return this;
        }
    }
}
#endif