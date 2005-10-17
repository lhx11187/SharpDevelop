﻿// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;

namespace ICSharpCode.Core
{
	/// <summary>
	/// Creates object instances by invocing a type's parameterless constructor
	/// via System.Reflection.
	/// </summary>
	/// <attribute name="class">
	/// The fully qualified type name of the attribute to create.
	/// </attribute>
	/// <usage>Everywhere where objects are expected.</usage>
	/// <returns>
	/// Any kind of object.
	/// </returns>
	public class ClassDoozer : IDoozer
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
			return codon.AddIn.CreateObject(codon.Properties["class"]);
		}
	}
}
