// ****************************************************************************
// <copyright file="SimpleIoc.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2011-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>10.4.2011</date>
// <project>GalaSoft.MvvmLight.Extras</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this project or http://www.galasoft.ch/license_MIT.txt
// </license>
// <LastBaseLevel>BL0005</LastBaseLevel>
// ****************************************************************************

namespace GalaSoft.MvvmLight.Ioc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A very simple IOC container with basic functionality needed to register and resolve
    /// instances. If needed, this class can be replaced by another more elaborate
    /// IOC container implementing the IServiceLocator interface.
    /// The inspiration for this class is at https://gist.github.com/716137 but it has
    /// been extended with additional features.
    /// </summary>
    //// [ClassInfo(typeof(SimpleIoc),
    ////  VersionString = "5.4.10",
    ////  DateString = "201801022330",
    ////  Description = "A very simple IOC container.",
    ////  UrlContacts = "http://www.galasoft.ch/contact_en.html",
    ////  Email = "laurent@galasoft.ch")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ioc")]
    public class SimpleIoc : ISimpleIoc
    {
        private static readonly object InstanceLock = new object();

        private static SimpleIoc defaultInstance;

        private readonly Dictionary<Type, ConstructorInfo> constructorInfos = new Dictionary<Type, ConstructorInfo>();

        private readonly string defaultKey = Guid.NewGuid().ToString();

        private readonly object[] emptyArguments = new object[0];

        private readonly Dictionary<Type, Dictionary<string, Delegate>> factories =
            new Dictionary<Type, Dictionary<string, Delegate>>();

        private readonly Dictionary<Type, Dictionary<string, object>> instancesRegistry =
            new Dictionary<Type, Dictionary<string, object>>();

        private readonly Dictionary<Type, Type> interfaceToClassMap = new Dictionary<Type, Type>();

        private readonly object syncLock = new object();

        /// <summary>
        /// Gets the class' default instance.
        /// </summary>
        public static SimpleIoc Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    lock (InstanceLock)
                    {
                        if (defaultInstance == null)
                        {
                            defaultInstance = new SimpleIoc();
                        }
                    }
                }

                return defaultInstance;
            }
        }

        /// <summary>
        /// Checks whether at least one instance of a given class is already created in the container.
        /// </summary>
        /// <typeparam name="TClass">The class that is queried.</typeparam>
        /// <returns>True if at least on instance of the class is already created, false otherwise.</returns>
        public bool ContainsCreated<TClass>()
        {
            return this.ContainsCreated<TClass>(null);
        }

        /// <summary>
        /// Checks whether the instance with the given key is already created for a given class
        /// in the container.
        /// </summary>
        /// <typeparam name="TClass">The class that is queried.</typeparam>
        /// <param name="key">The key that is queried.</param>
        /// <returns>True if the instance with the given key is already registered for the given class,
        /// false otherwise.</returns>
        public bool ContainsCreated<TClass>(string key)
        {
            Type classType = typeof(TClass);

            if (!this.instancesRegistry.ContainsKey(classType))
            {
                return false;
            }

            if (string.IsNullOrEmpty(key))
            {
                return this.instancesRegistry[classType].Count > 0;
            }

            return this.instancesRegistry[classType].ContainsKey(key);
        }

        /// <summary>
        /// Gets a value indicating whether a given type T is already registered.
        /// </summary>
        /// <typeparam name="T">The type that the method checks for.</typeparam>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public bool IsRegistered<T>()
        {
            Type classType = typeof(T);
            return this.interfaceToClassMap.ContainsKey(classType);
        }

        /// <summary>
        /// Gets a value indicating whether a given type T and a give key
        /// are already registered.
        /// </summary>
        /// <typeparam name="T">The type that the method checks for.</typeparam>
        /// <param name="key">The key that the method checks for.</param>
        /// <returns>True if the type and key are registered, false otherwise.</returns>
        public bool IsRegistered<T>(string key)
        {
            Type classType = typeof(T);

            if (!this.interfaceToClassMap.ContainsKey(classType) || !this.factories.ContainsKey(classType))
            {
                return false;
            }

            return this.factories[classType].ContainsKey(key);
        }

        /// <summary>
        /// Registers a given type for a given interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface for which instances will be resolved.</typeparam>
        /// <typeparam name="TClass">The type that must be used to create instances.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Register<TInterface, TClass>()
            where TInterface : class where TClass : class, TInterface
        {
            this.Register<TInterface, TClass>(false);
        }

        /// <summary>
        /// Registers a given type for a given interface with the possibility for immediate
        /// creation of the instance.
        /// </summary>
        /// <typeparam name="TInterface">The interface for which instances will be resolved.</typeparam>
        /// <typeparam name="TClass">The type that must be used to create instances.</typeparam>
        /// <param name="createInstanceImmediately">If true, forces the creation of the default
        /// instance of the provided class.</param>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Register<TInterface, TClass>(bool createInstanceImmediately)
            where TInterface : class where TClass : class, TInterface
        {
            lock (this.syncLock)
            {
                Type interfaceType = typeof(TInterface);
                Type classType = typeof(TClass);

                if (this.interfaceToClassMap.ContainsKey(interfaceType))
                {
                    if (this.interfaceToClassMap[interfaceType] != classType)
                    {
#if DEBUG
                        // Avoid some issues in the designer when the ViewModelLocator is instantiated twice
                        if (!Helpers.DesignerLibrary.IsInDesignMode)
                        {
#endif
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "There is already a class registered for {0}.",
                                    interfaceType.FullName));
#if DEBUG
                        }
#endif
                    }
                }
                else
                {
                    this.interfaceToClassMap.Add(interfaceType, classType);
                    this.constructorInfos.Add(classType, this.GetConstructorInfo(classType));
                }

                Func<TInterface> factory = this.MakeInstance<TInterface>;
                this.DoRegister(interfaceType, factory, this.defaultKey);

                if (createInstanceImmediately)
                {
                    this.GetInstance<TInterface>();
                }
            }
        }

        /// <summary>
        /// Registers a given type.
        /// </summary>
        /// <typeparam name="TClass">The type that must be used to create instances.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Register<TClass>()
            where TClass : class
        {
            this.Register<TClass>(false);
        }

        /// <summary>
        /// Registers a given type with the possibility for immediate
        /// creation of the instance.
        /// </summary>
        /// <typeparam name="TClass">The type that must be used to create instances.</typeparam>
        /// <param name="createInstanceImmediately">If true, forces the creation of the default
        /// instance of the provided class.</param>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Register<TClass>(bool createInstanceImmediately)
            where TClass : class
        {
            Type classType = typeof(TClass);
            if (classType.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("An interface cannot be registered alone.");
            }

            lock (this.syncLock)
            {
                if (this.factories.ContainsKey(classType) && this.factories[classType].ContainsKey(this.defaultKey))
                {
                    if (!this.constructorInfos.ContainsKey(classType))
                    {
#if DEBUG
                        // Avoid some issues in the designer when the ViewModelLocator is instantiated twice
                        if (!Helpers.DesignerLibrary.IsInDesignMode)
                        {
#endif

                            // Throw only if constructorinfos have not been
                            // registered, which means there is a default factory
                            // for this class.
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Class {0} is already registered.",
                                    classType));
#if DEBUG
                        }
#endif
                    }

                    return;
                }

                if (!this.interfaceToClassMap.ContainsKey(classType))
                {
                    this.interfaceToClassMap.Add(classType, null);
                }

                this.constructorInfos.Add(classType, this.GetConstructorInfo(classType));
                Func<TClass> factory = this.MakeInstance<TClass>;
                this.DoRegister(classType, factory, this.defaultKey);

                if (createInstanceImmediately)
                {
                    this.GetInstance<TClass>();
                }
            }
        }

        /// <summary>
        /// Registers a given instance for a given type.
        /// </summary>
        /// <typeparam name="TClass">The type that is being registered.</typeparam>
        /// <param name="factory">The factory method able to create the instance that
        /// must be returned when the given type is resolved.</param>
        public void Register<TClass>(Func<TClass> factory)
            where TClass : class
        {
            this.Register(factory, false);
        }

        /// <summary>
        /// Registers a given instance for a given type with the possibility for immediate
        /// creation of the instance.
        /// </summary>
        /// <typeparam name="TClass">The type that is being registered.</typeparam>
        /// <param name="factory">The factory method able to create the instance that
        /// must be returned when the given type is resolved.</param>
        /// <param name="createInstanceImmediately">If true, forces the creation of the default
        /// instance of the provided class.</param>
        public void Register<TClass>(Func<TClass> factory, bool createInstanceImmediately)
            where TClass : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (this.syncLock)
            {
                Type classType = typeof(TClass);

                if (this.factories.ContainsKey(classType) && this.factories[classType].ContainsKey(this.defaultKey))
                {
#if DEBUG
                    // Avoid some issues in the designer when the ViewModelLocator is instantiated twice
                    if (!Helpers.DesignerLibrary.IsInDesignMode)
                    {
#endif
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "There is already a factory registered for {0}.",
                                classType.FullName));
#if DEBUG
                    }
