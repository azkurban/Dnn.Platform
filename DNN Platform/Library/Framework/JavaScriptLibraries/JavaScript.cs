﻿#region Copyright

// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Host;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Installer.Packages;
using DotNetNuke.UI.Utilities;
using DotNetNuke.Web.Client.ClientResourceManagement;
using Globals = DotNetNuke.Common.Globals;

#endregion

namespace DotNetNuke.Framework.JavaScriptLibraries
{
    public class JavaScript
    {
        private const string ScriptPreix = "JSL.";
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof (JavaScript));

        #region Private Properties

        /// <summary>
        ///     checks whether the script file is a known javascript library
        /// </summary>
        /// <param name="jsname">script identifier</param>
        /// <returns></returns>
        public static bool IsInstalled(String jsname)
        {
            JavaScriptLibrary library = JavaScriptLibraryController.Instance.GetLibrary(l => l.LibraryName == jsname);
            return library != null;
        }

        /// <summary>
        ///     determine whether to use the debug script for a file
        /// </summary>
        /// <returns>whether to use the debug script</returns>
        public static bool UseDebugScript()
        {
            if (Globals.Status != Globals.UpgradeStatus.None)
            {
                return false;
            }
            return HttpContext.Current.IsDebuggingEnabled;
        }

        /// <summary>
        ///     returns the version of a javascript library from the database
        /// </summary>
        /// <param name="jsname">the library name</param>
        /// <returns></returns>
        public static string Version(String jsname)
        {
            JavaScriptLibrary library =
                JavaScriptLibraryController.Instance.GetLibrary(l => l.LibraryName == jsname);
            return library != null ? Convert.ToString(library.Version) : String.Empty;
        }

        /// <summary>
        ///     adds a request for a script into the page items collection
        /// </summary>
        /// <param name="jsname">the library name</param>
        public static void RequestRegistration(String jsname)
        {
            HttpContext.Current.Items[ScriptPreix + "." + jsname] = true;
        }

        /// <summary>
        ///     adds a request for a script into the page items collection
        /// </summary>
        /// <param name="jsname">the library name</param>
        public static void RequestRegistration(String jsname,Version version)
        {
            JavaScriptLibrary library = JavaScriptLibraryController.Instance.GetLibrary(l => l.Version == version);
            if (library != null)
            {
                HttpContext.Current.Items[ScriptPreix + "." + library.LibraryName] = true;
            }
            else
            {
                HttpContext.Current.Items[ScriptPreix + "." + jsname] = true;
            }
        }


        /// <summary>
        ///     adds a request for a script into the page items collection
        /// </summary>
        /// <param name="jsname">the library name</param>
        public static void RequestRegistration(String jsname, Version version, SpecificVersion specific)
        {
            JavaScriptLibrary library;
            bool isProcessed = false;
            switch (specific)
            {
                case SpecificVersion.Latest:
                    HttpContext.Current.Items[ScriptPreix + "." + jsname] = true;
                    isProcessed = true;
                    break;
                case SpecificVersion.LatestMajor:
                    library = JavaScriptLibraryController.Instance.GetLibrary(l => l.Version.Major >= version.Major);
                    if (library != null)
                    {
                        HttpContext.Current.Items[ScriptPreix + "." + library.LibraryName] = true;
                    }
                    isProcessed = true;
                    break;
                case SpecificVersion.LatestMinor:
                     library = JavaScriptLibraryController.Instance.GetLibrary(l => l.Version.Minor >= version.Minor);
                    if (library != null)
                    {
                        HttpContext.Current.Items[ScriptPreix + "." + library.LibraryName] = true;
                    }
                    isProcessed = true;
                    break;
            }
            if (isProcessed == false)
            {
                HttpContext.Current.Items[ScriptPreix + "." + jsname] = true;
            }
        }

        /// <summary>
        ///     method is called once per page event cycle and will
        ///     load all scripts requested during that page processing cycle
        /// </summary>
        /// <param name="page">reference to the current page</param>
        public static void Register(Page page)
        {
            var scripts = GetScriptVersions();
            foreach (string item in scripts)
            {
                if (item != "DnnJQueryPlugins")
                    {
                        RegisterScript(page, item, true);
                    }
                    else
                    {
                        RegisterScript(page, item);
                    }               
            }
        }

