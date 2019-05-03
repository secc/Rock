// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;

namespace Rock.Reporting.DataFilter.GroupMember
{
    /// <summary>
    /// Filter Group Members by their Attribute Values
    /// </summary>
    [Description( "Filter Group Members by their Attribute Values in a single group" )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", " Single Group Member Attributes Filter" )]
    public class SingleGroupMemberAttributesFilter : EntityFieldFilter
    {
        #region Settings

        /// <summary>
        ///     Settings for the Data Filter Component.
        /// </summary>
        private class FilterSettings : SettingsStringBase
        {
            public List<string> AttributeFilterSettings;
            public string AttributeKey;
            public int? GroupId;

            public FilterSettings()
            {
                AttributeFilterSettings = new List<string>();
            }

            public FilterSettings( string settingsString )
                : this()
            {
                FromSelectionString( settingsString );
            }

            public override bool IsValid
            {
                get
                {
                    if ( string.IsNullOrWhiteSpace( AttributeKey ) )
                    {
                        return false;
                    }

                    return true;
                }
            }

            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                // Parameter 1: Attribute Key
                AttributeKey = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 0 );

                // Parameter 2+: Remaining Parameters represent settings that are specific to the filter for the selected Attribute.
                AttributeFilterSettings = new List<string>();

                AttributeFilterSettings.AddRange( parameters.Skip( 1 ) );

                // Derive GroupId from AttributeKey
                var attributeGuid = AttributeKey.Split( '_' ).LastOrDefault().AsGuidOrNull();
                AttributeCache attribute = null;
                if ( attributeGuid.HasValue )
                {
                    attribute = AttributeCache.Get( attributeGuid.Value );
                }

                this.GroupId = null;
                if ( attribute != null && attribute.EntityTypeQualifierColumn == "GroupId" )
                {
                    this.GroupId = attribute.EntityTypeQualifierValue.AsIntegerOrNull();
                }
            }

            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( AttributeKey.ToStringSafe() );

                settings.AddRange( AttributeFilterSettings );

                return settings;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        /// The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Rock.Model.GroupMember ).FullName; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Additional Filters"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public override string GetTitle( Type entityType )
        {
            return "Single Group Member Attribute Values";
        }

        /// <summary>
        /// Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        /// will set the description of the filter to whatever is returned by this property.  If including script, the
        /// controls parent container can be referenced through a '$content' variable that is set by the control before 
        /// referencing this property.
        /// </summary>
        /// <value>
        /// The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return "GroupMemberPropertySelection( $content )";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            return GetSelectedFieldName( selection );
        }

