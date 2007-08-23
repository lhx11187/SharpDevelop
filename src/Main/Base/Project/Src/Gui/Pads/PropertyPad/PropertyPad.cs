﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.SharpDevelop.Gui
{
	public class PropertyPad : AbstractPadContent, IContextHelpProvider
	{
		static PropertyPad instance;
		
		// an empty container used to reset the property grid
		readonly PropertyContainer emptyContainer = new PropertyContainer(false);
		
		PropertyContainer activeContainer;
		
		internal static PropertyContainer ActiveContainer {
			get { return instance.activeContainer; }
		}
		
		void SetActiveContainer(PropertyContainer pc)
		{
			if (activeContainer == pc)
				return;
			if (pc == null)
				pc = emptyContainer;
			activeContainer = pc;
			
			UpdateHostIfActive(pc);
			UpdateSelectedObjectIfActive(pc);
			UpdateSelectableIfActive(pc);
			UpdatePropertyGridReplacementControl(pc);
		}
		
		internal static void UpdateSelectedObjectIfActive(PropertyContainer container)
		{
			if (instance == null) return;
			if (instance.activeContainer != container)
				return;
			LoggingService.Debug("UpdateSelectedObjectIfActive");
			if (container.SelectedObjects != null)
				instance.SetDesignableObjects(container.SelectedObjects);
			else
				instance.SetDesignableObject(container.SelectedObject);
		}
		
		internal static void UpdateHostIfActive(PropertyContainer container)
		{
			if (instance == null) return;
			if (instance.activeContainer != container)
				return;
			LoggingService.Debug("UpdateHostIfActive");
			if (instance.host == container.Host)
				return;
			if (instance.host != null)
				instance.RemoveHost(instance.host);
			if (container.Host != null)
				instance.SetDesignerHost(container.Host);
		}
		
		internal static void UpdateSelectableIfActive(PropertyContainer container)
		{
			if (instance == null) return;
			if (instance.activeContainer != container)
				return;
			LoggingService.Debug("UpdateSelectableIfActive");
			instance.SetSelectableObjects(container.SelectableObjects);
		}
		
		internal static void UpdatePropertyGridReplacementControl(PropertyContainer container)
		{
			if (instance == null) return;
			if (instance.activeContainer != container)
				return;
			LoggingService.Debug("UpdatePropertyGridReplacementControl");
			if (container.PropertyGridReplacementControl != null) {
				if (!instance.panel.Controls.Contains(container.PropertyGridReplacementControl)) {
					instance.panel.Controls.Clear();
					container.PropertyGridReplacementControl.Dock = DockStyle.Fill;
					instance.panel.Controls.Add(container.PropertyGridReplacementControl);
				}
			} else {
				if (!instance.panel.Controls.Contains(instance.grid)) {
					instance.panel.Controls.Clear();
					instance.panel.Controls.Add(instance.grid);
					instance.panel.Controls.Add(instance.comboBox);
				}
			}
		}
		
		Panel         panel;
		ComboBox      comboBox;
		PropertyGrid  grid;
		IDesignerHost host;
		
		/// <summary>
		/// Gets the underlying property grid. Returns null if the property pad has not yet been created.
		/// </summary>
		public static PropertyGrid Grid {
			get {
				if (instance == null)
					return null;
				else
					return instance.grid;
			}
		}
		
		public static event PropertyValueChangedEventHandler PropertyValueChanged;
		public static event EventHandler SelectedObjectChanged;
		public static event SelectedGridItemChangedEventHandler SelectedGridItemChanged;
		
		public override Control Control {
			get {
				return panel;
			}
		}
		
		IHasPropertyContainer previousContent;
		
		void WorkbenchActiveContentChanged(object sender, EventArgs e)
		{
			IHasPropertyContainer c = WorkbenchSingleton.Workbench.ActiveContent as IHasPropertyContainer;
			if (c == null) {
				if (previousContent == null) {
					c = WorkbenchSingleton.Workbench.ActiveViewContent as IHasPropertyContainer;
				} else {
					// if the previous content is no longer visible, we have to remove the active container
					if (previousContent is IViewContent && previousContent != WorkbenchSingleton.Workbench.ActiveViewContent) {
						c = null;
					} else {
						c = previousContent;
					}
				}
			}
			if (c != null) {
				SetActiveContainer(c.PropertyContainer);
			} else {
				SetActiveContainer(null);
			}
			previousContent = c;
		}
		
		public PropertyPad()
		{
			instance = this;
			panel = new Panel();
			
			grid = new PropertyGrid();
			grid.PropertySort = PropertyService.Get("FormsDesigner.DesignerOptions.PropertyGridSortAlphabetical", false) ? PropertySort.Alphabetical : PropertySort.CategorizedAlphabetical;
			grid.Dock = DockStyle.Fill;
			
			grid.SelectedObjectsChanged += delegate(object sender, EventArgs e) {
				if (SelectedObjectChanged != null)
					SelectedObjectChanged(sender, e);
			};
			grid.SelectedGridItemChanged += delegate(object sender, SelectedGridItemChangedEventArgs e) {
				if (SelectedGridItemChanged != null)
					SelectedGridItemChanged(sender, e);
			};
			
			comboBox = new ComboBox();
			comboBox.Dock = DockStyle.Top;
			comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox.DrawMode = DrawMode.OwnerDrawFixed;
			comboBox.Sorted = true;
			
			comboBox.DrawItem += new DrawItemEventHandler(ComboBoxDrawItem);
			comboBox.MeasureItem += new MeasureItemEventHandler(ComboBoxMeasureItem);
			comboBox.SelectedIndexChanged += new EventHandler(ComboBoxSelectedIndexChanged);
			
			panel.Controls.Add(grid);
			panel.Controls.Add(comboBox);
			
			ProjectService.SolutionClosed += CombineClosedEvent;
			
			grid.PropertyValueChanged += new PropertyValueChangedEventHandler(PropertyChanged);
			grid.ContextMenuStrip = MenuService.CreateContextMenu(this, "/SharpDevelop/Views/PropertyPad/ContextMenu");
			
			LoggingService.Debug("PropertyPad created");
			WorkbenchSingleton.Workbench.ActiveContentChanged += WorkbenchActiveContentChanged;
			// it is possible that ActiveContent changes fires before ActiveViewContent.
			// if the new content is not a IHasPropertyContainer and we listen only to ActiveContentChanged,
			// we might display the PropertyPad of a no longer active view content
			WorkbenchSingleton.Workbench.ActiveViewContentChanged += WorkbenchActiveContentChanged;
			WorkbenchActiveContentChanged(null, null);
		}
		
		void CombineClosedEvent(object sender, EventArgs e)
		{
			SetDesignableObjects(null);
		}
		
		void ComboBoxMeasureItem(object sender, MeasureItemEventArgs mea)
		{
			if (mea.Index < 0 || mea.Index >= comboBox.Items.Count) {
				mea.ItemHeight = comboBox.Font.Height;
				return;
			}
			object item = comboBox.Items[mea.Index];
			SizeF size = mea.Graphics.MeasureString(item.GetType().ToString(), comboBox.Font);
			
			mea.ItemHeight = (int)size.Height;
			mea.ItemWidth  = (int)size.Width;
			
			if (item is IComponent) {
				ISite site = ((IComponent)item).Site;
				if (site != null) {
					string name = site.Name;
					using (Font f = new Font(comboBox.Font, FontStyle.Bold)) {
						mea.ItemWidth += (int)mea.Graphics.MeasureString(name + "-", f).Width;
					}
				}
			}
		}
		
		void ComboBoxDrawItem(object sender, DrawItemEventArgs dea)
		{
			if (dea.Index < 0 || dea.Index >= comboBox.Items.Count) {
				return;
			}
			Graphics g = dea.Graphics;
			Brush stringColor = SystemBrushes.ControlText;
			
			if ((dea.State & DrawItemState.Selected) == DrawItemState.Selected) {
				if ((dea.State & DrawItemState.Focus) == DrawItemState.Focus) {
					g.FillRectangle(SystemBrushes.Highlight, dea.Bounds);
					stringColor = SystemBrushes.HighlightText;
				} else {
					g.FillRectangle(SystemBrushes.Window, dea.Bounds);
				}
			} else {
				g.FillRectangle(SystemBrushes.Window, dea.Bounds);
			}
			
			object item = comboBox.Items[dea.Index];
			int   xPos  = dea.Bounds.X;
			
			if (item is IComponent) {
				ISite site = ((IComponent)item).Site;
				if (site != null) {
					string name = site.Name;
					using (Font f = new Font(comboBox.Font, FontStyle.Bold)) {
						g.DrawString(name, f, stringColor, xPos, dea.Bounds.Y);
						xPos += (int)g.MeasureString(name + "-", f).Width;
					}
				}
			}
			
			string typeString = item.GetType().ToString();
			g.DrawString(typeString, comboBox.Font, stringColor, xPos, dea.Bounds.Y);
		}
		bool inUpdate = false;
		
		void ComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			if (!inUpdate) {
				if (host!=null) {
					ISelectionService selectionService = (ISelectionService)host.GetService(typeof(ISelectionService));
					if (comboBox.SelectedIndex >= 0) {
						selectionService.SetSelectedComponents(new object[] {comboBox.Items[comboBox.SelectedIndex] });
					} else {
						SetDesignableObject(null);
						selectionService.SetSelectedComponents(new object[] { });
					}
				}
			}
		}
		
		void SelectedObjectsChanged()
		{
			if (grid.SelectedObjects != null && grid.SelectedObjects.Length == 1) {
				for (int i = 0; i < comboBox.Items.Count; ++i) {
					if (grid.SelectedObject == comboBox.Items[i]) {
						comboBox.SelectedIndex = i;
					}
				}
			} else {
				comboBox.SelectedIndex = -1;
			}
		}
		
		public override void RedrawContent()
		{
			grid.Refresh();
		}
		
		public override void Dispose()
		{
			base.Dispose();
			if (grid != null) {
				ProjectService.SolutionClosed -= CombineClosedEvent;
				try {
					grid.SelectedObjects = null;
				} catch {}
				grid.Dispose();
				grid = null;
				instance = null;
			}
		}
		
		void SetDesignableObject(object obj)
		{
			inUpdate = true;
			grid.SelectedObject  = obj;
			SelectedObjectsChanged();
			inUpdate = false;
		}
		
		void SetDesignableObjects(object[] obj)
		{
			inUpdate = true;
			grid.SelectedObjects = obj;
			SelectedObjectsChanged();
			inUpdate = false;
		}
		
		void RemoveHost(IDesignerHost host)
		{
			this.host = null;
			grid.Site = null;
		}
		
		void SetDesignerHost(IDesignerHost host)
		{
			this.host = host;
			if (host != null) {
				grid.Site = (new IDEContainer(host)).CreateSite(grid);
				grid.PropertyTabs.AddTabType(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Document);
			} else {
				grid.Site = null;
			}
		}
		
		void SetSelectableObjects(ICollection coll)
		{
			inUpdate = true;
			try {
				comboBox.Items.Clear();
				if (coll != null) {
					foreach (object obj in coll) {
						comboBox.Items.Add(obj);
					}
				}
				SelectedObjectsChanged();
			} finally {
				inUpdate = false;
			}
		}
		
		#region ICSharpCode.SharpDevelop.Gui.IHelpProvider interface implementation
		public void ShowHelp()
		{
			LoggingService.Info("Show help on property pad");
			GridItem gridItem = grid.SelectedGridItem;
			if (gridItem != null) {
				Type component = gridItem.PropertyDescriptor.ComponentType;
				if (component != null) {
					ICSharpCode.SharpDevelop.Dom.IClass c = ParserService.CurrentProjectContent.GetClass(component.FullName, 0);
					if (c != null) {
						foreach (ICSharpCode.SharpDevelop.Dom.IProperty p in c.DefaultReturnType.GetProperties()) {
							if (gridItem.PropertyDescriptor.Name == p.Name) {
								HelpProvider.ShowHelp(p);
								return;
							}
						}
						HelpProvider.ShowHelp(c);
					}
				}
			}
		}
		#endregion
		
		void PropertyChanged(object sender, PropertyValueChangedEventArgs e)
		{
			OnPropertyValueChanged(sender, e);
		}
		
		void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
		{
			if (PropertyValueChanged != null) {
				PropertyValueChanged(sender, e);
			}
		}
	}
}
