// ****************************************************************************
// <copyright file="RelayCommand`1.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>22.4.2009</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this project or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************
// <credits>This class was developed by Josh Smith (http://joshsmithonwpf.wordpress.com) and
// slightly modified with his permission.</credits>
// ****************************************************************************

#if NET45
namespace GalaSoft.MvvmLight.CommandWpf
#else
namespace GalaSoft.MvvmLight.Command
#endif
{
    using System;
    using System.Reflection;
    using System.Windows.Input;

    using GalaSoft.MvvmLight.Helpers;

    /// <summary>
    /// A generic command whose sole purpose is to relay its functionality to other
    /// objects by invoking delegates. The default return value for the CanExecute
    /// method is 'true'. This class allows you to accept command parameters in the
    /// Execute and CanExecute callback methods.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <remarks>If you are using this class in WPF4.5 or above, you need to use the 
    /// GalaSoft.MvvmLight.CommandWpf namespace (instead of GalaSoft.MvvmLight.Command).
    /// This will enable (or restore) the CommandManager class which handles
    /// automatic enabling/disabling of controls based on the CanExecute delegate.</remarks>
    ////[ClassInfo(typeof(RelayCommand))]
    public class RelayCommand<T> : ICommand
    {
        private readonly WeakAction<T> execute;

        private readonly WeakFunc<T, bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class that 
        /// can always execute.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: If the action causes a closure,
        /// you must set keepTargetAlive to true to avoid side effects. </param>
        /// <param name="keepTargetAlive">If true, the target of the Action will
        /// be kept as a hard reference, which might cause a memory leak. You should only set this
        /// parameter to true if the action is causing a closure. See
        /// http://galasoft.ch/s/mvvmweakaction. </param>
        /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action<T> execute, bool keepTargetAlive = false)
            : this(execute, null, keepTargetAlive)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: If the action causes a closure,
        /// you must set keepTargetAlive to true to avoid side effects. </param>
        /// <param name="canExecute">The execution status logic.  IMPORTANT: If the func causes a closure,
        /// you must set keepTargetAlive to true to avoid side effects. </param>
        /// <param name="keepTargetAlive">If true, the target of the Action will
        /// be kept as a hard reference, which might cause a memory leak. You should only set this
        /// parameter to true if the action is causing a closure. See
        /// http://galasoft.ch/s/mvvmweakaction. </param>
        /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute, bool keepTargetAlive = false)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = new WeakAction<T>(execute, keepTargetAlive);

            if (canExecute != null)
            {
                this.canExecute = new WeakFunc<T, bool>(canExecute, keepTargetAlive);
            }
        }

#if NET45
        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }
#else
        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;
#endif

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
#if NET45
            CommandManager.InvalidateRequerySuggested();
#else
            EventHandler handler = this.CanExecuteChanged;
            handler?.Invoke(this, EventArgs.Empty);
#endif
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data 
        /// to be passed, this object can be set to a null reference</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            if (this.canExecute == null)
            {
                return true;
            }

            if (!this.canExecute.IsStatic && !this.canExecute.IsAlive)
            {
                return false;
            }

            switch (parameter)
            {
                case null when typeof(T).GetTypeInfo().IsValueType:
                    return this.canExecute.Execute(default(T));
                case null:
                case T _:
                    return this.canExecute.Execute((T)parameter);
            }

            return false;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked. 
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data 
        /// to be passed, this object can be set to a null reference</param>
        public virtual void Execute(object parameter)
        {
            object val = parameter;

            if (parameter != null && parameter.GetType() != typeof(T))
            {
                if (parameter is IConvertible)
                {
                    val = Convert.ChangeType(parameter, typeof(T), null);
                }
            }

            if (!this.CanExecute(val) || this.execute == null || (!this.execute.IsStatic && !this.execute.IsAlive))
            {
                return;
            }

            if (val == null)
            {
                if (typeof(T).GetTypeInfo().IsValueType)
                {
                    this.execute.Execute(default(T));
                }
                else
                {
                    // ReSharper disable ExpressionIsAlwaysNull
                    this.execute.Execute((T)val);
                    // ReSharper restore ExpressionIsAlwaysNull
                }
            }
            else
            {
                this.execute.Execute((T)val);
            }
        }
    }
}