        /// <summary>
        /// Gets the name of the selected field.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string GetSelectedFieldName( string selection )
        {
            string result = "Member Property";
            if ( !string.IsNullOrWhiteSpace( selection ) )
            {
                var settings = new FilterSettings( selection );
                var entityField = GetGroupMemberAttributes( settings.GroupId ).FirstOrDefault( f => f.UniqueName == settings.AttributeKey );
                if ( entityField != null )
                {
                    result = entityField.FormattedFilterDescription( settings.AttributeFilterSettings );
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Updates the selection from page parameters if there is a page parameter for the selection
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <param name="rockBlock">The rock block.</param>
        /// <returns></returns>
        public override string UpdateSelectionFromPageParameters( string selection, Rock.Web.UI.RockBlock rockBlock )
        {
            // don't modify the selection for the Filter based on PageParameters
            return selection;
        }

        /// <summary>
        /// Creates the child controls.
        /// Implement this version of CreateChildControls if your DataFilterComponent supports different FilterModes
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="filterControl"></param>
        /// <param name="filterMode"></param>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl, FilterMode filterMode )
        {
            var containerControl = new DynamicControlsPanel();
            containerControl.ID = string.Format( "{0}_containerControl", filterControl.ID );
            containerControl.CssClass = "js-container-control";
            filterControl.Controls.Add( containerControl );

            GroupPicker groupPicker = new GroupPicker();
            groupPicker.ID = filterControl.ID + "_groupPicker";
            groupPicker.Label = "Group";
            groupPicker.SelectItem += groupPicker_SelectItem;

            containerControl.Controls.Add( groupPicker );

            // set the GroupPicker selected value now so we can create the other controls the depending on know the groupid
            if ( filterControl.Page.IsPostBack )
            {
                // since we just created the GroupPicker, we'll have to sniff the GroupId from Request.Params
                int? groupId = filterControl.Page.Request.Params[groupPicker.Controls[0].UniqueID].AsIntegerOrNull();
                groupPicker.SetValue( groupId );
                EnsureSelectedGroupControls( groupPicker );
            }

            return new Control[] { containerControl };
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the groupPicker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void groupPicker_SelectItem( object sender, EventArgs e )
        {
            HtmlAnchor anchor = sender as HtmlAnchor;
            EnsureSelectedGroupControls( anchor.Parent as GroupPicker );
        }

        /// <summary>
        /// Ensures that the controls that are created based on the Group have been created
        /// </summary>
        /// <param name="groupPicker">The group type picker.</param>
        private void EnsureSelectedGroupControls( GroupPicker groupPicker )
        {
            DynamicControlsPanel containerControl = groupPicker.Parent as DynamicControlsPanel;
            FilterField filterControl = containerControl.FirstParentControlOfType<FilterField>();

            this.entityFields = GetGroupMemberAttributes( groupPicker.SelectedValueAsInt() );

            // Create the field selection dropdown
            string propertyControlId = string.Format( "{0}_ddlProperty", containerControl.ID );
            RockDropDownList ddlProperty = containerControl.Controls.OfType<RockDropDownList>().FirstOrDefault( a => a.ID == propertyControlId );
            if ( ddlProperty == null )
            {
                ddlProperty = new RockDropDownList();
                ddlProperty.ID = propertyControlId;
                ddlProperty.AutoPostBack = true;
                ddlProperty.SelectedIndexChanged += ddlProperty_SelectedIndexChanged;
                ddlProperty.AddCssClass( "js-property-dropdown" );
                containerControl.Controls.Add( ddlProperty );
            }

            // update the list of items just in case the Group changed
            ddlProperty.Items.Clear();

            // add Empty option first
            ddlProperty.Items.Add( new ListItem() );
            foreach ( var entityField in this.entityFields )
            {
                // Add the field to the dropdown of available fields
                ddlProperty.Items.Add( new ListItem( entityField.TitleWithoutQualifier, entityField.UniqueName ) );
            }

            if ( groupPicker.Page.IsPostBack )
            {
                ddlProperty.SetValue( groupPicker.Page.Request.Params[ddlProperty.UniqueID] );
            }

            foreach ( var entityField in this.entityFields )
            {
                string controlId = string.Format( "{0}_{1}", containerControl.ID, entityField.UniqueName );
                if ( !containerControl.Controls.OfType<Control>().Any( a => a.ID == controlId ) )
                {
                    var control = entityField.FieldType.Field.FilterControl( entityField.FieldConfig, controlId, true, filterControl.FilterMode );
                    if ( control != null )
                    {
                        containerControl.Controls.Add( control );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlProperty control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlProperty_SelectedIndexChanged( object sender, EventArgs e )
        {
            var ddlProperty = sender as RockDropDownList;
            var containerControl = ddlProperty.FirstParentControlOfType<DynamicControlsPanel>();
            FilterField filterControl = ddlProperty.FirstParentControlOfType<FilterField>();

            var entityField = this.entityFields.FirstOrDefault( a => a.UniqueName == ddlProperty.SelectedValue );
            if ( entityField != null )
            {
                string controlId = string.Format( "{0}_{1}", containerControl.ID, entityField.UniqueName );
                if ( !containerControl.Controls.OfType<Control>().Any( a => a.ID == controlId ) )
                {
                    var control = entityField.FieldType.Field.FilterControl( entityField.FieldConfig, controlId, true, filterControl.FilterMode );
                    if ( control != null )
                    {
                        // Add the filter controls of the selected field
                        containerControl.Controls.Add( control );
                    }
                }
            }
        }

        private List<EntityField> entityFields = null;

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="filterMode"></param>
        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls, FilterMode filterMode )
        {
            if ( controls.Length > 0 )
            {
                var containerControl = controls[0] as DynamicControlsPanel;
                if ( containerControl.Controls.Count >= 2 )
                {
                    GroupPicker groupPicker = containerControl.Controls[0] as GroupPicker;
                    groupPicker.RenderControl( writer );

                    DropDownList ddlProperty = containerControl.Controls[1] as DropDownList;
                    var entityFields = GetGroupMemberAttributes( groupPicker.SelectedValueAsInt() );

                    var panelControls = new List<Control>();
                    panelControls.AddRange( containerControl.Controls.OfType<Control>() );

                    RenderEntityFieldsControls( entityType, filterControl, writer, entityFields, ddlProperty, panelControls, containerControl.ID, filterMode );
                }
            }
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="filterMode"></param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls, FilterMode filterMode )
        {
            var settings = new FilterSettings();

            if ( controls.Length > 0 )
            {
                var containerControl = controls[0] as DynamicControlsPanel;
                if ( containerControl.Controls.Count >= 1 )
                {
                    // note: since this datafilter creates additional controls outside of CreateChildControls(), we'll use our _controlsToRender instead of the controls parameter
                    GroupPicker groupPicker = containerControl.Controls[0] as GroupPicker;
                    Guid groupGuid = Guid.Empty;
                    var groupId = groupPicker.SelectedValueAsInt();

                    if ( containerControl.Controls.Count > 1 )
                    {
                        DropDownList ddlProperty = containerControl.Controls[1] as DropDownList;
                        settings.AttributeKey = ddlProperty.SelectedValue;
                        var entityFields = GetGroupMemberAttributes( groupId );
                        var entityField = entityFields.FirstOrDefault( f => f.UniqueName == ddlProperty.SelectedValue );
                        if ( entityField != null )
                        {
                            var panelControls = new List<Control>();
                            panelControls.AddRange( containerControl.Controls.OfType<Control>() );

                            var control = panelControls.FirstOrDefault( c => c.ID.EndsWith( entityField.UniqueName ) );
                            if ( control != null )
                            {

                                entityField.FieldType.Field.GetFilterValues( control, entityField.FieldConfig, filterMode ).ForEach( v => settings.AttributeFilterSettings.Add( v ) );
                            }
                        }
                    }

                }
            }

            return settings.ToSelectionString();
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            var settings = new FilterSettings( selection );

            if ( !settings.IsValid )
            {
                return;
            }

            if ( controls.Length > 0 )
            {
                int? groupId = settings.GroupId;

                var containerControl = controls[0] as DynamicControlsPanel;
                if ( containerControl.Controls.Count > 0 )
                {
                    GroupPicker groupPicker = containerControl.Controls[0] as GroupPicker;
                    groupPicker.SetValue( groupId );
                    EnsureSelectedGroupControls( groupPicker );
                }

                if ( containerControl.Controls.Count > 1 )
                {
                    DropDownList ddlProperty = containerControl.Controls[1] as DropDownList;
                    var entityFields = GetGroupMemberAttributes( groupId );

                    var panelControls = new List<Control>();
                    panelControls.AddRange( containerControl.Controls.OfType<Control>() );

                    var parameters = new List<string> { settings.AttributeKey };

                    parameters.AddRange( settings.AttributeFilterSettings );

                    SetEntityFieldSelection( entityFields, ddlProperty, parameters, panelControls );
                }
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var settings = new FilterSettings( selection );

            if ( settings.IsValid && settings.AttributeFilterSettings.Any() )
            {
                var entityFields = GetGroupMemberAttributes( settings.GroupId );
                var entityField = entityFields.FindFromFilterSelection( settings.AttributeKey );
                if ( entityField != null )
                {
                    return GetAttributeExpression( serviceInstance, parameterExpression, entityField, settings.AttributeFilterSettings );
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the properties and attributes for the entity
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        private List<EntityField> GetGroupMemberAttributes( int? groupId )
        {
            List<EntityField> entityAttributeFields = new List<EntityField>();

            var fakeGroupMember = new Rock.Model.GroupMember();

            if ( groupId == null )
            {
                groupId = 0;
            }

            var group = new GroupService( new RockContext() ).Get( groupId.Value );
            var sortedFields = new List<EntityField>();

            if ( group != null )
            {
                fakeGroupMember.GroupId = group.Id;
                fakeGroupMember.Group = group;

                Rock.Attribute.Helper.LoadAttributes( fakeGroupMember );
                foreach ( var attribute in fakeGroupMember.Attributes.Select( a => a.Value ) )
                {
                    EntityHelper.AddEntityFieldForAttribute( entityAttributeFields, attribute );
                }

                int index = 0;
                foreach ( var entityProperty in entityAttributeFields.OrderBy( p => p.TitleWithoutQualifier ).ThenBy( p => p.Name ) )
                {
                    entityProperty.Index = index;
                    index++;
                    sortedFields.Add( entityProperty );
                }
            }

            return sortedFields;
        }

        #endregion
    }
}