#endif
                }

                if (!this.interfaceToClassMap.ContainsKey(classType))
                {
                    this.interfaceToClassMap.Add(classType, null);
                }

                this.DoRegister(classType, factory, this.defaultKey);

                if (createInstanceImmediately)
                {
                    this.GetInstance<TClass>();
                }
            }
        }

        /// <summary>
        /// Registers a given instance for a given type and a given key.
        /// </summary>
        /// <typeparam name="TClass">The type that is being registered.</typeparam>
        /// <param name="factory">The factory method able to create the instance that
        /// must be returned when the given type is resolved.</param>
        /// <param name="key">The key for which the given instance is registered.</param>
        public void Register<TClass>(Func<TClass> factory, string key)
            where TClass : class
        {
            this.Register(factory, key, false);
        }

        /// <summary>
        /// Registers a given instance for a given type and a given key with the possibility for immediate
        /// creation of the instance.
        /// </summary>
        /// <typeparam name="TClass">The type that is being registered.</typeparam>
        /// <param name="factory">The factory method able to create the instance that
        /// must be returned when the given type is resolved.</param>
        /// <param name="key">The key for which the given instance is registered.</param>
        /// <param name="createInstanceImmediately">If true, forces the creation of the default
        /// instance of the provided class.</param>
        public void Register<TClass>(Func<TClass> factory, string key, bool createInstanceImmediately)
            where TClass : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (this.syncLock)
            {
                Type classType = typeof(TClass);

                if (this.factories.ContainsKey(classType) && this.factories[classType].ContainsKey(key))
                {
#if DEBUG
                    // Avoid some issues in the designer when the ViewModelLocator is instantiated twice
                    if (!Helpers.DesignerLibrary.IsInDesignMode)
                    {
#endif
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "There is already a factory registered for {0} with key {1}.",
                                classType.FullName,
                                key));
#if DEBUG
                    }
