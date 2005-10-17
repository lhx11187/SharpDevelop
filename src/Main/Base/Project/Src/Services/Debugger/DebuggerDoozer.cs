// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;

namespace ICSharpCode.Core
{
	/// <summary>
	/// Creates debuggers.
	/// </summary>
	/// <attribute name="supportsStart">
	/// Specifies if the debugger supports the 'Start' command. Default: true
	/// </attribute>
	/// <attribute name="supportsStartWithoutDebugger">
	/// Specifies if the debugger supports the 'StartWithoutDebugger' command. Default: true
	/// </attribute>
	/// <attribute name="supportsStop">
	/// Specifies if the debugger supports the 'Stop' (kill running process) command. Default: true
	/// </attribute>
	/// <attribute name="supportsStepping">
	/// Specifies if the debugger supports stepping. Default: false
	/// </attribute>
	/// <attribute name="supportsExecutionControl">
	/// Specifies if the debugger supports execution control (break, resume). Default: false
	/// </attribute>
	/// <attribute name="class">
	/// Name of the IDebugger class.
	/// </attribute>
	/// <usage>Only in /SharpDevelop/Services/DebuggerService/Debugger</usage>
	/// <returns>
	/// An DebuggerDescriptor object that exposes the attributes and the IDebugger object (lazy-loading).
	/// </returns>
	public class DebuggerDoozer : IDoozer
	{
		/// <summary>
		/// Gets if the doozer handles codon conditions on its own.
		/// If this property return false, the item is excluded when the condition is not met.
		/// </summary>
		public bool HandleConditions {
			get {
				return false;
			}
		}
		
		public object BuildItem(object caller, Codon codon, ArrayList subItems)
		{
			return new DebuggerDescriptor(codon);
		}
	}
	
	public class DebuggerDescriptor
	{
		Codon codon;
		
		public DebuggerDescriptor(Codon codon)
		{
			this.codon = codon;
		}
		
		IDebugger debugger;
		
		public IDebugger Debugger {
			get {
				if (debugger == null)
					debugger = (IDebugger)codon.AddIn.CreateObject(codon.Properties["class"]);
				return debugger;
			}
		}
		
		public bool SupportsStart {
			get {
				return codon.Properties["supportsStart"] != "false";
			}
		}
		
		public bool SupportsStartWithoutDebugging {
			get {
				return codon.Properties["supportsStartWithoutDebugger"] != "false";
			}
		}
		
		public bool SupportsStop {
			get {
				return codon.Properties["supportsStop"] != "false";
			}
		}
		
		public bool SupportsStepping {
			get {
				return codon.Properties["supportsStepping"] == "true";
			}
		}
		
		public bool SupportsExecutionControl {
			get {
				return codon.Properties["supportsExecutionControl"] == "true";
			}
		}
	}
}
