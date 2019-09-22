// ****************************************************************************
// <copyright file="DesignerPlatformLibrary.cs" company="GalaSoft Laurent Bugnion">
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
    /// <summary>
    /// Defines values associated with the design library platforms.
    /// </summary>
    internal enum DesignerPlatformLibrary
    {
        /// <summary>
        /// The platform is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The platform is .NET.
        /// </summary>
        Net,

        /// <summary>
        /// The platform is WinRT.
        /// </summary>
        WinRt,

        /// <summary>
        /// The platform is Silverlight.
        /// </summary>
        Silverlight
    }
}