#endif
                }

                if (!this.interfaceToClassMap.ContainsKey(classType))
                {
                    this.interfaceToClassMap.Add(classType, null);
                }

                this.DoRegister(classType, factory, key);

                if (createInstanceImmediately)
                {
                    this.GetInstance<TClass>(key);
                }
            }
        }

        /// <summary>
        /// Resets the instance in its original states. This deletes all the
        /// registrations.
        /// </summary>
        public void Reset()
        {
            this.interfaceToClassMap.Clear();
            this.instancesRegistry.Clear();
            this.constructorInfos.Clear();
            this.factories.Clear();
        }

        /// <summary>
        /// Unregisters a class from the cache and removes all the previously
        /// created instances.
        /// </summary>
        /// <typeparam name="TClass">The class that must be removed.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Unregister<TClass>()
            where TClass : class
        {
            lock (this.syncLock)
            {
                Type serviceType = typeof(TClass);
                Type resolveTo;

                if (this.interfaceToClassMap.ContainsKey(serviceType))
                {
                    resolveTo = this.interfaceToClassMap[serviceType] ?? serviceType;
                }
                else
                {
                    resolveTo = serviceType;
                }

                if (this.instancesRegistry.ContainsKey(serviceType))
                {
                    this.instancesRegistry.Remove(serviceType);
                }

                if (this.interfaceToClassMap.ContainsKey(serviceType))
                {
                    this.interfaceToClassMap.Remove(serviceType);
                }

                if (this.factories.ContainsKey(serviceType))
                {
                    this.factories.Remove(serviceType);
                }

                if (this.constructorInfos.ContainsKey(resolveTo))
                {
                    this.constructorInfos.Remove(resolveTo);
                }
            }
        }

        /// <summary>
        /// Removes the given instance from the cache. The class itself remains
        /// registered and can be used to create other instances.
        /// </summary>
        /// <typeparam name="TClass">The type of the instance to be removed.</typeparam>
        /// <param name="instance">The instance that must be removed.</param>
        public void Unregister<TClass>(TClass instance)
            where TClass : class
        {
            lock (this.syncLock)
            {
                Type classType = typeof(TClass);

                if (!this.instancesRegistry.ContainsKey(classType))
                {
                    return;
                }

                Dictionary<string, object> list = this.instancesRegistry[classType];

                List<KeyValuePair<string, object>> pairs = list.Where(pair => pair.Value == instance).ToList();
                foreach (string key in pairs.Select(kvp => kvp.Key))
                {
                    list.Remove(key);
                }
            }
        }

        /// <summary>
        /// Removes the instance corresponding to the given key from the cache. The class itself remains
        /// registered and can be used to create other instances.
        /// </summary>
        /// <typeparam name="TClass">The type of the instance to be removed.</typeparam>
        /// <param name="key">The key corresponding to the instance that must be removed.</param>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "This syntax is better than the alternatives.")]
        public void Unregister<TClass>(string key)
            where TClass : class
        {
            lock (this.syncLock)
            {
                Type classType = typeof(TClass);

                if (this.instancesRegistry.ContainsKey(classType))
                {
                    Dictionary<string, object> list = this.instancesRegistry[classType];

                    List<KeyValuePair<string, object>> pairs = list.Where(pair => pair.Key == key).ToList();
                    foreach (KeyValuePair<string, object> kvp in pairs)
                    {
                        list.Remove(kvp.Key);
                    }
                }

                if (!this.factories.ContainsKey(classType))
                {
                    return;
                }

                if (this.factories[classType].ContainsKey(key))
                {
                    this.factories[classType].Remove(key);
                }
            }
        }

        /// <summary>
        /// Provides a way to get all the created instances of a given type available in the
        /// cache. Registering a class or a factory does not automatically
        /// create the corresponding instance! To create an instance, either register
        /// the class or the factory with createInstanceImmediately set to true,
        /// or call the GetInstance method before calling GetAllCreatedInstances.
        /// Alternatively, use the GetAllInstances method, which auto-creates default
        /// instances for all registered classes.
        /// </summary>
        /// <param name="serviceType">The class of which all instances
        /// must be returned.</param>
        /// <returns>All the already created instances of the given type.</returns>
        public IEnumerable<object> GetAllCreatedInstances(Type serviceType)
        {
            if (this.instancesRegistry.ContainsKey(serviceType))
            {
                return this.instancesRegistry[serviceType].Values;
            }

            return new List<object>();
        }

        /// <summary>
        /// Provides a way to get all the created instances of a given type available in the
        /// cache. Registering a class or a factory does not automatically
        /// create the corresponding instance! To create an instance, either register
        /// the class or the factory with createInstanceImmediately set to true,
        /// or call the GetInstance method before calling GetAllCreatedInstances.
        /// Alternatively, use the GetAllInstances method, which auto-creates default
        /// instances for all registered classes.
        /// </summary>
        /// <typeparam name="TService">The class of which all instances
        /// must be returned.</typeparam>
        /// <returns>All the already created instances of the given type.</returns>
        public IEnumerable<TService> GetAllCreatedInstances<TService>()
        {
            Type serviceType = typeof(TService);
            return this.GetAllCreatedInstances(serviceType).Select(instance => (TService)instance);
        }

        #region Implementation of IServiceProvider

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type serviceType has not
        /// been registered before calling this method.</exception>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.
        /// </returns>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        public object GetService(Type serviceType)
        {
            return this.DoGetService(serviceType, this.defaultKey);
        }

        #endregion

        #region Implementation of IServiceLocator

        /// <summary>
        /// Provides a way to get all the created instances of a given type available in the
        /// cache. Calling this method auto-creates default
        /// instances for all registered classes.
        /// </summary>
        /// <param name="serviceType">The class of which all instances
        /// must be returned.</param>
        /// <returns>All the instances of the given type.</returns>
        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            lock (this.factories)
            {
                if (this.factories.ContainsKey(serviceType))
                {
                    foreach (KeyValuePair<string, Delegate> factory in this.factories[serviceType])
                    {
                        this.GetInstance(serviceType, factory.Key);
                    }
                }
            }

            if (this.instancesRegistry.ContainsKey(serviceType))
            {
                return this.instancesRegistry[serviceType].Values;
            }

            return new List<object>();
        }

        /// <summary>
        /// Provides a way to get all the created instances of a given type available in the
        /// cache. Calling this method auto-creates default
        /// instances for all registered classes.
        /// </summary>
        /// <typeparam name="TService">The class of which all instances
        /// must be returned.</typeparam>
        /// <returns>All the instances of the given type.</returns>
        public IEnumerable<TService> GetAllInstances<TService>()
        {
            Type serviceType = typeof(TService);
            return this.GetAllInstances(serviceType).Select(instance => (TService)instance);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. If no instance had been instantiated 
        /// before, a new instance will be created. If an instance had already
        /// been created, that same instance will be returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type serviceType has not
        /// been registered before calling this method.</exception>
        /// <param name="serviceType">The class of which an instance
        /// must be returned.</param>
        /// <returns>An instance of the given type.</returns>
        public object GetInstance(Type serviceType)
        {
            return this.DoGetService(serviceType, this.defaultKey);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. This method
        /// always returns a new instance and doesn't cache it in the IOC container.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type serviceType has not
        /// been registered before calling this method.</exception>
        /// <param name="serviceType">The class of which an instance
        /// must be returned.</param>
        /// <returns>An instance of the given type.</returns>
        public object GetInstanceWithoutCaching(Type serviceType)
        {
            return this.DoGetService(serviceType, this.defaultKey, false);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type corresponding
        /// to a given key. If no instance had been instantiated with this
        /// key before, a new instance will be created. If an instance had already
        /// been created with the same key, that same instance will be returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type serviceType has not
        /// been registered before calling this method.</exception>
        /// <param name="serviceType">The class of which an instance must be returned.</param>
        /// <param name="key">The key uniquely identifying this instance.</param>
        /// <returns>An instance corresponding to the given type and key.</returns>
        public object GetInstance(Type serviceType, string key)
        {
            return this.DoGetService(serviceType, key);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. This method
        /// always returns a new instance and doesn't cache it in the IOC container.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type serviceType has not
        /// been registered before calling this method.</exception>
        /// <param name="serviceType">The class of which an instance must be returned.</param>
        /// <param name="key">The key uniquely identifying this instance.</param>
        /// <returns>An instance corresponding to the given type and key.</returns>
        public object GetInstanceWithoutCaching(Type serviceType, string key)
        {
            return this.DoGetService(serviceType, key, false);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. If no instance had been instantiated 
        /// before, a new instance will be created. If an instance had already
        /// been created, that same instance will be returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type TService has not
        /// been registered before calling this method.</exception>
        /// <typeparam name="TService">The class of which an instance
        /// must be returned.</typeparam>
        /// <returns>An instance of the given type.</returns>
        public TService GetInstance<TService>()
        {
            return (TService)this.DoGetService(typeof(TService), this.defaultKey);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. This method
        /// always returns a new instance and doesn't cache it in the IOC container.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type TService has not
        /// been registered before calling this method.</exception>
        /// <typeparam name="TService">The class of which an instance
        /// must be returned.</typeparam>
        /// <returns>An instance of the given type.</returns>
        public TService GetInstanceWithoutCaching<TService>()
        {
            return (TService)this.DoGetService(typeof(TService), this.defaultKey, false);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type corresponding
        /// to a given key. If no instance had been instantiated with this
        /// key before, a new instance will be created. If an instance had already
        /// been created with the same key, that same instance will be returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type TService has not
        /// been registered before calling this method.</exception>
        /// <typeparam name="TService">The class of which an instance must be returned.</typeparam>
        /// <param name="key">The key uniquely identifying this instance.</param>
        /// <returns>An instance corresponding to the given type and key.</returns>
        public TService GetInstance<TService>(string key)
        {
            return (TService)this.DoGetService(typeof(TService), key);
        }

        /// <summary>
        /// Provides a way to get an instance of a given type. This method
        /// always returns a new instance and doesn't cache it in the IOC container.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the type TService has not
        /// been registered before calling this method.</exception>
        /// <typeparam name="TService">The class of which an instance must be returned.</typeparam>
        /// <param name="key">The key uniquely identifying this instance.</param>
        /// <returns>An instance corresponding to the given type and key.</returns>
        public TService GetInstanceWithoutCaching<TService>(string key)
        {
            return (TService)this.DoGetService(typeof(TService), key, false);
        }

        #endregion

        [SuppressMessage(
            "Microsoft.Naming",
            "CA2204:Literals should be spelled correctly",
            MessageId = "PreferredConstructor")]
        private static ConstructorInfo GetPreferredConstructorInfo(
            IEnumerable<ConstructorInfo> constructorInfos,
            Type resolveTo)
        {
            ConstructorInfo preferredConstructorInfo =
                (from t in constructorInfos
                 let attribute = t.GetCustomAttribute(typeof(PreferredConstructorAttribute))
                 where attribute != null
                 select t).FirstOrDefault();

            if (preferredConstructorInfo == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot register: Multiple constructors found in {0} but none marked with PreferredConstructor.",
                        resolveTo.Name));
            }

            return preferredConstructorInfo;
        }

        private object DoGetService(Type serviceType, string key, bool cache = true)
        {
            lock (this.syncLock)
            {
                if (string.IsNullOrEmpty(key))
                {
                    key = this.defaultKey;
                }

                Dictionary<string, object> instances = null;

                if (!this.instancesRegistry.ContainsKey(serviceType))
                {
                    if (!this.interfaceToClassMap.ContainsKey(serviceType))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Type not found in cache: {0}.",
                                serviceType.FullName));
                    }

                    if (cache)
                    {
                        instances = new Dictionary<string, object>();
                        this.instancesRegistry.Add(serviceType, instances);
                    }
                }
                else
                {
                    instances = this.instancesRegistry[serviceType];
                }

                if (instances != null && instances.ContainsKey(key))
                {
                    return instances[key];
                }

                object instance = null;

                if (this.factories.ContainsKey(serviceType))
                {
                    if (this.factories[serviceType].ContainsKey(key))
                    {
                        instance = this.factories[serviceType][key].DynamicInvoke(null);
                    }
                    else
                    {
                        if (this.factories[serviceType].ContainsKey(this.defaultKey))
                        {
                            instance = this.factories[serviceType][this.defaultKey].DynamicInvoke(null);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Type not found in cache without a key: {0}",
                                    serviceType.FullName));
                        }
                    }
                }

                if (cache)
                {
                    instances?.Add(key, instance);
                }

                return instance;
            }
        }

        private void DoRegister<TClass>(Type classType, Func<TClass> factory, string key)
        {
            if (this.factories.ContainsKey(classType))
            {
                if (this.factories[classType].ContainsKey(key))
                {
                    // The class is already registered, ignore and continue.
                    return;
                }

                this.factories[classType].Add(key, factory);
            }
            else
            {
                var list = new Dictionary<string, Delegate> { { key, factory } };

                this.factories.Add(classType, list);
            }
        }

        private ConstructorInfo GetConstructorInfo(Type serviceType)
        {
            Type resolveTo;

            if (this.interfaceToClassMap.ContainsKey(serviceType))
            {
                resolveTo = this.interfaceToClassMap[serviceType] ?? serviceType;
            }
            else
            {
                resolveTo = serviceType;
            }

            ConstructorInfo[] constructorInfos =
                resolveTo.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic).ToArray();

            if (constructorInfos.Length > 1)
            {
                if (constructorInfos.Length > 2)
                {
                    return GetPreferredConstructorInfo(constructorInfos, resolveTo);
                }

                if (constructorInfos.FirstOrDefault(i => i.Name == ".cctor") == null)
                {
                    return GetPreferredConstructorInfo(constructorInfos, resolveTo);
                }

                ConstructorInfo first = constructorInfos.FirstOrDefault(i => i.Name != ".cctor");

                if (first == null || !first.IsPublic)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cannot register: No public constructor found in {0}.",
                            resolveTo.Name));
                }

                return first;
            }

            if (constructorInfos.Length == 0 || (constructorInfos.Length == 1 && !constructorInfos[0].IsPublic))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot register: No public constructor found in {0}.",
                        resolveTo.Name));
            }

            return constructorInfos[0];
        }


        private TClass MakeInstance<TClass>()
        {
            Type serviceType = typeof(TClass);

            ConstructorInfo constructor = this.constructorInfos.ContainsKey(serviceType)
                                              ? this.constructorInfos[serviceType]
                                              : this.GetConstructorInfo(serviceType);

            ParameterInfo[] parameterInfos = constructor.GetParameters();

            if (parameterInfos.Length == 0)
            {
                return (TClass)constructor.Invoke(this.emptyArguments);
            }

            var parameters = new object[parameterInfos.Length];

            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                parameters[parameterInfo.Position] = this.GetService(parameterInfo.ParameterType);
            }

            return (TClass)constructor.Invoke(parameters);
        }
    }
}