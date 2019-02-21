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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Reporting.DataSelect.Person
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Show if person is in a group of a specific group type purpose" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Select if Person in specific group type purpose" )]
    public class InGroupGroupTypePurposeSelect : DataSelectComponent
    {
        #region Properties

        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get
            {
                return typeof( Rock.Model.Person ).FullName;
            }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get
            {
                return "In Group Type Of Purpose";
            }
        }

        /// <summary>
        /// Gets the section that this will appear in in the Field Selector
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Groups"; }
        }

        /// <summary>
        /// Gets the type of the column field.
        /// </summary>
        /// <value>
        /// The type of the column field.
        /// </value>
        public override Type ColumnFieldType
        {
            get { return typeof( bool? ); }
        }

        /// <summary>
        /// Gets the default column header text.
        /// </summary>
        /// <value>
        /// The default column header text.
        /// </value>
        public override string ColumnHeaderText
        {
            get
            {
                return "In Group Type Of Purpose";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "In Group of Group Type Purpose";
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( RockContext context, MemberExpression entityIdProperty, string selection )
        {
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                GroupTypeService groupTypeService = new GroupTypeService( ( RockContext ) context );
                GroupMemberService groupMemberService = new GroupMemberService( ( RockContext ) context );

                var groupTypePurposeId = selectionValues[0].AsInteger();
                var groupsQry = groupTypeService.Queryable().Where( gt => gt.GroupTypePurposeValueId == groupTypePurposeId )
                    .SelectMany( gt => gt.Groups );

                //Filter by group status
                bool? groupStatus = null;
                if ( selectionValues.Length >= 4 )
                {
                    groupStatus = selectionValues[3].AsBooleanOrNull();
                }

                if ( groupStatus.HasValue )
                {
                    groupsQry = groupsQry.Where( g => g.IsActive == groupStatus.Value );
                }

                var groupMemberQry = groupMemberService.Queryable().Where( gm => groupsQry.Select( g => g.Id ).Contains( gm.GroupId ) );

                //Filter by group status
                bool? groupRoleType = selectionValues[1].AsBooleanOrNull();
                if ( groupRoleType.HasValue )
                {
                    groupMemberQry = groupMemberQry.Where( gm => gm.GroupRole.IsLeader == groupRoleType.Value );
                }

                GroupMemberStatus? groupMemberStatus = null;
                if ( selectionValues.Length >= 3 )
                {
                    groupMemberStatus = selectionValues[2].ConvertToEnumOrNull<GroupMemberStatus>();
                }

                if ( groupMemberStatus.HasValue )
                {
                    groupMemberQry = groupMemberQry.Where( xx => xx.GroupMemberStatus == groupMemberStatus.Value );
                }

                var groupMemberIds = groupMemberQry.Select( gm => gm.PersonId )
                    .Distinct();

                var qry = new PersonService( ( RockContext ) context ).Queryable()
                    .Where( p => groupMemberIds.Contains( p.Id ) );

                Expression selectExpression = SelectExpressionExtractor.Extract( qry, entityIdProperty, "p" );

                return selectExpression;
            }

            return null;
        }

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <param name="parentControl"></param>
        /// <returns></returns>
        public override System.Web.UI.Control[] CreateChildControls( System.Web.UI.Control parentControl )
        {

            RockDropDownList dllGroupTypePurpose = new RockDropDownList();
            dllGroupTypePurpose.ID = parentControl.ID + "_groupTypePurposePicker";
            dllGroupTypePurpose.CssClass = "js-group-type-purpose";
            dllGroupTypePurpose.Label = "Group Type Purpose";
            dllGroupTypePurpose.BindToDefinedType( DefinedTypeCache.Get( SystemGuid.DefinedType.GROUPTYPE_PURPOSE ) );
            dllGroupTypePurpose.AutoPostBack = true;
            parentControl.Controls.Add( dllGroupTypePurpose );

            RockDropDownList ddlMemberRoleType = new RockDropDownList();
            ddlMemberRoleType.Label = "with Role Type";
            ddlMemberRoleType.ID = parentControl.ID + "_ddlMemberRoleType";
            ddlMemberRoleType.CssClass = "js-member-type";
            ddlMemberRoleType.Items.Insert( 0, new ListItem( "[All]", "" ) );
            ddlMemberRoleType.Items.Insert( 1, new ListItem( "Leader", "True" ) );
            ddlMemberRoleType.Items.Insert( 2, new ListItem( "Non Leader", "False" ) );
            ddlMemberRoleType.SetValue( "" );
            parentControl.Controls.Add( ddlMemberRoleType );

            RockDropDownList ddlGroupMemberStatus = new RockDropDownList();
            ddlGroupMemberStatus.CssClass = "js-group-member-status";
            ddlGroupMemberStatus.ID = parentControl.ID + "_ddlGroupMemberStatus";
            ddlGroupMemberStatus.Label = "with Group Member Status";
            ddlGroupMemberStatus.Help = "Select a specific group member status to only include group members with that status. Leaving this blank will return all members.";
            ddlGroupMemberStatus.BindToEnum<GroupMemberStatus>( true );
            ddlGroupMemberStatus.SetValue( GroupMemberStatus.Active.ConvertToInt() );
            parentControl.Controls.Add( ddlGroupMemberStatus );

            RockDropDownList ddlGroupStatus = new RockDropDownList();
            ddlGroupStatus.CssClass = "js-group-status";
            ddlGroupStatus.ID = parentControl.ID + "_ddlGroupStatus";
            ddlGroupStatus.Label = "with Group Status";
            ddlGroupStatus.Items.Insert( 0, new ListItem( "[All]", "" ) );
            ddlGroupStatus.Items.Insert( 1, new ListItem( "Active", "True" ) );
            ddlGroupStatus.Items.Insert( 2, new ListItem( "Inactive", "False" ) );
            ddlGroupStatus.SetValue( true.ToString() );
            parentControl.Controls.Add( ddlGroupStatus );

            return new Control[4] { dllGroupTypePurpose, ddlMemberRoleType, ddlGroupMemberStatus, ddlGroupStatus  };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( System.Web.UI.Control parentControl, System.Web.UI.HtmlTextWriter writer, System.Web.UI.Control[] controls )
        {
            base.RenderControls( parentControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( System.Web.UI.Control[] controls )
        {
            var dllGroupTypePurpose = ( controls[0] as RockDropDownList );
            var ddlMemberRoleType = ( controls[1] as RockDropDownList );
            var ddlMemberStatus = ( controls[2] as RockDropDownList );
            var ddlGroupStatus = ( controls[3] as RockDropDownList );


            int groupTypePurposeId = dllGroupTypePurpose.SelectedValueAsId() ?? 0;

            var memberRoleType = ddlMemberRoleType.SelectedValue;

            var memberStatusValue = ddlMemberStatus.SelectedValue;

            var groupStatus = ddlGroupStatus.SelectedValue;

            return groupTypePurposeId.ToString() + "|" + memberRoleType + "|" + memberStatusValue + "|" + groupStatus;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( System.Web.UI.Control[] controls, string selection )
        {
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                string groupTypePurposeId = selectionValues[0];
                ( controls[0] as RockDropDownList ).SelectedValue = groupTypePurposeId;

                string memberRoleType = selectionValues[1];
                ( controls[1] as RockDropDownList ).SelectedValue = memberRoleType;
                
                RockDropDownList ddlGroupMemberStatus = controls[2] as RockDropDownList;
                if ( selectionValues.Length >= 3 )
                {
                    ddlGroupMemberStatus.SetValue( selectionValues[2] );
                }
                else
                {
                    ddlGroupMemberStatus.SetValue( string.Empty );
                }

                RockDropDownList ddlGroupStatus = controls[3] as RockDropDownList;
                if ( selectionValues.Length >= 4 )
                {
                    ddlGroupStatus.SetValue( selectionValues[3] );
                }
                else
                {
                    ddlGroupStatus.SetValue( string.Empty );
                }
            }
        }

        #endregion
    }
}
