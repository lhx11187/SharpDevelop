﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using Debugger;
using System;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Debugging;

namespace ICSharpCode.SharpDevelop.Services
{
	public class DebuggingOptions: Debugger.Options
	{
		public static DebuggingOptions Instance {
			get {
				return PropertyService.Get("DebuggingOptions", new DebuggingOptions());
			}
		}
		
		public bool ICorDebugVisualizerEnabled;
		public bool ShowValuesInHexadecimal;
		public bool ShowArgumentNames;
		public bool ShowArgumentValues;
		public bool ShowExternalMethods;
	}
}