        private static IEnumerable<string> GetScriptVersions()
        {
            var orderedScripts= (from object item in HttpContext.Current.Items where item.ToString().StartsWith(ScriptPreix) select item.ToString().Substring(3)).ToList();
            orderedScripts.Sort();
            foreach (var orderedScript in orderedScripts)
            {
                //find dependencies
               
                JavaScriptLibrary library = JavaScriptLibraryController.Instance.GetLibrary(l => l.LibraryName == orderedScript);
                if (library != null)
                {
                    var package=PackageController.Instance.GetExtensionPackage(Null.NullInteger, (p) => p.PackageID == library.PackageID);
                    if (package.Dependencies.Count > 0)
                    {
                        foreach (var dependency in package.Dependencies)
                        {
                            if (HttpContext.Current.Items[ScriptPreix + "." + dependency.PackageName]!=null)
                            {
                                orderedScripts.Add(ScriptPreix + "." + dependency.PackageName);
                            }
                            
                        }
                    }
                }
                
            }
            return orderedScripts;
        }

        #endregion

        protected JavaScript()
        {
        }

        private static JavaScriptLibrary GetJavascriptLibrary(String jsname)
        {
            JavaScriptLibrary library = JavaScriptLibraryController.Instance.GetLibrary(l => l.LibraryName == jsname);
            if (library == null)
            {
                //this should only occur if packages are incorrect or a RequestRegistration call has a typo
                Logger.TraceFormat("Missing Library - {0}", jsname);
                return null;
            }
            return library;
        }

        private static string GetScriptPath(JavaScriptLibrary js)
        {
            if (Host.CdnEnabled)
            {
                //cdn enabled but jsl does not have one defined
                if (!String.IsNullOrEmpty(js.CDNPath))
                {
                return js.CDNPath;
                }
            }
            return HttpContext.Current.Server.MapPath("~/Resources/libraries/" + js.LibraryName + "/" + js.FileName);
        }

        private static string GetScriptLocation(JavaScriptLibrary js)
        {
            switch (js.PreferredScriptLocation)
            {
                case ScriptLocation.PageHead:
                    return "DnnPageHeaderProvider";
                case ScriptLocation.BodyBottom:
                    return "DnnFormBottomProvider";
                case ScriptLocation.BodyTop:
                    return "DnnBodyProvider";
            }

            return String.Empty;
        }

        private static void RegisterScript(Page page, string js, bool clientApiRequired = false)
        {
            if (clientApiRequired)
            {
                //This method maybe called when Page.Form hasn't initialized yet, in that situation if needed should reference dnn js manually.
                //such as call jQuery.RegisterDnnJQueryPlugins in Control.OnInit.
                if (page.Form != null)
                {
                    ClientAPI.RegisterClientReference(page, ClientAPI.ClientNamespaceReferences.dnn);
                }
            }
            JavaScriptLibrary jsl = GetJavascriptLibrary(js);
            ClientResourceManager.RegisterScript(page, GetScriptLocation(jsl), jsl.PackageID + 500, GetScriptPath(jsl));

            if (Host.CdnEnabled && !String.IsNullOrEmpty(jsl.ObjectName))
            {
                string pagePortion;
                switch (jsl.PreferredScriptLocation)
                {
                    case ScriptLocation.PageHead:

                        pagePortion = "ClientDependencyHeadJs";
                        break;
                    case ScriptLocation.BodyBottom:
                        pagePortion = "ClientResourcesFormBottom";
                        break;
                    case ScriptLocation.BodyTop:
                        pagePortion = "BodySCRIPTS";
                        break;
                    default:
                        pagePortion = "BodySCRIPTS";
                        break;
                }
                Control scriptloader = page.FindControl(pagePortion);
                var fallback = new DnnJsIncludeFallback(jsl.ObjectName, jsl.FileName);
                if (scriptloader != null)
                {
                    scriptloader.Controls.Add(fallback);
                }
            }
        }
    }
}