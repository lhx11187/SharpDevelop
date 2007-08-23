﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.Core;

namespace ICSharpCode.SharpDevelop
{
	/// <summary>
	/// Represents an opened file.
	/// </summary>
	public abstract class OpenedFile : ICanBeDirty
	{
		protected IViewContent currentView;
		bool inLoadOperation;
		bool inSaveOperation;
		
		/// <summary>
		/// holds unsaved file content in memory when view containing the file was closed but no other view
		/// activated
		/// </summary>
		byte[] fileData;
		
		#region IsDirty
		bool isDirty;
		public event EventHandler IsDirtyChanged;
		
		/// <summary>
		/// Gets/sets if the file is has unsaved changes.
		/// </summary>
		public bool IsDirty {
			get { return isDirty;}
			set {
				if (isDirty != value) {
					isDirty = value;
					
					if (IsDirtyChanged != null) {
						IsDirtyChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		
		/// <summary>
		/// Marks the file as dirty if it currently is not in a load operation.
		/// </summary>
		public virtual void MakeDirty()
		{
			if (!inLoadOperation) {
				this.IsDirty = true;
			}
		}
		#endregion
		
		bool isUntitled;
		
		/// <summary>
		/// Gets if the file is untitled. Untitled files show a "Save as" dialog when they are saved.
		/// </summary>
		public bool IsUntitled {
			get { return isUntitled; }
			protected set { isUntitled = value; }
		}
		
		string fileName;
		
		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		public string FileName {
			get { return fileName; }
			set {
				if (fileName == value) return;
				
				value = FileUtility.NormalizePath(value);
				
				if (fileName != value) {
					ChangeFileName(value);
				}
			}
		}
		
		protected virtual void ChangeFileName(string newValue)
		{
			fileName = newValue;
			
			if (FileNameChanged != null) {
				FileNameChanged(this, EventArgs.Empty);
			}
		}
		
		/// <summary>
		/// Occurs when the file name has changed.
		/// </summary>
		public event EventHandler FileNameChanged;
		
		/// <summary>
		/// Use this method to save the file to disk using a new name.
		/// </summary>
		public void SaveToDisk(string newFileName)
		{
			this.FileName = newFileName;
			this.IsUntitled = false;
			SaveToDisk();
		}
		
		public abstract void RegisterView(IViewContent view);
		public abstract void UnregisterView(IViewContent view);
		
		public virtual void CloseIfAllViewsClosed()
		{
		}
		
		/// <summary>
		/// Forces initialization of the specified view.
		/// </summary>
		public virtual void ForceInitializeView(IViewContent view)
		{
			if (view == null)
				throw new ArgumentNullException("view");
			
			if (currentView != view) {
				if (currentView == null) {
					SwitchedToView(view);
				} else {
					try {
						inLoadOperation = true;
						using (Stream sourceStream = OpenRead()) {
							view.Load(this, sourceStream);
						}
					} finally {
						inLoadOperation = false;
					}
				}
			}
		}
		
		/// <summary>
		/// Gets the list of view contents registered with this opened file.
		/// </summary>
		public abstract IList<IViewContent> RegisteredViewContents {
			get;
		}
		
		/// <summary>
		/// Gets the view content that currently edits this file.
		/// If there are multiple view contents registered, this returns the view content that was last
		/// active. The property might return null even if view contents are registered if the last active
		/// content was closed. In that case, the file is stored in-memory and loaded when one of the
		/// registered view contents becomes active.
		/// </summary>
		public IViewContent CurrentView {
			get { return currentView; }
		}
		
		/// <summary>
		/// Opens the file for reading.
		/// </summary>
		public virtual Stream OpenRead()
		{
			if (fileData != null) {
				return new MemoryStream(fileData, false);
			} else {
				return new FileStream(FileName, FileMode.Open, FileAccess.Read);
			}
		}
		
		/// <summary>
		/// Sets the internally stored data to the specified byte array.
		/// This method should only be used when there is no current view or by the
		/// current view.
		/// </summary>
		/// <remarks>
		/// Use this method to specify the initial file content if you use a OpenedFile instance
		/// for a file that doesn't exist on disk but should be automatically created when a view
		/// with the file is saved, e.g. for .resx files created by the forms designer.
		/// </remarks>
		public virtual void SetData(byte[] fileData)
		{
			if (fileData == null)
				throw new ArgumentNullException("fileData");
			if (inLoadOperation)
				throw new InvalidOperationException("SetData cannot be used while loading");
			if (inSaveOperation)
				throw new InvalidOperationException("SetData cannot be used while saving");
			
			this.fileData = fileData;
		}
		
		/// <summary>
		/// Save the file to disk using the current name.
		/// </summary>
		public virtual void SaveToDisk()
		{
			if (IsUntitled)
				throw new InvalidOperationException("Cannot save an untitled file to disk!");
			
			/*
			 * TODO: Reimplement "safe saving"
			if (document.TextEditorProperties.CreateBackupCopy) {
				try {
					if (File.Exists(fileName)) {
						string backupName = fileName + ".bak";
						File.Copy(fileName, backupName, true);
					}
				} catch (Exception) {
	//
	//				MessageService.ShowError(e, "Can not create backup copy of " + fileName);
				}
			}
			 */
			using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write)) {
				if (currentView != null) {
					SaveCurrentViewToStream(fs);
				} else {
					fs.Write(fileData, 0, fileData.Length);
				}
			}
			IsDirty = false;
		}
		
//		/// <summary>
//		/// Called before saving the current view. This event is raised both when saving to disk and to memory (for switching between views).
//		/// </summary>
//		public event EventHandler SavingCurrentView;
//
//		/// <summary>
//		/// Called after saving the current view. This event is raised both when saving to disk and to memory (for switching between views).
//		/// </summary>
//		public event EventHandler SavedCurrentView;
		
		
		void SaveCurrentViewToStream(Stream stream)
		{
//			if (SavingCurrentView != null)
//				SavingCurrentView(this, EventArgs.Empty);
			inSaveOperation = true;
			try {
				currentView.Save(this, stream);
			} finally {
				inSaveOperation = false;
			}
//			if (SavedCurrentView != null)
//				SavedCurrentView(this, EventArgs.Empty);
		}
		
		protected void SaveCurrentView()
		{
			using (MemoryStream memoryStream = new MemoryStream()) {
				SaveCurrentViewToStream(memoryStream);
				fileData = memoryStream.ToArray();
			}
		}
		
		
		protected void SwitchedToView(IViewContent newView)
		{
			if (currentView != null) {
				if (newView.SupportsSwitchToThisWithoutSaveLoad(this, currentView)
				    || currentView.SupportsSwitchFromThisWithoutSaveLoad(this, newView))
				{
					// switch without Save/Load
					currentView.SwitchFromThisWithoutSaveLoad(this, newView);
					newView.SwitchToThisWithoutSaveLoad(this, currentView);
					
					currentView = newView;
					return;
				}
			}
			if (currentView != null) {
				SaveCurrentView();
			}
			try {
				inLoadOperation = true;
				using (Stream sourceStream = OpenRead()) {
					currentView = newView;
					fileData = null;
					newView.Load(this, sourceStream);
				}
			} finally {
				inLoadOperation = false;
			}
		}
		
		public virtual void ReloadFromDisk()
		{
			fileData = null;
			if (currentView != null) {
				try {
					inLoadOperation = true;
					using (Stream sourceStream = OpenRead()) {
						currentView.Load(this, sourceStream);
					}
				} finally {
					inLoadOperation = false;
				}
			}
		}
	}
	
	sealed class FileServiceOpenedFile : OpenedFile
	{
		List<IViewContent> registeredViews = new List<IViewContent>();
		
		protected override void ChangeFileName(string newValue)
		{
			FileService.OpenedFileFileNameChange(this, this.FileName, newValue);
			base.ChangeFileName(newValue);
		}
		
		internal FileServiceOpenedFile(string fileName)
		{
			this.FileName = fileName;
			IsUntitled = false;
		}
		
		internal FileServiceOpenedFile(byte[] fileData)
		{
			this.FileName = null;
			SetData(fileData);
			IsUntitled = true;
			MakeDirty();
		}
		
		/// <summary>
		/// Gets the list of view contents registered with this opened file.
		/// </summary>
		public override IList<IViewContent> RegisteredViewContents {
			get { return registeredViews.AsReadOnly(); }
		}
		
		public override void ForceInitializeView(IViewContent view)
		{
			if (view == null)
				throw new ArgumentNullException("view");
			if (!registeredViews.Contains(view))
				throw new ArgumentException("registeredViews must contain view");
			
			base.ForceInitializeView(view);
		}
		
		public override void RegisterView(IViewContent view)
		{
			if (view == null)
				throw new ArgumentNullException("view");
			if (registeredViews.Contains(view))
				throw new ArgumentException("registeredViews already contains view");
			
			registeredViews.Add(view);
			
			if (WorkbenchSingleton.Workbench != null) {
				WorkbenchSingleton.Workbench.ActiveViewContentChanged += WorkbenchActiveViewContentChanged;
				if (WorkbenchSingleton.Workbench.ActiveViewContent == view) {
					SwitchedToView(view);
				}
			}
			#if DEBUG
			view.Disposed += ViewDisposed;
			#endif
		}
		
		public override void UnregisterView(IViewContent view)
		{
			if (view == null)
				throw new ArgumentNullException("view");
			Debug.Assert(registeredViews.Contains(view));
			
			if (WorkbenchSingleton.Workbench != null) {
				WorkbenchSingleton.Workbench.ActiveViewContentChanged -= WorkbenchActiveViewContentChanged;
			}
			#if DEBUG
			view.Disposed -= ViewDisposed;
			#endif
			
			registeredViews.Remove(view);
			if (registeredViews.Count > 0) {
				if (currentView == view) {
					SaveCurrentView();
					currentView = null;
				}
			} else {
				// all views to the file were closed
				FileService.OpenedFileClosed(this);
			}
		}
		
		public override void CloseIfAllViewsClosed()
		{
			if (registeredViews.Count == 0) {
				FileService.OpenedFileClosed(this);
			}
		}
		
		#if DEBUG
		void ViewDisposed(object sender, EventArgs e)
		{
			Debug.Fail("View was disposed while still registered with OpenedFile!");
		}
		#endif
		
		void WorkbenchActiveViewContentChanged(object sender, EventArgs e)
		{
			IViewContent newView = WorkbenchSingleton.Workbench.ActiveViewContent;
			
			if (!registeredViews.Contains(newView))
				return;
			
			SwitchedToView(newView);
		}
	}
}
