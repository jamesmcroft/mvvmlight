// ****************************************************************************
// <copyright file="DesignerLibrary.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2016
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>30.09.2014</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.mvvmlight.net</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

namespace GalaSoft.MvvmLight.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Helper class for platform detection.
    /// </summary>
    internal static class DesignerLibrary
    {
        private static DesignerPlatformLibrary? detectedDesignerPlatformLibrary;

        private static bool? isInDesignMode;

        /// <summary>
        /// Gets a value indicating whether the code is being executed in design mode.
        /// </summary>
        public static bool IsInDesignMode
        {
            get
            {
                if (!isInDesignMode.HasValue)
                {
                    isInDesignMode = IsInDesignModePortable();
                }

                return isInDesignMode.Value;
            }
        }

        internal static DesignerPlatformLibrary DetectedDesignerLibrary
        {
            get
            {
                if (detectedDesignerPlatformLibrary == null)
                {
                    detectedDesignerPlatformLibrary = GetCurrentPlatform();
                }

                return detectedDesignerPlatformLibrary.Value;
            }
        }

        private static DesignerPlatformLibrary GetCurrentPlatform()
        {
            // We check Silverlight first because when in the VS designer, the .NET libraries will resolve
            // If we can resolve the SL libs, then we're in SL or WP
            // Then we check .NET because .NET will load the WinRT library (even though it can't really run it)
            // When running in WinRT, it will not load the PresentationFramework lib

            // Check Silverlight
            try
            {
                Type silverlightDesignerPropertiesType = Type.GetType(
                    "System.ComponentModel.DesignerProperties, System.Windows, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
                if (silverlightDesignerPropertiesType != null)
                {
                    return DesignerPlatformLibrary.Silverlight;
                }
            }
            catch
            {
                // Fix for https://github.com/lbugnion/mvvmlight/issues/41
                // Ignore exceptions here and fall through to the next check
            }

            // Check .NET 
            Type dotNetDesignerPropertiesType = Type.GetType(
                "System.ComponentModel.DesignerProperties, PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            if (dotNetDesignerPropertiesType != null)
            {
                // loaded the assembly, could be .net 
                return DesignerPlatformLibrary.Net;
            }

            // Check WinRT next
            Type winrtDesignModeType =
                Type.GetType("Windows.ApplicationModel.DesignMode, Windows, ContentType=WindowsRuntime");
            if (winrtDesignModeType != null)
            {
                return DesignerPlatformLibrary.WinRt;
            }

            return DesignerPlatformLibrary.Unknown;
        }

        private static bool IsInDesignModePortable()
        {
            // As a portable lib, we need see what framework we're running on and use reflection to get the designer value.
            DesignerPlatformLibrary platform = DesignerLibrary.DetectedDesignerLibrary;

            switch (platform)
            {
                case DesignerPlatformLibrary.WinRt:
                    return IsInDesignModeMetro();
                case DesignerPlatformLibrary.Silverlight:
                    bool desMode = IsInDesignModeSilverlight();
                    if (!desMode)
                    {
                        desMode = IsInDesignModeNet(); // hard to tell these apart in the designer
                    }

                    return desMode;
                case DesignerPlatformLibrary.Net:
                    return IsInDesignModeNet();
                default:
                    return false;
            }
        }

        private static bool IsInDesignModeSilverlight()
        {
            try
            {
                Type silverlightDesignerPropertiesType = Type.GetType(
                    "System.ComponentModel.DesignerProperties, System.Windows, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");

                PropertyInfo isInDesignToolProperty = silverlightDesignerPropertiesType?.GetTypeInfo()
                    .GetDeclaredProperty("IsInDesignTool");

                if (isInDesignToolProperty == null)
                {
                    return false;
                }

                return (bool)isInDesignToolProperty.GetValue(null, null);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsInDesignModeMetro()
        {
            try
            {
                Type winrtDesignModeType = Type.GetType(
                    "Windows.ApplicationModel.DesignMode, Windows, ContentType=WindowsRuntime");

                PropertyInfo designModeEnabledProperty =
                    winrtDesignModeType.GetTypeInfo().GetDeclaredProperty("DesignModeEnabled");
                return (bool)designModeEnabledProperty.GetValue(null, null);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsInDesignModeNet()
        {
            try
            {
                Type dotNetDesignerPropertiesType = Type.GetType(
                    "System.ComponentModel.DesignerProperties, PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                if (dotNetDesignerPropertiesType == null)
                {
                    return false;
                }

                object isInDesignModeValue = dotNetDesignerPropertiesType.GetTypeInfo()
                    .GetDeclaredField("IsInDesignModeProperty").GetValue(null);

                Type dependencyPropertyDescriptorType = Type.GetType(
                    "System.ComponentModel.DependencyPropertyDescriptor, WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                Type frameworkElementType = Type.GetType(
                    "System.Windows.FrameworkElement, PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                if (dependencyPropertyDescriptorType == null || frameworkElementType == null)
                {
                    return false;
                }

                List<MethodInfo> fromPropertyMethods = dependencyPropertyDescriptorType.GetTypeInfo()
                    .GetDeclaredMethods("FromProperty").ToList();

                if (fromPropertyMethods == null || fromPropertyMethods.Count == 0)
                {
                    return false;
                }

                MethodInfo fromPropertyMethod = fromPropertyMethods.FirstOrDefault(
                    mi => mi.IsPublic && mi.IsStatic && mi.GetParameters().Length == 2);

                object descriptor = fromPropertyMethod?.Invoke(
                    null,
                    new[] { isInDesignModeValue, frameworkElementType });

                if (descriptor == null)
                {
                    return false;
                }

                PropertyInfo metadataProperty =
                    dependencyPropertyDescriptorType.GetTypeInfo().GetDeclaredProperty("Metadata");

                if (metadataProperty == null)
                {
                    return false;
                }

                object metadataValue = metadataProperty.GetValue(descriptor, null);
                Type propertyMetadataType = Type.GetType(
                    "System.Windows.PropertyMetadata, WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                if (metadataValue == null || propertyMetadataType == null)
                {
                    return false;
                }

                PropertyInfo defaultValueProperty =
                    propertyMetadataType.GetTypeInfo().GetDeclaredProperty("DefaultValue");

                if (defaultValueProperty == null)
                {
                    return false;
                }

                bool defaultMetadataValue = (bool)defaultValueProperty.GetValue(metadataValue, null);
                return defaultMetadataValue;
            }
            catch
            {
                return false;
            }
        }
    }
}